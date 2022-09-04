Shader "Custom/Animal Crossing/Standard"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _SlopeFactor ("Slope Factor", Float) = 0
        _SlopeOffset ("Slope Offset", Float) = 0
    }
    SubShader
    {
        Tags
        {
            "LightMode"="UniversalForward"
            "RenderType"="Opaque"
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ ANIMAL_CROSSING_SLOPE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/AnimalCrossing/Shaders/AnimalCrossingSlope.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 texcoord : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;

            Varyings vert (Attributes v)
            {
                v.positionOS.xyz = ApplySlope(v.positionOS.xyz);

                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS = mul(UNITY_MATRIX_M, float4(v.normalOS, 0));
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 ambient = half4(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w, 0);
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * dot(normalize(i.normalWS), _MainLightPosition.xyz) + ambient;
            }
            ENDHLSL
        }
    }
}
