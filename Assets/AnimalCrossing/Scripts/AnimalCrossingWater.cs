using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class AnimalCrossingWater : MonoBehaviour
{
    const string WATER_CAUSTIC_KEYWORD = "ANIMAL_CROSSING_WATER_CAUSTICS";
    static readonly int WATER_CAUSTICS_MASK_PROP_ID = Shader.PropertyToID("_WaterCausticsMask");
    static readonly int WATER_CAUSTICS_MASK_ST_PROP_ID = Shader.PropertyToID("_WaterCausticMask_ST");
    static readonly int WATER_CAUSTICS_DISTORTION_PROP_ID = Shader.PropertyToID("_WaterCausticsDistortion");
    static readonly int WATER_CAUSTICS_DISTORTION_ST_PROP_ID = Shader.PropertyToID("_WaterCausticsDistortion_ST");

    [SerializeField] Camera m_Camera;

    [SerializeField] Terrain m_Terrain;
    [SerializeField] int m_SideVertexCount = 200;

    [Space, SerializeField] Texture2D m_WaterCausticsMask;
    [SerializeField] Vector4 m_WaterCausticMask_ST;

    [Space, SerializeField] Texture2D m_WaterCausticsDistortion;
    [SerializeField] Vector4 m_WaterCausticsDistortion_ST;

    public Bounds TerrainWorldBounds { get; private set; }
    public Renderer Renderer { get; private set; }

    Vector4 m_LastFrameVisibleAreaOffsetScale;

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
        SetupCausticsUniforms();
        Shader.EnableKeyword(WATER_CAUSTIC_KEYWORD);

        // it will be drawn manually in AnimalCrossingWaterRenderPass
        Renderer.enabled = false;
    }

    void Update()
    {
        Matrix4x4 projInv = m_Camera.projectionMatrix.inverse;
        Matrix4x4 viewInv = m_Camera.transform.localToWorldMatrix;
        Vector3 cameraPosWS = m_Camera.transform.position;

        Vector4[] cornersWS = new[]
        {
                new Vector4(1, 1, 1, 1),
                new Vector4(-1, 1, 1, 1),
                new Vector4(-1, -1, 1, 1),
                new Vector4(1, -1, 1, 1)
            };

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector4(float.MinValue, float.MinValue);
        for (int i = 0; i < cornersWS.Length; i++)
        {
            cornersWS[i] = projInv * cornersWS[i];
            cornersWS[i].z *= -1;
            cornersWS[i] /= cornersWS[i].w;
            cornersWS[i] = viewInv * cornersWS[i];

            Vector3 l = Vector3.Normalize(new Vector3(cornersWS[i].x, cornersWS[i].y, cornersWS[i].z) - cameraPosWS);
            float d = Vector3.Dot(Vector3.zero - cameraPosWS, Vector3.up) / Vector3.Dot(l, Vector3.up);
            cornersWS[i] = cameraPosWS + l * d;

            min.x = Mathf.Min(min.x, cornersWS[i].x);
            min.y = Mathf.Min(min.y, cornersWS[i].z);
            max.x = Mathf.Max(max.x, cornersWS[i].x);
            max.y = Mathf.Max(max.y, cornersWS[i].z);
        }

        Vector2 center = (min + max) * 0.5f;
        float side = Mathf.Max(max.x - min.x, max.y - min.y);
        min = center - new Vector2(side, side) * 0.5f;
        max = center + new Vector2(side, side) * 0.5f;

        Vector4 offsetScale = new Vector4();
        offsetScale.x = (min.x - TerrainWorldBounds.min.x) / TerrainWorldBounds.size.x;
        offsetScale.y = (min.y - TerrainWorldBounds.min.z) / TerrainWorldBounds.size.z;

        Vector2 size = max - min;
        offsetScale.z = TerrainWorldBounds.size.x / size.x;
        offsetScale.w = TerrainWorldBounds.size.z / size.y;

        Shader.SetGlobalVector("_LastFrameVisibleAreaOffsetScale", m_LastFrameVisibleAreaOffsetScale);
        Shader.SetGlobalVector("_VisibleAreaOffsetScale", offsetScale);

        m_LastFrameVisibleAreaOffsetScale = offsetScale;
    }

    void OnDestroy()
    {
        Shader.DisableKeyword(WATER_CAUSTIC_KEYWORD);
    }

    void SetupCausticsUniforms()
    {
        Shader.SetGlobalTexture(WATER_CAUSTICS_MASK_PROP_ID, m_WaterCausticsMask);
        Shader.SetGlobalVector(WATER_CAUSTICS_MASK_ST_PROP_ID, m_WaterCausticMask_ST);
        Shader.SetGlobalTexture(WATER_CAUSTICS_DISTORTION_PROP_ID, m_WaterCausticsDistortion);
        Shader.SetGlobalVector(WATER_CAUSTICS_DISTORTION_ST_PROP_ID, m_WaterCausticsDistortion_ST);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        SetupCausticsUniforms();
    }
#endif
}
