using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class AnimalCrossingWater : MonoBehaviour
{
    const string WATER_CAUSTIC_KEYWORD = "ANIMAL_CROSSING_WATER_CAUSTICS";
    static readonly int WATER_CAUSTICS_MASK_PROP_ID = Shader.PropertyToID("_WaterCausticsMask");
    static readonly int WATER_CAUSTICS_MASK_ST_PROP_ID = Shader.PropertyToID("_WaterCausticMask_ST");

    [SerializeField] Terrain m_Terrain;
    [SerializeField] int m_SideVertexCount = 200;

    [Space, SerializeField] Texture2D m_WaterCausticsMask;
    [SerializeField] Vector4 m_WaterCausticMask_ST;

    public Bounds TerrainWorldBounds { get; private set; }
    public Renderer Renderer { get; private set; }

    void Awake()
    {
        if (m_Terrain == null)
            return;

        Bounds bounds = m_Terrain.terrainData.bounds;
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        Vector3 boundsCenterWS = m_Terrain.transform.localToWorldMatrix.MultiplyPoint(bounds.center);
        TerrainWorldBounds = new Bounds(boundsCenterWS, bounds.size);

        Vector2 dist = new Vector2((max.x - min.x) / (m_SideVertexCount - 1), (max.z - min.z) / (m_SideVertexCount - 1));

        Vector3[] verts = new Vector3[m_SideVertexCount * m_SideVertexCount];
        Vector2[] uvs = new Vector2[verts.Length];
        for (int i = 0; i < m_SideVertexCount; ++i)
        {
            for (int j = 0; j < m_SideVertexCount; ++j)
            {
                int index = j * m_SideVertexCount + i;
                verts[index] = new Vector3(min.x + dist.x * i, 0, min.z + dist.y * j);
                uvs[index] = new Vector2((float)i / (m_SideVertexCount - 1), (float)j / (m_SideVertexCount - 1));
            }
        }

        List<int> triangles = new List<int>();
        for (int i = 0; i < m_SideVertexCount - 1; ++i)
        {
            for (int j = 0; j < m_SideVertexCount - 1; ++j)
            {
                triangles.Add(j * m_SideVertexCount + i + 0);
                triangles.Add((j + 1) * m_SideVertexCount + i);
                triangles.Add(j * m_SideVertexCount + i + 1);

                triangles.Add(j * m_SideVertexCount + i + 1);
                triangles.Add((j + 1) * m_SideVertexCount + i);
                triangles.Add((j + 1) * m_SideVertexCount + i + 1);
            }
        }

        Mesh m = new Mesh()
        {
            vertices = verts,
            triangles = triangles.ToArray(),
            uv = uvs
        };
        m.RecalculateNormals();

        GetComponent<MeshFilter>().sharedMesh = m;

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        block.SetFloat("_UVOffset", 1.0f / m_SideVertexCount);
        Renderer = GetComponent<MeshRenderer>();
        Renderer.SetPropertyBlock(block);

        // setup water caustics
        Shader.SetGlobalTexture(WATER_CAUSTICS_MASK_PROP_ID, m_WaterCausticsMask);
        Shader.SetGlobalVector(WATER_CAUSTICS_MASK_ST_PROP_ID, m_WaterCausticMask_ST);
        Shader.EnableKeyword(WATER_CAUSTIC_KEYWORD);

        // it will be drawn manually in AnimalCrossingWaterRenderPass
        Renderer.enabled = false;
    }

    private void OnDestroy()
    {
        Shader.DisableKeyword(WATER_CAUSTIC_KEYWORD);
    }
}
