Shader "Hidden/VolumetricEffects/Compose"
{
    Properties
    {
        _MainTex("Main Tex", 2D) = "black"    
    }
    SubShader
    {
        ZTest Always
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Compose"
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex FullscreenVert
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Fullscreen.hlsl"

            TEXTURE2D_HALF(_MainTex);
            SAMPLER(sampler_MainTex);

            half4 Fragment(Varyings input) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "Clear"

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

        Pass
        {
            Name "Blur Horizontal"

            HLSLPROGRAM
            #pragma vertex FullscreenVert
            #pragma fragment Fragment

            #define HORIZONTAL
            #include "GaussianBlur.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Blur Vertical"

            HLSLPROGRAM
            #pragma vertex FullscreenVert
            #pragma fragment Fragment

            #define VERTICAL
            #include "GaussianBlur.hlsl"
            ENDHLSL
        }
    }
}
