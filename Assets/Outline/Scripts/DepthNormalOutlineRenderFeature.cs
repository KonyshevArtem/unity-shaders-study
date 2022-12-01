using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DepthNormalOutlineRenderFeature : ScriptableRendererFeature
{
    public static bool Enabled { get; set; }
    
    [SerializeField] private Shader m_OutlineShader;
    
    private DepthNormalOutlineRenderPass m_Pass;
    
    public override void Create()
    {
        m_Pass = new DepthNormalOutlineRenderPass(m_OutlineShader);
    }

    public override void AddRenderPasses(ScriptableRenderer _Renderer, ref RenderingData _RenderingData)
    {
        if (Enabled)
        {
            _Renderer.EnqueuePass(m_Pass);
        }
    }
}