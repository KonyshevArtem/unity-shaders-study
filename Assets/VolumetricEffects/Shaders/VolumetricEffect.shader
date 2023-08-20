Shader "Custom/Volumetric Effect/Volumetric Effect"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Absorption ("Absorption", Float) = 1
        _Scattering("Scattering", Float) = 1
        _Step ("Step", Float) = 0.1
        _Density ("Density", 3D) = "white"
        _DensityIndexScale("Density Index Scale", Float) = 64
        _WindVelocity("Wind Velocity", Vector) = (0, 0, 0)
        _SDFOffset("SDF Offset", Float) = 0
        _SDFFadeDistance("SDF Fade Distance", Float) = 0.1
        _SDF("_SDF", 3D) = "black"
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

        ZTest Always
        ZWrite Off
        Cull Front

        BlendOp 0 Add
        BlendOp 1 Max
        Blend 0 One OneMinusSrcAlpha
        Blend 1 One Zero

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SDF_SHAPE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #pragma multi_compile _ _DITHER

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/VolumetricEffects/Shaders/BoxIntersection.hlsl"

            #define MAX_VOLUME_STEPS 50
            #define MAX_LIGHT_STEPS 10

            // material properties
            uniform half4 _Color;
            uniform float _Absorption;
            uniform float _Scattering;
            uniform float _Step;
            uniform float _DensityIndexScale;
            uniform float3 _WindVelocity;
            uniform half _SDFOffset;
            uniform half _SDFFadeDistance;

            // manually set from render pass
            uniform half4 _AmbientScale; // xyz - ambient color, w - RT scale

            TEXTURE3D_HALF(_Density); SAMPLER(sampler_point_mirror);
            TEXTURE2D_FLOAT(_CameraDepthTexture); SAMPLER(sampler_CameraDepthTexture);
            TEXTURE3D_HALF(_SDF); SAMPLER(sampler_SDF);
            half4 _Density_ST;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD00;
            };

            struct FragmentOutput
            {
                half4 color : SV_Target0;
                float2 depthMinMax : SV_Target1;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;

                VertexPositionInputs vertexInputs = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = vertexInputs.positionCS;
                o.positionWS = vertexInputs.positionWS;

                return o;
            }

            half getDensity(float3 pos)
            {
                half3 uv;
                uv.xz = TRANSFORM_TEX(pos.xz, _Density) + _WindVelocity.xz * _Time.xx;
                uv.y = pos.y * _DensityIndexScale + _WindVelocity.y * _Time.x;
                half density = SAMPLE_TEXTURE3D(_Density, sampler_point_mirror, uv).r;

#ifdef _SDF_SHAPE
                float3 localPos = mul(UNITY_MATRIX_I_M, float4(pos, 1)).xyz;
                float sdfDistance = saturate(SAMPLE_TEXTURE3D(_SDF, sampler_SDF, localPos + 0.5).r + _SDFOffset);
                density *= saturate(1 - sdfDistance / _SDFFadeDistance);
#endif

                return density;
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

                half lightTotalDensity = 0;
                half lightSegments = _Step > 0 ? floor(distance(lightNearIntersection, lightFarIntersection) / _Step) : 0;

                UNITY_UNROLL
                for (int j = 0; j < MAX_LIGHT_STEPS; ++j)
                {
                    if (j < lightSegments)
                    {
                        float3 lightPos = pos + _Step * j * _MainLightPosition.xyz;
                        lightTotalDensity += getDensity(lightPos);
                    }
                }

                float3 inScattering = _MainLightColor.rgb * beersLaw(lightTotalDensity);
                float outScattering = _Scattering * density;

                float4 shadowCoord = TransformWorldToShadowCoord(pos);
                half shadow = MainLightRealtimeShadow(shadowCoord);

                return inScattering * outScattering * _Step * shadow;
            }

            half2 getScreenUV(float2 positionCS)
            {
                return positionCS.xy * _AmbientScale.w * (_ScreenParams.zw - 1);   
            }
            
            float3 getDepthPosWS(half2 screenUV)
            {
                float depth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, screenUV).r;
                screenUV.y = lerp(screenUV.y, 1 - screenUV.y, saturate(_ScaleBiasRt.x));
                return ComputeWorldSpacePosition(screenUV, depth, UNITY_MATRIX_I_VP);
            }
            
            float gradientNoise(float2 uv)
            {
                // Interleaved gradient function from Jimenez 2014 http://goo.gl/eomGso
                uv = floor(uv * _ScreenParams.xy / _AmbientScale.w);
                float f = dot(float2(0.06711056f, 0.00583715f), uv);
                return frac(52.9829189f * frac(f));
            }

            FragmentOutput frag(Varyings input)
            {
                float3 forward = normalize(input.positionWS - _WorldSpaceCameraPos.xyz);

                float3 nearIntersection;
                float3 farIntersection;
                boxIntersection(_WorldSpaceCameraPos, forward, nearIntersection, farIntersection);

                half2 screenUV = getScreenUV(input.positionCS.xy);
                float3 depthPos = getDepthPosWS(screenUV);
                float3 cameraToDepth = depthPos - _WorldSpaceCameraPos;
                float3 cameraToNearInt = nearIntersection - _WorldSpaceCameraPos;
                clip(dot(cameraToDepth, cameraToDepth) - dot(cameraToNearInt, cameraToNearInt));

#ifdef _DITHER
                float dither = gradientNoise(screenUV);
                nearIntersection += forward * dither * 0.1; // dither near position to avoid banding artifacts
#endif

                float dist = min(distance(nearIntersection, farIntersection), distance(nearIntersection, depthPos));
                float segments = _Step > 0 ? floor(dist / _Step) : 0;

                float3 light = 0;
                float transmittance = 1;
                
#ifdef _SDF_SHAPE
                float4 min = 0;
                float4 max = 0;
#else
                float4 min = float4(nearIntersection, 1);
                float4 max = float4(farIntersection, 1);
#endif

                for (int i = 0; i < MAX_VOLUME_STEPS; ++i)
                {
                    if (i < segments)
                    {
                        float3 pos = nearIntersection + _Step * i * forward;
                        half density = getDensity(pos);
                        transmittance *= beersLaw(density);
                        light += transmittance * calculateLight(pos, density);

#ifdef _SDF_SHAPE
                        min = density > 0.01 ? lerp(float4(pos, 1), min, min.w) : min;
                        max = density > 0.01 ? float4(pos, 1) : max;
#endif
                    }
                }

                light += _AmbientScale.rgb;
                
                min = mul(UNITY_MATRIX_VP, min);
                max = mul(UNITY_MATRIX_VP, max);

                float alpha = 1 - transmittance;
                
                FragmentOutput output;
                output.color = half4(_Color.rgb * light * alpha, alpha);
                output.depthMinMax = float2(min.z / min.w, 1 - max.z / max.w);
                return output;
            }
            ENDHLSL
        }
    }
}