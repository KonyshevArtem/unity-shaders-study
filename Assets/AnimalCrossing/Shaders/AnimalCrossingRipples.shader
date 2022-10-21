Shader "Custom/Animal Crossing/Ripples" 
{
	Properties
	{
		_MainTex ("Main Tex", 2D) = "bump" {}
		_NormalMap ("Normal Map", 2D) = "bump" {}
	}

HLSLINCLUDE
#pragma vertex vert
#pragma fragment frag

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct Attributes
{
	float4 positionOS : POSITION;
	float2 texcoord : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 positionCS : POSITION;
	float2 uv : TEXCOORD0;
};

TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

Varyings vert(Attributes i)
{
	UNITY_SETUP_INSTANCE_ID(i);

	Varyings o;		
	o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
	o.uv = i.texcoord;
	return o;
}
ENDHLSL

	SubShader
	{
		Pass
        {
			Name "Ripple"

			HLSLPROGRAM
			#pragma multi_compile_instancing

			TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);

			uniform float2 _OneOverSize;

			half4 frag(Varyings i) : SV_Target
			{
				float2 normalMapUV = i.positionCS.xy * _OneOverSize;
				float3 currentNormal = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, normalMapUV).rgb * 2 - 1;
				float3 rippleNormal = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).rgb * 2 - 1;
				float3 normal = normalize(float3(currentNormal.xy + rippleNormal.xy, currentNormal.z));
				return half4(normal * 0.5 + 0.5, 0);
			}

			ENDHLSL
		}

		Pass
		{
			Name "Dissolve"

			HLSLPROGRAM

			half4 frag(Varyings i) : SV_Target
			{
				float3 normal = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).rgb * 2 - 1;
				float2 diff = 0 - normal.xy;
				normal.xy += diff * unity_DeltaTime.x * 5;
				return half4(normalize(normal) * 0.5 + 0.5, 0);
			}

			ENDHLSL
		}

		Pass
		{
			Name "Copy and Move"

			HLSLPROGRAM

			uniform float4 _VisibleArea;
			uniform float4 _LastFrameVisibleArea;

			half4 frag(Varyings i) : SV_Target
			{
				float2 uvDiff = (_VisibleArea.xy - _LastFrameVisibleArea.xy) * _VisibleArea.zw;
				return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + uvDiff);
			}

			ENDHLSL
		}
	} 
}
