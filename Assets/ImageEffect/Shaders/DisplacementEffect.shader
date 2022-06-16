Shader "Hidden/ImageEffects/DisplacementEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
    
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _MainTex;
            uniform sampler2D _Displacement;
            uniform float _Magnitude;

            v2f vert (appdata_base v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float2 displacement = tex2D(_Displacement, i.uv + _Time) * 2 - 1;
                return tex2D(_MainTex, i.uv + displacement * _Magnitude);
            }
            ENDHLSL
        }
    }
}
