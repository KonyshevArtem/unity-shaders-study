Shader "PortalEffect/PortalOutline"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _OutlineScale ("Outline scale", Float) = 1
        _OutlineWidth ("Outline width", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            float4 _Color;
            float _OutlineScale;
            float _OutlineWidth;

            float invLerp(float from, float to, float value){
                return (value - from) / (to - from);
            }

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD;
            };

            v2f vert (appdata_base v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = (v.texcoord * 2 - 1) * _OutlineScale;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            { 
                half phase = sqrt(i.uv.x * i.uv.x + i.uv.y * i.uv.y);
                half outlinePhase = 1 - _OutlineWidth;
                half alpha = max(0, invLerp(outlinePhase, 1, phase));
                return half4(_Color.xyz, alpha);
            }
            ENDCG
        }
    }
}
