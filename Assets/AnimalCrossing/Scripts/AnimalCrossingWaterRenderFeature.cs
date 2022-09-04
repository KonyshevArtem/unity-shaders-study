using UnityEngine.Rendering.Universal;

public class AnimalCrossingWaterRenderFeature : ScriptableRendererFeature
{
    AnimalCrossingWaterRenderPass m_Pass;
    AnimalCrossingWater m_Water;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_Pass == null || m_Water == null || m_Water.Renderer == null)
            return;

        m_Pass.Setup(m_Water);
        renderer.EnqueuePass(m_Pass);
    }

    public override void Create()
    {
        m_Pass = new AnimalCrossingWaterRenderPass();
        m_Water = FindObjectOfType<AnimalCrossingWater>();
    }
}
