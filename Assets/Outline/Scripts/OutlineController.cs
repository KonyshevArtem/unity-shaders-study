using UnityEngine;

[ExecuteInEditMode]
public class OutlineController : MonoBehaviour
{
    private static readonly int OUTLINE_STRENGTH_PROP_ID = Shader.PropertyToID("_OutlineStrength");
    private static readonly int OUTLINE_COLOR_PROP_ID = Shader.PropertyToID("_OutlineColor");
    private static readonly int DEPTH_NORMAL_PARAMS_COLOR_PROP_ID = Shader.PropertyToID("_DepthNormalOutlineParams");

    [Header("Backface Outline Settings")]
    [SerializeField] private float m_OutlineStrength;
    [SerializeField] private Color m_OutlineColor;

    [Space, Header("Depth Normal Outline Settings")] 
    [SerializeField] private float m_NormalMultiplier = 1;
    [SerializeField] private float m_NormalBias = 1;
    [SerializeField] private float m_DepthMultiplier = 1;
    [SerializeField] private float m_DepthBias = 1;

    void Awake()
    {
        SetOutlineProps();
    }

    void SetOutlineProps()
    {
        Shader.SetGlobalFloat(OUTLINE_STRENGTH_PROP_ID, m_OutlineStrength);
        Shader.SetGlobalColor(OUTLINE_COLOR_PROP_ID, m_OutlineColor);
        Shader.SetGlobalVector(DEPTH_NORMAL_PARAMS_COLOR_PROP_ID, new Vector4(m_NormalMultiplier, m_NormalBias, m_DepthMultiplier, m_DepthBias));
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

        if (GUILayout.Button("Enable depth normals outline"))
        {
            DisableOutline();
            SetDepthNormalOutline(true);
        }
    }

    static void DisableOutline()
    {
        SetBackfaceOutline(false);
        SetDepthNormalOutline(false);
    }

    static void SetBackfaceOutline(bool _Enabled)
    {
        BackfaceOutlineRenderFeature.Enabled = _Enabled;
    }

    static void SetDepthNormalOutline(bool _Enabled)
    {
        DepthNormalOutlineRenderFeature.Enabled = _Enabled;
    }
}