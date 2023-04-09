using JetBrains.Annotations;
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
        [UsedImplicitly] public float Smoothness;
        [UsedImplicitly] public float Emission;
    }

    private readonly Material m_PathTracingMaterial;
    private readonly Material m_BlendMaterial;
    private readonly ProfilingSampler m_ProfilingSampler;
    private readonly Mesh m_Mesh;
    private readonly Cubemap m_Skybox;

    private Object[] m_ObjectsBuffer;
    private ComputeBuffer m_ComputeBuffer;
    private PathTracingObject[] m_Objects;
    private float m_LastFrameBlendWeight;
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

        m_Mesh = new Mesh
        {
            vertices = new[]
            {
                new Vector3(-1, -1, 0),
                new Vector3(1, -1, 0),
                new Vector3(1, 1, 0),
                new Vector3(-1, 1, 0)
            },
            triangles = new[] { 0, 1, 2, 0, 2, 3 }
        };

        m_CurrentFrameHandle.Init("_CurrentFrame");
        m_CurrentFrameIdentifier = new RenderTargetIdentifier(m_CurrentFrameHandle.id);

        renderPassEvent = RenderPassEvent.AfterRendering;
    }

    public void Setup(PathTracingObject[] objects, float lastFrameBlendWeight, uint maxBounces, uint maxIterations,
        float skyboxRotationY)
    {
        m_Objects = objects;
        m_LastFrameBlendWeight = lastFrameBlendWeight;
        m_MaxBounces = maxBounces;
        m_MaxIterations = maxIterations;

        m_SkyboxRotationSinCos = new Vector2(math.sin(skyboxRotationY), math.cos(skyboxRotationY));
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        base.Configure(cmd, cameraTextureDescriptor);

        int objectSize = sizeof(Object);
        if (m_ComputeBuffer == null || m_Objects.Length != m_ComputeBuffer.count)
        {
            m_ObjectsBuffer = new Object[m_Objects.Length];

            m_ComputeBuffer?.Release();
            m_ComputeBuffer = new ComputeBuffer(m_Objects.Length, objectSize,
                ComputeBufferType.Constant | ComputeBufferType.Structured, ComputeBufferMode.Dynamic);
        }

        for (int i = 0; i < m_Objects.Length; ++i)
        {
            PathTracingObject obj = m_Objects[i];
            m_ObjectsBuffer[i].Position = new float4(obj.Position, obj.Size);
            m_ObjectsBuffer[i].Color = obj.Color;
            m_ObjectsBuffer[i].Smoothness = obj.Smoothness;
            m_ObjectsBuffer[i].Emission = obj.Emission;
        }

        cmd.SetBufferData(m_ComputeBuffer, m_ObjectsBuffer);
        cmd.SetGlobalBuffer("_Objects", m_ComputeBuffer);
        cmd.SetGlobalInteger("_ObjectsCount", m_ObjectsBuffer.Length);

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

            m_Mesh.vertices = corners;
            m_Mesh.UploadMeshData(false);

            Transform cameraTransform = renderingData.cameraData.camera.transform;
            Vector3 cameraForward = cameraTransform.forward;
            Matrix4x4 trs = Matrix4x4.TRS(
                cameraTransform.position,
                Quaternion.LookRotation(cameraForward, cameraTransform.up),
                Vector3.one);

            cmd.SetGlobalInt("_MaxBounces", (int)m_MaxBounces);
            cmd.SetGlobalInt("_MaxIterations", (int)m_MaxIterations);
            cmd.SetGlobalVector("_SkyboxRotationSinCos", m_SkyboxRotationSinCos);
            cmd.SetGlobalFloat("_LastFrameBlendWeight", m_LastFrameBlendWeight);

            cmd.SetRenderTarget(m_CurrentFrameIdentifier);
            cmd.DrawMesh(m_Mesh, trs, m_PathTracingMaterial);

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
}