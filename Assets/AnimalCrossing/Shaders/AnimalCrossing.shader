Shader "Custom/AnimalCrossing"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _SlopeFactor ("Slope Factor", Float) = 0
        _SlopeOffset ("Slope Offset", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ ANIMAL_CROSSING_SLOPE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;

            uniform float _SlopeFactor;
            uniform float _SlopeOffset;

            Varyings vert (Attributes v)
            {
                Varyings o;
                float3 posWS = mul(UNITY_MATRIX_M, float4(v.positionOS.xyz, 1));

                #if ANIMAL_CROSSING_SLOPE
                float dist = max(posWS.z - _WorldSpaceCameraPos.z - _SlopeOffset, 0);
                posWS.y -= dist * dist * _SlopeFactor;
                #endif

                o.positionCS = mul(UNITY_MATRIX_VP, float4(posWS.xyz, 1));
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
            }
            ENDHLSL
        }
    }
}
