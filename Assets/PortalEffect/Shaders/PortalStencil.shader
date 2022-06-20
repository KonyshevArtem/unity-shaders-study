Shader "PortalEffect/PortalStencil"
{
    Properties
    {
        [HideInInspector] _PortalID ("Portal ID", Int) = 0
    }
    
    SubShader
    {
        Tags
        { 
            "RenderType"="Transparent"
            "Queue"="Transparent+1"
        }

        ColorMask 0
        ZWrite Off

        Stencil
        {
            Ref [_PortalID]
            Comp Always
            Pass Replace
            WriteMask [_PortalID]
        }

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
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
