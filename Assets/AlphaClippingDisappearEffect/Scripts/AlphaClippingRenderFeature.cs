using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class AlphaClippingRenderFeature : ScriptableRendererFeature
{
    private static readonly List<AlphaClippingController> m_Controllers = new List<AlphaClippingController>();

    [SerializeField] private bool m_DebugMask;
    
    private AlphaClippingRenderPass m_Pass;

    public override void Create()
    {
        ParticleSystemsContainer container = FindObjectOfType<ParticleSystemsContainer>();
        if (container != null)
        {
            m_Pass = new AlphaClippingRenderPass(container.ParticleSystems);
        }
    }

    public static void AddController(AlphaClippingController controller)
    {
        m_Controllers.Add(controller);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_Pass != null && renderingData.cameraData.cameraType == CameraType.Game && m_Controllers.Count > 0)
        {
            m_Pass.Setup(m_Controllers, m_DebugMask);
            renderer.EnqueuePass(m_Pass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        m_Pass?.Dispose();
    }
}