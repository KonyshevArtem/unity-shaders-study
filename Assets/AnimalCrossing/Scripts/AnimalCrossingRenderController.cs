using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class AnimalCrossingRenderController : MonoBehaviour
{
    static readonly string KEYWORD = "ANIMAL_CROSSING_SLOPE";

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
            Shader.EnableKeyword(KEYWORD);
        }
        else
        {
            Shader.DisableKeyword(KEYWORD);
        }
    }
}
