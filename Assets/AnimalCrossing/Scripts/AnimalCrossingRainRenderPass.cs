using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class AnimalCrossingRainRenderPass : ScriptableRenderPass
{
    public const int RAIN_DROP_NORMAL_MAP_SIZE_PX = 1024;

    const string PROFILER_TAG = "Rain";
    static readonly int RIPPLE_NORMAL_MAP_PROP_ID = Shader.PropertyToID("_RippleNormalMap");

    readonly ProfilingSampler m_Sampler;
    readonly Material m_RippleMaterial;
    readonly Matrix4x4 m_ViewMatrix;
    readonly Matrix4x4 m_ProjMatrix;

    AnimalCrossingRain m_Rain;
    RenderTexture m_RippleNormalMap;

    public AnimalCrossingRainRenderPass(Shader rippleShader, Texture2D rippleNormal)
    {
        m_Sampler = new ProfilingSampler(PROFILER_TAG);
        renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

        m_RippleMaterial = new Material(rippleShader);
        m_RippleMaterial.SetTexture("_MainTex", rippleNormal);
        m_RippleMaterial.SetVector("_OneOverSize", new Vector4(1.0f / RAIN_DROP_NORMAL_MAP_SIZE_PX, 1.0f / RAIN_DROP_NORMAL_MAP_SIZE_PX, 0, 0));

        m_ViewMatrix = Matrix4x4.TRS(Vector3.up, Quaternion.LookRotation(Vector3.down, Vector3.forward), new Vector3(1, 1, -1)).inverse;
        m_ProjMatrix = Matrix4x4.Ortho(0, RAIN_DROP_NORMAL_MAP_SIZE_PX, 0, RAIN_DROP_NORMAL_MAP_SIZE_PX, 0.1f, 1.1f);
    }

    public void Setup(AnimalCrossingRain rain)
    {
        m_Rain = rain;
        m_RippleMaterial.SetBuffer("_Infos", m_Rain.InfosBuffer);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get(PROFILER_TAG);
        using (new ProfilingScope(cmd, m_Sampler))
        {
            cmd.Clear();

            cmd.DrawRenderer(m_Rain.Renderer, m_Rain.Renderer.sharedMaterial);

            if (renderingData.cameraData.cameraType == CameraType.Game)
            {
                if (m_RippleNormalMap == null)
                {
                    m_RippleNormalMap = new RenderTexture(
                        RAIN_DROP_NORMAL_MAP_SIZE_PX,
                        RAIN_DROP_NORMAL_MAP_SIZE_PX,
                        0,
                        UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm);

                    m_RippleNormalMap.wrapMode = TextureWrapMode.Repeat;

                    Shader.SetGlobalTexture(RIPPLE_NORMAL_MAP_PROP_ID, m_RippleNormalMap);
                }

                cmd.SetRenderTarget(m_RippleNormalMap);
                cmd.ClearRenderTarget(false, true, new Color(0.5f, 1, 0.5f, 1));
                cmd.SetViewProjectionMatrices(m_ViewMatrix, m_ProjMatrix);
                cmd.DrawMeshInstancedIndirect(m_Rain.RippleMesh, 0, m_RippleMaterial, 0, m_Rain.IndirectArgsBuffer);

                cmd.SetRenderTarget(renderingData.cameraData.renderer.cameraColorTarget);
            }

            context.ExecuteCommandBuffer(cmd);

            cmd.Clear();

            // crashes unity for some reason
            //CommandBufferPool.Release(cmd);
        }
    }

    public void Dispose()
    {
        if (m_RippleNormalMap != null)
        {
            m_RippleNormalMap.Release();
            m_RippleNormalMap = null;
        }
    }
}
