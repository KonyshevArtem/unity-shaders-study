using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class AnimalCrossingRain : MonoBehaviour
{
    [SerializeField] Vector2 m_MinRainDropSize;
    [SerializeField] Vector2 m_MaxRainDropSize;
    [SerializeField] int m_RainDropCount = 10;

    MeshRenderer m_Renderer;

    public MeshRenderer Renderer => m_Renderer;

    void Awake()
    {
        float aspect = Camera.main.aspect;

        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[m_RainDropCount * 4];
        Vector3[] normals = new Vector3[m_RainDropCount * 4];
        Vector3[] centers = new Vector3[m_RainDropCount * 4];
        int[] triangles = new int[m_RainDropCount * 6];
        for (int i = 0; i < m_RainDropCount; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-aspect, aspect), Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f));
            Vector2 size = new Vector2(Random.Range(m_MinRainDropSize.x, m_MaxRainDropSize.x), Random.Range(m_MinRainDropSize.y, m_MaxRainDropSize.y));
            vertices[i * 4 + 0] = pos + new Vector3(-size.x, -size.y, 0);
            vertices[i * 4 + 1] = pos + new Vector3(-size.x, size.y, 0);
            vertices[i * 4 + 2] = pos + new Vector3(size.x, size.y, 0);
            vertices[i * 4 + 3] = pos + new Vector3(size.x, -size.y, 0);

            triangles[i * 6 + 0] = i * 4 + 0;
            triangles[i * 6 + 1] = i * 4 + 1;
            triangles[i * 6 + 2] = i * 4 + 2;
            triangles[i * 6 + 3] = i * 4 + 0;
            triangles[i * 6 + 4] = i * 4 + 2;
            triangles[i * 6 + 5] = i * 4 + 3;

            for (int j = 0; j < 4; j++)
            {
                normals[i * 4 + j] = new Vector3(0, 0, -1);
                centers[i * 4 + j] = pos;
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, centers);

        MeshFilter filter = GetComponent<MeshFilter>();
        filter.sharedMesh = mesh;

        MaterialPropertyBlock properties = new MaterialPropertyBlock();
        properties.SetFloat("_Width", aspect * 2);

        m_Renderer = GetComponent<MeshRenderer>();
        m_Renderer.SetPropertyBlock(properties);

        // it will be drawn in separate render pass
        m_Renderer.enabled = false;
    }
}
