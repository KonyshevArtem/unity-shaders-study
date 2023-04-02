using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PathTracingRenderFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader m_Shader;

    private PathTracingRenderPass m_Pass;
    private PathTracingController m_Controller;

    public override void Create()
    {
        m_Controller = FindObjectOfType<PathTracingController>();
        
        if (m_Controller != null)
        {
            m_Pass = new PathTracingRenderPass(m_Shader, m_Controller);
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game && m_Controller != null)
        {
            renderer.EnqueuePass(m_Pass);
        }
    }
}