using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ImageEffectsRenderPass : ScriptableRenderPass
{
    public ImageEffectsRenderPass(Shader _DisplacementShader, Shader _BoxBlurShader, Shader _ChromaticAbberationShader)
    {
        if (_DisplacementShader != null)
            m_DisplacementMaterial = new Material(_DisplacementShader);
        if (_BoxBlurShader != null)
            m_BoxBlurMaterial = new Material(_BoxBlurShader);
        if (_ChromaticAbberationShader != null)
            m_ChromaticAbberationMaterial = new Material(_ChromaticAbberationShader);

        m_TempHandle1.Init("_ImageEffectsTarget1");
        m_TempHandle2.Init("_ImageEffectsTarget2");
        m_TempTarget1 = new RenderTargetIdentifier(m_TempHandle1.id);
        m_TempTarget2 = new RenderTargetIdentifier(m_TempHandle2.id);

        m_UVAtTop = SystemInfo.graphicsUVStartsAtTop;

        renderPassEvent = RenderPassEvent.AfterRendering;
        ConfigureClear(ClearFlag.None, Color.clear);
    }

    readonly bool m_UVAtTop;

    readonly Material m_DisplacementMaterial;
    readonly Material m_BoxBlurMaterial;
    readonly Material m_ChromaticAbberationMaterial;

    static readonly int m_DisplacementPropId = Shader.PropertyToID("_Displacement");
    static readonly int m_MagnitudePropId = Shader.PropertyToID("_Magnitude");
    static readonly int m_RedOffsetPropId = Shader.PropertyToID("_RedOffset");
    static readonly int m_GreenOffsetPropId = Shader.PropertyToID("_GreenOffset");
    static readonly int m_BlueOffsetPropId = Shader.PropertyToID("_BlueOffset");

    readonly RenderTargetHandle m_TempHandle1;
    readonly RenderTargetHandle m_TempHandle2;
    readonly RenderTargetIdentifier m_TempTarget1;
    readonly RenderTargetIdentifier m_TempTarget2;

    ImageEffectsController m_Controller;

    bool m_SwapTempTargets;

    RenderTargetIdentifier Source => m_SwapTempTargets ? m_TempTarget2 : m_TempTarget1;
    RenderTargetIdentifier Destination => m_SwapTempTargets ? m_TempTarget1 : m_TempTarget2;

    public void Setup(ImageEffectsController _Controller)
    {
        m_Controller = _Controller;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (m_Controller == null || !m_Controller.AnyEnabled)
            return;

        CommandBuffer cmd = CommandBufferPool.Get();

        RenderTargetIdentifier cameraTarget = renderingData.cameraData.renderer.cameraColorTarget;

        using (new ProfilingScope(cmd, new ProfilingSampler("Image Effects")))
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            cmd.GetTemporaryRT(m_TempHandle1.id, renderingData.cameraData.cameraTargetDescriptor);
            cmd.GetTemporaryRT(m_TempHandle2.id, renderingData.cameraData.cameraTargetDescriptor);

            cmd.Blit(cameraTarget, Source);

            Displacement(cmd);
            BoxBlur(cmd, renderingData.cameraData.cameraTargetDescriptor);
            ChromaticAbberation(cmd);

            bool flipped = renderingData.cameraData.IsCameraProjectionMatrixFlipped();
            cmd.Blit(Source, cameraTarget, new Vector2(1, flipped ? 1 : -1), new Vector2(0, flipped ? 0 : 1));

            cmd.ReleaseTemporaryRT(m_TempHandle1.id);
            cmd.ReleaseTemporaryRT(m_TempHandle2.id);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    void Displacement(CommandBuffer _Cmd)
    {
        if (!m_Controller.DisplacementEnabled || m_DisplacementMaterial == null)
            return;

        _Cmd.SetGlobalTexture(m_DisplacementPropId, m_Controller.DisplacementTexture);
        _Cmd.SetGlobalFloat(m_MagnitudePropId, m_Controller.DisplacementMagnitude);
        _Cmd.Blit(Source, Destination, m_DisplacementMaterial);

        m_SwapTempTargets = !m_SwapTempTargets;
    }

    void BoxBlur(CommandBuffer _Cmd, RenderTextureDescriptor _Descriptor)
    {
        if (!m_Controller.BoxBlurEnabled || m_BoxBlurMaterial == null)
            return;

        if (Mathf.Approximately(m_Controller.BoxBlurDownscale, 1))
        {
            for (int i = 0; i < m_Controller.BoxBlurIterations; ++i)
            {
                _Cmd.Blit(Source, Destination, m_BoxBlurMaterial);
                m_SwapTempTargets = !m_SwapTempTargets;
            }
        }
        else
        {
            int width = (int)(_Descriptor.width * m_Controller.BoxBlurDownscale);
            int height = (int)(_Descriptor.height * m_Controller.BoxBlurDownscale);

            RenderTargetHandle targetHandle1 = new RenderTargetHandle();
            RenderTargetHandle targetHandle2 = new RenderTargetHandle();

            targetHandle1.Init("_BoxBlurTarget1");
            targetHandle2.Init("_BoxBlurTarget2");

            RenderTargetIdentifier target1 = new RenderTargetIdentifier(targetHandle1.id);
            RenderTargetIdentifier target2 = new RenderTargetIdentifier(targetHandle2.id);
            bool swap = false;

            _Cmd.GetTemporaryRT(targetHandle1.id, new RenderTextureDescriptor(width, height, _Descriptor.colorFormat, _Descriptor.depthBufferBits), FilterMode.Bilinear);
            _Cmd.Blit(Source, target1, m_BoxBlurMaterial);

            for (int i = 0; i < m_Controller.BoxBlurIterations; ++i)
            {
                width = Mathf.Max((int)(width * m_Controller.BoxBlurDownscale), 1);
                height = Mathf.Max((int)(height * m_Controller.BoxBlurDownscale), 1);

                _Cmd.GetTemporaryRT(swap ? targetHandle1.id : targetHandle2.id, new RenderTextureDescriptor(width, height, _Descriptor.colorFormat, _Descriptor.depthBufferBits), FilterMode.Bilinear);
                _Cmd.Blit(swap ? target2 : target1, swap ? target1 : target2, m_BoxBlurMaterial);
                _Cmd.ReleaseTemporaryRT(swap ? targetHandle2.id : targetHandle1.id);

                swap = !swap;
            }

            _Cmd.Blit(swap ? target2 : target1, Destination);
            _Cmd.ReleaseTemporaryRT(swap ? targetHandle2.id : targetHandle1.id);

            m_SwapTempTargets = !m_SwapTempTargets;
        }
    }

    void ChromaticAbberation(CommandBuffer _Cmd)
    {
        if (!m_Controller.ChromaticAbberationEnabled || m_ChromaticAbberationMaterial == null)
            return;

        _Cmd.SetGlobalVector(m_RedOffsetPropId, m_Controller.ChromaticAbberationRedOffset);
        _Cmd.SetGlobalVector(m_GreenOffsetPropId, m_Controller.ChromaticAbberationGreenOffset);
        _Cmd.SetGlobalVector(m_BlueOffsetPropId, m_Controller.ChromaticAbberationBlueOffset);
        _Cmd.Blit(Source, Destination, m_ChromaticAbberationMaterial);

        m_SwapTempTargets = !m_SwapTempTargets;
    }
}
