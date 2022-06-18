Shader "Custom/Mask"
{
    Properties
    {
        _MaskId ("Mask Id", Int) = 0
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
            "Queue"="Geometry-1"
        }
      
        ZWrite Off
        ColorMask 0

        Stencil
        {
            Ref [_MaskId]
            Pass Replace
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
            
            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }
            
            float4 frag(Varyings i) : SV_Target
            {
                return 0;
            }
            
            ENDHLSL
        }
    }
}
