Shader "Hidden/ImageEffects/DisplacementEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
    
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "EffectsCommon.hlsl"

            TEXTURE2D(_Displacement); SAMPLER(sampler_Displacement);
            uniform float _Magnitude;

            half4 frag (Varyings i) : SV_Target
            {
                float2 displacement = SAMPLE_TEXTURE2D(_Displacement, sampler_Displacement, i.uv + _Time) * 2 - 1;
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + displacement * _Magnitude);
            }
            ENDHLSL
        }
    }
}
