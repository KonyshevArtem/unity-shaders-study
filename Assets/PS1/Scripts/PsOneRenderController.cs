using UnityEngine;
using UnityEngine.Rendering.Universal;

[ExecuteInEditMode]
public class PsOneRenderController : MonoBehaviour
{
    private static readonly int RESOLUTION_PROP_ID = Shader.PropertyToID("_Resolution");
    private static readonly Vector2 TARGET_RESOLUTION = new Vector2(320, 240);

    [SerializeField, Range(0.01f, 1)] private float m_VertexPrecisionFactor = 1;
    
    private Camera m_Camera;

    private void Awake()
    {
        m_Camera = GetComponent<Camera>();
    }

    private void Update()
    {
        if (m_Camera == null)
        {
            return;
        }

        m_Camera.aspect = TARGET_RESOLUTION.x / TARGET_RESOLUTION.y;
        UniversalRenderPipeline.asset.renderScale = TARGET_RESOLUTION.y / m_Camera.pixelHeight;

        Shader.SetGlobalVector(RESOLUTION_PROP_ID, Vector2.Lerp(Vector2.zero, TARGET_RESOLUTION, m_VertexPrecisionFactor));
    }

    private void OnDisable()
    {
        UniversalRenderPipeline.asset.renderScale = 1;
    }
}