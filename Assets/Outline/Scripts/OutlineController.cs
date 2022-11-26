using UnityEngine;

[ExecuteInEditMode]
public class OutlineController : MonoBehaviour
{
    private static readonly int OUTLINE_STRENGTH_PROP_ID = Shader.PropertyToID("_OutlineStrength");
    private static readonly int OUTLINE_COLOR_PROP_ID = Shader.PropertyToID("_OutlineColor");

    [SerializeField] private float m_OutlineStrength;
    [SerializeField] private Color m_OutlineColor;

    void Awake()
    {
        SetOutlineProps();
    }

    void SetOutlineProps()
    {
        Shader.SetGlobalFloat(OUTLINE_STRENGTH_PROP_ID, m_OutlineStrength);
        Shader.SetGlobalColor(OUTLINE_COLOR_PROP_ID, m_OutlineColor);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        SetOutlineProps();
    }
#endif

    void OnGUI()
    {
        if (GUILayout.Button("Disable outline"))
        {
            DisableOutline();
        }

        if (GUILayout.Button("Enable backface outline"))
        {
            DisableOutline();
            SetBackfaceOutline(true);
        }
    }

    void DisableOutline()
    {
        SetBackfaceOutline(false);
    }

    void SetBackfaceOutline(bool _Enabled)
    {
        BackfaceOutlineRenderFeature.Enabled = _Enabled;
    }
}