using System;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct PathTracingObject
{
    public float4 PositionWS;
    public float4 Color;
    [Range(0, 1)] public float Smoothness;
}

public class PathTracingController : MonoBehaviour
{
    public Cubemap Skybox;
    public PathTracingObject[] Objects;
}