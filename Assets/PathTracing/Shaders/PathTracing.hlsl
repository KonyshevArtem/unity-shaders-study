#ifndef PATH_TRACING_HLSL
#define PATH_TRACING_HLSL

#define MAX_DEPTH 3

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct Attributes
{
    float3 positionOS : POSITION;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 viewDir : TEXCOORD0;
};

struct Object
{
    float4 posWS;
    float4 color;
    float smoothness;
};

struct HitInfo
{
    float4 positionSmoothness;
    half4 colorAttenuation;
};

uniform StructuredBuffer<Object> _Objects;
uniform uint _ObjectsCount;

TEXTURECUBE(_Skybox);
SAMPLER(sampler_Skybox);

Varyings vert(Attributes input)
{
    Varyings output;
    output.positionCS = TransformObjectToHClip(input.positionOS);

    float3 posWS = TransformObjectToWorld(input.positionOS);
    output.viewDir = GetWorldSpaceViewDir(posWS);

    return output;
}

bool SphereIntersection(float3 pos, float3 dir, float4 sphere, out float3 position, out float distance)
{
    position = (float3)0;
    distance = 0;

    float3 sphereToCam = sphere.xyz - pos;
    float dirDotVector = dot(dir, sphereToCam);
    float det = dirDotVector * dirDotVector - (dot(sphereToCam, sphereToCam) - sphere.w * sphere.w);
    if (det < 0)
    {
        return false;
    }

    float sqrtDet = sqrt(det);
    distance = min(dirDotVector + sqrtDet, dirDotVector - sqrtDet);
    if (distance < 0)
    {
        return false;
    }

    position = pos + dir * distance;
    return true;
}

half4 SampleSkybox(float3 dir)
{
    return SAMPLE_TEXTURECUBE_LOD(_Skybox, sampler_Skybox, dir, 0);
}

half4 TraceRay(float3 rayStartPoint, float3 rayDir, int currentDepth)
{
    HitInfo hits[MAX_DEPTH];

    for (uint i = 0; i < MAX_DEPTH; i++)
    {
        int closestObject = -1;
        float3 closestObjectPos = 0;
        float closestObjectDistance = 0;
        for (uint j = 0; j < _ObjectsCount; j++)
        {
            Object object = _Objects[j];

            float3 position;
            float distance;
            if (SphereIntersection(rayStartPoint, rayDir, object.posWS, position, distance))
            {
                if (closestObject < 0 || distance < closestObjectDistance)
                {
                    closestObject = j;
                    closestObjectPos = position;
                    closestObjectDistance = distance;
                }
            }
        }

        if (closestObject >= 0)
        {
            Object object = _Objects[closestObject];

            float3 normal = normalize(closestObjectPos - object.posWS.xyz);
            float attenuation = saturate(dot(_MainLightPosition.xyz, normal));

            hits[i].positionSmoothness = float4(closestObjectPos, object.smoothness);
            hits[i].colorAttenuation = half4(object.color.rgb, attenuation);

            rayStartPoint = closestObjectPos;
            rayDir = reflect(rayDir, normal);
        }
        else
        {
            hits[i].positionSmoothness = 0;
            hits[i].colorAttenuation = half4(SampleSkybox(rayDir).rgb, 1);
            break;
        }
    }

    half3 finalColor = 0;
    for (int i = MAX_DEPTH - 1; i >= 0; i--)
    {
        float smoothness = hits[i].positionSmoothness.a;
        half4 hitColorAttenuation = hits[i].colorAttenuation;
        finalColor = lerp(finalColor, hitColorAttenuation.rgb, 1 - smoothness) * hitColorAttenuation.a;
    }

    return half4(finalColor, 1);
}

half4 frag(Varyings input) : SV_Target
{
    float3 viewDir = -normalize(input.viewDir);

    return TraceRay(_WorldSpaceCameraPos, viewDir, 1);
}

#endif
