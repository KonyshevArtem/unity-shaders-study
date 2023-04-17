Shader "Custom/Path Tracing/Blend"
{
    Properties 
    {
        _MainTex("_MainTex", 2D) = "white"
    }
    SubShader
    {
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            uniform float _FrameCount;

            half4 frag(Varyings input) : SV_Target
            {
                half3 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).rgb;
                return half4(c, 1 / max(_FrameCount, 1));
            }
            ENDHLSL
        }
    }
}