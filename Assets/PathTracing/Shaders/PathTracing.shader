Shader "Custom/Path Tracing/Path Tracing"
{
    Properties
    {
        _Skybox("Skybox", Cube) = "white"
    }
    SubShader
    {
        Tags
        {
            "LightMode"="UniversalForward"
        }

        Pass
        {
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MATRICES_PRE_APPLIED
            #pragma multi_compile _ _NO_INDICES

            #include "PathTracing.hlsl"
            ENDHLSL
        }
    }
}