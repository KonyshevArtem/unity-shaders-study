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

            #include "PathTracing.hlsl"
            ENDHLSL
        }
    }
}