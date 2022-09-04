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
            #include "Assets/AnimalCrossing/Shaders/AnimalCrossingSlope.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 flatPositionWS : TEXCOORD2;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD0;
            };

            uniform float _UVOffset;
            uniform half4 _DeepWaterColor;
            uniform half4 _ShallowWaterColor;
            uniform float _DeepWaterDepth;

            TEXTURE2D(_BigWavesNoise); SAMPLER(sampler_BigWavesNoise);
            uniform float4 _BigWavesNoise_ST;
            uniform float _BigWavesStrength;

            TEXTURE2D(_SmallWavesNoise); SAMPLER(sampler_SmallWavesNoise);
            uniform float4 _SmallWavesNoise_ST;
            uniform float _SmallWavesStrength;
            uniform float _SmallWavesSpeed;

            TEXTURE2D(_DepthMap); SAMPLER(sampler_DepthMap);
            uniform float4x4 _DepthMapVP;

            float3 NormalFromHeightMap(TEXTURE2D_PARAM(_HeightMap, sampler_HeightMap), float2 uv, float strength){
                const float k_NormalFactor = 5;
                float r = SAMPLE_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap, uv + float2(_UVOffset, 0), 0).r * strength;
                float l = SAMPLE_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap, uv - float2(_UVOffset, 0), 0).r * strength;
                float t = SAMPLE_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap, uv + float2(0, _UVOffset), 0).r * strength;
                float b = SAMPLE_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap, uv - float2(0, _UVOffset), 0).r * strength;
                return normalize(float3((l - r) * 0.5 * k_NormalFactor, 1, (b - t) * 0.5 * k_NormalFactor));
            }

            Varyings vert (Attributes v)
            {
                float3 slopedPositionOS = ApplySlope(v.positionOS.xyz);

                Varyings o = (Varyings) 0;

                float4 flatPosWS = mul(UNITY_MATRIX_M, float4(v.positionOS.xyz, 1));
                float4 slopedPosWS = mul(UNITY_MATRIX_M, float4(slopedPositionOS, 1));

                float2 bigWavesUV = TRANSFORM_TEX(v.texcoord, _BigWavesNoise) + _Time.xx;
                float2 smallWavesUV = TRANSFORM_TEX(v.texcoord, _SmallWavesNoise) + _Time.xx * _SmallWavesSpeed;

                float height = (SAMPLE_TEXTURE2D_LOD(_BigWavesNoise, sampler_BigWavesNoise, bigWavesUV, 0).r * 2.0 - 1.0) * _BigWavesStrength;
                height += (SAMPLE_TEXTURE2D_LOD(_SmallWavesNoise, sampler_SmallWavesNoise, smallWavesUV, 0).r * 2.0 - 1.0) * _SmallWavesStrength;
                flatPosWS.y += height;
                slopedPosWS.y += height;

                float3 normalOS = NormalFromHeightMap(TEXTURE2D_ARGS(_BigWavesNoise, sampler_BigWavesNoise), bigWavesUV, _BigWavesStrength);

                o.positionCS = mul(UNITY_MATRIX_VP, slopedPosWS);
                o.flatPositionWS = flatPosWS;
                o.normalWS = mul(UNITY_MATRIX_M, float4(normalOS, 0)).xyz;
                o.uv = v.texcoord;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float2 smallWavesUV = TRANSFORM_TEX(i.uv, _SmallWavesNoise) + _Time.xx * _SmallWavesSpeed;
                float3 normalOS = NormalFromHeightMap(TEXTURE2D_ARGS(_SmallWavesNoise, sampler_SmallWavesNoise), smallWavesUV, _SmallWavesStrength);
                i.normalWS += mul(UNITY_MATRIX_M, float4(normalOS, 0)).xyz;

                float4 depthMapCoord = mul(_DepthMapVP, float4(i.flatPositionWS.xyz, 1)) * 0.5 + 0.5;
                float terrainDepth = SAMPLE_TEXTURE2D(_DepthMap, sampler_DepthMap, depthMapCoord.xy).r;
                float waterDepth = 1 - depthMapCoord.z;
                float depthTerm = saturate(saturate(waterDepth - terrainDepth) / _DeepWaterDepth);

                half4 color = lerp(_ShallowWaterColor, _DeepWaterColor, depthTerm);
                half4 ambient = half4(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w, 0);
                return half4(color.rgb * dot(normalize(i.normalWS), _MainLightPosition.xyz), color.a) + ambient;
            }
            ENDHLSL
        }
    }
}
