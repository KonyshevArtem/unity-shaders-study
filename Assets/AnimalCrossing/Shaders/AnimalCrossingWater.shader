Shader "Custom/Animal Crossing/Water"
{
    Properties
    {
        _DeepWaterColor("Deep Water Color", Color) = (0, 0, 1)
        _ShallowWaterColor("Shallow Water Color", Color) = (0, 0.5, 0.5)
        _DeepWaterDepth("Deep Water Depth", Float) = 1

        _BigWavesNoise("Big Waves Noise", 2D) = "white"
        _BigWavesStrength("Big Waves Strength", Float) = 1

        _SmallWavesNoise("Small Waves Noise", 2D) = "white"
        _SmallWavesStrength("Small Waves Strength", Float) = 1
        _SmallWavesSpeed("Small Waves Speed", Float) = 1

        _SlopeFactor ("Slope Factor", Float) = 0
        _SlopeOffset ("Slope Offset", Float) = 0

        _SpecularHardness("Specular Hardness", Float) = 30
    }
    SubShader
    {
        Tags
        {
            "LightMode" = "UniversalForward"
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ ANIMAL_CROSSING_SLOPE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/AnimalCrossing/Shaders/AnimalCrossingCommon.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 flatPositionWS : TEXCOORD2;
                float4 uv : TEXCOORD0;
            };

            uniform float _UVOffset;
            uniform half4 _DeepWaterColor;
            uniform half4 _ShallowWaterColor;
            uniform float _DeepWaterDepth;
            uniform float _SpecularHardness;

            TEXTURE2D(_BigWavesNoise); SAMPLER(sampler_BigWavesNoise);
            uniform float4 _BigWavesNoise_ST;
            uniform float _BigWavesStrength;

            TEXTURE2D(_SmallWavesNoise); SAMPLER(sampler_SmallWavesNoise);
            uniform float4 _SmallWavesNoise_ST;
            uniform float _SmallWavesStrength;
            uniform float _SmallWavesSpeed;

            TEXTURE2D(_DepthMap); SAMPLER(sampler_DepthMap);
            uniform float4x4 _DepthMapVP;

            float sampleHeight(float4 uv)
            {
                float height = (SAMPLE_TEXTURE2D_LOD(_BigWavesNoise, sampler_BigWavesNoise, uv.xy, 0).r * 2.0 - 1.0) * _BigWavesStrength;
                height += (SAMPLE_TEXTURE2D_LOD(_SmallWavesNoise, sampler_SmallWavesNoise, uv.zw, 0).r * 2.0 - 1.0) * _SmallWavesStrength;
                return height;
            }

            float3 normalFromHeight(float4 uv, float depthTerm)
            {
                float heightFactor = max(0.1, depthTerm);
                float r = sampleHeight(uv + float4(_UVOffset, 0, _UVOffset, 0)) * heightFactor;
                float l = sampleHeight(uv - float4(_UVOffset, 0, _UVOffset, 0)) * heightFactor;
                float t = sampleHeight(uv + float4(0, _UVOffset, 0, _UVOffset)) * heightFactor;
                float b = sampleHeight(uv - float4(0, _UVOffset, 0, _UVOffset)) * heightFactor;
                return normalize(float3(l - r, 1, b - t));
            }

            float getDepthTerm(float3 posWS)
            {
                float4 depthMapCoord = mul(_DepthMapVP, float4(posWS, 1)) * 0.5 + 0.5;
                float terrainDepth = SAMPLE_TEXTURE2D_LOD(_DepthMap, sampler_DepthMap, depthMapCoord.xy, 0).r;
                float waterDepth = 1 - depthMapCoord.z;
                return saturate(saturate(waterDepth - terrainDepth) / _DeepWaterDepth);
            }

            Varyings vert (Attributes v)
            {
                float3 slopedPositionOS = ApplySlope(v.positionOS.xyz);

                Varyings o = (Varyings) 0;

                float4 flatPosWS = mul(UNITY_MATRIX_M, float4(v.positionOS.xyz, 1));
                float4 slopedPosWS = mul(UNITY_MATRIX_M, float4(slopedPositionOS, 1));

                float2 bigWavesUV = TRANSFORM_TEX(v.texcoord, _BigWavesNoise) + _Time.xx;
                float2 smallWavesUV = TRANSFORM_TEX(v.texcoord, _SmallWavesNoise) + _Time.xx * _SmallWavesSpeed;
                float4 uv = float4(bigWavesUV, smallWavesUV);

                float height = sampleHeight(uv);
                height *= max(0.1, getDepthTerm(flatPosWS.xyz));

                flatPosWS.y += height;
                slopedPosWS.y += height;

                o.positionCS = mul(UNITY_MATRIX_VP, slopedPosWS);
                o.flatPositionWS = flatPosWS;
                o.uv = uv;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float depthTerm = getDepthTerm(i.flatPositionWS.xyz);
                float3 normalOS = normalFromHeight(i.uv, depthTerm);
                float3 normalWS = mul(UNITY_MATRIX_M, float4(normalOS, 0)).xyz;

                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(i.flatPositionWS.xyz);
                float3 H = normalize(viewDirWS + _MainLightPosition.xyz);
		        float NdotH = saturate(dot(normalWS, H));
		        float specularIntensity = pow(NdotH, _SpecularHardness);
                half4 specular = half4(specularIntensity, specularIntensity, specularIntensity, 0);

                half4 color = lerp(_ShallowWaterColor, _DeepWaterColor, depthTerm);
                half4 ambient = half4(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w, 0);

                float NdotL = saturate(dot(normalize(normalWS), _MainLightPosition.xyz));
                return half4(color.rgb * NdotL, color.a) + ambient + specular * NdotL;
            }
            ENDHLSL
        }
    }
}
