using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class AnimalCrossingRenderController : MonoBehaviour
{
    static readonly string KEYWORD = "ANIMAL_CROSSING_SLOPE";

    static readonly int SLOPE_PARAMS_PROP_ID = Shader.PropertyToID("_SlopeParams");

    [SerializeField] Camera m_CullingCamera;
    [SerializeField] Vector2 m_CenterOffset;
    [SerializeField] float m_SlopeRadius;

    Camera m_Camera;

    void Awake()
    {
        m_Camera = GetComponent<Camera>();

        RenderPipelineManager.beginCameraRendering += OnCameraBeginRender;
    }

    void OnDestroy()
    {
        RenderPipelineManager.beginCameraRendering -= OnCameraBeginRender;
    }

    void OnCameraBeginRender(ScriptableRenderContext _Context, Camera _Camera)
    {
        if (m_Camera == _Camera)
        {
            if (m_CullingCamera != null)
                _Camera.cullingMatrix = m_CullingCamera.cullingMatrix;

            Vector3 camPos = _Camera.transform.position;
            Vector3 center = new Vector3(camPos.x + m_CenterOffset.x, -m_SlopeRadius, camPos.z + m_CenterOffset.y);
            Shader.SetGlobalVector(SLOPE_PARAMS_PROP_ID, new Vector4(center.x, center.y, center.z, m_SlopeRadius));
            Shader.EnableKeyword(KEYWORD);
        }
        else
        {
            Shader.DisableKeyword(KEYWORD);
        }
    }
}
