Shader "ImageEffects/DisplacementEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Displacement ("Displacement map", 2D) = "black" {}
        _Magnitude ("Magnitude", Float) = 0
    }
    SubShader
    {
        ZWrite Off
    
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 displacementUv: TEXCOORD1;
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _MainTex;
            sampler2D _Displacement;
            float4 _Displacement_ST;
            float _Magnitude;

            v2f vert (appdata_base v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.displacementUv = TRANSFORM_TEX(v.texcoord, _Displacement);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed2 displacement = tex2D(_Displacement, i.displacementUv + _Time) * 2 - 1;
                fixed4 col = tex2D(_MainTex, i.uv + displacement * _Magnitude);
                return col;
            }
            ENDCG
        }
    }
}
