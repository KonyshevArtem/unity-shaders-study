using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalID : MonoBehaviour
{
    [SerializeField, Range(1, 8)] int m_PortalID;
    [SerializeField] Material m_Material;

    void Awake()
    {
        Init();
    }

    void Init()
    {
        Renderer r = GetComponent<Renderer>();
        if (r == null)
            return;

        Material material = new Material(m_Material);
        material.SetInt("_PortalID", 1 << m_PortalID);
        r.sharedMaterial = material;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        Init();
    }
#endif
}
