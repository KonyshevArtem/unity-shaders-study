Shader "PortalEffect/PortalRim"
{
    Properties
    {
        _RimColor ("Rim Color", Color) = (1, 1, 1, 1)
        _RimOuterRadius ("Rim Outer Radius", Float) = 0.5
        _RimInnerRadius ("Rim Inner Radius", Float) = 0.25
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent"
            "Queue" = "Transparent+4"
        }

        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 texcoord : TEXCOORD;
            };

            struct Varyings
            {
                float4 positionCS : POSITION;
                float2 uv : TEXCOORD0;
            };  

            uniform half4 _RimColor;
            uniform float _RimOuterRadius;
            uniform float _RimInnerRadius;
            
            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.texcoord;
                return o;
            }
            
            half4 frag(Varyings i) : SV_Target
            {
                float outerRadiusSqr = _RimOuterRadius * _RimOuterRadius;
                float innerRadiusSqr = _RimInnerRadius * _RimInnerRadius;
                float2 dir = abs(i.uv - float2(0.5, 0.5));
                float distSqr = dot(dir, dir);
                float rimAlpha = smoothstep(innerRadiusSqr, outerRadiusSqr, distSqr);
                return half4(_RimColor.rgb, rimAlpha);
            }
            
            ENDHLSL
        }
    }
}