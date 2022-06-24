Shader "Custom/Division Effect"
{
    Properties
    {
        [NoScaleOffset] _InterferenceNoise ("Interference Noise", 2D) = "gray" {}
        _InterferenceStrength ("Interference Strength", Float) = 1

        _TrianglesMoveNoise ("Triangles Move Noise", 2D) = "gray" {}
        _TrianglesMoveStrength ("Triangles Move Strength", Float) = 1

        _OffsetStrength ("Offset Strength", Float) = 1
        _OffsetDistance ("Offset Distance", Float) = 0.2

        _ColorFront ("Color Front", Color) = (1, 1, 1, 1)
        _ColorSide ("Color Side", Color) = (1, 1, 1, 1)
        _FalloffStrength("Falloff Strength", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : POSITION;
                float3 positionWS : TEXCOORD2;
                nointerpolation float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD0;
            };  

            TEXTURE2D(_InterferenceNoise); SAMPLER(sampler_InterferenceNoise);
            TEXTURE2D(_TrianglesMoveNoise); SAMPLER(sampler_TrianglesMoveNoise);
            float4 _TrianglesMoveNoise_ST;

            uniform float3 _TargetPosition;

            float _OffsetStrength;
            float _OffsetDistance;

            half3 _ColorFront;
            half3 _ColorSide;
            float _FalloffStrength;

            float _InterferenceStrength;
            float _TrianglesMoveStrength;

            Varyings vert (Attributes v)
            {
                Varyings o = (Varyings) 0;

                VertexPositionInputs input = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = input.positionCS;
                o.positionWS = input.positionWS;

                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                return o;
            }

            [maxvertexcount(3)]
            void geom(triangle Varyings input[3], inout TriangleStream<Varyings> output)
            {
                float3 offset = 0;
                for (int i = 0; i < 3; i++)
                {
                    float3 dir = input[i].positionWS - _TargetPosition;
                    dir.y = 0;
                    offset += input[i].normalWS * max(_OffsetDistance - length(dir), 0) * _OffsetStrength;
                }
                offset /= 3;

                for (int j = 0; j < 3; j++)
                {
                    float2 uv = float2(input[j].positionCS.x, input[j].positionCS.y + _Time.y);

                    float interferenceOffset = SAMPLE_TEXTURE2D_LOD(_InterferenceNoise, sampler_InterferenceNoise, uv, 0).r * 2.0 - 1.0;
                    interferenceOffset *= _InterferenceStrength;

                    uv = TRANSFORM_TEX(uv, _TrianglesMoveNoise);
                    float2 trianglesMoveOffset = SAMPLE_TEXTURE2D_LOD(_TrianglesMoveNoise, sampler_TrianglesMoveNoise, uv, 0).rg * 2.0 - 1.0;
                    trianglesMoveOffset *= _TrianglesMoveStrength * saturate(length(offset));

                    Varyings v = (Varyings) 0;
                    v.positionWS = input[j].positionWS + offset;
                    v.positionCS = TransformWorldToHClip(v.positionWS) + float4(interferenceOffset, 0, 0, 0) + float4(trianglesMoveOffset, 0, 0);
                    v.normalWS = input[j].normalWS;
                    v.viewDirWS = input[j].viewDirWS;
                    output.Append(v);
                }

                output.RestartStrip();
            }

            half4 frag (Varyings i) : SV_Target
            {
                half3 color = lerp(_ColorSide, _ColorFront, pow(max(dot(i.normalWS, i.viewDirWS), 0), _FalloffStrength));
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
