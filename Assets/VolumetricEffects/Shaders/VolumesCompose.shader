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

            TEXTURE2D_HALF(_VolumesRT);
            SAMPLER(sampler_VolumesRT);

            half4 Fragment(Varyings input) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_VolumesRT, sampler_VolumesRT, input.uv);
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "Clear"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex FullscreenVert
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Fullscreen.hlsl"

            struct FragmentOutput
            {
                half4 color : SV_Target0;
                float2 depthMinMax : SV_Target1;
            };
            
            FragmentOutput Fragment(Varyings input) : SV_Target
            {
                return (FragmentOutput) 0;
            }
            ENDHLSL
        }
    }
}
