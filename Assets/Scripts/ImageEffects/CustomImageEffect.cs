using UnityEngine;

[ExecuteInEditMode]
public class CustomImageEffect : MonoBehaviour
{
    [SerializeField] private Material ImageEffectMaterial;

    void Awake()
    {
        GetComponent<Camera>().depthTextureMode = DepthTextureMode.Depth;
    }
    
    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (ImageEffectMaterial != null)
            Graphics.Blit(src, dest, ImageEffectMaterial);
        else
            Graphics.Blit(src, dest);
    }
}