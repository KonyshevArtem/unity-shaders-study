using UnityEngine;
using UnityEngine.Rendering.Universal;

public class AnimalCrossingRenderFeature : ScriptableRendererFeature
{
    [SerializeField] Shader m_RippleShader;
    [SerializeField] Texture2D m_RippleNormal;

    AnimalCrossingWaterRenderPass m_WaterColorPass;
    AnimalCrossingWaterDepthPrePass m_WaterDepthPrePass;

    AnimalCrossingRainRenderPass m_RainPass;

    AnimalCrossingWater m_Water;
    AnimalCrossingRain m_Rain;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_WaterColorPass != null && m_WaterDepthPrePass != null && m_Water != null &&
            m_Water.Renderer != null && !renderingData.cameraData.isPreviewCamera)
        {
            m_WaterColorPass.Setup(m_Water);
            m_WaterDepthPrePass.Setup(m_Water);

            renderer.EnqueuePass(m_WaterColorPass);
            renderer.EnqueuePass(m_WaterDepthPrePass);
        }

        if (m_RainPass != null && m_Rain != null && m_Rain.Renderer != null && !renderingData.cameraData.isPreviewCamera)
        {
            m_RainPass.Setup(m_Rain);

            renderer.EnqueuePass(m_RainPass);
        }
    }

    public override void Create()
    {
        m_WaterColorPass = new AnimalCrossingWaterRenderPass();
        m_WaterDepthPrePass = new AnimalCrossingWaterDepthPrePass();
        m_RainPass = new AnimalCrossingRainRenderPass(m_RippleShader, m_RippleNormal);

        m_Water = FindObjectOfType<AnimalCrossingWater>();
        m_Rain = FindObjectOfType<AnimalCrossingRain>();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (m_RainPass != null)
        {
            m_RainPass.Dispose();
            m_RainPass = null;
        }
    }
}
