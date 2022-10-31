Shader "Custom/Animal Crossing/Standard"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Smoothness ("Smoothness", Range(0.0, 1.0)) = 1
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
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
                float3 positionWS : TEXCOORD2;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD0;

                #ifdef ANIMAL_CROSSING_WATER_CAUSTICS
                float3 flatPositionWS : TEXCOORD3;
                #endif

                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 4);
            };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float _Smoothness;

            Varyings vert (Attributes v)
            {
                #ifdef ANIMAL_CROSSING_WATER_CAUSTICS
                float3 flatPosWS = mul(UNITY_MATRIX_M, float4(v.positionOS.xyz, 1)).xyz;
                #endif

                ApplySlope(v.positionOS.xyz, v.normalOS);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS.xyz);

                Varyings o;
                o.positionCS = vertexInput.positionCS;
                o.positionWS = vertexInput.positionWS;
                o.normalWS = mul(UNITY_MATRIX_M, float4(v.normalOS, 0)).xyz;
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);

                #ifdef ANIMAL_CROSSING_WATER_CAUSTICS
                o.flatPositionWS = flatPosWS;
                #endif

                OUTPUT_SH(o.normalWS.xyz, o.vertexSH);

                return o;
            }

            void InitializeInputData(Varyings input, out InputData inputData)
            {
                inputData = (InputData)0;           
                inputData.normalWS = NormalizeNormalPerPixel(input.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
            
            #if defined(DYNAMICLIGHTMAP_ON)
                inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV, input.vertexSH, inputData.normalWS);
            #else
                inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
            #endif
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * dot(normalize(i.normalWS), _MainLightPosition.xyz);

                InputData inputData;
                InitializeInputData(i, inputData);
                half4 color = UniversalFragmentPBR(inputData, albedo.rgb, 0, /* specular */ half3(0.0h, 0.0h, 0.0h), _Smoothness, 1, /* emission */ half3(0, 0, 0), 1);

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
