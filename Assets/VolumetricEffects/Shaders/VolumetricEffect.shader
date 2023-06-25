Shader "Custom/Volumetric Effect/Volumetric Effect"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Absorption ("Absorption", Float) = 1
        _Scattering("Scattering", Float) = 1
        _Step ("Step", Float) = 0.1
        _Noise ("Noise", 3D) = "white"
        _NoiseIndexScale("Noise Index Scale", Float) = 64
        _WindVelocity("Wind Velocity", Vector) = (0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "LightMode"="VolumetricEffect"
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

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/VolumetricEffects/Shaders/BoxIntersection.hlsl"

            #define MAX_VOLUME_STEPS 50
            #define MAX_LIGHT_STEPS 10

            uniform half4 _Color;
            uniform float _Absorption;
            uniform float _Scattering;
            uniform float _Step;
            uniform float _NoiseIndexScale;
            uniform float3 _WindVelocity;

            uniform half3 _Ambient;

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

            half3 getNoiseUV(float3 pos)
            {
                half3 uv;
                uv.xz = TRANSFORM_TEX(pos.xz, _Noise) + _WindVelocity.xz * _Time.xx;
                uv.y = pos.y * _NoiseIndexScale + _WindVelocity.y * _Time.x;
                return uv;
            }

            half beersLaw(half distance)
            {
                return exp(-distance * _Step * (_Absorption + _Scattering));
            }

            float3 calculateLight(float3 pos, float density)
            {
                // https://cglearn.eu/pub/advanced-computer-graphics/volumetric-rendering

                float3 lightNearIntersection;
                float3 lightFarIntersection;
                boxIntersection(pos, _MainLightPosition.xyz, lightNearIntersection, lightFarIntersection);

                float3 lightTotalDensity = 0;
                half lightSegments = _Step > 0 ? floor(distance(lightNearIntersection, lightFarIntersection) / _Step) : 0;
                for (int j = 0; j < MAX_LIGHT_STEPS; ++j)
                {
                    float3 lightPos = pos + _Step * j * _MainLightPosition.xyz;
                    half3 uv = getNoiseUV(lightPos);
                    lightTotalDensity += j < lightSegments ? SAMPLE_TEXTURE3D(_Noise, sampler_Noise, uv).r : 0;
                }

                float3 inScattering = _MainLightColor.rgb * beersLaw(lightTotalDensity);
                float outScattering = _Scattering * density;

                float4 shadowCoord = TransformWorldToShadowCoord(pos);
                half shadow = MainLightRealtimeShadow(shadowCoord);

                return inScattering * outScattering * _Step * shadow;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                float3 forward = normalize(input.positionWS - _WorldSpaceCameraPos.xyz);

                float3 nearIntersection;
                float3 farIntersection;
                boxIntersection(_WorldSpaceCameraPos, forward, nearIntersection, farIntersection);

                float dist = distance(nearIntersection, farIntersection);
                float segments = _Step > 0 ? floor(dist / _Step) : 0;

                float3 light = 0;
                float transmittance = 1;
                for (int i = 0; i < MAX_VOLUME_STEPS; ++i)
                {
                    float3 pos = nearIntersection + _Step * i * forward;
                    half3 uv = getNoiseUV(pos);
                    half density = i < segments ? SAMPLE_TEXTURE3D(_Noise, sampler_Noise, uv).r : 0;
                    transmittance *= beersLaw(density);
                    light += transmittance * calculateLight(pos, density);
                }

                light += _Ambient;
                
                return half4(_Color.rgb * light, 1 - transmittance);
            }
            ENDHLSL
        }
    }
}