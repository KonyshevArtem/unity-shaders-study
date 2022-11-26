using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

public class BackfaceOutlineRenderFeature : ScriptableRendererFeature
{
    public static bool Enabled { get; set; }

    private DrawObjectsPass m_Pass;

    public override void Create()
    {
        m_Pass = new DrawObjectsPass("Backface Outline",
            new[] { new ShaderTagId("BackfaceOutline") },
            true,
            RenderPassEvent.AfterRenderingOpaques,
            RenderQueueRange.opaque,
            -1,
            StencilState.defaultValue,
            0);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (Enabled)
        {
            renderer.EnqueuePass(m_Pass);
        }
    }
}