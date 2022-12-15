Shader "Custom/PlayStation One/UVTest"
{
    Properties
    {
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
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = mul(UNITY_MATRIX_MVP, float4(v.positionOS.xyz, 1));
                o.uv = v.texcoord;
                return o;
            }
            
            half4 frag(Varyings i) : SV_Target
            {
                return half4(i.uv, 0, 1);
            }
            ENDHLSL
        }
    }
}
