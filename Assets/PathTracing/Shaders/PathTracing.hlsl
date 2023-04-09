#ifndef PATH_TRACING_HLSL
#define PATH_TRACING_HLSL

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
    float emission;
};

struct Hit
{
    int ObjectIndex;
    float3 Position;
    float3 Normal;
};

uniform StructuredBuffer<Object> _Objects;
uniform uint _ObjectsCount;

uniform uint _MaxBounces;
uniform uint _MaxIterations;
uniform float2 _SkyboxRotationSinCos;

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
    float3 sphereToCam = sphere.xyz - pos;
    float dirDotVector = dot(dir, sphereToCam);
    float radius = sphere.w * 0.5f;
    float det = dirDotVector * dirDotVector - (dot(sphereToCam, sphereToCam) - radius * radius);
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
    float2x2 rotMatrix = float2x2(_SkyboxRotationSinCos.y, _SkyboxRotationSinCos.x, -_SkyboxRotationSinCos.x, _SkyboxRotationSinCos.y);
    dir = float3(mul(rotMatrix, dir.xz).xy, dir.y).xzy;
    return SAMPLE_TEXTURECUBE_LOD(_Skybox, sampler_Skybox, dir, 0);
}

float Random(half2 st)
{
    return frac(sin(dot(st.xy, half2(12.9898, 78.233))) * 43758.5453123) * 2 - 1;
}

bool GetClosestObjectHit(float3 rayStartPoint, float3 rayDir, out Hit hit)
{
    hit.ObjectIndex = -1;
    hit.Position = 0;

    float closestObjectDistance = 0;
    for (uint j = 0; j < _ObjectsCount; j++)
    {
        Object object = _Objects[j];

        float3 position;
        float distance;
        if (SphereIntersection(rayStartPoint, rayDir, object.posWS, position, distance))
        {
            if (hit.ObjectIndex < 0 || distance < closestObjectDistance)
            {
                hit.ObjectIndex = j;
                hit.Position = position;
                closestObjectDistance = distance;
            }
        }
    }
    
    if (hit.ObjectIndex >= 0)
    {
        hit.Normal = normalize(hit.Position - _Objects[hit.ObjectIndex].posWS);
        return true;
    }

    return false; 
}

float3 Trace(float3 rayStartPoint, float3 rayDir, float2 randomSeed)
{
    half3 color = 1;
    float3 light = 0;

    Hit hit;
    for (uint i = 0; i < _MaxBounces + 1; i++)
    {
        if (GetClosestObjectHit(rayStartPoint, rayDir, hit))
        {
            Object object = _Objects[hit.ObjectIndex];

            float x = Random(randomSeed + i);
            float y = Random(randomSeed + x * 35674 + i);
            float z = Random(randomSeed + y * 224546 + i);
            float3 diffuseDir = normalize(float3(x, y, z));
            diffuseDir *= dot(diffuseDir, hit.Normal) < 0 ? -1 : 1;

            rayDir = lerp(diffuseDir, reflect(rayDir, hit.Normal), object.smoothness);
            rayStartPoint = hit.Position;

            light += object.emission * color * dot(hit.Normal, rayDir);
            color *= object.color;
        }
        else
        {
            light += SampleSkybox(rayDir).rgb * color;
            break;
        }
    }

    return light;
}

half4 frag(Varyings input) : SV_Target
{
    float3 rayStartPoint = _WorldSpaceCameraPos;
    float3 rayDir = -normalize(input.viewDir);
    float2 randomSeed = input.positionCS + _Time.xx;

    float3 light = 0;
    for (uint i = 0; i < _MaxIterations + 1; ++i)
    {
        light += Trace(rayStartPoint, rayDir, randomSeed * (i + 1));
    }
    light /= _MaxIterations;

    return half4(light, 1);
}

#endif
