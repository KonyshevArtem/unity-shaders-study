using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PathTracingRenderFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader m_PathTracingShader;
    [SerializeField] private Shader m_BlendShader;
    [SerializeField] private Cubemap m_Skybox;
    [SerializeField] private float m_SkyboxRotationY;
    [SerializeField] private uint m_MaxBounces;
    [SerializeField] private uint m_MaxIterations;
    [SerializeField] private bool m_EnableInSceneView;
    [Space]
    [SerializeField] private bool m_PreApplyModelMatrix;
    [SerializeField] private bool m_NoIndices;

    private Material m_PathTracingMaterial;
    private Material m_BlendMaterial;
    private PathTracingRenderPass m_Pass;

    public override void Create()
    {
        m_PathTracingMaterial = CoreUtils.CreateEngineMaterial(m_PathTracingShader);
        m_BlendMaterial = CoreUtils.CreateEngineMaterial(m_BlendShader);
        
        m_Pass = new PathTracingRenderPass(m_PathTracingMaterial, m_BlendMaterial, m_Skybox);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        PathTracingObject[] objects = FindObjectsOfType<PathTracingObject>();
        if ((renderingData.cameraData.cameraType == CameraType.Game || renderingData.cameraData.cameraType == CameraType.SceneView && m_EnableInSceneView) && objects.Length > 0)
        {
            m_Pass.Setup(objects, m_MaxBounces, m_MaxIterations, m_SkyboxRotationY, m_PreApplyModelMatrix, m_NoIndices);
            renderer.EnqueuePass(m_Pass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        m_Pass?.Dispose();
        
        CoreUtils.Destroy(m_PathTracingMaterial);
        CoreUtils.Destroy(m_BlendMaterial);
    }
}