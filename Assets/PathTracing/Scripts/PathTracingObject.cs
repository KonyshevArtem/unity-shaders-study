using UnityEngine;

[ExecuteInEditMode]
public class PathTracingObject : MonoBehaviour
{
    public enum ObjectType
    {
        Sphere,
        TriangleMesh
    }

    [SerializeField] private ObjectType m_Type;
    [SerializeField] private Color m_Color;
    [SerializeField, Range(0, 1)] private float m_Smoothness;
    [SerializeField, Min(0)] private float m_Emission;

    private MeshFilter m_MeshFilter;
    private Renderer m_Renderer;

    public ObjectType Type => m_Type;
    public Vector3 Position => transform.position;
    public float Size => transform.localScale.x;
    public Vector4 Color => m_Color;
    public float Smoothness => m_Smoothness;
    public float Emission => m_Emission;
    public Mesh Mesh => m_Type == ObjectType.TriangleMesh && m_MeshFilter != null ? m_MeshFilter.sharedMesh : null;
    public Bounds AABB => m_Renderer != null ? m_Renderer.bounds : default;
    public uint TrianglesOffset { get; set; }
    public uint TrianglesCount { get; set; }

    void Awake()
    {
        m_MeshFilter = GetComponent<MeshFilter>();
        m_Renderer = GetComponent<Renderer>();
    }
}