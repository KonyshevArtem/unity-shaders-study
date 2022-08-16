using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteInEditMode]
class MirrorManager : MonoBehaviour
{
    struct MirrorInfo
    {
        public RenderTexture RenderTexture;
        public Vector3 Position;
        public Quaternion Rotation;
        public float FOV;
        public float Aspect;
        public Matrix4x4 ProjectionMatrix;
    }

    [SerializeField, Range(0, 10)] int m_MaxDepth = 1;

    Mirror[] m_Mirrors;
    RenderTextureDescriptor m_Descriptor;

    void Awake()
    {
        m_Mirrors = FindObjectsOfType<Mirror>(true);

        m_Descriptor = new RenderTextureDescriptor(
            Mathf.Max(Screen.width, 1),
            Mathf.Max(Screen.height, 1),
            SystemInfo.GetGraphicsFormat(UnityEngine.Experimental.Rendering.DefaultFormat.LDR),
            32
        );

        RenderPipelineManager.beginCameraRendering += BeginRenderCamera;
        RenderPipelineManager.endCameraRendering += EndRenderCamera;
    }

    void OnDestroy()
    {
        RenderPipelineManager.beginCameraRendering -= BeginRenderCamera;
        RenderPipelineManager.endCameraRendering -= EndRenderCamera;
    }

    void BeginRenderCamera(ScriptableRenderContext _Context, Camera _Camera)
    {
        foreach (Mirror m in m_Mirrors)
            m.CopyCameraParameters(_Camera);

        foreach (Mirror m in m_Mirrors)
            RenderMirror(_Context, _Camera, m, 0);
    }

    void EndRenderCamera(ScriptableRenderContext _Context, Camera _Camera)
    {
        foreach (Mirror m in m_Mirrors)
            ResetMirrorTexture(m, null);
    }

    void RenderMirror(ScriptableRenderContext _Context, Camera _Camera, Mirror _Mirror, int _Depth)
    {
        _Mirror.SetupCamera(_Camera);
        _Mirror.SetupTexture(RenderTexture.GetTemporary(m_Descriptor));

        Dictionary<Mirror, MirrorInfo> oldMirrorInfos = new Dictionary<Mirror, MirrorInfo>();

        if (_Depth < m_MaxDepth)
        {
            foreach (Mirror m in m_Mirrors.Where(_M => _M != _Mirror))
                oldMirrorInfos[m] = GetMirrorInfo(m);

            foreach (Mirror m in m_Mirrors.Where(_M => _M != _Mirror))
                RenderMirror(_Context, _Mirror.Camera, m, _Depth + 1);
        }

        UniversalRenderPipeline.RenderSingleCamera(_Context, _Mirror.Camera);

        if (_Depth < m_MaxDepth)
        {
            foreach (Mirror m in m_Mirrors.Where(_M => _M != _Mirror))
                SetMirrorInfo(m, oldMirrorInfos[m]);
        }
    }

    static void ResetMirrorTexture(Mirror _Mirror, RenderTexture _OldTexture)
    {
        RenderTexture t = _Mirror.Texture;
        _Mirror.SetupTexture(_OldTexture);
        RenderTexture.ReleaseTemporary(t);
    }

    static MirrorInfo GetMirrorInfo(Mirror _Mirror)
    {
        Transform camTransform = _Mirror.Camera.transform;
        return new MirrorInfo
        {
            RenderTexture = _Mirror.Texture,
            Position = camTransform.position,
            Rotation = camTransform.rotation,
            FOV = _Mirror.Camera.fieldOfView,
            Aspect = _Mirror.Camera.aspect,
            ProjectionMatrix = _Mirror.Camera.projectionMatrix
        };
    }

    static void SetMirrorInfo(Mirror _Mirror, MirrorInfo _Info)
    {
        Transform camTransform = _Mirror.Camera.transform;
        camTransform.position = _Info.Position;
        camTransform.rotation = _Info.Rotation;
        _Mirror.Camera.fieldOfView = _Info.FOV;
        _Mirror.Camera.aspect = _Info.Aspect;
        _Mirror.Camera.projectionMatrix = _Info.ProjectionMatrix;
        ResetMirrorTexture(_Mirror, _Info.RenderTexture);
    }
}
