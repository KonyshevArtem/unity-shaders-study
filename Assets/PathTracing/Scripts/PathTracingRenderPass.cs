using JetBrains.Annotations;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public unsafe class PathTracingRenderPass : ScriptableRenderPass
{
    private static readonly int SKYBOX_PROP_ID = Shader.PropertyToID("_Skybox");

    private struct MaterialParameters
    {
        [UsedImplicitly] public float4 Color;
        [UsedImplicitly] public float2 SmoothnessEmission;
    }

    private struct Sphere
    {
        [UsedImplicitly] public float4 PositionRadius;
        [UsedImplicitly] public MaterialParameters Material;
    }

    private struct TriangleMesh
    {
        [UsedImplicitly] public float4x4 ModelMatrix;
        [UsedImplicitly] public MaterialParameters Material;
        [UsedImplicitly] public uint2 TrianglesOffsetCount;
        [UsedImplicitly] public float3 Min;
        [UsedImplicitly] public float3 Max;
    }

    private readonly Material m_PathTracingMaterial;
    private readonly Material m_BlendMaterial;
    private readonly ProfilingSampler m_ProfilingSampler;
    private readonly Cubemap m_Skybox;

    private Mesh m_FullScreenMesh;
    private ComputeBuffer m_SpheresBuffer;
    private ComputeBuffer m_TriangleMeshesBuffer;
    private ComputeBuffer m_VerticesBuffer;
    private ComputeBuffer m_IndicesBuffer;
    private PathTracingObject[] m_SceneObjects;
    private uint m_MaxBounces;
    private uint m_MaxIterations;
    private bool m_PreApplyModelMatrix;
    private bool m_NoIndices;
    private Vector2 m_SkyboxRotationSinCos;
    private int m_FrameCount;

    private RenderTargetHandle m_CurrentFrameHandle;
    private RenderTargetIdentifier m_CurrentFrameIdentifier;
    private RenderTexture m_LastFrameRT;

#if UNITY_EDITOR
    private RenderTexture m_LastFrameSceneViewRT;
#endif

    public PathTracingRenderPass(Material pathTracingShader, Material blendShader, Cubemap skybox)
    {
        m_PathTracingMaterial = pathTracingShader;
        m_BlendMaterial = blendShader;

        m_Skybox = skybox;

        m_ProfilingSampler = new ProfilingSampler("Path Tracing");

        m_CurrentFrameHandle.Init("_CurrentFrame");
        m_CurrentFrameIdentifier = new RenderTargetIdentifier(m_CurrentFrameHandle.id);

        renderPassEvent = RenderPassEvent.AfterRendering;
    }

    public void Setup(PathTracingObject[] objects, uint maxBounces, uint maxIterations, float skyboxRotationY, bool preApplyModelMatrix, bool noIndices)
    {
        m_SceneObjects = objects;
        m_MaxBounces = maxBounces;
        m_MaxIterations = maxIterations;
        m_PreApplyModelMatrix = preApplyModelMatrix;
        m_NoIndices = noIndices;

        m_SkyboxRotationSinCos = new Vector2(math.sin(skyboxRotationY), math.cos(skyboxRotationY));
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        base.Configure(cmd, cameraTextureDescriptor);

        SetupBuffers(cmd);

        RenderTextureDescriptor desc = new RenderTextureDescriptor(
            cameraTextureDescriptor.width,
            cameraTextureDescriptor.height,
            GraphicsFormat.R8G8B8A8_UNorm, // no srgb for better linear blending
            0); // no need for depth

        cmd.GetTemporaryRT(m_CurrentFrameHandle.id, desc);

        m_PathTracingMaterial.SetTexture(SKYBOX_PROP_ID, m_Skybox);
    }

    private static void ConfigureLastFrameRT(ref RenderTexture lastFrameRT, RenderTextureDescriptor descriptor)
    {
        if (lastFrameRT != null && lastFrameRT.width == descriptor.width && lastFrameRT.height == descriptor.height)
        {
            return;
        }

        if (lastFrameRT != null)
        {
            lastFrameRT.Release();
        }

        lastFrameRT = new RenderTexture(descriptor);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, m_ProfilingSampler))
        {
#if UNITY_EDITOR
            if (renderingData.cameraData.isSceneViewCamera)
            {
                ConfigureLastFrameRT(ref m_LastFrameSceneViewRT, renderingData.cameraData.cameraTargetDescriptor);
            }
            else
#endif
            {
                ConfigureLastFrameRT(ref m_LastFrameRT, renderingData.cameraData.cameraTargetDescriptor);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            Vector3[] corners = new Vector3[4];
            renderingData.cameraData.camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1),
                (renderingData.cameraData.camera.nearClipPlane + renderingData.cameraData.camera.farClipPlane) * 0.5f,
                Camera.MonoOrStereoscopicEye.Mono, corners);

            EnsureFullScreenMesh();
            m_FullScreenMesh.vertices = corners;
            m_FullScreenMesh.UploadMeshData(false);

            Transform cameraTransform = renderingData.cameraData.camera.transform;
            Vector3 cameraForward = cameraTransform.forward;
            Matrix4x4 trs = Matrix4x4.TRS(
                cameraTransform.position,
                Quaternion.LookRotation(cameraForward, cameraTransform.up),
                Vector3.one);

            cmd.SetGlobalInt("_MaxBounces", (int)m_MaxBounces);
            cmd.SetGlobalInt("_MaxIterations", (int)m_MaxIterations);
            cmd.SetGlobalVector("_SkyboxRotationSinCos", m_SkyboxRotationSinCos);
            cmd.SetGlobalFloat("_FrameCount", ++m_FrameCount);

            cmd.SetRenderTarget(m_CurrentFrameIdentifier);
            cmd.DrawMesh(m_FullScreenMesh, trs, m_PathTracingMaterial);

            if (Application.isPlaying)
            {
#if UNITY_EDITOR
                if (renderingData.cameraData.isSceneViewCamera)
                {
                    BlendLastFrame(cmd, renderingData.cameraData.renderer.cameraColorTarget, m_LastFrameSceneViewRT);
                }
                else
#endif
                {
                    BlendLastFrame(cmd, renderingData.cameraData.renderer.cameraColorTarget, m_LastFrameRT);
                }
            }
            else
            {
                cmd.Blit(m_CurrentFrameIdentifier, renderingData.cameraData.renderer.cameraColorTarget);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    private void BlendLastFrame(CommandBuffer cmd, RenderTargetIdentifier currentFrameIdentifier, RenderTexture lastFrameRT)
    {
        cmd.Blit(m_CurrentFrameIdentifier, lastFrameRT, m_BlendMaterial);
        cmd.Blit(m_LastFrameRT, currentFrameIdentifier);
    }

    public void Dispose()
    {
        if (m_LastFrameRT != null)
        {
            m_LastFrameRT.Release();
            m_LastFrameRT = null;
        }

#if UNITY_EDITOR
        if (m_LastFrameSceneViewRT != null)
        {
            m_LastFrameSceneViewRT.Release();
            m_LastFrameSceneViewRT = null;
        }
#endif
        
        m_SpheresBuffer?.Release();
        m_SpheresBuffer = null;

        m_TriangleMeshesBuffer?.Release();
        m_TriangleMeshesBuffer = null;
        
        m_VerticesBuffer?.Release();
        m_VerticesBuffer = null;

        m_IndicesBuffer?.Release();
        m_IndicesBuffer = null;
        
        CoreUtils.Destroy(m_FullScreenMesh);
        m_FullScreenMesh = null;
    }

    private void EnsureFullScreenMesh()
    {
        if (m_FullScreenMesh == null)
        {
            m_FullScreenMesh = new Mesh
            {
                vertices = new[]
                {
                    new Vector3(-1, -1, 0),
                    new Vector3(1, -1, 0),
                    new Vector3(1, 1, 0),
                    new Vector3(-1, 1, 0)
                },
                triangles = new[] { 0, 1, 2, 0, 2, 3 },
                uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) }
            };
        }
    }

    private void SetupBuffers(CommandBuffer cmd)
    {
        NativeList<float3> vertices = default;
        NativeList<int> indices = default;

        NativeList<Sphere> spheresBuffer = new NativeList<Sphere>(m_SceneObjects.Length, Allocator.Temp);
        NativeList<TriangleMesh> triangleMeshesBuffer = new NativeList<TriangleMesh>(m_SceneObjects.Length, Allocator.Temp);
        
        for (int i = 0; i < m_SceneObjects.Length; ++i)
        {
            PathTracingObject sceneObject = m_SceneObjects[i];
            
            MaterialParameters material = new MaterialParameters
            {
                Color = sceneObject.Color,
                SmoothnessEmission = new float2(sceneObject.Smoothness, sceneObject.Emission)
            };
            
            if (sceneObject.Type == PathTracingObject.ObjectType.TriangleMesh)
            {
                Matrix4x4 modelMatrix = sceneObject.transform.localToWorldMatrix;

                if (m_VerticesBuffer == null)
                {
                    if (!vertices.IsCreated)
                    {
                        vertices = new NativeList<float3>(0, Allocator.Temp);
                        indices = new NativeList<int>(0, Allocator.Temp);
                    }

                    Mesh mesh = sceneObject.Mesh;
                    Vector3[] meshVertices = mesh.vertices;
                    int[] meshIndices = mesh.triangles;

                    int baseVertexIndex = vertices.Length;
                    if (m_NoIndices)
                    {
                        Vector3[] meshVerticesNoShared = new Vector3[meshIndices.Length];
                        for (int j = 0; j < meshIndices.Length; j++)
                        {
                            meshVerticesNoShared[j] = meshVertices[meshIndices[j]];
                        }

                        meshVertices = meshVerticesNoShared;
                    }
                    else
                    {
                        for (int j = 0; j < meshIndices.Length; j++)
                        {
                            meshIndices[j] += baseVertexIndex;
                        }
                    }
                    
                    if (m_PreApplyModelMatrix)
                    {
                        for (int j = 0; j < meshVertices.Length; j++)
                        {
                            meshVertices[j] = modelMatrix.MultiplyPoint(meshVertices[j]);
                        }
                    }

                    sceneObject.TrianglesBegin = (uint)indices.Length / 3;
                    sceneObject.TrianglesEnd = sceneObject.TrianglesBegin + (uint)meshIndices.Length / 3;

                    vertices.Resize(vertices.Length + meshVertices.Length, NativeArrayOptions.UninitializedMemory);
                    indices.Resize(indices.Length + meshIndices.Length, NativeArrayOptions.UninitializedMemory);

                    void* verticesPtr = (float3*)vertices.GetUnsafePtr() + baseVertexIndex;
                    void* indicesPtr = (int*)indices.GetUnsafePtr() + sceneObject.TrianglesBegin * 3;

                    fixed (Vector3* meshVerticesPtr = &meshVertices[0])
                    fixed (int* meshIndicesPtr = &meshIndices[0])
                    {
                        UnsafeUtility.MemCpy(verticesPtr, meshVerticesPtr, sizeof(float3) * meshVertices.Length);
                        UnsafeUtility.MemCpy(indicesPtr, meshIndicesPtr, sizeof(int) * sceneObject.TrianglesCount * 3);
                    }
                }

                Bounds aabb = sceneObject.AABB;
                
                TriangleMesh triangleMesh = new TriangleMesh
                {
                    Material = material,
                    ModelMatrix = modelMatrix,
                    TrianglesOffsetCount = new uint2(sceneObject.TrianglesBegin, sceneObject.TrianglesEnd),
                    Min = aabb.min,
                    Max = aabb.max
                };
                triangleMeshesBuffer.Add(triangleMesh);
            }
            else
            {
                Sphere sphere = new Sphere
                {
                    PositionRadius = new float4(sceneObject.Position, sceneObject.Size),
                    Material = material,
                };
                spheresBuffer.Add(sphere);   
            }
        }
        
        if (m_SpheresBuffer == null || spheresBuffer.Length != m_SpheresBuffer.count)
        {
            m_SpheresBuffer?.Release();
            m_SpheresBuffer = new ComputeBuffer(spheresBuffer.Length, sizeof(Sphere), ComputeBufferType.Constant | ComputeBufferType.Structured, ComputeBufferMode.Dynamic);
        }

        if (m_TriangleMeshesBuffer == null || triangleMeshesBuffer.Length != m_TriangleMeshesBuffer.count)
        {
            m_TriangleMeshesBuffer?.Release();
            m_TriangleMeshesBuffer = new ComputeBuffer(triangleMeshesBuffer.Length, sizeof(TriangleMesh), ComputeBufferType.Constant | ComputeBufferType.Structured, ComputeBufferMode.Dynamic);
        }

        if (m_VerticesBuffer == null)
        {
            m_VerticesBuffer = new ComputeBuffer(vertices.Length, sizeof(float3), ComputeBufferType.Constant | ComputeBufferType.Structured, ComputeBufferMode.Dynamic);
            m_IndicesBuffer = new ComputeBuffer(indices.Length, sizeof(int), ComputeBufferType.Constant | ComputeBufferType.Structured, ComputeBufferMode.Dynamic);
            cmd.SetBufferData(m_VerticesBuffer, vertices.AsArray());
            cmd.SetBufferData(m_IndicesBuffer, indices.AsArray());
        }

        cmd.SetBufferData(m_SpheresBuffer, spheresBuffer.AsArray());
        cmd.SetBufferData(m_TriangleMeshesBuffer, triangleMeshesBuffer.AsArray());
        cmd.SetGlobalBuffer("_Spheres", m_SpheresBuffer);
        cmd.SetGlobalBuffer("_TriangleMeshes", m_TriangleMeshesBuffer);
        cmd.SetGlobalBuffer("_Vertices", m_VerticesBuffer);
        cmd.SetGlobalBuffer("_Indices", m_IndicesBuffer);
        cmd.SetGlobalInteger("_SpheresCount", spheresBuffer.Length);
        cmd.SetGlobalInteger("_TriangleMeshesCount", triangleMeshesBuffer.Length);

        CoreUtils.SetKeyword(cmd, "_MATRICES_PRE_APPLIED", m_PreApplyModelMatrix);
        CoreUtils.SetKeyword(cmd, "_NO_INDICES", m_NoIndices);

        spheresBuffer.Dispose();
        if (vertices.IsCreated)
        {
            vertices.Dispose();
            indices.Dispose();
        }
    }
}