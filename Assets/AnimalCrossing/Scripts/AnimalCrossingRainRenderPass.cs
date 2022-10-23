using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class AnimalCrossingRainRenderPass : ScriptableRenderPass
{
    const int RAIN_DROP_SIZE_PX = 10;
    const int RAIN_DROP_NORMAL_MAP_SIZE_PX = 1024;

    const string PROFILER_TAG = "Rain";
    static readonly int RIPPLE_NORMAL_MAP_PROP_ID = Shader.PropertyToID("_RippleNormalMap");
    static readonly int NORMAL_MAP_PROP_ID = Shader.PropertyToID("_NormalMap");

    readonly ProfilingSampler m_Sampler;
    readonly Mesh m_RippleMesh;
    readonly Material m_RippleMaterial;
    readonly Matrix4x4 m_ViewMatrix;
    readonly Matrix4x4 m_ProjMatrix;

    AnimalCrossingRain m_Rain;
    RenderTexture m_RippleNormalMapA;
    RenderTexture m_RippleNormalMapB;
    bool m_RippleNormalMapSwap;

    RenderTexture RippleNormalMapFront => m_RippleNormalMapSwap ? m_RippleNormalMapB : m_RippleNormalMapA;
    RenderTexture RippleNormalMapBack => m_RippleNormalMapSwap ? m_RippleNormalMapA : m_RippleNormalMapB;

    Matrix4x4[] m_Matrices = new Matrix4x4[32];

    public AnimalCrossingRainRenderPass(Shader rippleShader, Texture2D rippleNormal)
    {
        m_Sampler = new ProfilingSampler(PROFILER_TAG);
        renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

        m_RippleMesh = new Mesh();
        m_RippleMesh.SetVertices(new[] {
            new Vector3(-0.5f, 0, -0.5f),
            new Vector3(0.5f, 0, -0.5f),
            new Vector3(0.5f, 0, 0.5f),
            new Vector3(-0.5f, 0, 0.5f)});
        m_RippleMesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
        m_RippleMesh.SetNormals(new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up });
        m_RippleMesh.SetUVs(0, new[] {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)});

        m_RippleMaterial = new Material(rippleShader);
        m_RippleMaterial.SetTexture("_MainTex", rippleNormal);
        m_RippleMaterial.SetVector("_OneOverSize", new Vector4(1.0f / RAIN_DROP_NORMAL_MAP_SIZE_PX, 1.0f / RAIN_DROP_NORMAL_MAP_SIZE_PX, 0, 0));
        m_RippleMaterial.enableInstancing = true;

        m_ViewMatrix = Matrix4x4.TRS(Vector3.up, Quaternion.LookRotation(Vector3.down, Vector3.forward), new Vector3(1, 1, -1)).inverse;
        m_ProjMatrix = Matrix4x4.Ortho(0, RAIN_DROP_NORMAL_MAP_SIZE_PX, 0, RAIN_DROP_NORMAL_MAP_SIZE_PX, 0.1f, 1.1f);
    }

    public void Setup(AnimalCrossingRain rain)
    {
        m_Rain = rain;
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
                InitNormalMaps(cmd);

                cmd.Blit(RippleNormalMapBack, RippleNormalMapFront, m_RippleMaterial, 2);

                cmd.SetRenderTarget(RippleNormalMapFront);
                cmd.SetViewProjectionMatrices(m_ViewMatrix, m_ProjMatrix);

                for (int i = 0; i < m_Matrices.Length; i++)
                {
                    m_Matrices[i] = Matrix4x4.TRS(
                        new Vector3(Random.Range(0, RAIN_DROP_NORMAL_MAP_SIZE_PX), 0, Random.Range(0, RAIN_DROP_NORMAL_MAP_SIZE_PX)),
                        Quaternion.identity,
                        new Vector3(RAIN_DROP_SIZE_PX, 1, RAIN_DROP_SIZE_PX));
                }

                m_RippleMaterial.SetTexture(NORMAL_MAP_PROP_ID, RippleNormalMapBack);
                cmd.DrawMeshInstanced(m_RippleMesh, 0, m_RippleMaterial, 0, m_Matrices, m_Matrices.Length);

                cmd.SetRenderTarget(renderingData.cameraData.renderer.cameraColorTarget);

                SwapRippleNormalMaps();

                cmd.Blit(RippleNormalMapBack, RippleNormalMapFront, m_RippleMaterial, 1);

                cmd.SetGlobalTexture(RIPPLE_NORMAL_MAP_PROP_ID, RippleNormalMapFront);
                SwapRippleNormalMaps();
            }

            context.ExecuteCommandBuffer(cmd);

            cmd.Clear();

            // crashes unity for some reason
            //CommandBufferPool.Release(cmd);
        }
    }

    void InitNormalMaps(CommandBuffer cmd)
    {
        if (m_RippleNormalMapA == null)
        {
            RenderTextureDescriptor desc = new RenderTextureDescriptor(
                RAIN_DROP_NORMAL_MAP_SIZE_PX,
                RAIN_DROP_NORMAL_MAP_SIZE_PX,
                UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm,
                0);

            m_RippleNormalMapA = new RenderTexture(desc);
            m_RippleNormalMapB = new RenderTexture(desc);

            m_RippleNormalMapA.wrapMode = TextureWrapMode.Repeat;
            m_RippleNormalMapB.wrapMode = TextureWrapMode.Repeat;

            cmd.SetRenderTarget(RippleNormalMapBack);
            cmd.ClearRenderTarget(false, true, new Color(0.5f, 1, 0.5f, 1));
        }
    }

    void SwapRippleNormalMaps()
    {
        m_RippleNormalMapSwap = !m_RippleNormalMapSwap;
    }

    public void Dispose()
    {
        if (m_RippleNormalMapA != null)
        {
            m_RippleNormalMapA.Release();
            m_RippleNormalMapB.Release();

            m_RippleNormalMapA = null;
            m_RippleNormalMapB = null;
        }
    }
}
