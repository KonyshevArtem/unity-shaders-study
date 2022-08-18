Shader "Custom/Mirror"
{
    Properties
    {
        _GrainMask ("Grain Mask", 2D) = "black" {}
        _GrainColor ("Grain Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _MirrorTex ("Mirror Tex", 2D) = "white" {}
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

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : POSITION;
                float2 uv : TEXCOORD0;
            };  

            uniform float _Tint;
            uniform half4 _GrainColor;
            uniform float4 _GrainMask_ST;

            TEXTURE2D(_GrainMask); SAMPLER(sampler_GrainMask);
            TEXTURE2D(_MirrorTex); SAMPLER(sampler_MirrorTex);

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.texcoord;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float2 uv = i.positionCS * (_ScreenParams.zw - 1.0);
                uv.x = 1 - uv.x;
                uv.y = lerp(uv.y, 1 - uv.y, saturate(_ProjectionParams.x));
                half3 mirrorColor = SAMPLE_TEXTURE2D(_MirrorTex, sampler_MirrorTex, uv).rgb * _Tint;
                half grainMask = 1 - SAMPLE_TEXTURE2D(_GrainMask, sampler_GrainMask, TRANSFORM_TEX(i.uv, _GrainMask)).r;
                half3 color = lerp(mirrorColor, _GrainColor.xyz, grainMask * _GrainColor.a);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
