#ifndef ANIMAL_CROSSING_SLOPE_HLSL
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

#endif