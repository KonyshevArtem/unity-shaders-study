using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class AnimalCrossingRainRenderPass : ScriptableRenderPass
{
    const string PROFILER_TAG = "Rain";

    readonly ProfilingSampler m_Sampler;

    AnimalCrossingRain m_Rain;

    public AnimalCrossingRainRenderPass()
    {
        m_Sampler = new ProfilingSampler(PROFILER_TAG);
        renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    public void Setup(AnimalCrossingRain rain)
    {
        m_Rain = rain;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get(PROFILER_TAG);
        using (new ProfilingScope(cmd, m_Sampler))
        {
            cmd.Clear();

            cmd.DrawRenderer(m_Rain.Renderer, m_Rain.Renderer.sharedMaterial);
            context.ExecuteCommandBuffer(cmd);

            cmd.Clear();

            // crashes unity for some reason
            //CommandBufferPool.Release(cmd);
        }
    }
}
