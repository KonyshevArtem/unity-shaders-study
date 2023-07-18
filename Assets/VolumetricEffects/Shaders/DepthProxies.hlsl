#ifndef DEPTH_PROXIES_HLSL
#define DEPTH_PROXIES_HLSL

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D_HALF(_DepthProxy);
SAMPLER(sampler_linear_clamp);

float getDepthFactor(float positionZ, half2 screenUV, out float nearDistance)
{
    // https://www.eidosmontreal.com/news/depth-proxy-transparency-rendering/
    float2 minMax = SAMPLE_TEXTURE2D(_DepthProxy, sampler_linear_clamp, screenUV).rg;
    float minZ = Linear01Depth(minMax.x, _ZBufferParams);
    float maxZ = Linear01Depth(1 - minMax.y, _ZBufferParams);

    positionZ = Linear01Depth(positionZ, _ZBufferParams);
    nearDistance = max(0, positionZ - minZ);

    return saturate((positionZ - minZ) / abs(maxZ - minZ));
}

#endif