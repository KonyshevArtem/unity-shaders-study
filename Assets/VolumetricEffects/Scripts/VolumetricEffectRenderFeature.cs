using UnityEngine;
using UnityEngine.Rendering.Universal;

public class VolumetricEffectRenderFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader m_ComposeShader;
    [SerializeField, Range(0.01f, 1.0f)] private float m_Downscale;
    [SerializeField] private bool m_Dither;
    [SerializeField] private bool m_Blur;

    private VolumetricEffectRenderPass m_Pass;

    public override void Create()
    {
        m_Pass = new VolumetricEffectRenderPass(m_ComposeShader, m_Downscale, m_Dither, m_Blur);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_Pass != null && renderingData.cameraData.cameraType is CameraType.Game or CameraType.SceneView)
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