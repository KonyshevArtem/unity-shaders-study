using UnityEngine;

public class PathTracingObject : MonoBehaviour
{
    [SerializeField] private Color m_Color;
    [SerializeField, Range(0, 1)] private float m_Smoothness;
    [SerializeField, Min(0)] private float m_Emission;

    public Vector3 Position => transform.position;
    public float Size => transform.localScale.x;
    public Vector4 Color => m_Color;
    public float Smoothness => m_Smoothness;
    public float Emission => m_Emission;
}