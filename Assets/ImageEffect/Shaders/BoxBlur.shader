Shader "ImageEffects/BoxBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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

            float4 boxBlur(float2 uv) 
            {
                float x = _MainTex_TexelSize.x;
                float y = _MainTex_TexelSize.y;
                float4 sum = tex2D(_MainTex, uv) + tex2D(_MainTex, float2(uv.x - x, uv.y - y)) + tex2D(_MainTex, float2(uv.x, uv.y - y))
                                + tex2D(_MainTex, float2(uv.x + x, uv.y - y)) + tex2D(_MainTex, float2(uv.x - x, uv.y))
                                + tex2D(_MainTex, float2(uv.x + x, uv.y)) + tex2D(_MainTex, float2(uv.x - x, uv.y + y))
                                + tex2D(_MainTex, float2(uv.x, uv.y + y)) + tex2D(_MainTex, float2(uv.x + x, uv.y + y));
                return sum / 9;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return boxBlur(i.uv);
            }
            ENDCG
        }
    }
}
