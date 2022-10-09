Shader "Custom/Animal Crossing/Rain"
{
    Properties
    {
        _RainSpeed ("Rain Speed", Float) = 10
        _Color ("Color", Color) = (1, 1, 1, 1)
        _MoveMultiplier("Move Multiplier", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            uniform float _RainSpeed;
            uniform float _Width;
            uniform float4 _Color;
            uniform float _MoveMultiplier;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 center : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                float3 centerDiff = v.center - v.positionOS.xyz;
                float3 rainCenterWS = mul(UNITY_MATRIX_M, float4(0, 0, 0, 1)).xyz * _MoveMultiplier;

                float3 bounds = float3(_Width, 2, 2);
                float3 halfBounds = bounds * 0.5;

                v.center -= (float3(rainCenterWS.x, _Time.x * _RainSpeed, rainCenterWS.z)) % bounds;
                v.center = lerp(v.center, v.center + bounds, step(v.center, -halfBounds));
                v.center.xz = lerp(v.center.xz, v.center.xz - bounds.xz, step(halfBounds.xz, v.center.xz));

                v.positionOS.xyz = v.center + centerDiff;

                o.positionCS = mul(UNITY_MATRIX_MVP, float4(v.positionOS.xyz, 1));
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                return half4(_Color.rgb, 1);
            }
            ENDHLSL
        }
    }
}
