using UnityEngine.Rendering.Universal;

public class AnimalCrossingWaterRenderFeature : ScriptableRendererFeature
{
    AnimalCrossingWaterRenderPass m_ColorPass;
    AnimalCrossingWaterDepthPrePass m_DepthPrePass;

    AnimalCrossingWater m_Water;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_ColorPass == null || m_DepthPrePass == null || m_Water == null ||
            m_Water.Renderer == null || renderingData.cameraData.isPreviewCamera)
            return;

        m_ColorPass.Setup(m_Water);
        m_DepthPrePass.Setup(m_Water);

        renderer.EnqueuePass(m_ColorPass);
        renderer.EnqueuePass(m_DepthPrePass);
    }

    public override void Create()
    {
        m_ColorPass = new AnimalCrossingWaterRenderPass();
        m_DepthPrePass = new AnimalCrossingWaterDepthPrePass();

        m_Water = FindObjectOfType<AnimalCrossingWater>();
    }
}
