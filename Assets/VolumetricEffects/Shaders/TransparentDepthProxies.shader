Shader "Custom/Volumetric Effect/Transparent Depth Proxies"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "LightMode"="UniversalForward"
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        ZWrite Off

        Pass
        {
            Name "Transparent Depth Proxies"

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "DepthProxies.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                half3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
            };

            half4 _Color;

            TEXTURE2D_HALF(_VolumesRT); SAMPLER(sampler_VolumesRT);

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInputs.positionCS;
                output.positionWS = vertexInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = _Color.rgb;
                surfaceData.alpha = _Color.a;
                surfaceData.occlusion = 1;

                InputData inputData = (InputData)0;
                inputData.positionCS = input.positionCS;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = input.normalWS;
                inputData.viewDirectionWS = SafeNormalize(GetCameraPositionWS() - input.positionWS);

                half4 finalColor = UniversalFragmentPBR(inputData, surfaceData);

                half2 screenUV = input.positionCS.xy * (_ScreenParams.zw - 1);
                screenUV.y = lerp(screenUV.y, 1 - screenUV.y, saturate(_ScaleBiasRt.x));

                float nearDistance;
                float depthFactor = saturate(getDepthFactor(input.positionCS.z, screenUV, nearDistance));

                half4 volume = SAMPLE_TEXTURE2D(_VolumesRT, sampler_VolumesRT, screenUV);
                half alphaFactor = saturate(depthFactor * pow(1 + volume.a + nearDistance, 2));

                finalColor.rgb = lerp(finalColor.rgb, volume.rgb, min(alphaFactor, volume.a));
                return finalColor;
            }
            ENDHLSL
        }
    }
}