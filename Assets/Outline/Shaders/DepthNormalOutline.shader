Shader "Custom/Outline/Utils/DepthNormalOutline"
{
    Properties {}

    HLSLINCLUDE
    struct Attributes
    {
        float4 positionOS : POSITION;
    };


    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 uv : TEXCOORD0;
    };

    Varyings vert(Attributes i)
    {
        Varyings output;
        output.positionCS = float4(i.positionOS.xyz, 1);
        output.uv = i.positionOS.xy * 0.5 + 0.5;
        return output;
    }
    ENDHLSL
    
    SubShader
    {
        Pass
        {
            Name "DepthNormalsOutlineMask"
            ZTest Off
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            uniform float4 _DepthNormalOutlineParams; // xy - normals mult bias, zw - depth mult bias

            TEXTURE2D(_CameraNormalsTexture); SAMPLER(sampler_CameraNormalsTexture);
            TEXTURE2D(_CameraDepthTexture); SAMPLER(sampler_CameraDepthTexture);

            half SampleDepth(float2 uv)
            {
                return Linear01Depth(SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r, _ZBufferParams);
            }

            half3 SampleNormal(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_CameraNormalsTexture, sampler_CameraNormalsTexture, uv).xyz * 2 - 1;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uvOffset = (_ScreenParams.zw - 1);

                float normalTerm = 0;
                half3 normal = SampleNormal(i.uv);
                normalTerm += distance(normal, SampleNormal(i.uv + uvOffset * float2(1, 0)));
                normalTerm += distance(normal, SampleNormal(i.uv + uvOffset * float2(0, 1)));
                normalTerm += distance(normal, SampleNormal(i.uv + uvOffset * float2(-1, 0)));
                normalTerm += distance(normal, SampleNormal(i.uv + uvOffset * float2(0, -1)));
                normalTerm *= _DepthNormalOutlineParams.x;
                normalTerm = saturate(normalTerm);
                normalTerm = pow(normalTerm, _DepthNormalOutlineParams.y);

                float depthTerm = 0;
                half depth = SampleDepth(i.uv);
                depthTerm += abs(depth - SampleDepth(i.uv + uvOffset * float2(1, 0)));
                depthTerm += abs(depth - SampleDepth(i.uv + uvOffset * float2(0, 1)));
                depthTerm += abs(depth - SampleDepth(i.uv + uvOffset * float2(-1, 0)));
                depthTerm += abs(depth - SampleDepth(i.uv + uvOffset * float2(0, -1)));
                depthTerm *= _DepthNormalOutlineParams.z;
                depthTerm = saturate(depthTerm);
                depthTerm = pow(depthTerm, _DepthNormalOutlineParams.w);

                return normalTerm + depthTerm;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormalsOutlineApply"
            ZTest Off
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            uniform half4 _OutlineColor;

            TEXTURE2D(_CameraOpaqueTexture); SAMPLER(sampler_CameraOpaqueTexture);
            TEXTURE2D(_OutlineMask); SAMPLER(sampler_OutlineMask);

            half4 frag(Varyings i) : SV_Target
            {
                half mask = SAMPLE_TEXTURE2D(_OutlineMask, sampler_OutlineMask, i.uv).r;
                half3 color = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, float2(i.uv.x, 1 - i.uv.y)).rgb;
                return half4(lerp(color, _OutlineColor.rgb, mask), 1);
            }
            ENDHLSL
        }
    }
}