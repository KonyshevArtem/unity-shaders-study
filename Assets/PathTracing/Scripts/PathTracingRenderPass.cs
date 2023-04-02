using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public unsafe class PathTracingRenderPass : ScriptableRenderPass
{
    private Material m_PathTracingMaterial;
    private ProfilingSampler m_ProfilingSampler;
    private Mesh m_Mesh;
    private PathTracingController m_Controller;
    private ComputeBuffer m_ComputeBuffer;

    public PathTracingRenderPass(Shader shader, PathTracingController controller)
    {
        m_PathTracingMaterial = new Material(shader);
        m_Controller = controller;

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

        renderPassEvent = RenderPassEvent.AfterRendering;
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        base.Configure(cmd, cameraTextureDescriptor);

        PathTracingObject[] objects = m_Controller.Objects;

        int objectSize = sizeof(PathTracingObject);
        if (m_ComputeBuffer == null || objects.Length != m_ComputeBuffer.count)
        {
            m_ComputeBuffer?.Release();
            m_ComputeBuffer = new ComputeBuffer(objects.Length, objectSize,
                ComputeBufferType.Constant | ComputeBufferType.Structured, ComputeBufferMode.Dynamic);
        }

        cmd.SetBufferData(m_ComputeBuffer, objects);
        cmd.SetGlobalBuffer("_Objects", m_ComputeBuffer);
        cmd.SetGlobalInteger("_ObjectsCount", objects.Length);

        m_PathTracingMaterial.SetTexture("_Skybox", m_Controller.Skybox);
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

            cmd.DrawMesh(m_Mesh, trs, m_PathTracingMaterial);
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}