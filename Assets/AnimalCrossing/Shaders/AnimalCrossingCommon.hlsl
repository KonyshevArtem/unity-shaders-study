﻿#ifndef ANIMAL_CROSSING_SLOPE_HLSL
#define ANIMAL_CROSSING_SLOPE_HLSL

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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

TEXTURE2D(_WaterCausticsMask); SAMPLER(sampler_WaterCausticsMask);
TEXTURE2D(_WaterCausticsDistortion); SAMPLER(sampler_WaterCausticsDistortion);
TEXTURE2D(_WaterDepth); SAMPLER(sampler_WaterDepth);
uniform float4 _WaterCausticMask_ST;
uniform float4 _WaterCausticsDistortion_ST;
uniform float4x4 _TopDownDepthVP;

float SampleWaterCaustic(float3 posWS, float3 normalWS)
{
    #ifdef ANIMAL_CROSSING_WATER_CAUSTICS
    const float bias = 0.1;

    float NdotUp = saturate(dot(normalWS, float3(0, 1, 0)));
    float3 causticUV = mul(_TopDownDepthVP, float4(posWS, 1)).xyz;
    float depth = causticUV.z * 0.5 + 0.5 - bias;
    float waterDepth = 1 - SAMPLE_TEXTURE2D(_WaterDepth, sampler_WaterDepth, causticUV.xy).r;
    float2 distortion = SAMPLE_TEXTURE2D(_WaterCausticsDistortion, sampler_WaterCausticsDistortion, TRANSFORM_TEX(causticUV.xy, _WaterCausticsDistortion)).rg;

    causticUV.xy = TRANSFORM_TEX(causticUV.xy, _WaterCausticMask) + distortion;
    half caustics = SAMPLE_TEXTURE2D(_WaterCausticsMask, sampler_WaterCausticsMask, causticUV.xy + _Time.xx).r;

    return smoothstep(0.05, 0.15, depth - waterDepth) * caustics * NdotUp;
    #else
    return 0;
    #endif
}

#endif