using UnityEngine;

[ExecuteInEditMode]
public class ChromaticAberration : MonoBehaviour
{
    [SerializeField] private Material material;
    [SerializeField] private Vector2 redOffset;
    [SerializeField] private Vector2 greenOffset;
    [SerializeField] private Vector2 blueOffset;
    
    private static readonly int RedOffset = Shader.PropertyToID("_RedOffset");
    private static readonly int GreenOffset = Shader.PropertyToID("_GreenOffset");
    private static readonly int BlueOffset = Shader.PropertyToID("_BlueOffset");

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (material == null)
        {
            Graphics.Blit(src, dest);
            return;
        }
        
        material.SetVector(RedOffset, redOffset);
        material.SetVector(GreenOffset, greenOffset);
        material.SetVector(BlueOffset, blueOffset);
        
        Graphics.Blit(src, dest, material);
    }
}
