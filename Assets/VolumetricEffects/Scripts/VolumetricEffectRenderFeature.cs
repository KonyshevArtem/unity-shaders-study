using UnityEngine;
using UnityEngine.Rendering.Universal;

public class VolumetricEffectRenderFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader m_ComposeShader;
    [SerializeField, Range(0.01f, 1.0f)] private float m_Downscale;

    private VolumetricEffectRenderPass m_Pass;

    public override void Create()
    {
        m_Pass = new VolumetricEffectRenderPass(m_ComposeShader, m_Downscale);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_Pass != null)
        {
            renderer.EnqueuePass(m_Pass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (m_Pass != null)
        {
            m_Pass.Dispose();
            m_Pass = null;
        }
    }
}