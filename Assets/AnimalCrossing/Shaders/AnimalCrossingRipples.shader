Shader "Custom/Animal Crossing/Ripples" 
{
	Properties
	{
		_MainTex ("Main Tex", 2D) = "bump" {}
		_NormalMap ("Normal Map", 2D) = "bump" {}
	}

	SubShader
	{
		Pass
        {
			Name "Ripple"

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#define ANIMAL_CROSSING_RAIN_RIPPLES

			#include "Assets/AnimalCrossing/Shaders/AnimalCrossingCommon.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct Attributes
			{
				float4 positionOS : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : POSITION;
				float2 uv : TEXCOORD0;
				float alpha : TEXCOORD1;
			};

			uniform StructuredBuffer<float4> _Infos; // xy - pos, z - scale, w - alpha

			TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

			uniform float2 _OneOverSize;

			Varyings vert(Attributes i, uint instanceID : SV_InstanceID)
			{
				Varyings o;
				float4 info = _Infos[instanceID];
				float3 posWS = i.positionOS.xyz * info.z + float3(info.x, 0, info.y);
				o.positionCS = mul(UNITY_MATRIX_VP, float4(posWS, 1));
				o.uv = i.texcoord;
				o.alpha = info.w;
				return o;
			}

			half4 frag(Varyings i) : SV_Target
			{
				float2 normalMapUV = i.positionCS.xy * _OneOverSize;
				float3 currentNormal = reconstructNormal(unpackNormal(SAMPLE_TEXTURE2D(_RippleNormalMap, sampler_RippleNormalMap, normalMapUV)) * 2 - 1);
				float3 rippleNormal = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).rgb * 2 - 1;
				float2 normalXY = normalize(float3(currentNormal.xy + rippleNormal.xy, currentNormal.z)).xy * 0.5 + 0.5;
				normalXY = lerp(0.5, normalXY, i.alpha);
				return packNormal(normalXY);
			}

			ENDHLSL
		}
	} 
}
