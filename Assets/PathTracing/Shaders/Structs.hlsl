#ifndef UNITY_SHADERS_STUDY_STRUCTS
#define UNITY_SHADERS_STUDY_STRUCTS

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

struct MaterialParameters
{
    float4 color;
    float2 smoothnessEmission;
};

struct Sphere
{
    float4 positionRadius;
    MaterialParameters material;
};

struct TriangleMesh
{
    float4x4 modelMatrix;
    MaterialParameters material;
    uint2 trianglesBeginEnd;
    float3 min;
    float3 max;
};

struct Hit
{
    MaterialParameters material;
    float3 position;
    float3 normal;
};

struct Triangle
{
    #ifdef _NO_INDICES
    float3 vertex1;
    float3 vertex2;
    float3 vertex3;
    #else
    int index1;
    int index2;
    int index3;
    #endif
};

#endif