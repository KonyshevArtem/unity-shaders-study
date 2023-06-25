using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VolumetricEffects.Scripts;

public class VolumetricEffectRenderPass : ScriptableRenderPass
{
    private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Volumetric Effects");

    private VolumetricEffect[] m_Volumes;

    private readonly RenderTargetHandle m_TargetHandle;
    private readonly RenderTargetIdentifier m_TargetIdentifier;
    private readonly Material m_ComposeMaterial;
    private readonly float m_Downscale;

    public VolumetricEffectRenderPass(Shader composeShader, float downscale)
    {
        renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

        m_TargetHandle.Init("_VolumesRT");
        m_TargetIdentifier = new RenderTargetIdentifier(m_TargetHandle.id);

        m_ComposeMaterial = CoreUtils.CreateEngineMaterial(composeShader);
        m_Downscale = downscale;

        ConfigureClear(ClearFlag.Color, Color.clear);
    }

    public void Setup(VolumetricEffect[] volumes)
    {
        m_Volumes = volumes;
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        base.Configure(cmd, cameraTextureDescriptor);

        // seems like Unity does not setup ambient light for custom passes, so I do it manually
        SphericalHarmonicsL2 sh = RenderSettings.ambientProbe;
        Color ambient = new Color(sh[0, 0], sh[1, 0], sh[2, 0]);
        cmd.SetGlobalColor("_Ambient", ambient);

        cmd.GetTemporaryRT(m_TargetHandle.id, (int) (cameraTextureDescriptor.width * m_Downscale), (int)(cameraTextureDescriptor.height * m_Downscale), 0, FilterMode.Bilinear, GraphicsFormat.R8G8B8A8_SRGB);
        ConfigureTarget(m_TargetIdentifier);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, m_ProfilingSampler))
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            for (int i = 0; i < m_Volumes.Length; i++)
            {
                Renderer renderer = m_Volumes[i].Renderer;
                Material material = m_Volumes[i].Material;
                if (renderer != null && material != null)
                {
                    cmd.DrawRenderer(renderer, material);
                }
            }

            cmd.Blit(m_TargetIdentifier, renderingData.cameraData.renderer.cameraColorTarget, m_ComposeMaterial);
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        base.FrameCleanup(cmd);

        cmd.ReleaseTemporaryRT(m_TargetHandle.id);
    }

    public void Dispose()
    {
        CoreUtils.Destroy(m_ComposeMaterial);
    }
}