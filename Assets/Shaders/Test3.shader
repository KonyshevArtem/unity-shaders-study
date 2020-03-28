Shader "Custom/Test 3"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
        [NoScaleOffset] _HeightMap ("Texture", 2D) = "black" {}
        _Offset ("Offset", Vector) = (0, 0, 0, 0)
        _Zoom ("Zoom", Float) = 1
        _Scale ("Scale", Float) = 1
    }
    
    SubShader
    {   
        Lighting On
        Tags { "RenderType"="Opaque" }
    
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            struct v2f
            {
                float4 pos: POSITION;
                float2 uv: TEXCOORD0;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            
            sampler2D _HeightMap;
            float4 _HeightMap_ST;
            
            float _Scale;
            float4 _Offset;
            float _Zoom;
            
            float getHeight(float2 coordinate)
            {
                float height = 0;
                height += tex2Dlod(_HeightMap, fixed4(coordinate + fixed2(-1, -1), 0, 0)).r;
                height += tex2Dlod(_HeightMap, fixed4(coordinate + fixed2(-1, 0), 0, 0)).r;
                height += tex2Dlod(_HeightMap, fixed4(coordinate + fixed2(-1, 1), 0, 0)).r;
                height += tex2Dlod(_HeightMap, fixed4(coordinate + fixed2(0, -1), 0, 0)).r;
                height += tex2Dlod(_HeightMap, fixed4(coordinate + fixed2(0, 0), 0, 0)).r;
                height += tex2Dlod(_HeightMap, fixed4(coordinate + fixed2(0, 1), 0, 0)).r;
                height += tex2Dlod(_HeightMap, fixed4(coordinate + fixed2(1, -1), 0, 0)).r;
                height += tex2Dlod(_HeightMap, fixed4(coordinate + fixed2(1, 0), 0, 0)).r;
                height += tex2Dlod(_HeightMap, fixed4(coordinate + fixed2(1, 1), 0, 0)).r;
                return height / 9;
            }
            
            v2f vert (appdata_base v)
            {
                v2f o;
                float aspect = _MainTex_TexelSize.w / _MainTex_TexelSize.z;
                float2 uvFactor = float2(aspect * _Zoom, _Zoom);
                float2 uv = v.texcoord * uvFactor + _Offset;
                
                o.pos = UnityObjectToClipPos(v.vertex + v.normal * getHeight(uv) * _Scale);
                o.uv = uv;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            
            ENDCG
        }
    }
}