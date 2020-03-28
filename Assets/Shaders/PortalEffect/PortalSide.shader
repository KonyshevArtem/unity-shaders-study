Shader "PortalEffect/PortalSide"
{
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Front
        ZWrite Off
        
        Stencil
        {
            Ref 1
            Comp always
            Pass replace
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata_base v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(0, 0, 0, 0);
            }
            ENDCG
        }
    }
}
