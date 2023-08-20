using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class SDFGenerator : MonoBehaviour
{
    [SerializeField] private Mesh m_Mesh;
    [SerializeField] private ComputeShader m_Shader;
    [SerializeField] private Vector3Int m_SDFResolution;

    private RenderTexture m_SDF;
    
    void Awake()
    {
        if (m_Mesh == null || m_Shader == null || m_SDFResolution.x == 0 || m_SDFResolution.y == 0 || m_SDFResolution.z == 0)
        {
            return;
        }

        int kernelIndex = m_Shader.FindKernel("GenerateSDF");
        m_Shader.GetKernelThreadGroupSizes(kernelIndex, out uint x, out uint y, out uint z);

        CommandBuffer cmd = new CommandBuffer();

        m_SDF = new RenderTexture(m_SDFResolution.x, m_SDFResolution.y, 0, GraphicsFormat.R16_SFloat)
        {
            dimension = TextureDimension.Tex3D,
            volumeDepth = m_SDFResolution.z,
            enableRandomWrite = true
        };
        m_SDF.Create();

        GraphicsBuffer vertexBuffer = m_Mesh.GetVertexBuffer(0);

        int[] triangles = m_Mesh.triangles;
        GraphicsBuffer indexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, triangles.Length, 4);
        indexBuffer.SetData(triangles);

        cmd.SetComputeTextureParam(m_Shader, kernelIndex, "_SDF", m_SDF);
        cmd.SetComputeBufferParam(m_Shader, kernelIndex, "_Vertices", vertexBuffer);
        cmd.SetComputeBufferParam(m_Shader, kernelIndex, "_Indices", indexBuffer);
        cmd.SetComputeIntParam(m_Shader, "_Stride", m_Mesh.GetVertexBufferStride(0));
        cmd.SetComputeIntParam(m_Shader, "_PositionOffset", m_Mesh.GetVertexAttributeOffset(VertexAttribute.Position));
        cmd.SetComputeIntParam(m_Shader, "_NormalOffset", m_Mesh.GetVertexAttributeOffset(VertexAttribute.Normal));
        cmd.SetComputeIntParam(m_Shader, "_VerticesCount", m_Mesh.vertexCount);
        cmd.SetComputeIntParam(m_Shader, "_TrianglesCount", triangles.Length);
        cmd.SetComputeVectorParam(m_Shader, "_SDFResolution", new Vector4(m_SDFResolution.x, m_SDFResolution.y, m_SDFResolution.z, 0));
        cmd.SetComputeVectorParam(m_Shader, "_BoundsMin", m_Mesh.bounds.min);
        cmd.SetComputeVectorParam(m_Shader, "_BoundsMax", m_Mesh.bounds.max);

        cmd.DispatchCompute(m_Shader, kernelIndex,
            Mathf.CeilToInt((float)m_SDFResolution.x / x),
            Mathf.CeilToInt((float)m_SDFResolution.y / y),
            Mathf.CeilToInt((float)m_SDFResolution.z / z));

        Graphics.ExecuteCommandBuffer(cmd);

        vertexBuffer.Dispose();
        indexBuffer.Dispose();

        cmd.Release();

        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sharedMaterial.SetTexture("_SDF", m_SDF);
            mr.sharedMaterial.EnableKeyword("_SDF_SHAPE");
        }
    }

    private void OnDestroy()
    {
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sharedMaterial.SetTexture("_SDF", null);
            mr.sharedMaterial.DisableKeyword("_SDF_SHAPE");
        }        
        
        if (m_SDF != null)
        {
            m_SDF.Release();
            m_SDF = null;
        }
    }
}