Shader "Hidden/ImageEffects/ChromaticAberation"
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

            v2f vert (appdata_base v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            
            uniform float2 _RedOffset;
            uniform float2 _GreenOffset;
            uniform float2 _BlueOffset;

            half4 frag (v2f i) : SV_Target
            {
                float r = tex2D(_MainTex, i.uv + float2(_RedOffset.x * _MainTex_TexelSize.x, _RedOffset.y * _MainTex_TexelSize.y)).x;
                float g = tex2D(_MainTex, i.uv + float2(_GreenOffset.x * _MainTex_TexelSize.x, _GreenOffset.y * _MainTex_TexelSize.y)).y;
                float b = tex2D(_MainTex, i.uv + float2(_BlueOffset.x * _MainTex_TexelSize.x, _BlueOffset.y * _MainTex_TexelSize.y)).z;
                
                return half4(r, g, b, 1);
            }
            ENDHLSL
        }
    }
}
