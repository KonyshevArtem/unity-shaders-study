using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteInEditMode]
class MirrorManager : MonoBehaviour
{
    const int MIRROR_MAX_DEPTH = 1;

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
            RenderMirror(_Context, _Camera, m, 0);
    }

    void EndRenderCamera(ScriptableRenderContext _Context, Camera _Camera)
    {
        foreach (Mirror m in m_Mirrors)
            ResetMirror(m, null);
    }

    void RenderMirror(ScriptableRenderContext _Context, Camera _Camera, Mirror _Mirror, int _Depth)
    {
        _Mirror.SetupCamera(_Camera);
        _Mirror.SetupTexture(RenderTexture.GetTemporary(m_Descriptor));

        Dictionary<Mirror, RenderTexture> oldTextures = new Dictionary<Mirror, RenderTexture>();

        if (_Depth < MIRROR_MAX_DEPTH)
        {
            foreach (Mirror m in m_Mirrors.Where(_M => _M != _Mirror))
            {
                oldTextures[m] = m.Texture;

                RenderMirror(_Context, _Mirror.Camera, m, _Depth + 1);
            }
        }

        //_Mirror.Setup(_Camera, _Texture);
        UniversalRenderPipeline.RenderSingleCamera(_Context, _Mirror.Camera);

        if (_Depth < MIRROR_MAX_DEPTH)
        {
            foreach (Mirror m in m_Mirrors.Where(_M => _M != _Mirror))
                ResetMirror(m, oldTextures[m]);
        }
    }

    void ResetMirror(Mirror _Mirror, RenderTexture _OldTexture)
    {
        RenderTexture t = _Mirror.Texture;
        _Mirror.SetupTexture(_OldTexture);
        RenderTexture.ReleaseTemporary(t);
    }
}
