Shader "Hidden/VolumetricEffects/Compose"
{
    SubShader
    {
        Pass
        {
            Name "Compose"
            ZTest Always
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex FullscreenVert
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Fullscreen.hlsl"

            TEXTURE2D_X(_VolumesRT);
            SAMPLER(sampler_VolumesRT);

            half4 Fragment(Varyings input) : SV_Target
            {
                return SAMPLE_TEXTURE2D_X(_VolumesRT, sampler_VolumesRT, input.uv);
            }
            ENDHLSL
        }
    }
}
