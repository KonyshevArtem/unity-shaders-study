using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshRenderer)), ExecuteInEditMode]
public class DivisionEffect : MonoBehaviour
{
    [SerializeField] Transform m_Target;
    [SerializeField] ComputeShader m_ComputeShader;

    Renderer m_Renderer;
    MaterialPropertyBlock m_Properties;

    bool m_UseCSFallback;
    GraphicsBuffer m_VertexBuffer;
    Camera m_CurrentCamera;
    int[] m_VertexAttributesData;
    int m_KernelIndex;
    int m_ThreadsCount;

    static readonly int VERTEX_BUFFER_PROP_ID = Shader.PropertyToID("_VertexBuffer");
    static readonly int VERTEX_DATA_PROP_ID = Shader.PropertyToID("_VertexAttributesData");
    static readonly int ECHO_PARAMS_PROP_ID = Shader.PropertyToID("_EchoParams");
    static readonly int TARGET_POSITION_PROP_ID = Shader.PropertyToID("_TargetPosition");
    static readonly int TIME_PROP_ID = Shader.PropertyToID("_Time");
    static readonly int MODEL_MATRIX_PROP_ID = Shader.PropertyToID("_ModelMatrix");
    static readonly int VP_MATRIX_PROP_ID = Shader.PropertyToID("_VPMatrix");
    static readonly int VP_MATRIX_INV_PROP_ID = Shader.PropertyToID("_MVPMatrixInv");

    static readonly int INTERFERENCE_NOISE_PROP_ID = Shader.PropertyToID("_InterferenceNoise");
    static readonly int INTERFERENCE_STRENGTH_PROP_ID = Shader.PropertyToID("_InterferenceStrength");
    static readonly int TRIANGLES_MOVE_NOISE_PROP_ID = Shader.PropertyToID("_TrianglesMoveNoise");
    static readonly int TRIANGLES_MOVE_NOISE_ST_PROP_ID = Shader.PropertyToID("_TrianglesMoveNoise_ST");
    static readonly int TRIANGLES_MOVE_STR_PROP_ID = Shader.PropertyToID("_TrianglesMoveStrength");
    static readonly int OFFSET_STR_PROP_ID = Shader.PropertyToID("_OffsetStrength");
    static readonly int OFFSET_DIST_PROP_ID = Shader.PropertyToID("_OffsetDistance");


    void Awake()
    {
        m_UseCSFallback = !SystemInfo.supportsGeometryShaders && SystemInfo.supportsComputeShaders;

        m_Renderer = GetComponent<Renderer>();

        if (m_UseCSFallback && Application.isPlaying)
        {
            PrepareComputeShaderData();
            RenderPipelineManager.beginCameraRendering += BeginCameraRendering;
        }
        else
            m_Properties = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (m_UseCSFallback || m_Properties == null || m_Renderer == null)
            return;

        m_Properties.SetVector(TARGET_POSITION_PROP_ID, m_Target.position);
        m_Renderer.SetPropertyBlock(m_Properties);
    }

    void OnWillRenderObject()
    {
        if (!m_UseCSFallback || !Application.isPlaying)
            return;

        CommandBuffer cmd = CommandBufferPool.Get();

        Material material = m_Renderer.sharedMaterial;
        float offsetStr = material.GetFloat(OFFSET_STR_PROP_ID);
        float offsetDist = material.GetFloat(OFFSET_DIST_PROP_ID);
        float interferenceStr = material.GetFloat(INTERFERENCE_STRENGTH_PROP_ID);
        float trianglesMoveStr = material.GetFloat(TRIANGLES_MOVE_STR_PROP_ID);
        Texture interferenceTex = material.GetTexture(INTERFERENCE_NOISE_PROP_ID);
        Texture trianglesMoveTex = material.GetTexture(TRIANGLES_MOVE_NOISE_PROP_ID);
        Vector4 triangleMoveTexST = material.GetVector(TRIANGLES_MOVE_NOISE_ST_PROP_ID);

        cmd.SetComputeBufferParam(m_ComputeShader, m_KernelIndex, VERTEX_BUFFER_PROP_ID, m_VertexBuffer);
        cmd.SetComputeIntParams(m_ComputeShader, VERTEX_DATA_PROP_ID, m_VertexAttributesData);
        cmd.SetComputeVectorParam(m_ComputeShader, TARGET_POSITION_PROP_ID, m_Target.position);

        Vector4 echoParams = new Vector4(offsetStr, offsetDist, interferenceStr, trianglesMoveStr);
        cmd.SetComputeVectorParam(m_ComputeShader, ECHO_PARAMS_PROP_ID, echoParams);
        cmd.SetComputeFloatParam(m_ComputeShader, TIME_PROP_ID, Time.realtimeSinceStartup);

        cmd.SetComputeTextureParam(m_ComputeShader, m_KernelIndex, INTERFERENCE_NOISE_PROP_ID, interferenceTex);
        cmd.SetComputeTextureParam(m_ComputeShader, m_KernelIndex, TRIANGLES_MOVE_NOISE_PROP_ID, trianglesMoveTex);
        cmd.SetComputeVectorParam(m_ComputeShader, TRIANGLES_MOVE_NOISE_ST_PROP_ID, triangleMoveTexST);

        cmd.SetComputeMatrixParam(m_ComputeShader, MODEL_MATRIX_PROP_ID, transform.localToWorldMatrix);

        Matrix4x4 vpMatrix = m_CurrentCamera.projectionMatrix * m_CurrentCamera.worldToCameraMatrix;
        Matrix4x4 mvpMatrix = vpMatrix * transform.localToWorldMatrix;
        cmd.SetComputeMatrixParam(m_ComputeShader, VP_MATRIX_PROP_ID, vpMatrix);
        cmd.SetComputeMatrixParam(m_ComputeShader, VP_MATRIX_INV_PROP_ID,  mvpMatrix.inverse);

        cmd.DispatchCompute(m_ComputeShader, m_KernelIndex, m_ThreadsCount, 1, 1);

        Graphics.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    void OnDestroy()
    {
        if (m_UseCSFallback && Application.isPlaying)
        {
            if (m_VertexBuffer != null)
            {
                m_VertexBuffer.Dispose();
                m_VertexBuffer = null;
            }

            RenderPipelineManager.beginCameraRendering -= BeginCameraRendering;
        }
    }

    void PrepareComputeShaderData()
    {
        MeshFilter filter = GetComponent<MeshFilter>();
        Mesh mesh = PrepareMesh(filter.sharedMesh);
        filter.sharedMesh = mesh;

        mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        
        m_VertexBuffer = mesh.GetVertexBuffer(0);
        int vertexBufferStrideBytes = mesh.GetVertexBufferStride(0);
        int normalOffsetBytes = mesh.GetVertexAttributeOffset(VertexAttribute.Normal);

        m_KernelIndex = m_ComputeShader.FindKernel("DivisionEffect");
        m_ComputeShader.GetKernelThreadGroupSizes(m_KernelIndex, out uint x, out _, out _);

        int trianglesCount = mesh.triangles.Length / 3;
        m_ThreadsCount = Mathf.CeilToInt((float) trianglesCount / x);

        m_VertexAttributesData = new int[] { vertexBufferStrideBytes, normalOffsetBytes, trianglesCount};
    }

    void BeginCameraRendering(ScriptableRenderContext _Context, Camera _Camera)
    {
        m_CurrentCamera = _Camera;
    }

    static Mesh PrepareMesh(Mesh _OriginalMesh)
    {
        Vector3[] origVertices = _OriginalMesh.vertices;
        Vector3[] origNormals = _OriginalMesh.normals;
        Vector4[] origTangents = _OriginalMesh.tangents;
        int[] origTriangles = _OriginalMesh.triangles;
        Vector2[] origUv = _OriginalMesh.uv;
        Vector2 [] origUv2 = _OriginalMesh.uv2;
        Vector2 [] origUv3 = _OriginalMesh.uv3;
        Vector2 [] origUv4 = _OriginalMesh.uv4;
        Vector2 [] origUv5 = _OriginalMesh.uv5;
        Vector2[] origUv6 = _OriginalMesh.uv6;

        int vertexCount = origTriangles.Length;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];
        Vector4[] tangents = new Vector4[vertexCount];
        int[] triangles = new int[origTriangles.Length];
        Vector2[] uv = new Vector2[vertexCount];
        Vector2[] uv2 = origUv2.Length > 0 ? new Vector2[vertexCount] : new Vector2[0];
        Vector2[] uv3 = origUv3.Length > 0 ? new Vector2[vertexCount] : new Vector2[0];
        Vector2[] uv4 = origUv4.Length > 0 ? new Vector2[vertexCount] : new Vector2[0];
        Vector2[] uv5 = origUv5.Length > 0 ? new Vector2[vertexCount] : new Vector2[0];
        Vector2[] uv6 = origUv6.Length > 0 ? new Vector2[vertexCount] : new Vector2[0];

        //make unique vertices for each triangle
        //so they could be moved independently like in geometry shader
        for (int i = 0; i < origTriangles.Length; ++i)
        {
            int vid = origTriangles[i];
            vertices[i] = origVertices[vid];
            normals[i] = origNormals[vid];
            tangents[i] = origTangents[vid];
            triangles[i] = i;
            uv[i] = origUv[vid];
            if (uv2.Length > 0)
                uv2[i] = origUv2[vid];
            if (uv3.Length > 0)
                uv3[i] = origUv3[vid];
            if (uv4.Length > 0)
                uv4[i] = origUv4[vid];
            if (uv5.Length > 0)
                uv5[i] = origUv5[vid];
            if (uv6.Length > 0)
                uv6[i] = origUv6[vid];
        }

        Mesh mesh = new()
        {
            hideFlags = HideFlags.DontSave,
            vertices = vertices,
            normals = normals,
            tangents = tangents,
            triangles = triangles,
            uv = uv,
            uv2 = uv2,
            uv3 = uv3,
            uv4 = uv4,
            uv5 = uv5,
            uv6 = uv6
        };

        // store vertex positions backup in uv8 channel
        mesh.SetUVs(7, vertices);

        return mesh;
    }
}
