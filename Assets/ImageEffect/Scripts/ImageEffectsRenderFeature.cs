using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ImageEffectsRenderFeature : ScriptableRendererFeature
{
    [SerializeField] Shader m_DisplacementShader;
    [SerializeField] Shader m_BoxBlurShader;
    [SerializeField] Shader m_ChromaticAbberationShader;

    ImageEffectsRenderPass m_Pass;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        ImageEffectsController controller = renderingData.cameraData.camera.GetComponent<ImageEffectsController>();
        if (controller != null)
        {
            m_Pass.Setup(controller);
            renderer.EnqueuePass(m_Pass);
        }
    }

    public override void Create()
    {
        m_Pass = new ImageEffectsRenderPass(m_DisplacementShader, m_BoxBlurShader, m_ChromaticAbberationShader);
    }
}
