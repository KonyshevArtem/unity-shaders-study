using UnityEngine;

namespace VolumetricEffects.Scripts
{
    [ExecuteInEditMode]
    public class VolumetricEffect : MonoBehaviour
    {
        public Renderer Renderer { get; private set; }
        public Material Material { get; private set; }

        private void Awake()
        {
            Renderer = GetComponent<Renderer>();
            Material = Renderer.sharedMaterial;
        }
    }
}