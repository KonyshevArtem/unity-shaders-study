using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PsOneRenderPass : ScriptableRenderPass
{
    private Material m_Material;
    private ProfilingSampler m_ProfilingSampler;
    private RenderTargetHandle m_TargetHandle;
    private RenderTargetIdentifier m_TargetIdentifier;
    private RenderTargetIdentifier m_CameraOpaqueTextureIdentifier;

    public PsOneRenderPass(Shader _UtilShader)
    {
        m_Material = new Material(_UtilShader);

        m_TargetHandle.Init("_PsOneTarget");
        m_TargetIdentifier = new RenderTargetIdentifier(m_TargetHandle.id);
        m_CameraOpaqueTextureIdentifier = new RenderTargetIdentifier("_CameraOpaqueTexture");

        // after final blit because we need to output with point sampling
        renderPassEvent = RenderPassEvent.AfterRendering + 2;
        
        // request color texture because after final blit there is no way to get camera target
        ConfigureInput(ScriptableRenderPassInput.Color);
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        base.Configure(cmd, cameraTextureDescriptor);

        cmd.GetTemporaryRT(m_TargetHandle.id, cameraTextureDescriptor.width, cameraTextureDescriptor.height, 0,
            FilterMode.Point, GraphicsFormat.R8G8B8A8_SNorm);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, m_ProfilingSampler))
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            
            cmd.Blit(m_CameraOpaqueTextureIdentifier, m_TargetIdentifier, m_Material, 0);
            cmd.Blit(m_TargetIdentifier, renderingData.cameraData.renderer.cameraColorTarget, m_Material, 0);
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        base.FrameCleanup(cmd);
        
        cmd.ReleaseTemporaryRT(m_TargetHandle.id);
    }
}