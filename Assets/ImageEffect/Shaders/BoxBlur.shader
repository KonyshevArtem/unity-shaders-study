Shader "Hidden/ImageEffects/BoxBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "EffectsCommon.hlsl"

            half4 boxBlur(float2 uv) 
            {
                float x = _MainTex_TexelSize.x;
                float y = _MainTex_TexelSize.y;
                half4 sum = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) + 
                            SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(uv.x - x, uv.y - y)) + 
                            SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(uv.x, uv.y - y)) + 
                            SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(uv.x + x, uv.y - y)) + 
                            SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(uv.x - x, uv.y)) + 
                            SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(uv.x + x, uv.y)) + 
                            SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(uv.x - x, uv.y + y)) + 
                            SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(uv.x, uv.y + y)) + 
                            SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(uv.x + x, uv.y + y));
                return sum / 9;
            }

            half4 frag (Varyings i) : SV_Target
            {
                return boxBlur(i.uv);
            }
            ENDHLSL
        }
    }
}
