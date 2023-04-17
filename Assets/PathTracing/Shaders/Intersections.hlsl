#ifndef UNITY_SHADER_STUDY_INTERSECTIONS
#define UNITY_SHADER_STUDY_INTERSECTIONS

bool SphereIntersection(float3 pos, float3 dir, float4 positionRadius, out float3 position, out float3 normal, out float distance)
{
    float3 sphereToCam = positionRadius.xyz - pos;
    float dirDotVector = dot(dir, sphereToCam);
    float radius = positionRadius.w * 0.5f;
    float det = dirDotVector * dirDotVector - (dot(sphereToCam, sphereToCam) - radius * radius);
    if (det < 0)
    {
        return false;
    }

    float sqrtDet = sqrt(det);
    distance = min(dirDotVector + sqrtDet, dirDotVector - sqrtDet);
    if (distance < 0)
    {
        return false;
    }

    position = pos + dir * distance;
    normal = position - positionRadius.xyz;
    return true;
}

bool TriangleIntersection(float3 pos, float3 dir, float3 v0, float3 v1, float3 v2, out float3 position, out float3 normal, out float distance)
{
    //https://www.scratchapixel.com/lessons/3d-basic-rendering/ray-tracing-rendering-a-triangle/ray-triangle-intersection-geometric-solution.html
    
    // compute the plane's normal
    float3 v0v1 = v1 - v0;
    float3 v0v2 = v2 - v0;
    // no need to normalize
    normal = cross(v0v1, v0v2); // N
 
    // Step 1: finding P
    
    // check if the ray and plane are parallel.
    float NdotRayDirection = dot(normal, dir);
    if (abs(NdotRayDirection) < 0.0001) // almost 0
        return false; // they are parallel, so they don't intersect! 

    // compute d parameter using equation 2
    float d = -dot(normal, v0);
    
    // compute t (equation 3)
    distance = -(dot(normal, pos) + d) / NdotRayDirection;
    
    // check if the triangle is behind the ray
    if (distance < 0)
        return false; // the triangle is behind
 
    // compute the intersection point using equation 1
    position = pos + distance * dir;
 
    // Step 2: inside-outside test
    float3 C; // vector perpendicular to triangle's plane
 
    // edge 0
    float3 edge0 = v1 - v0; 
    float3 vp0 = position - v0;
    C = cross(edge0, vp0);
    if (dot(normal, C) < 0) return false; // P is on the right side
 
    // edge 1
    float3 edge1 = v2 - v1; 
    float3 vp1 = position - v1;
    C = cross(edge1, vp1);
    if (dot(normal, C) < 0)  return false; // P is on the right side
 
    // edge 2
    float3 edge2 = v0 - v2; 
    float3 vp2 = position - v2;
    C = cross(edge2, vp2);
    if (dot(normal, C) < 0) return false; // P is on the right side;

    return true; // this ray hits the triangle
}

bool BoundingBoxIntersection(float3 boundsMin, float3 boundsMax, float3 pos, float3 dir)
{
    //https://gamedev.stackexchange.com/a/18459

    float3 dirfrac;
    dirfrac.x = 1.0f / dir.x;
    dirfrac.y = 1.0f / dir.y;
    dirfrac.z = 1.0f / dir.z;

    float t1 = (boundsMin.x - pos.x) * dirfrac.x;
    float t2 = (boundsMax.x - pos.x) * dirfrac.x;
    float t3 = (boundsMin.y - pos.y) * dirfrac.y;
    float t4 = (boundsMax.y - pos.y) * dirfrac.y;
    float t5 = (boundsMin.z - pos.z) * dirfrac.z;
    float t6 = (boundsMax.z - pos.z) * dirfrac.z;

    float tmin = max(max(min(t1, t2), min(t3, t4)), min(t5, t6));
    float tmax = min(min(max(t1, t2), max(t3, t4)), max(t5, t6));

    return tmax >= 0 && tmin <= tmax;
}	

#endif