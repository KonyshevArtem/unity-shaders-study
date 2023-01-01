using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class AlphaClippingRenderPass : ScriptableRenderPass
{
    private struct PixelInfo
    {
        public float Depth;
        public float EmitterIndex;
    }

    private struct EmitInfo
    {
        public float3 Position;
        public uint EmitterIndex;
    }

    private const int MAX_PARTICLES_PER_FRAME = 128;
    private const int HEIGHT = 256;

    private List<AlphaClippingController> m_Controllers;

    private ProfilingSampler m_ProfilingSampler;
    private RenderTargetIdentifier m_TargetIdentifier;
    private RenderTexture m_AlphaClipDiffMask;
    private ParticleSystem[] m_ParticleSystems;

    private float4x4 m_InvViewProj;
    private bool m_DebugMask;

    public AlphaClippingRenderPass(ParticleSystem[] particleSystems)
    {
        m_ProfilingSampler = new ProfilingSampler("Cutoff Diff");

        renderPassEvent = RenderPassEvent.AfterRendering;
        m_ParticleSystems = particleSystems;
    }

    public void Setup(List<AlphaClippingController> controllers, bool debugMask)
    {
        m_Controllers = controllers;
        m_DebugMask = debugMask;
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        base.Configure(cmd, cameraTextureDescriptor);

        ValidateTexture();

        ConfigureTarget(m_TargetIdentifier);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, m_ProfilingSampler))
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            cmd.WaitAllAsyncReadbackRequests();

            cmd.ClearRenderTarget(false, true, Color.clear);
            for (int i = 0; i < m_Controllers.Count; i++)
            {
                cmd.DrawRenderer(m_Controllers[i].Renderer, m_Controllers[i].Renderer.sharedMaterial, 0, 2);
            }

            m_InvViewProj = (GL.GetGPUProjectionMatrix(renderingData.cameraData.GetProjectionMatrix(), false) *
                             renderingData.cameraData.GetViewMatrix()).inverse;
            cmd.RequestAsyncReadback(m_AlphaClipDiffMask, PixelsReadFinished);

#if UNITY_EDITOR
            if (m_DebugMask)
            {
                cmd.Blit(m_TargetIdentifier, renderingData.cameraData.renderer.cameraColorTarget);
            }
#endif
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    private void ValidateTexture()
    {
        float aspect = (float)Screen.width / Screen.height;
        int width = (int)(HEIGHT * aspect);
        if (m_AlphaClipDiffMask != null && m_AlphaClipDiffMask.width == width)
        {
            return;
        }

        if (m_AlphaClipDiffMask != null)
        {
            m_AlphaClipDiffMask.Release();
        }

        m_AlphaClipDiffMask = new RenderTexture(width, HEIGHT, 0, GraphicsFormat.R32G32_SFloat);
        m_AlphaClipDiffMask.filterMode = FilterMode.Point;

        m_TargetIdentifier = new RenderTargetIdentifier(m_AlphaClipDiffMask);
    }

    private void PixelsReadFinished(AsyncGPUReadbackRequest request)
    {
        if (!request.done)
        {
            return;
        }

        using (NativeArray<PixelInfo> pixels = request.GetData<PixelInfo>())
        {
            NativeArray<EmitInfo> emits = new NativeArray<EmitInfo>(MAX_PARTICLES_PER_FRAME, Allocator.Temp,
                NativeArrayOptions.UninitializedMemory);

            int emitCount = 0;
            for (int i = 0; i < pixels.Length && emitCount < emits.Length; i++)
            {
                PixelInfo info = pixels[i];
                if (info.Depth > 0)
                {
                    int pixelIndex = i;
                    float x = (float)(pixelIndex % m_AlphaClipDiffMask.width) / m_AlphaClipDiffMask.width;
                    float y = (float)(pixelIndex / m_AlphaClipDiffMask.width) / m_AlphaClipDiffMask.height;

                    float4 clipSpace = new float4(x * 2 - 1, y * 2 - 1, info.Depth, 1);
                    float4 worldSpace = math.mul(m_InvViewProj, clipSpace);
                    worldSpace.xyz /= worldSpace.w;

                    emits[emitCount++] = new EmitInfo
                    {
                        Position = worldSpace.xyz,
                        EmitterIndex = (uint)info.EmitterIndex
                    };
                }
            }

            for (int i = 0; i < emitCount; i++)
            {
                m_ParticleSystems[emits[i].EmitterIndex]
                    .Emit(new ParticleSystem.EmitParams { position = emits[i].Position }, 1);
            }

            emits.Dispose();
        }
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        base.OnCameraCleanup(cmd);

        m_Controllers.Clear();
    }

    public void Dispose()
    {
        if (m_AlphaClipDiffMask != null)
        {
            m_AlphaClipDiffMask.Release();
        }
    }
}