Shader "Hidden/ImageEffects/ChromaticAberation"
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
            
            uniform float2 _RedOffset;
            uniform float2 _GreenOffset;
            uniform float2 _BlueOffset;

            half4 frag (Varyings i) : SV_Target
            {
                float r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(_RedOffset.x * _MainTex_TexelSize.x, _RedOffset.y * _MainTex_TexelSize.y)).x;
                float g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(_GreenOffset.x * _MainTex_TexelSize.x, _GreenOffset.y * _MainTex_TexelSize.y)).y;
                float b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(_BlueOffset.x * _MainTex_TexelSize.x, _BlueOffset.y * _MainTex_TexelSize.y)).z;
                
                return half4(r, g, b, 1);
            }
            ENDHLSL
        }
    }
}
