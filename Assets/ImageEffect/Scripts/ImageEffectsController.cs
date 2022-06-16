using UnityEngine;

public class ImageEffectsController : MonoBehaviour
{
    [Header("Displacement")]
    [SerializeField] bool m_DisplacementEnabled;
    [SerializeField] Texture m_DisplacementTexture;
    [SerializeField] float m_DisplacementMagnitude;

    [Header("Box Blur")]
    [SerializeField] bool m_BoxBlurEnabled;
    [SerializeField] uint m_BoxBlurIterations;
    [SerializeField, Range(0, 1)] float m_BoxBlurDownscale;

    [Header("Chromatic Abberation")]
    [SerializeField] bool m_ChromaticAbberationEnabled;
    [SerializeField] private Vector2 m_ChromaticAbberationRedOffset;
    [SerializeField] private Vector2 m_ChromaticAbberationGreenOffset;
    [SerializeField] private Vector2 m_ChromaticAbberationBlueOffset;


    public bool DisplacementEnabled => m_DisplacementEnabled;
    public Texture DisplacementTexture => m_DisplacementTexture;
    public float DisplacementMagnitude => m_DisplacementMagnitude;

    public bool BoxBlurEnabled => m_BoxBlurEnabled;
    public uint BoxBlurIterations => m_BoxBlurIterations;
    public float BoxBlurDownscale => m_BoxBlurDownscale;

    public bool ChromaticAbberationEnabled => m_ChromaticAbberationEnabled;
    public Vector2 ChromaticAbberationRedOffset => m_ChromaticAbberationRedOffset;
    public Vector2 ChromaticAbberationGreenOffset => m_ChromaticAbberationGreenOffset;
    public Vector2 ChromaticAbberationBlueOffset => m_ChromaticAbberationBlueOffset;

    public bool AnyEnabled => m_DisplacementEnabled || m_BoxBlurEnabled || m_ChromaticAbberationEnabled;

}
