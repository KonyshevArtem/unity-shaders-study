#ifndef UNITY_SHADERS_STUDY_RANDOM
#define UNITY_SHADERS_STUDY_RANDOM#iffndef UNITY_SHADERS_STUDY_CUSTOM_#deinfine #endif

float Random(half2 st)
{
    //https://gist.github.com/keijiro/ee7bc388272548396870
    return frac(sin(dot(st.xy, half2(12.9898, 78.233))) * 43758.5453123) * 2 - 1;
}

float3 RandomDirection(inout float2 randomSeed)
{
    float x = Random(randomSeed);
    randomSeed = x;
    float y = Random(randomSeed);
    randomSeed = y;
    float z = Random(randomSeed);
    randomSeed = z;

    return normalize(float3(x, y, z));
}

float3 RandomDirectionInHemisphere(inout float2 randomSeed, float3 hemisphereDirection)
{
    float3 randomDir = RandomDirection(randomSeed);
    randomDir *= dot(randomDir, hemisphereDirection) < 0 ? -1 : 1;
    return randomDir;
}

#endif