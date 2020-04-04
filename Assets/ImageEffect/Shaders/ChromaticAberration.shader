Shader "ImageEffects/ChromaticAberation"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _RedOffset ("Red Offset", Vector) = (0, 0, 0, 0)
        _GreenOffset ("Green Offset", Vector) = (0, 0, 0, 0)
        _BlueOffset ("Blue Offset", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
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
            
            float2 _RedOffset;
            float2 _GreenOffset;
            float2 _BlueOffset;

            fixed4 frag (v2f i) : SV_Target
            {
                float r = tex2D(_MainTex, i.uv + float2(_RedOffset.x * _MainTex_TexelSize.x, _RedOffset.y * _MainTex_TexelSize.y)).x;
                float g = tex2D(_MainTex, i.uv + float2(_GreenOffset.x * _MainTex_TexelSize.x, _GreenOffset.y * _MainTex_TexelSize.y)).y;
                float b = tex2D(_MainTex, i.uv + float2(_BlueOffset.x * _MainTex_TexelSize.x, _BlueOffset.y * _MainTex_TexelSize.y)).z;
                
                return fixed4(r, g, b, 1);
            }
            ENDCG
        }
    }
}
