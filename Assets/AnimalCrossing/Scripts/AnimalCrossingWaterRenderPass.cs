using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class AnimalCrossingWaterRenderPass : ScriptableRenderPass
{
    const string PROFILER_TAG = "WaterColorPass";
    const string CAMERA_COLOR_COPY = "_CameraColorCopy";
    static readonly int CAMERA_COLOR_COPY_PROP_ID = Shader.PropertyToID(CAMERA_COLOR_COPY);

    readonly ProfilingSampler m_Sampler;
    readonly RenderTargetHandle m_CameraColorCopyHandle;
    readonly RenderTargetIdentifier m_CameraColorCopyIdentifier;

    AnimalCrossingWater m_Water;

    public AnimalCrossingWaterRenderPass()
    {
        m_Sampler = new ProfilingSampler(PROFILER_TAG);
        renderPassEvent = RenderPassEvent.AfterRenderingSkybox;

        m_CameraColorCopyHandle.Init(CAMERA_COLOR_COPY);
        m_CameraColorCopyIdentifier = new RenderTargetIdentifier(m_CameraColorCopyHandle.id);

        ConfigureInput(ScriptableRenderPassInput.Color);
    }

    public void Setup(AnimalCrossingWater _Water)
    {
        m_Water = _Water;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get(PROFILER_TAG);
        using (new ProfilingScope(cmd, m_Sampler))
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            // copy camera color texture
            {
                RenderTextureDescriptor desc = new RenderTextureDescriptor(
                    (int) (renderingData.cameraData.cameraTargetDescriptor.width * 0.5f),
                    (int) (renderingData.cameraData.cameraTargetDescriptor.height * 0.5f),
                    renderingData.cameraData.cameraTargetDescriptor.colorFormat,
                    0);
                cmd.GetTemporaryRT(m_CameraColorCopyHandle.id, desc);
                cmd.Blit(renderingData.cameraData.renderer.cameraColorTarget, m_CameraColorCopyIdentifier);
            }

            // draw water
            {
                cmd.SetRenderTarget(renderingData.cameraData.renderer.cameraColorTarget);
                cmd.SetGlobalTexture(CAMERA_COLOR_COPY_PROP_ID, m_CameraColorCopyIdentifier);
                cmd.DrawRenderer(m_Water.Renderer, m_Water.Renderer.sharedMaterial, 0, 0);
                context.ExecuteCommandBuffer(cmd);
                context.Submit();
            }

            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        base.OnCameraCleanup(cmd);

        cmd.ReleaseTemporaryRT(m_CameraColorCopyHandle.id);
    }
}
