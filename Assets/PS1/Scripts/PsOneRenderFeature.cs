using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PsOneRenderFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader m_PsOneUtilShader; 
    
    private PsOneRenderPass m_Pass;
    private PsOneRenderController m_Controller;
        
    public override void Create()
    {
        m_Pass = new PsOneRenderPass(m_PsOneUtilShader);

        m_Controller = FindObjectOfType<PsOneRenderController>();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_Controller != null)
        {
            renderer.EnqueuePass(m_Pass);
        }
    }
}
