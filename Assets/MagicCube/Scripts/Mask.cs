using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class Mask : MonoBehaviour
{
    [SerializeField] private int maskId;
    [SerializeField] private Color color;
    [SerializeField] private Material maskMaterial;

    private Renderer renderer;
    
    private void Awake()
    {
        renderer = GetComponent<Renderer>();
        Material material = new Material(maskMaterial);
        material.SetInt("_MaskId", maskId);
        renderer.material = material;
    }

    void Update()
    {
        if (renderer != null)
        {
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            properties.SetColor("_Color", color);
            renderer.SetPropertyBlock(properties);
        }
    }
}
