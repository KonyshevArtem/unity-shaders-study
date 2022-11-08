Shader "Custom/Animal Crossing/Water"
{
    Properties
    {
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 1

        _DeepWaterColor("Deep Water Color", Color) = (0, 0, 1)
        _ShallowWaterColor("Shallow Water Color", Color) = (0, 0.5, 0.5)
        _DeepWaterDepth("Deep Water Depth", Float) = 1

        _BigWavesNoise("Big Waves Noise", 2D) = "white"
        _BigWavesStrength("Big Waves Strength", Float) = 1

        _SmallWavesNoise("Small Waves Noise", 2D) = "white"
        _SmallWavesStrength("Small Waves Strength", Float) = 1
        _SmallWavesSpeed("Small Waves Speed", Float) = 1

        _Foam("Foam", 2D) = "white"

        _UnderwaterDistortionStrength("Underwater Distortion Strength", Float) = 1
    }

HLSLINCLUDE

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Assets/AnimalCrossing/Shaders/AnimalCrossingCommon.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    float2 texcoord : TEXCOORD0;
};

uniform float _DeepWaterDepth;

TEXTURE2D(_BigWavesNoise); SAMPLER(sampler_BigWavesNoise);
uniform float4 _BigWavesNoise_ST;
uniform float _BigWavesStrength;

TEXTURE2D(_SmallWavesNoise); SAMPLER(sampler_SmallWavesNoise);
uniform float4 _SmallWavesNoise_ST;
uniform float _SmallWavesStrength;
uniform float _SmallWavesSpeed;

TEXTURE2D(_DepthMap); SAMPLER(sampler_DepthMap);

float sampleHeight(float4 uv)
{
    float height = (SAMPLE_TEXTURE2D_LOD(_BigWavesNoise, sampler_BigWavesNoise, uv.xy, 0).r * 2.0 - 1.0) * _BigWavesStrength;
    height += (SAMPLE_TEXTURE2D_LOD(_SmallWavesNoise, sampler_SmallWavesNoise, uv.zw, 0).r * 2.0 - 1.0) * _SmallWavesStrength;
    return height;
}

float getDepthTerm(float3 posWS)
{
    float4 depthMapCoord = mul(_TopDownDepthVP, float4(posWS, 1)) * 0.5 + 0.5;
    float terrainDepth = SAMPLE_TEXTURE2D_LOD(_DepthMap, sampler_DepthMap, depthMapCoord.xy, 0).r;
    #if UNITY_UV_STARTS_AT_TOP
    float depthDiff = 1 - depthMapCoord.z - terrainDepth;
    #else
    float depthDiff = terrainDepth - depthMapCoord.z;
    #endif
    return saturate(saturate(depthDiff) / _DeepWaterDepth);
}

ENDHLSL

    SubShader
    {
        Tags
        {
            "LightMode" = "UniversalForward"
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }
        Cull Back

        Pass
        {
            Name "ForwardLit"
    
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ ANIMAL_CROSSING_SLOPE
            #pragma multi_compile _ ANIMAL_CROSSING_RAIN_RIPPLES

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD3;
                float3 flatPositionWS : TEXCOORD2;
                float4 wavesUV: TEXCOORD1;
                float2 uv : TEXCOORD0;

                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 4);
            };

            uniform float _UVOffset;
            uniform half4 _DeepWaterColor;
            uniform half4 _ShallowWaterColor;
            uniform float _Smoothness;

            TEXTURE2D(_Foam); SAMPLER(sampler_Foam);
            uniform float4 _Foam_ST;

            TEXTURE2D(_CameraColorCopy); SAMPLER(sampler_CameraColorCopy);

            uniform float _UnderwaterDistortionStrength;

            float3 normalFromHeight(float4 uv, float depthTerm)
            {
                float heightFactor = max(0.1, depthTerm);
                float r = sampleHeight(uv + float4(_UVOffset, 0, _UVOffset, 0)) * heightFactor;
                float l = sampleHeight(uv - float4(_UVOffset, 0, _UVOffset, 0)) * heightFactor;
                float t = sampleHeight(uv + float4(0, _UVOffset, 0, _UVOffset)) * heightFactor;
                float b = sampleHeight(uv - float4(0, _UVOffset, 0, _UVOffset)) * heightFactor;
                return normalize(float3(l - r, 1, b - t));
            }

            Varyings vert (Attributes v)
            {
                float3 slopedPositionOS = v.positionOS.xyz;
                float3 normalOS = float3(0, 0, 0);
                ApplySlope(slopedPositionOS, normalOS);

                Varyings o = (Varyings) 0;

                float3 flatPosWS = mul(UNITY_MATRIX_M, float4(v.positionOS.xyz, 1)).xyz;
                float3 slopedPosWS = mul(UNITY_MATRIX_M, float4(slopedPositionOS, 1)).xyz;

                v.texcoord = GetVisibleAreaUVs(v.texcoord);
                float2 bigWavesUV = TRANSFORM_TEX(v.texcoord, _BigWavesNoise) + _Time.xx;
                float2 smallWavesUV = TRANSFORM_TEX(v.texcoord, _SmallWavesNoise) + _Time.xx * _SmallWavesSpeed;
                float4 wavesUV = float4(bigWavesUV, smallWavesUV);

                float height = sampleHeight(wavesUV);
                height *= max(0.1, getDepthTerm(flatPosWS));

                flatPosWS.y += height;
                slopedPosWS.y += height;

                o.positionCS = mul(UNITY_MATRIX_VP, float4(slopedPosWS, 1));
                o.positionWS = slopedPosWS;
                o.flatPositionWS = flatPosWS;
                o.wavesUV = wavesUV;
                o.uv = v.texcoord;

                OUTPUT_SH(float3(0, 1, 0), o.vertexSH);

                return o;
            }

            void InitializeInputData(Varyings input, float3 normalWS, out InputData inputData)
            {
                inputData = (InputData)0;
                inputData.normalWS = NormalizeNormalPerPixel(normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

            #if defined(DYNAMICLIGHTMAP_ON)
                inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV, input.vertexSH, inputData.normalWS);
            #else
                inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
            #endif
            }

            half4 frag (Varyings i) : SV_Target
            {
                // calc normal from height map
                float depthTerm = getDepthTerm(i.flatPositionWS);
                float3 normalOS = normalFromHeight(i.wavesUV, depthTerm);
                ApplySlopeWaterNormal(i.flatPositionWS, normalOS);
                float3 tangentOS = normalize(cross(normalOS, float3(0, 0, 1)));
                float3 bitangentOS = normalize(cross(tangentOS, normalOS));
                float3x3 tbn = float3x3(tangentOS, bitangentOS, normalOS);

                float3 rippleNormalTS = getRippleNormal(i.flatPositionWS);
                normalOS = normalize(mul(tbn, rippleNormalTS));

                float3 normalWS = normalize(mul(UNITY_MATRIX_M, float4(normalOS, 0)).xyz);
                float3 normalCS = normalize(mul(UNITY_MATRIX_VP, float4(normalWS, 0)).xyz);

                // water color
                half4 color = lerp(_ShallowWaterColor, _DeepWaterColor, depthTerm);

                // waves foam
                float wavesFoamHeight = SAMPLE_TEXTURE2D_LOD(_SmallWavesNoise, sampler_SmallWavesNoise, i.wavesUV.zw, 0).r;
                wavesFoamHeight *= SAMPLE_TEXTURE2D_LOD(_BigWavesNoise, sampler_BigWavesNoise, i.wavesUV.xy, 0).r;
                wavesFoamHeight *= depthTerm;
                half4 wavesFoamColor = SAMPLE_TEXTURE2D(_Foam, sampler_Foam, TRANSFORM_TEX(i.wavesUV.zw, _Foam));
                color = lerp(color, wavesFoamColor, smoothstep(0.2, 0.5, wavesFoamHeight));

                // shore foam
                half4 shoreFoamColor = SAMPLE_TEXTURE2D(_Foam, sampler_Foam, TRANSFORM_TEX(i.uv, _Foam));
                float shoreFoamOffset = (_SinTime.w * 0.5 + 0.5) * 0.2;
                color = lerp(color, shoreFoamColor, 1 - smoothstep(0.1, 0.3 + shoreFoamOffset, depthTerm));
                color = lerp(color, half4(.8, .8, .8, 1), 1 - smoothstep(0, 0.2, depthTerm));

                // input data
                InputData inputData;
                InitializeInputData(i, normalWS, inputData);

                // underwater
                float height = sampleHeight(i.wavesUV) * depthTerm;
                float2 screenUV = inputData.normalizedScreenSpaceUV;
                screenUV += normalCS.xy * height * _UnderwaterDistortionStrength;
                half3 underwaterColor = SAMPLE_TEXTURE2D(_CameraColorCopy, sampler_CameraColorCopy, screenUV).rgb;
                color.rgb = lerp(underwaterColor, color.rgb, color.a);

                // final color
                color = UniversalFragmentPBR(inputData, color.rgb, 0, /* specular */ half3(0.0h, 0.0h, 0.0h), _Smoothness, 1, /* emission */ half3(0, 0, 0), 1);
                return half4(color.rgb, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "TopDownDepth"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ ANIMAL_CROSSING_SLOPE

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert (Attributes v)
            {
                Varyings o = (Varyings) 0;

                float4 flatPosWS = mul(UNITY_MATRIX_M, float4(v.positionOS.xyz, 1));

                v.texcoord = GetVisibleAreaUVs(v.texcoord);
                float2 bigWavesUV = TRANSFORM_TEX(v.texcoord, _BigWavesNoise) + _Time.xx;
                float2 smallWavesUV = TRANSFORM_TEX(v.texcoord, _SmallWavesNoise) + _Time.xx * _SmallWavesSpeed;
                float4 wavesUV = float4(bigWavesUV, smallWavesUV);

                float height = sampleHeight(wavesUV);
                height *= getDepthTerm(flatPosWS.xyz);

                flatPosWS.y += height;

                o.positionCS = mul(UNITY_MATRIX_VP, flatPosWS);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float a = i.positionCS.z;
                return half4(a, a, a, 1);
            }
            ENDHLSL
        }
    }
}
