Shader "Custom/Volumetric Effect/Volumetric Effect"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Absorption ("Absorption", Float) = 1
        _Step ("Step", Float) = 0.1
        _Noise ("Noise", 3D) = "white"
        _NoiseIndexScale("Noise Index Scale", Float) = 64
        _WindVelocity("Wind Velocity", Vector) = (0, 0, 0)
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

            #define MAX_VOLUME_STEPS 50

            uniform half4 _Color;
            uniform float _Absorption;
            uniform float _Step;
            uniform float _NoiseIndexScale;
            uniform float3 _WindVelocity;

            TEXTURE3D_HALF(_Noise); SAMPLER(sampler_Noise);
            half4 _Noise_ST;

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

            half4 frag(Varyings input) : SV_Target
            {
                float3 forward = normalize(input.positionWS - _WorldSpaceCameraPos.xyz);

                float3 nearIntersection;
                float3 farIntersection;
                boxIntersection(_WorldSpaceCameraPos, forward, nearIntersection, farIntersection);

                float dist = distance(nearIntersection, farIntersection);
                float segments = _Step > 0 ? floor(dist / _Step) : 0;
                
                float distance = 0;
                for (int i = 0; i < MAX_VOLUME_STEPS; ++i)
                {
                    float3 pos = nearIntersection + _Step * i * forward;
                    half3 uv;
                    uv.xz = TRANSFORM_TEX(pos.xz, _Noise) + _WindVelocity.xz * _Time.xx;
                    uv.y = pos.y * _NoiseIndexScale + _WindVelocity.y * _Time.x;
                    half density = i < segments ? SAMPLE_TEXTURE3D(_Noise, sampler_Noise, uv).r : 0;
                    distance += _Step * density;
                }
                
                return half4(_Color.rgb, 1 - exp(-distance * _Absorption));
            }
            ENDHLSL
        }
    }
}