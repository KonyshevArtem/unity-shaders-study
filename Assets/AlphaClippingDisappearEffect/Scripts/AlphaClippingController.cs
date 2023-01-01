using UnityEngine;

[ExecuteInEditMode]
public class AlphaClippingController : MonoBehaviour
{
    private static readonly int CUTOFF_PROP_ID = Shader.PropertyToID("_Cutoff");
    private static readonly int PREV_FRAME_CUTOFF_PROP_ID = Shader.PropertyToID("_PrevFrameCutoff");

    [SerializeField, Range(0, 1)] private float m_Cutoff;

    private Renderer m_Renderer;
    private MaterialPropertyBlock m_PropertyBlock;
    private float m_PrevFrameCutoff;

    public float Cutoff
    {
        get => m_Cutoff;
        set => m_Cutoff = value;
    }

    public Renderer Renderer => m_Renderer;

    void Awake()
    {
        m_Renderer = GetComponent<Renderer>();
        m_PropertyBlock = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (m_PropertyBlock != null && m_Renderer != null)
        {
            m_PropertyBlock.SetFloat(PREV_FRAME_CUTOFF_PROP_ID, m_PrevFrameCutoff);
            m_PropertyBlock.SetFloat(CUTOFF_PROP_ID, m_Cutoff);
            m_Renderer.SetPropertyBlock(m_PropertyBlock);

            m_PrevFrameCutoff = m_Cutoff;
        }
    }

    private void OnWillRenderObject()
    {
        AlphaClippingRenderFeature.AddController(this);
    }
}