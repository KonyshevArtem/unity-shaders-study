using UnityEngine;

[RequireComponent(typeof(Renderer)), ExecuteInEditMode]
public class DivisionEffectPosition : MonoBehaviour
{
    [SerializeField] Transform m_Target;

    Renderer m_Renderer;
    MaterialPropertyBlock m_Properties;

    static readonly int TARGET_POSITION_PROP_ID = Shader.PropertyToID("_TargetPosition");

    void Awake()
    {
        m_Renderer = GetComponent<Renderer>();
        m_Properties = new MaterialPropertyBlock();
    }

    void Update()
    {
        m_Properties.SetVector(TARGET_POSITION_PROP_ID, m_Target.position);
        m_Renderer.SetPropertyBlock(m_Properties);
    }
}
