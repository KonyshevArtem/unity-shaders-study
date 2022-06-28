using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteInEditMode, RequireComponent(typeof(Renderer))]
public class Mirror : MonoBehaviour
{
    [SerializeField] Camera m_Camera;

    Renderer m_Renderer;
    RenderTexture m_Texture;
    MaterialPropertyBlock m_Properties;

    void Awake()
    {
        RenderPipelineManager.beginCameraRendering += BeginFrame;

        m_Texture = new RenderTexture(
                Mathf.Max(Screen.width, 1),
                Mathf.Max(Screen.height, 1),
                32,
                SystemInfo.GetGraphicsFormat(UnityEngine.Experimental.Rendering.DefaultFormat.LDR)
            );

        m_Renderer = GetComponent<Renderer>();
        m_Properties = new MaterialPropertyBlock();
        m_Properties.SetTexture("_MirrorTex", m_Texture);
    }

    void OnDestroy()
    {
        RenderPipelineManager.beginCameraRendering -= BeginFrame;

        if (m_Texture != null)
            m_Texture.Release();
    }

    void BeginFrame(ScriptableRenderContext _Context, Camera _Camera)
    {
#if UNITY_EDITOR
        if (SceneView.currentDrawingSceneView != null && SceneView.currentDrawingSceneView.camera == _Camera)
            return;
#endif

        Transform cameraTransform = _Camera.transform;

        Vector3 cameraWorldPos = cameraTransform.position;
        Matrix4x4 mirrorMatrix = Matrix4x4.Scale(new Vector3(1, -1, 1)) * transform.worldToLocalMatrix * cameraTransform.localToWorldMatrix;

        //Matrix4x4 mirrorMatrix = Matrix4x4.Scale(new Vector3(1, -1, 1)) * transform.worldToLocalMatrix * cameraTransform.localToWorldMatrix;

        //Vector3 cameraLocalPos = mirrorMatrix * new Vector4(cameraWorldPos.x, cameraWorldPos.y, cameraWorldPos.z, 1);
        Vector3 cameraLocalPos = mirrorMatrix * new Vector4(0, 0, 0, 1);

        m_Camera.transform.localPosition = cameraLocalPos;
        m_Camera.transform.localRotation = mirrorMatrix.rotation;

        m_Renderer.SetPropertyBlock(m_Properties);
        m_Camera.targetTexture = m_Texture;

        UniversalRenderPipeline.RenderSingleCamera(_Context, m_Camera);
    }
}
