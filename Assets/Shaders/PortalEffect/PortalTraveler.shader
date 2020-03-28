Shader "PortalEffect/PortalTraveler"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        
        Stencil
        {
            Ref 1
            Comp equal
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 normal: NORMAL;
            };

            v2f vert (appdata_base v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(1, 1, 1, 1) * dot(_WorldSpaceLightPos0, i.normal) * _LightColor0;
            }
            ENDCG
        }
    }
}
