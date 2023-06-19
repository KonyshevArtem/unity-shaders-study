#ifndef BOX_INTERSECTION_HLSL
#define BOX_INTERSECTION_HLSL

void boxIntersection(in float3 rayOrigin, in float3 rayDirection, out float3 nearIntersection, out float3 farIntersection)
{
    float3 rd = mul(UNITY_MATRIX_I_M, float4(rayDirection, 0)).xyz;
    float3 ro = mul(UNITY_MATRIX_I_M, float4(rayOrigin, 1)).xyz;
    half3 rad = half3(0.5, 0.5, 0.5); // half size of the cube

    float tN, tF;
    {
        // https://iquilezles.org/articles/boxfunctions/

        float3 m = 1.0 / rd;
        float3 n = m * ro;
        float3 k = abs(m) * rad;
        float3 t1 = -n - k;
        float3 t2 = -n + k;

        tN = max(max(t1.x, t1.y), t1.z);
        tF = min(min(t2.x, t2.y), t2.z);

        //if (tN > tF || tF < 0.0) return float2(-1.0, -1.0); // no intersection

        //oN = -sign(rd) * step(t1.yzx, t1.xyz) * step(t1.zxy, t1.xyz);
    }

    // if the rayOrigin is inside the box then it is equal to nearIntersection point
    tN = max(tN, 0);

    nearIntersection = mul(UNITY_MATRIX_M, float4(ro + tN * rd, 1)).xyz;
    farIntersection = mul(UNITY_MATRIX_M, float4(ro + tF * rd, 1)).xyz;
}

#endif
