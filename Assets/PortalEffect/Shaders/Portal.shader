// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "PortalEffect/Portal"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        
        Stencil
        {
            Ref 1
            Comp always
            Pass replace
        }
        
        ZWrite Off
    
        Pass
        {
            CGPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            struct v2f
            {
                float4 pos: POSITION;
                float3 uv: TEXCOORD0;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;    
            float _BorderWidth;
            
            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = float3(((o.pos.xy + o.pos.w) * 0.5), o.pos.w); 
                return o;
            }
            
            half4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv.xy / i.uv.z;
                return tex2D(_MainTex, uv);
            }
            
            ENDCG
        }
    }
}