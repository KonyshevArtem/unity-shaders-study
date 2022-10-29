Shader "Custom/Animal Crossing/Standard"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
            #pragma multi_compile _ ANIMAL_CROSSING_WATER_CAUSTICS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/AnimalCrossing/Shaders/AnimalCrossingCommon.hlsl"

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

                #ifdef ANIMAL_CROSSING_WATER_CAUSTICS
                float3 flatPositionWS : TEXCOORD2;
                #endif
            };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;

            Varyings vert (Attributes v)
            {
                #ifdef ANIMAL_CROSSING_WATER_CAUSTICS
                float3 flatPosWS = mul(UNITY_MATRIX_M, float4(v.positionOS.xyz, 1)).xyz;
                #endif

                v.positionOS.xyz = ApplySlope(v.positionOS.xyz);

                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS = mul(UNITY_MATRIX_M, float4(v.normalOS, 0)).xyz;
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);

                #ifdef ANIMAL_CROSSING_WATER_CAUSTICS
                o.flatPositionWS = flatPosWS;
                #endif

                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 ambient = half4(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w, 0);
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * dot(normalize(i.normalWS), _MainLightPosition.xyz) + ambient;

                #ifdef ANIMAL_CROSSING_WATER_CAUSTICS
                half caustics = SampleWaterCaustic(i.flatPositionWS, i.normalWS.xyz);
                color.rgb += half3(caustics, caustics, caustics);
                #endif

                return color;
            }
            ENDHLSL
        }
    }
}
