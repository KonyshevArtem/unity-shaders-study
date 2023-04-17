#ifndef UNITY_SHADERS_STUDY_CUSTOM_SKYBOX
#define UNITY_SHADERS_STUDY_CUSTOM_SKYBOX

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

uniform float2 _SkyboxRotationSinCos;

TEXTURECUBE(_Skybox); SAMPLER(sampler_Skybox);

half4 SampleSkybox(float3 dir)
{
    float2x2 rotMatrix = float2x2(_SkyboxRotationSinCos.y, _SkyboxRotationSinCos.x, -_SkyboxRotationSinCos.x, _SkyboxRotationSinCos.y);
    dir = float3(mul(rotMatrix, dir.xz).xy, dir.y).xzy;
    return SAMPLE_TEXTURECUBE_LOD(_Skybox, sampler_Skybox, dir, 0);
}

#endif