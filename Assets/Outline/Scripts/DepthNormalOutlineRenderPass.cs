using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DepthNormalOutlineRenderPass : ScriptableRenderPass
{
    private ProfilingSampler m_ProfilingSampler;
    private RenderTargetHandle m_OutlineMaskTargetHandle;
    private RenderTargetIdentifier m_OutlineMaskTargetIdentifier;
    private Material m_OutlineMaterial;

    public DepthNormalOutlineRenderPass(Shader _OutlineShader)
    {
        m_OutlineMaterial = new Material(_OutlineShader);

        m_OutlineMaskTargetHandle.Init("_OutlineMask");
        m_OutlineMaskTargetIdentifier = new RenderTargetIdentifier(m_OutlineMaskTargetHandle.id);

        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        base.Configure(cmd, cameraTextureDescriptor);

        cmd.GetTemporaryRT(m_OutlineMaskTargetHandle.id,
            new RenderTextureDescriptor(cameraTextureDescriptor.width, cameraTextureDescriptor.height,
                RenderTextureFormat.R8));
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, m_ProfilingSampler))
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            cmd.SetRenderTarget(m_OutlineMaskTargetIdentifier);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_OutlineMaterial, 0, 0);
            cmd.SetGlobalTexture(m_OutlineMaskTargetHandle.id, m_OutlineMaskTargetIdentifier);
            cmd.SetRenderTarget(renderingData.cameraData.renderer.cameraColorTarget);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_OutlineMaterial, 0, 1);
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        base.FrameCleanup(cmd);

        cmd.ReleaseTemporaryRT(m_OutlineMaskTargetHandle.id);
    }
}