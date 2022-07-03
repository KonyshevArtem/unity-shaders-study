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
        Transform mirrorTransform = transform;
        Vector3 mirrorPos = mirrorTransform.position;
        Vector3 mirrorNormal = mirrorTransform.up;

        Transform sourceTransform = _Camera.transform;
        Transform destTransform = m_Camera.transform;

        Vector3 cameraUp = Vector3.Reflect(sourceTransform.up, mirrorNormal);
        Vector3 cameraFwd = Vector3.Reflect(sourceTransform.forward, mirrorNormal);
        destTransform.rotation = Quaternion.LookRotation(cameraFwd, cameraUp);

        Vector3 dir = sourceTransform.position - mirrorPos;
        float projDistance = Vector3.Dot(dir, mirrorNormal);
        destTransform.position = sourceTransform.position - 2 * mirrorNormal * projDistance;

        m_Renderer.SetPropertyBlock(m_Properties);
        m_Camera.targetTexture = m_Texture;
        m_Camera.fieldOfView = _Camera.fieldOfView;
        m_Camera.aspect = _Camera.aspect;

        Matrix4x4 destViewMatrix = m_Camera.worldToCameraMatrix;
        Vector3 mirrorPosCS = destViewMatrix.MultiplyPoint(mirrorPos);
        Vector3 mirrorNormalCS = destViewMatrix.MultiplyVector(mirrorNormal);
        m_Camera.projectionMatrix = m_Camera.CalculateObliqueMatrix(new Vector4(mirrorNormalCS.x, mirrorNormalCS.y, mirrorNormalCS.z, -Vector3.Dot(mirrorNormalCS, mirrorPosCS)));

        UniversalRenderPipeline.RenderSingleCamera(_Context, m_Camera);
    }
}
