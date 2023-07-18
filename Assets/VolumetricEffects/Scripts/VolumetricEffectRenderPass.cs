using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VolumetricEffectRenderPass : ScriptableRenderPass
{
    private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Volumetric Effects");
    private static readonly ShaderTagId m_ShaderTagId = new ShaderTagId("VolumetricEffect");
    private static readonly int m_AmbientScalePropId = Shader.PropertyToID("_AmbientScale");

    private static readonly string[] m_TargetNames = { "_VolumesRT", "_DepthProxy" };
    private static readonly GraphicsFormat[] m_GraphicsFormats = { GraphicsFormat.R8G8B8A8_SRGB, GraphicsFormat.R16G16_UNorm };

    private FilteringSettings m_FilteringSettings;

    private readonly RenderTargetHandle[] m_TargetHandles = new RenderTargetHandle[m_TargetNames.Length];
    private readonly RenderTargetIdentifier[] m_TargetIdentifiers = new RenderTargetIdentifier[m_TargetNames.Length];

    private readonly Material m_ComposeMaterial;
    private readonly float m_Downscale;

    public VolumetricEffectRenderPass(Shader composeShader, float downscale)
    {
        renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;

        for (int i = 0; i < m_TargetNames.Length; ++i)
        {
            m_TargetHandles[i].Init(m_TargetNames[i]);
            m_TargetIdentifiers[i] = new RenderTargetIdentifier(m_TargetHandles[i].id);
        }

        m_ComposeMaterial = CoreUtils.CreateEngineMaterial(composeShader);
        m_Downscale = downscale;

        m_FilteringSettings = new FilteringSettings(RenderQueueRange.transparent);

        ConfigureInput(ScriptableRenderPassInput.Depth);
        ConfigureClear(ClearFlag.None, Color.clear); // clear manually
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        base.Configure(cmd, cameraTextureDescriptor);

        // seems like Unity does not setup ambient light for custom passes, so I do it manually
        SphericalHarmonicsL2 sh = RenderSettings.ambientProbe;
        Vector4 ambientScale = new Vector4(sh[0, 0], sh[1, 0], sh[2, 0], 1 / m_Downscale);
        cmd.SetGlobalVector(m_AmbientScalePropId, ambientScale);
        
        int width = (int)(cameraTextureDescriptor.width * m_Downscale);
        int height = (int)(cameraTextureDescriptor.height * m_Downscale);

        for (int i = 0; i < m_TargetHandles.Length; ++i)
        {
            cmd.GetTemporaryRT(m_TargetHandles[i].id, width, height, 0, FilterMode.Bilinear, m_GraphicsFormats[i]);
        }

        ConfigureTarget(m_TargetIdentifiers, BuiltinRenderTextureType.None);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, m_ProfilingSampler))
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            ClearTargets(ref context, cmd, ref renderingData);

            DrawingSettings drawSettings = CreateDrawingSettings(m_ShaderTagId, ref renderingData, SortingCriteria.CommonTransparent);
            context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref m_FilteringSettings);

            cmd.Blit(m_TargetIdentifiers[0], renderingData.cameraData.renderer.cameraColorTarget, m_ComposeMaterial, 0);
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    private void ClearTargets(ref ScriptableRenderContext context, CommandBuffer cmd, ref RenderingData renderingData)
    {
        cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
        cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_ComposeMaterial, 0, 1);
        cmd.SetViewProjectionMatrices(renderingData.cameraData.GetViewMatrix(), renderingData.cameraData.GetProjectionMatrix());

        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        base.FrameCleanup(cmd);

        for (int i = 0; i < m_TargetHandles.Length; ++i)
        {
            cmd.ReleaseTemporaryRT(m_TargetHandles[i].id);
        }
    }

    public void Dispose()
    {
        CoreUtils.Destroy(m_ComposeMaterial);
    }
}