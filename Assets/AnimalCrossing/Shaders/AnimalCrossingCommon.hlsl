#ifndef ANIMAL_CROSSING_SLOPE_HLSL
#define ANIMAL_CROSSING_SLOPE_HLSL

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

/// Slope ///

uniform float _SlopeFactor;
uniform float _SlopeOffset;

float3 ApplySlope(float3 positionOS)
{
    #if ANIMAL_CROSSING_SLOPE
    float3 posWS = mul(UNITY_MATRIX_M, float4(positionOS, 1)).xyz;
    
    float dist = max(posWS.z - _WorldSpaceCameraPos.z - _SlopeOffset, 0);
    posWS.y -= dist * dist * _SlopeFactor;

    return mul(UNITY_MATRIX_I_M, float4(posWS, 1)).xyz;
    #else
    return positionOS;
    #endif
}

/// ---- ///


/// Water Caustics ///

uniform float4x4 _TopDownDepthVP;

#ifdef ANIMAL_CROSSING_WATER_CAUSTICS
TEXTURE2D(_WaterCausticsMask); SAMPLER(sampler_WaterCausticsMask);
TEXTURE2D(_WaterCausticsDistortion); SAMPLER(sampler_WaterCausticsDistortion);
TEXTURE2D(_WaterDepth); SAMPLER(sampler_WaterDepth);
uniform float4 _WaterCausticMask_ST;
uniform float4 _WaterCausticsDistortion_ST;
#endif

float SampleWaterCaustic(float3 posWS, float3 normalWS)
{
    #ifdef ANIMAL_CROSSING_WATER_CAUSTICS
    const float bias = 0.1;

    float NdotUp = saturate(dot(normalWS, float3(0, 1, 0)));
    float3 causticUV = mul(_TopDownDepthVP, float4(posWS, 1)).xyz;
    float depth = causticUV.z * 0.5 + 0.5 - bias;
    float waterDepth = SAMPLE_TEXTURE2D(_WaterDepth, sampler_WaterDepth, causticUV.xy).r;
    #if UNITY_UV_STARTS_AT_TOP
    waterDepth = 1 - waterDepth;
    #endif
    float2 distortion = SAMPLE_TEXTURE2D(_WaterCausticsDistortion, sampler_WaterCausticsDistortion, TRANSFORM_TEX(causticUV.xy, _WaterCausticsDistortion)).rg;

    causticUV.xy = TRANSFORM_TEX(causticUV.xy, _WaterCausticMask) + distortion;
    half caustics = SAMPLE_TEXTURE2D(_WaterCausticsMask, sampler_WaterCausticsMask, causticUV.xy + _Time.xx).r;

    return smoothstep(0.05, 0.15, depth - waterDepth) * caustics * NdotUp;
    #else
    return 0;
    #endif
}

/// ---- ///


/// Ripples ///

#ifdef ANIMAL_CROSSING_RAIN_RIPPLES
TEXTURE2D(_RippleNormalMap); SAMPLER(sampler_RippleNormalMap);
uniform float4 _VisibleArea; // xy - posWS, zw - 1/size

half4 packNormal(float2 normalXY)
{
    normalXY *= (256.0 * 256.0 - 1.0) / (256.0 * 256.0);
    half3 encodeX = frac(normalXY.x * half3(1.0, 256.0, 256.0 * 256.0));
    half3 encodeY = frac(normalXY.y * half3(1.0, 256.0, 256.0 * 256.0));
    return half4(encodeX.xy - encodeX.yz / 256.0 + 1.0 / 512.0,
                 encodeY.xy - encodeY.yz / 256.0 + 1.0 / 512.0);
}

float2 unpackNormal(half4 pack)
{
    float normalX = dot(pack.xy, half2(1.0, 1.0 / 256.0));
    float normalY = dot(pack.zw, half2(1.0, 1.0 / 256.0));
    return float2(normalX, normalY) * (256.0 * 256.0) / (256.0 * 256.0 - 1.0);
}

float3 reconstructNormal(float2 normalXY)
{
    float normalZ = sqrt(1 - normalXY.x * normalXY.x - normalXY.y - normalXY.y);
    return float3(normalXY, normalZ);
}
#endif

float3 getRippleNormal(float3 posWS)
{
    #ifdef ANIMAL_CROSSING_RAIN_RIPPLES
    float2 uv = (posWS.xz - _VisibleArea.xy) * _VisibleArea.zw;
    half4 pack = SAMPLE_TEXTURE2D(_RippleNormalMap, sampler_RippleNormalMap, uv);
    float2 normalXY = unpackNormal(pack) * 2 - 1;
    return reconstructNormal(normalXY);
    #else
    return float3(0, 0, 1);
    #endif
}

/// ---- ///

#endif