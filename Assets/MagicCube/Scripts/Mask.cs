using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class Mask : MonoBehaviour
{
    [SerializeField] int m_MaskID;
    [SerializeField] Material m_MaskMaterial;

    Renderer m_Renderer;

    void Awake()
    {
        Init();
    }

    void Init()
    {
        m_Renderer = GetComponent<Renderer>();
        Material material = new Material(m_MaskMaterial);
        material.SetInt("_MaskId", m_MaskID);
        m_Renderer.material = material;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        Init();
    }
#endif
}
