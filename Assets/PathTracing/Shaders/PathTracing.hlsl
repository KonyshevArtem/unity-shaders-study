#ifndef PATH_TRACING_HLSL
#define PATH_TRACING_HLSL

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct Attributes
{
    float3 positionOS : POSITION;
    float2 uv : TEXCOORD;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 viewDir : TEXCOORD0;
    float2 uv : TEXCOORD1;
};

struct Object
{
    float4 posWS;
    float4 color;
    float2 smoothnessEmission;
    uint2 trianglesOffsetCount;
    float isMesh;
    float4x4 modelMatrix;
};

struct Hit
{
    int ObjectIndex;
    float3 Position;
    float3 Normal;
};

struct Triangle
{
    int index1;
    int index2;
    int index3;
};

uniform StructuredBuffer<Object> _Objects;
uniform StructuredBuffer<float3> _Vertices;
uniform StructuredBuffer<Triangle> _Indices;
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
    output.uv = input.uv;

    return output;
}

bool SphereIntersection(float3 pos, float3 dir, float4 sphere, out float3 position, out float3 normal, out float distance)
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
    normal = position - sphere.xyz;
    return true;
}

bool TriangleIntersection(float3 pos, float3 dir, float3 v0, float3 v1, float3 v2, out float3 position, out float3 normal, out float distance)
{
    // compute the plane's normal
    float3 v0v1 = v1 - v0;
    float3 v0v2 = v2 - v0;
    // no need to normalize
    normal = cross(v0v1, v0v2); // N
 
    // Step 1: finding P
    
    // check if the ray and plane are parallel.
    float NdotRayDirection = dot(normal, dir);
    if (abs(NdotRayDirection) < 0.0001) // almost 0
        return false; // they are parallel, so they don't intersect! 

    // compute d parameter using equation 2
    float d = -dot(normal, v0);
    
    // compute t (equation 3)
    distance = -(dot(normal, pos) + d) / NdotRayDirection;
    
    // check if the triangle is behind the ray
    if (distance < 0)
        return false; // the triangle is behind
 
    // compute the intersection point using equation 1
    position = pos + distance * dir;
 
    // Step 2: inside-outside test
    float3 C; // vector perpendicular to triangle's plane
 
    // edge 0
    float3 edge0 = v1 - v0; 
    float3 vp0 = position - v0;
    C = cross(edge0, vp0);
    if (dot(normal, C) < 0) return false; // P is on the right side
 
    // edge 1
    float3 edge1 = v2 - v1; 
    float3 vp1 = position - v1;
    C = cross(edge1, vp1);
    if (dot(normal, C) < 0)  return false; // P is on the right side
 
    // edge 2
    float3 edge2 = v0 - v2; 
    float3 vp2 = position - v2;
    C = cross(edge2, vp2);
    if (dot(normal, C) < 0) return false; // P is on the right side;

    return true; // this ray hits the triangle
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

bool GetClosestTriangleHit(float3 rayStartPoint, float3 rayDir, uint triangleOffset, uint triangleCount, float4x4 modelMatrix, out float3 position, out float3 normal, out float distance)
{
    float3 outPosition;
    float3 outNormal;
    float outDistance;

    bool hasIntersection = false;
    float closestTriangleDistance = 0;
    for (uint j = triangleOffset; j < triangleOffset + triangleCount / 3; ++j)
    {
        Triangle indices = _Indices[j];
        float3 v1 = mul(modelMatrix, float4(_Vertices[indices.index1], 1)).xyz;
        float3 v2 = mul(modelMatrix, float4(_Vertices[indices.index2], 1)).xyz;
        float3 v3 = mul(modelMatrix, float4(_Vertices[indices.index3], 1)).xyz;
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
    hit.ObjectIndex = -1;
    hit.Position = 0;

    float closestObjectDistance = 0;
    for (uint i = 0; i < _ObjectsCount; i++)
    {
        Object object = _Objects[i];

        float3 position;
        float3 normal;
        float distance = 0;
        if (object.isMesh)
        {
            if (!GetClosestTriangleHit(rayStartPoint, rayDir, object.trianglesOffsetCount.x, object.trianglesOffsetCount.y, object.modelMatrix, position, normal, distance))
            {
                continue;
            }
        }
        else
        {
            if (!SphereIntersection(rayStartPoint, rayDir, object.posWS, position, normal, distance))
            {
                continue;
            }
        }

        if (hit.ObjectIndex < 0 || distance < closestObjectDistance)
        {
            hit.ObjectIndex = i;
            hit.Position = position;
            hit.Normal = normalize(normal);
            closestObjectDistance = distance;
        }
    }
    
    return hit.ObjectIndex >= 0;
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
            randomSeed = x;
            float y = Random(randomSeed + i);
            randomSeed = y;
            float z = Random(randomSeed + i);
            randomSeed = z;
            float3 diffuseDir = normalize(float3(x, y, z));
            diffuseDir *= dot(diffuseDir, hit.Normal) < 0 ? -1 : 1;

            rayDir = lerp(diffuseDir, reflect(rayDir, hit.Normal), object.smoothnessEmission.x);
            rayStartPoint = hit.Position + hit.Normal * 0.0001;

            light += object.smoothnessEmission.y * color * dot(hit.Normal, rayDir);
            color *= object.color.rgb;
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
