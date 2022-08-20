using UnityEngine;

[ExecuteInEditMode, RequireComponent(typeof(Renderer))]
public class Mirror : MonoBehaviour
{
    static readonly int MIRROR_TEX_PROP_ID = Shader.PropertyToID("_MirrorTex");
    static readonly int TINT_PROP_ID = Shader.PropertyToID("_Tint");

    [SerializeField] Camera m_Camera;

    Renderer m_Renderer;
    RenderTexture m_Texture;
    MaterialPropertyBlock m_Properties;

    public Camera Camera => m_Camera;
    public RenderTexture Texture => m_Texture;

    void Awake()
    {
        m_Renderer = GetComponent<Renderer>();
        m_Properties = new MaterialPropertyBlock();
    }

    public void CopyCameraParameters(Camera _Camera)
    {
        m_Camera.fieldOfView = _Camera.fieldOfView;
        m_Camera.aspect = _Camera.aspect;
        m_Camera.projectionMatrix = _Camera.projectionMatrix;
    }

    public void SetupCamera(Camera _Camera)
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

        m_Camera.fieldOfView = _Camera.fieldOfView;
        m_Camera.aspect = _Camera.aspect;

        Matrix4x4 destViewMatrix = m_Camera.worldToCameraMatrix;
        Vector3 mirrorPosCS = destViewMatrix.MultiplyPoint(mirrorPos);
        Vector3 mirrorNormalCS = destViewMatrix.MultiplyVector(mirrorNormal);
        m_Camera.projectionMatrix = m_Camera.CalculateObliqueMatrix(new Vector4(mirrorNormalCS.x, mirrorNormalCS.y, mirrorNormalCS.z, -Vector3.Dot(mirrorNormalCS, mirrorPosCS)));
    }

    public void SetupTexture(RenderTexture _Texture)
    {
        m_Texture = _Texture;

        if (_Texture != null)
        {
            m_Properties.SetTexture(MIRROR_TEX_PROP_ID, _Texture);
            m_Renderer.SetPropertyBlock(m_Properties);
        }

        m_Camera.targetTexture = _Texture;
    }

    public void SetDepth(int _Depth, int _MaxDepth)
    {
        float tint = (float)_Depth / (_MaxDepth + 1);
        tint = 1 - tint * tint;

        m_Properties.SetFloat(TINT_PROP_ID, tint);
        m_Renderer.SetPropertyBlock(m_Properties);
    }

    public void SetupBlackTexture()
    {
        m_Texture = null;
        m_Properties.SetTexture(MIRROR_TEX_PROP_ID, Texture2D.blackTexture);
        m_Renderer.SetPropertyBlock(m_Properties);
    }
}
