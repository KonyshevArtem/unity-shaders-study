Shader "Custom/PlayStation One/PS1"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }

        Pass
        {
            Name "Playstation One"
            
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
                float4 positionCS : SV_POSITION;
                noperspective float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            uniform float4 _MainTex_ST;
            uniform float2 _Resolution;

            float4 snap(float4 positionCS)
            {
                positionCS.xyz /= positionCS.w;
                positionCS.xy = floor(_Resolution * positionCS.xy) / _Resolution;
                positionCS.xyz *= positionCS.w;
                return positionCS;
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = snap(mul(UNITY_MATRIX_MVP, float4(v.positionOS.xyz, 1)));
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }
            
            half4 frag(Varyings i) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv); 
            }
            ENDHLSL
        }
    }
}