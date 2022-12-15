Shader "Custom/PlayStation One/Utils"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }

        Pass
        {
            Name "PsOne Blit"
            ZTest Off
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex FullscreenVert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Fullscreen.hlsl"
            
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            
            half4 frag(Varyings i) : SV_Target
            {
                const uint numBits = 5;
                
                float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                color.r = UnpackUIntToFloat(PackFloatToUInt(color.r, 0, numBits), 0, numBits);
                color.g = UnpackUIntToFloat(PackFloatToUInt(color.g, 0, numBits), 0, numBits);
                color.b = UnpackUIntToFloat(PackFloatToUInt(color.b, 0, numBits), 0, numBits);
                return color; 
            }
            ENDHLSL
        }
    }
}