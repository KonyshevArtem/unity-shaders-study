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

    private struct Object
    {
        [UsedImplicitly] public float4 Position;
        [UsedImplicitly] public float4 Color;
        [UsedImplicitly] public float2 SmoothnessEmission;
        [UsedImplicitly] public uint2 TrianglesOffsetCount;
        [UsedImplicitly] public float IsMesh;
        [UsedImplicitly] public float4x4 ModelMatrix;
    }

    private readonly Material m_PathTracingMaterial;
    private readonly Material m_BlendMaterial;
    private readonly ProfilingSampler m_ProfilingSampler;
    private readonly Cubemap m_Skybox;

    private Mesh m_FullScreenMesh;
    private ComputeBuffer m_ObjectsBuffer;
    private ComputeBuffer m_VerticesBuffer;
    private ComputeBuffer m_IndicesBuffer;
    private PathTracingObject[] m_SceneObjects;
    private uint m_MaxBounces;
    private uint m_MaxIterations;
    private Vector2 m_SkyboxRotationSinCos;

    private RenderTargetHandle m_CurrentFrameHandle;
    private RenderTargetIdentifier m_CurrentFrameIdentifier;
    private RenderTexture m_LastFrameRT;

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

    public void Setup(PathTracingObject[] objects, uint maxBounces, uint maxIterations, float skyboxRotationY)
    {
        m_SceneObjects = objects;
        m_MaxBounces = maxBounces;
        m_MaxIterations = maxIterations;

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

        if (m_LastFrameRT == null || m_LastFrameRT.width != desc.width || m_LastFrameRT.height != desc.height)
        {
            if (m_LastFrameRT != null)
            {
                m_LastFrameRT.Release();
            }

            m_LastFrameRT = new RenderTexture(desc);
        }

        m_PathTracingMaterial.SetTexture(SKYBOX_PROP_ID, m_Skybox);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, m_ProfilingSampler))
        {
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
            cmd.SetGlobalFloat("_FrameCount", Time.frameCount);

            cmd.SetRenderTarget(m_CurrentFrameIdentifier);
            cmd.DrawMesh(m_FullScreenMesh, trs, m_PathTracingMaterial);

            if (Application.isPlaying)
            {
                cmd.Blit(m_CurrentFrameIdentifier, m_LastFrameRT, m_BlendMaterial);
                cmd.Blit(m_LastFrameRT, renderingData.cameraData.renderer.cameraColorTarget);
            }
            else
            {
                cmd.Blit(m_CurrentFrameIdentifier, renderingData.cameraData.renderer.cameraColorTarget);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    public void Dispose()
    {
        if (m_LastFrameRT != null)
        {
            m_LastFrameRT.Release();
            m_LastFrameRT = null;
        }
        
        m_ObjectsBuffer?.Release();
        m_ObjectsBuffer = null;
        
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
        if (m_ObjectsBuffer == null || m_SceneObjects.Length != m_ObjectsBuffer.count)
        {
            m_ObjectsBuffer?.Release();
            m_ObjectsBuffer = new ComputeBuffer(m_SceneObjects.Length, sizeof(Object), ComputeBufferType.Constant | ComputeBufferType.Structured, ComputeBufferMode.Dynamic);
        }

        NativeList<float3> vertices = default;
        NativeList<int> indices = default;
        NativeArray<Object> bufferObjects = new NativeArray<Object>(m_SceneObjects.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        
        for (int i = 0; i < m_SceneObjects.Length; ++i)
        {
            PathTracingObject sceneObject = m_SceneObjects[i];
            
            uint trianglesOffset = 0;
            uint trianglesCount = 0;
            float4x4 modelMatrix = 0;
            
            bool isMesh = sceneObject.Type == PathTracingObject.ObjectType.TriangleMesh;
            if (isMesh)
            {
                if (!vertices.IsCreated)
                {
                    vertices = new NativeList<float3>(0, Allocator.Temp);
                    indices = new NativeList<int>(0, Allocator.Temp);
                }
                
                Mesh mesh = sceneObject.Mesh;
                Vector3[] meshVertices = mesh.vertices;
                int[] meshIndices = mesh.triangles;

                int oldVerticesLength = vertices.Length;
                trianglesOffset = (uint) indices.Length;
                trianglesCount = (uint) meshIndices.Length;
                modelMatrix = sceneObject.transform.localToWorldMatrix;
                
                vertices.Resize(vertices.Length + meshVertices.Length, NativeArrayOptions.UninitializedMemory);
                indices.Resize(indices.Length + meshIndices.Length, NativeArrayOptions.UninitializedMemory);
                
                void* verticesPtr = (float3*)vertices.GetUnsafePtr() + oldVerticesLength;
                void* indicesPtr = (int*)indices.GetUnsafePtr() + trianglesOffset;
                
                fixed (Vector3* meshVerticesPtr = &meshVertices[0])
                fixed (int* meshIndicesPtr = &meshIndices[0])
                {
                    UnsafeUtility.MemCpy(verticesPtr, meshVerticesPtr, sizeof(float3) * meshVertices.Length);
                    UnsafeUtility.MemCpy(indicesPtr, meshIndicesPtr, sizeof(int) * trianglesCount);    
                }
            }
            
            Object bufferObject = new Object
            {
                Position = new float4(sceneObject.Position, sceneObject.Size),
                Color = sceneObject.Color,
                SmoothnessEmission = new float2(sceneObject.Smoothness, sceneObject.Emission),
                TrianglesOffsetCount = new uint2(trianglesOffset, trianglesCount),
                IsMesh = isMesh ? 1 : 0,
                ModelMatrix = modelMatrix
            };
            bufferObjects[i] = bufferObject;
        }

        if (m_VerticesBuffer == null)
        {
            m_VerticesBuffer = new ComputeBuffer(vertices.Length, sizeof(float3), ComputeBufferType.Constant | ComputeBufferType.Structured, ComputeBufferMode.Dynamic);
            m_IndicesBuffer = new ComputeBuffer(indices.Length, sizeof(int), ComputeBufferType.Constant | ComputeBufferType.Structured, ComputeBufferMode.Dynamic);
            cmd.SetBufferData(m_VerticesBuffer, vertices.AsArray());
            cmd.SetBufferData(m_IndicesBuffer, indices.AsArray());
        }

        cmd.SetBufferData(m_ObjectsBuffer, bufferObjects);
        cmd.SetGlobalBuffer("_Objects", m_ObjectsBuffer);
        cmd.SetGlobalBuffer("_Vertices", m_VerticesBuffer);
        cmd.SetGlobalBuffer("_Indices", m_IndicesBuffer);
        cmd.SetGlobalInteger("_ObjectsCount", bufferObjects.Length);

        bufferObjects.Dispose();
        if (vertices.IsCreated)
        {
            vertices.Dispose();
            indices.Dispose();
        }
    }
}