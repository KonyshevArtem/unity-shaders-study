#ifndef BACKFACE_OUTLINE_PASS_HLSL
#define BACKFACE_OUTLINE_PASS_HLSL

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

uniform float _OutlineStrength;
uniform half4 _OutlineColor;

struct Attributes
{
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
};

struct Varyings
{
    float4 positionCS : SV_Position;
};

Varyings vert(Attributes i)
{
    Varyings output;
    float3 posWS = mul(UNITY_MATRIX_M, float4(i.positionOS.xyz, 1)).xyz;
    float3 normalWS = normalize(mul(i.normalOS, (float3x3) UNITY_MATRIX_M));
    posWS += normalWS * _OutlineStrength;
    
    output.positionCS = mul(UNITY_MATRIX_VP, float4(posWS, 1));
    return output;
}

half4 frag(Varyings v) : SV_Target
{
    return _OutlineColor;
}

#endif