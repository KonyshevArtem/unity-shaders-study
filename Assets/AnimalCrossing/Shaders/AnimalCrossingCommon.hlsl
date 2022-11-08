#ifndef ANIMAL_CROSSING_SLOPE_HLSL
#define ANIMAL_CROSSING_SLOPE_HLSL

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

/// Visible Area UV ///

uniform float4 _VisibleArea; // xy - posWS, zw - 1/size
uniform float4 _VisibleAreaUV; // xy - offset, zw - scale

float2 GetVisibleAreaUVs(float2 uv)
{
    return _VisibleAreaUV.xy + uv * _VisibleAreaUV.zw;    
}

/// --- ///

/// Slope ///

uniform float4 _SlopeParams; // xyz - center, w - radius

struct SlopeConfig
{
    float3 slopedPosWS;
    float3 dirToRadius;
};

void ApplySlopeToNormal(float3 dirToRadius, inout float3 normalOS)
{
    float3x3 tbnFromInv = transpose(float3x3(float3(1, 0, 0), float3(0, 0, 1), float3(0, 1, 0)));
    float3x3 tbnTo = float3x3(float3(1, 0, 0), cross(float3(1, 0, 0), dirToRadius), dirToRadius);
    float3 normalWS = mul(UNITY_MATRIX_M, float4(normalOS, 0)).xyz;
    float3 normalTBN = mul(tbnFromInv, normalWS).xyz;
    normalWS = mul(tbnTo, normalTBN).xyz;
    normalOS = mul(UNITY_MATRIX_I_M, float4(normalWS, 0)).xyz;
}

SlopeConfig GetSlopeConfig(float3 positionWS)
{
    SlopeConfig config;
    config.slopedPosWS = float3(positionWS.x, 0, positionWS.z);
    float3 toCenter = _SlopeParams.xyz - config.slopedPosWS;
    toCenter.x = 0;
    config.dirToRadius = -normalize(toCenter);
    float3 toRadius = config.dirToRadius * _SlopeParams.w;
    config.slopedPosWS += toCenter + toRadius + config.dirToRadius * positionWS.y;
    return config;
}

void ApplySlope(inout float3 positionOS, inout float3 normalOS)
{
    #if ANIMAL_CROSSING_SLOPE
    float3 posWS = mul(UNITY_MATRIX_M, float4(positionOS, 1)).xyz;

    SlopeConfig config = GetSlopeConfig(posWS);
    positionOS = mul(UNITY_MATRIX_I_M, float4(config.slopedPosWS, 1)).xyz;

    ApplySlopeToNormal(config.dirToRadius, normalOS);
    #endif
}

void ApplySlopeWaterNormal(float3 positionWS, inout float3 normalOS)
{
    #if ANIMAL_CROSSING_SLOPE
    SlopeConfig config = GetSlopeConfig(positionWS);
    ApplySlopeToNormal(config.dirToRadius, normalOS);
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

    posWS.xz = _VisibleArea.xy + posWS.xz / _VisibleArea.zw;
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