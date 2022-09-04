using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class AnimalCrossingWaterRenderPass : ScriptableRenderPass
{
    const string PROFILER_TAG = "Water";
    static readonly int DEPTH_MAP_PROP_ID = Shader.PropertyToID("_DepthMap");
    static readonly int DEPTH_MAP_VP_PROP_ID = Shader.PropertyToID("_DepthMapVP");

    readonly ProfilingSampler m_Sampler;
    readonly ShaderTagId m_WaterDepthTagId;
    readonly RenderTargetHandle m_DepthMapHandle;
    readonly RenderTargetIdentifier m_DepthMapIdentifier;
    FilteringSettings m_FilteringSettings;

    AnimalCrossingWater m_Water;

    public AnimalCrossingWaterRenderPass()
    {
        m_Sampler = new ProfilingSampler(PROFILER_TAG);
        m_WaterDepthTagId = new ShaderTagId("WaterDepth");
        m_FilteringSettings = new FilteringSettings(RenderQueueRange.all);
        renderPassEvent = RenderPassEvent.AfterRenderingOpaques;

        m_DepthMapHandle.Init("_DepthMap");
        m_DepthMapIdentifier = new RenderTargetIdentifier(m_DepthMapHandle.id);

        ConfigureClear(ClearFlag.All, Color.clear);
        ConfigureTarget(m_DepthMapIdentifier);
    }

    public void Setup(AnimalCrossingWater _Water)
    {
        m_Water = _Water;
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        base.Configure(cmd, cameraTextureDescriptor);

        cmd.GetTemporaryRT(m_DepthMapHandle.id, 1024, 1024, 32, FilterMode.Bilinear, RenderTextureFormat.Depth);
        cmd.SetRenderTarget(m_DepthMapIdentifier);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get(PROFILER_TAG);
        using (new ProfilingScope(cmd, m_Sampler))
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            Bounds bounds = m_Water.TerrainWorldBounds;
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;

            Matrix4x4 view = Matrix4x4.TRS(
                new Vector3(center.x, bounds.max.y + 0.1f, center.z),
                Quaternion.LookRotation(Vector3.down, Vector3.forward),
                new Vector3(1, 1, -1)).inverse;

            Matrix4x4 proj = Matrix4x4.Ortho(-extents.x, extents.x, -extents.z, extents.z, 0.01f, extents.y * 2 + 0.1f);

            cmd.SetViewProjectionMatrices(view, proj);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            DrawingSettings drawingSettings = CreateDrawingSettings(m_WaterDepthTagId, ref renderingData, SortingCriteria.CommonOpaque);
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings);
            context.Submit();

            cmd.SetRenderTarget(renderingData.cameraData.renderer.cameraColorTarget, renderingData.cameraData.renderer.cameraDepthTarget);
            cmd.SetGlobalMatrix(DEPTH_MAP_VP_PROP_ID, proj * view);
            cmd.SetGlobalTexture(DEPTH_MAP_PROP_ID, m_DepthMapIdentifier);
            cmd.SetViewProjectionMatrices(renderingData.cameraData.GetViewMatrix(), renderingData.cameraData.GetProjectionMatrix());
            cmd.DrawRenderer(m_Water.Renderer, m_Water.Renderer.sharedMaterial);
            context.ExecuteCommandBuffer(cmd);
            context.Submit();

            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        base.OnCameraCleanup(cmd);

        cmd.ReleaseTemporaryRT(m_DepthMapHandle.id);
    }
}
