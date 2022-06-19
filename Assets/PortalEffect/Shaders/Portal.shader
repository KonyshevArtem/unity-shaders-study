Shader "PortalEffect/Portal"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [HideInInspector] _PortalID ("Portal ID", Int) = 0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent"
            "Queue" = "Transparent+2"
        }

        Stencil
        {
            Ref [_PortalID]
            Comp Equal
            Pass Zero
            ReadMask [_PortalID]
            WriteMask [_PortalID]
        }

        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : POSITION;
                float3 positionWS : TEXCOORD1;
            };
            
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            uniform float4 _MainTex_ST;
            
            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs posInputs = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = posInputs.positionCS;
                o.positionWS = posInputs.positionWS;
                return o;
            }
            
            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.positionCS.xy;
                uv *= (_ScreenParams.zw - 1);
                uv = uv * 0.5 + 0.5;
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, TRANSFORM_TEX(uv, _MainTex));
            }
            
            ENDHLSL
        }
    }
}