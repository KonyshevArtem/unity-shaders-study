using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class AnimalCrossingRain : MonoBehaviour
{
    const int RIPPLES_COUNT = 300;
    const int RIPPLE_SIZE_PX = 10;

    const float RIPPLE_MIN_LIFETIME = 0.2f;
    const float RIPPLE_MAX_LIFETIME = 0.4f;
    const float RIPPLE_MIN_SCALE = 0.5f;
    const float RIPPLE_MAX_SCALE = 1.5f;
    const float RIPPLE_GROW_SPEED = 2f;

    const string RAIN_RIPPLES_KEYWORD = "ANIMAL_CROSSING_RAIN_RIPPLES";

    [SerializeField] Vector2 m_MinRainDropSize;
    [SerializeField] Vector2 m_MaxRainDropSize;
    [SerializeField] int m_RainDropCount = 10;

    MeshRenderer m_Renderer;

    NativeArray<float4> m_Infos;
    NativeArray<float2> m_RipplesLifetime;

    public ComputeBuffer IndirectArgsBuffer { get; private set; }
    public ComputeBuffer InfosBuffer { get; private set; }

    public MeshRenderer Renderer => m_Renderer;
    public Mesh RippleMesh { get; private set; }

    void Awake()
    {
        InitRainMesh();
        InitRippleMesh();

        InfosBuffer = new ComputeBuffer(RIPPLES_COUNT, 4 * sizeof(float), ComputeBufferType.Structured);

        NativeArray<uint> indirectArgs = new NativeArray<uint>(5, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        indirectArgs[0] = RippleMesh.GetIndexCount(0);
        indirectArgs[1] = RIPPLES_COUNT;
        indirectArgs[2] = RippleMesh.GetIndexStart(0);
        indirectArgs[3] = RippleMesh.GetBaseVertex(0);
        indirectArgs[4] = 0;

        IndirectArgsBuffer = new ComputeBuffer(1, indirectArgs.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        IndirectArgsBuffer.SetData(indirectArgs);
        indirectArgs.Dispose();

        m_Infos = new NativeArray<float4>(RIPPLES_COUNT, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        m_RipplesLifetime = new NativeArray<float2>(RIPPLES_COUNT, Allocator.Persistent);

        Shader.EnableKeyword(RAIN_RIPPLES_KEYWORD);
    }

    void InitRainMesh()
    {
        float aspect = Camera.main.aspect;

        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[m_RainDropCount * 4];
        Vector3[] normals = new Vector3[m_RainDropCount * 4];
        Vector3[] centers = new Vector3[m_RainDropCount * 4];
        int[] triangles = new int[m_RainDropCount * 6];
        for (int i = 0; i < m_RainDropCount; i++)
        {
            Vector3 pos = new Vector3(UnityEngine.Random.Range(-aspect, aspect), UnityEngine.Random.Range(-1.0f, 1.0f), UnityEngine.Random.Range(-1.0f, 1.0f));
            Vector2 size = new Vector2(UnityEngine.Random.Range(m_MinRainDropSize.x, m_MaxRainDropSize.x), UnityEngine.Random.Range(m_MinRainDropSize.y, m_MaxRainDropSize.y));
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

    void InitRippleMesh()
    {
        RippleMesh = new Mesh();
        RippleMesh.SetVertices(new[] {
            new Vector3(-0.5f, 0, -0.5f),
            new Vector3(0.5f, 0, -0.5f),
            new Vector3(0.5f, 0, 0.5f),
            new Vector3(-0.5f, 0, 0.5f)});
       RippleMesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
       RippleMesh.SetNormals(new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up });
       RippleMesh.SetUVs(0, new[] {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)});
    }

    private void Update()
    {
        if (!m_Infos.IsCreated || !m_RipplesLifetime.IsCreated)
        {
            return;
        }

        int mapSize = AnimalCrossingRainRenderPass.RAIN_DROP_NORMAL_MAP_SIZE_PX;
        Vector4 visibleAreaDiff = AnimalCrossingWater.LastFrameVisibleArea - AnimalCrossingWater.VisibleArea;
        for (int i = 0; i < m_Infos.Length; i++)
        {
            float2 lifetime = m_RipplesLifetime[i];
            float4 info = m_Infos[i];
            if (lifetime.x <= 0)
            {
                info.x = UnityEngine.Random.Range(0, mapSize);
                info.y = UnityEngine.Random.Range(0, mapSize);
                info.z = RIPPLE_SIZE_PX * UnityEngine.Random.Range(RIPPLE_MIN_SCALE, RIPPLE_MAX_SCALE);
                info.w = 1;

                lifetime.xy = UnityEngine.Random.Range(RIPPLE_MIN_LIFETIME, RIPPLE_MAX_LIFETIME);
            }
            else
            {
                info.x += mapSize * visibleAreaDiff.x * AnimalCrossingWater.VisibleArea.z;
                info.y += mapSize * visibleAreaDiff.y * AnimalCrossingWater.VisibleArea.w;
                info.z += RIPPLE_SIZE_PX * RIPPLE_GROW_SPEED * Time.deltaTime;
                info.w = lifetime.x / lifetime.y;

                lifetime.x -= Time.deltaTime;
            }

            m_Infos[i] = info;
            m_RipplesLifetime[i] = lifetime;
        }

        InfosBuffer.SetData(m_Infos);
    }

    private void OnDestroy()
    {
        if (IndirectArgsBuffer != null)
        {
            IndirectArgsBuffer.Dispose();
            IndirectArgsBuffer = null;
        }

        if (InfosBuffer != null)
        {
            InfosBuffer.Dispose();
            InfosBuffer = null;
        }

        if (m_Infos.IsCreated)
        {
            m_Infos.Dispose();
        }

        Shader.DisableKeyword(RAIN_RIPPLES_KEYWORD);
    }
}
