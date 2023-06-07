Shader "Custom/Volumetric Effect/Box Volumetric Effect"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Absorption ("Absorption", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "LightMode"="UniversalForward"
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "DisableBatching"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Front

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/VolumetricEffects/Shaders/BoxIntersection.hlsl"

            uniform half4 _Color;
            uniform float _Absorption;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD00;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;

                VertexPositionInputs vertexInputs = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = vertexInputs.positionCS;
                o.positionWS = vertexInputs.positionWS;

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 forward = normalize(i.positionWS - _WorldSpaceCameraPos.xyz);

                float3 nearIntersection;
                float3 farIntersection;
                boxIntersection(_WorldSpaceCameraPos, forward, nearIntersection, farIntersection);

                float dist = distance(nearIntersection, farIntersection);

                return half4(_Color.rgb, 1 - exp(-dist * _Absorption));
            }
            ENDHLSL
        }
    }
}