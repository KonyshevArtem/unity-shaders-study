using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public static class WorleyGenerator
{
    private const int k_Size = 64;
    private const GraphicsFormat k_GraphicsFormat = GraphicsFormat.R8_UNorm;

    [MenuItem("Tools/Generate Worley")]
    public static void GenerateWorley()
    {
        string guid = AssetDatabase.FindAssets($"WorleyGenerator t:{nameof(ComputeShader)}")[0];
        ComputeShader shader = AssetDatabase.LoadAssetAtPath<ComputeShader>(AssetDatabase.GUIDToAssetPath(guid));
        if (shader == null)
        {
            Debug.LogError("WorleyGenerator shader not found");
            return;
        }

        int kernelId = shader.FindKernel("CSMain");
        shader.GetKernelThreadGroupSizes(kernelId, out uint x, out uint y, out uint z);

        RenderTexture renderTexture = new RenderTexture(k_Size, k_Size, 0, k_GraphicsFormat)
        {
            dimension = TextureDimension.Tex3D,
            volumeDepth = k_Size,
            enableRandomWrite = true
        };

        CommandBuffer cmd = new CommandBuffer();

        cmd.SetRenderTarget(renderTexture);
        cmd.SetComputeTextureParam(shader, kernelId, "_Texture", renderTexture);
        cmd.SetComputeVectorParam(shader, "_TextureDimensions", new Vector4(k_Size, k_Size, k_Size, 0));
        cmd.DispatchCompute(shader, kernelId, k_Size / (int)x, k_Size / (int)y, k_Size / (int)z);
        cmd.RequestAsyncReadback(renderTexture, 0, 0, k_Size, 0, k_Size, 0, k_Size, ReadbackCallback);
        cmd.WaitAllAsyncReadbackRequests();

        Graphics.ExecuteCommandBuffer(cmd);
        cmd.Release();

        renderTexture.Release();
    }

    private static void ReadbackCallback(AsyncGPUReadbackRequest _Obj)
    {
        if (_Obj.hasError)
        {
            Debug.LogError("Error");
            return;
        }

        Texture3D worleyTexture = new Texture3D(k_Size, k_Size, k_Size, k_GraphicsFormat, TextureCreationFlags.None);
        NativeList<byte> pixelsData = new NativeList<byte>(_Obj.layerDataSize * _Obj.depth, Allocator.Temp);
        for (int i = 0; i < _Obj.depth; ++i)
        {
            pixelsData.AddRange(_Obj.GetData<byte>(i));
        }

        worleyTexture.SetPixelData(pixelsData.AsArray(), 0);
        worleyTexture.Apply(false, true);

        AssetDatabase.CreateAsset(worleyTexture, "Assets/VolumetricEffects/Textures/Worley.asset");
        AssetDatabase.SaveAssets();

        pixelsData.Dispose();
    }
}