using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class AnimalCrossingWaterDepthPrePass : ScriptableRenderPass
{
    const string PROFILER_TAG = "WaterDepthPrePass";
    const string DEPTH_MAP = "_DepthMap";
    const string WATER_DEPTH = "_WaterDepth";
    static readonly int DEPTH_MAP_PROP_ID = Shader.PropertyToID(DEPTH_MAP);
    static readonly int WATER_DEPTH_PROP_ID = Shader.PropertyToID(WATER_DEPTH);
    static readonly int TOP_DOWN_DEPTH_VP_PROP_ID = Shader.PropertyToID("_TopDownDepthVP");

    readonly ProfilingSampler m_Sampler;
    readonly RenderTargetHandle m_DepthMapHandle;
    readonly RenderTargetHandle m_WaterDepthHandle;
    readonly RenderTargetIdentifier m_DepthMapIdentifier;
    readonly RenderTargetIdentifier m_WaterDepthIdentifier;
    readonly ShaderTagId m_WaterDepthTagId;
    FilteringSettings m_FilteringSettings;

    AnimalCrossingWater m_Water;

    public AnimalCrossingWaterDepthPrePass()
    {
        m_Sampler = new ProfilingSampler(PROFILER_TAG);

        m_WaterDepthTagId = new ShaderTagId("WaterDepth");
        m_FilteringSettings = new FilteringSettings(RenderQueueRange.all);

        renderPassEvent = RenderPassEvent.BeforeRendering;

        m_DepthMapHandle.Init(DEPTH_MAP);
        m_WaterDepthHandle.Init(WATER_DEPTH);
        m_DepthMapIdentifier = new RenderTargetIdentifier(m_DepthMapHandle.id);
        m_WaterDepthIdentifier = new RenderTargetIdentifier(m_WaterDepthHandle.id);
        
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

        cmd.GetTemporaryRT(m_DepthMapHandle.id, 2048, 2048, 32, FilterMode.Bilinear, RenderTextureFormat.Depth);
        cmd.GetTemporaryRT(m_WaterDepthHandle.id, 2048, 2048, 32, FilterMode.Bilinear, RenderTextureFormat.Depth);

        cmd.SetRenderTarget(m_DepthMapIdentifier);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get(PROFILER_TAG);
        using (new ProfilingScope(cmd, m_Sampler))
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            Vector3 center = m_Water.TerrainWorldBounds.center;
            Vector3 extents = m_Water.TerrainWorldBounds.extents;

            Matrix4x4 view = Matrix4x4.TRS(
                new Vector3(center.x, m_Water.TerrainWorldBounds.max.y + 0.1f, center.z),
                Quaternion.LookRotation(Vector3.down, Vector3.forward),
                new Vector3(1, 1, -1)).inverse;

            Matrix4x4 proj = Matrix4x4.Ortho(-extents.x, extents.x, -extents.z, extents.z, 0.01f, extents.y * 2 + 0.1f);

            cmd.SetViewProjectionMatrices(view, proj);
            cmd.SetGlobalMatrix(TOP_DOWN_DEPTH_VP_PROP_ID, proj * view);
            cmd.SetGlobalTexture(DEPTH_MAP_PROP_ID, m_DepthMapIdentifier);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            // draw environment depth for water
            {
                DrawingSettings drawingSettings = CreateDrawingSettings(m_WaterDepthTagId, ref renderingData, SortingCriteria.CommonOpaque);
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings);
                context.Submit();
            }

            // draw water depth
            {
                cmd.SetRenderTarget(m_WaterDepthIdentifier);
                cmd.ClearRenderTarget(true, true, Color.clear);
                cmd.SetGlobalTexture(WATER_DEPTH_PROP_ID, m_WaterDepthIdentifier);
                cmd.DrawRenderer(m_Water.Renderer, m_Water.Renderer.sharedMaterial, 0, 1);
                context.ExecuteCommandBuffer(cmd);
                context.Submit();
            }

            CommandBufferPool.Release(cmd);
        }
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        base.OnCameraCleanup(cmd);

        cmd.ReleaseTemporaryRT(m_DepthMapHandle.id);
        cmd.ReleaseTemporaryRT(m_WaterDepthHandle.id);
    }
}
