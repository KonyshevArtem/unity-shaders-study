using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VolumetricEffectRenderPass : ScriptableRenderPass
{
    private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Volumetric Effects");
    private static readonly ShaderTagId m_ShaderTagId = new ShaderTagId("VolumetricEffect");
    private static readonly int m_AmbientScalePropId = Shader.PropertyToID("_AmbientScale");

    private FilteringSettings m_FilteringSettings;

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

        m_FilteringSettings = new FilteringSettings(RenderQueueRange.transparent);

        ConfigureInput(ScriptableRenderPassInput.Depth);
        ConfigureClear(ClearFlag.Color, Color.clear);
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        base.Configure(cmd, cameraTextureDescriptor);

        // seems like Unity does not setup ambient light for custom passes, so I do it manually
        SphericalHarmonicsL2 sh = RenderSettings.ambientProbe;
        Vector4 ambientScale = new Vector4(sh[0, 0], sh[1, 0], sh[2, 0], 1 / m_Downscale);
        cmd.SetGlobalVector(m_AmbientScalePropId, ambientScale);
        
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

            DrawingSettings drawSettings = CreateDrawingSettings(m_ShaderTagId, ref renderingData, SortingCriteria.CommonTransparent);
            context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref m_FilteringSettings);

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