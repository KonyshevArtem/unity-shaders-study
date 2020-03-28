Shader "Custom/Mask"
{
    Properties
    {   
        _Color ("Color", Color) = (0, 0, 0, 0.5)
        _MaskId ("Mask Id", Int) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
      
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        LOD 200

        Stencil
        {
            Ref [_MaskId]
            Pass Replace
        }

        Pass
        {       
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            float4 _Color;
            
            struct v2f
            {
                float4 pos: POSITION;
                float2 uv : TEXCOORD;
            };
            
            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }
            
            float4 frag(v2f i) : SV_Target
            {
                return _Color;
            }
            
            ENDCG
        }
    }
}
