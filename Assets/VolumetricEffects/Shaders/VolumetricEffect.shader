Shader "Custom/Volumetric Effect/Volumetric Effect"
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

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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

            float2 boxIntersection(in float3 ro, in float3 rd, in float3 rad)
            {
                // https://iquilezles.org/articles/boxfunctions/

                float3 m = 1.0 / rd;
                float3 n = m * ro;
                float3 k = abs(m) * rad;
                float3 t1 = -n - k;
                float3 t2 = -n + k;

                float tN = max(max(t1.x, t1.y), t1.z);
                float tF = min(min(t2.x, t2.y), t2.z);

                if (tN > tF || tF < 0.0) return float2(-1.0, -1.0); // no intersection

                //oN = -sign(rd) * step(t1.yzx, t1.xyz) * step(t1.zxy, t1.xyz);

                return float2(tN, tF);
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 rd = mul(UNITY_MATRIX_I_M, float4(normalize(i.positionWS - _WorldSpaceCameraPos.xyz), 0));
                float3 ro = mul(UNITY_MATRIX_I_M, float4(_WorldSpaceCameraPos.xyz, 1));

                float2 ot = boxIntersection(ro, rd, half3(0.5, 0.5, 0.5));
                float3 nearIntersection = mul(UNITY_MATRIX_M, float4(ro + ot.x * rd, 1));
                float3 farIntersection = mul(UNITY_MATRIX_M, float4(ro + ot.y * rd, 1));

                float dist = distance(nearIntersection, farIntersection);
                
                return half4(_Color.rgb, 1 - exp(-dist * _Absorption));
            }
            ENDHLSL
        }
    }
}