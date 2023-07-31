#ifndef GAUSSIAN_BLUR
#define GAUSSIAN_BLUR

#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Fullscreen.hlsl"

TEXTURE2D_HALF (_MainTex); SAMPLER (sampler_MainTex);
half4 _MainTex_TexelSize;

half4 Fragment(Varyings input) : SV_Target
{
    const int kernelRadius = 3;
    const float gaussian[kernelRadius * 2 + 1] =
    {
        0.040218913292475,
        0.11428215322398229,
        0.21379142950442534,
        0.2634150079582347,
        0.21379142950442534,
        0.11428215322398229,
        0.040218913292475,
    };

    half4 color = 0;
    for (int i = -kernelRadius; i <= kernelRadius; ++i)
    {
        #if defined(VERTICAL)
        half2 offset = half2(0, 1);
        #elif defined(HORIZONTAL)
        half2 offset = half2(1, 0);
        #endif

        float coef = gaussian[kernelRadius + i];
        color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + _MainTex_TexelSize.xy * offset * i) * coef;  
    }

    return half4(color.rgb, color.a);
}

#endif