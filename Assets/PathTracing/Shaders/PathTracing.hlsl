#ifndef PATH_TRACING_HLSL
#define PATH_TRACING_HLSL

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Assets/PathTracing/Shaders/Structs.hlsl"
#include "Assets/PathTracing/Shaders/Random.hlsl"
#include "Assets/PathTracing/Shaders/Intersections.hlsl"
#include "Assets/PathTracing/Shaders/CustomSkybox.hlsl"

#ifdef _NO_INDICES
uniform StructuredBuffer<Triangle> _Vertices;
#else
uniform StructuredBuffer<float3> _Vertices;
uniform StructuredBuffer<Triangle> _Indices;
#endif

uniform StructuredBuffer<Sphere> _Spheres;
uniform StructuredBuffer<TriangleMesh> _TriangleMeshes;
uniform uint _SpheresCount;
uniform uint _TriangleMeshesCount;

uniform uint _MaxBounces;
uniform uint _MaxIterations;

Varyings vert(Attributes input)
{
    Varyings output;
    output.positionCS = TransformObjectToHClip(input.positionOS);

    float3 posWS = TransformObjectToWorld(input.positionOS);
    output.viewDir = GetWorldSpaceViewDir(posWS);
    output.uv = input.uv;

    return output;
}

bool GetClosestTriangleHit(float3 rayStartPoint, float3 rayDir, uint trianglesBegin, uint trianglesEnd, float4x4 modelMatrix, out float3 position, out float3 normal, out float distance)
{
    float3 outPosition;
    float3 outNormal;
    float outDistance;

    bool hasIntersection = false;
    float closestTriangleDistance = 0;
    for (uint j = trianglesBegin; j < trianglesEnd; ++j)
    {
        #ifdef _NO_INDICES
        Triangle vertices = _Vertices[j];
        float3 v1 = vertices.vertex1;
        float3 v2 = vertices.vertex2;
        float3 v3 = vertices.vertex3;
        #else
        Triangle indices = _Indices[j];
        float3 v1 = _Vertices[indices.index1];
        float3 v2 = _Vertices[indices.index2];
        float3 v3 = _Vertices[indices.index3];
        #endif
        
        #ifndef _MATRICES_PRE_APPLIED
        v1 = mul(modelMatrix, float4(v1, 1)).xyz;
        v2 = mul(modelMatrix, float4(v2, 1)).xyz;
        v3 = mul(modelMatrix, float4(v3, 1)).xyz;
        #endif
        
        if (!TriangleIntersection(rayStartPoint, rayDir, v1, v2, v3, outPosition, outNormal, outDistance))
        {
            continue;
        }

        if (!hasIntersection || outDistance < closestTriangleDistance)
        {
            hasIntersection = true;
            position = outPosition;
            normal = outNormal;
            distance = outDistance;
            closestTriangleDistance = outDistance;
        }
    }

    return hasIntersection;
}

bool GetClosestObjectHit(float3 rayStartPoint, float3 rayDir, out Hit hit)
{
    bool hasHit = false;
    hit.position = 0;
    hit.normal = 0;

    float3 position;
    float3 normal;
    float distance = 0;
    float closestObjectDistance = 0;

    for (uint i = 0; i < _SpheresCount; i++)
    {
        Sphere sphere = _Spheres[i];

        if (SphereIntersection(rayStartPoint, rayDir, sphere.positionRadius, position, normal, distance))
        {
            if (!hasHit || distance < closestObjectDistance)
            {
                hasHit = true;
                hit.material = sphere.material; 
                hit.position = position;
                hit.normal = normal;
                closestObjectDistance = distance;
            }
        }
    }

    for (uint i = 0; i < _TriangleMeshesCount; i++)
    {
        TriangleMesh mesh = _TriangleMeshes[i];

        if (!BoundingBoxIntersection(mesh.min, mesh.max, rayStartPoint, rayDir))
        {
            continue;
        }
        
        if (GetClosestTriangleHit(rayStartPoint, rayDir, mesh.trianglesBeginEnd.x, mesh.trianglesBeginEnd.y, mesh.modelMatrix, position, normal, distance))
        {
            if (!hasHit || distance < closestObjectDistance)
            {
                hasHit = true;
                hit.material = mesh.material;
                hit.position = position;
                hit.normal = normal;
                closestObjectDistance = distance;
            }
        }
    }

    if (hasHit)
    {
        hit.normal = normalize(hit.normal);
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
            float3 diffuseDir = RandomDirectionInHemisphere(randomSeed, hit.normal);

            rayDir = lerp(diffuseDir, reflect(rayDir, hit.normal), hit.material.smoothnessEmission.x);
            rayStartPoint = hit.position + hit.normal * 0.0001;

            light += hit.material.smoothnessEmission.y * color * dot(hit.normal, rayDir);
            color *= hit.material.color.rgb;
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
    float2 randomSeed = input.uv + _Time.xx;

    float3 light = 0;
    for (uint i = 0; i < _MaxIterations + 1; ++i)
    {
        light += Trace(rayStartPoint, rayDir, randomSeed * (i + 1));
    }
    light /= _MaxIterations;

    return half4(light, 1);
}

#endif
