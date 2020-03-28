using System;
using UnityEngine;

[ExecuteInEditMode]
public class BoxBlur : MonoBehaviour
{
    [SerializeField] private Material boxBlurMaterial;
    [SerializeField] private int iterations;
    [SerializeField] private int downScaleFactor;
    
    private static readonly int IterationsProperty = Shader.PropertyToID("_Iterations");

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (boxBlurMaterial == null || iterations <= 0 || downScaleFactor <= 0)
        {
            Graphics.Blit(src, dest);
            return;
        }

        RenderTexture blurred = new RenderTexture(src.width / downScaleFactor, src.height / downScaleFactor, src.depth);
        Graphics.Blit(src, blurred);
        
        for (int i = 0; i < iterations; ++i)
        {
            RenderTexture tmp = new RenderTexture(blurred);
            Graphics.Blit(blurred, tmp, boxBlurMaterial);
            blurred.Release();
            blurred = tmp;
        }
        
        Graphics.Blit(blurred, dest, boxBlurMaterial);
        blurred.Release();
    }
}
