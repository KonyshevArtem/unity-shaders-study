using UnityEngine;

[ExecuteInEditMode]
public class AnimalCrossingBounds : MonoBehaviour
{
    const float BOUNDS_SCALE = 10;

    MeshRenderer m_Renderer;

    void Awake()
    {
        m_Renderer = GetComponent<MeshRenderer>();
        if (m_Renderer == null)
            return;

        Bounds bounds = m_Renderer.localBounds;
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;
        extents.x *= BOUNDS_SCALE;
        extents.z *= BOUNDS_SCALE;
        center.z -= (BOUNDS_SCALE - 1) * 0.5f;
        bounds.center = center;
        bounds.extents = extents;
        m_Renderer.localBounds = bounds;
    }

    void OnDestroy()
    {
        if (m_Renderer != null)
            m_Renderer.ResetLocalBounds();    
    }

    void OnDrawGizmosSelected()
    {
        if (m_Renderer == null)
            return;

        Matrix4x4 m = Gizmos.matrix;
        Color color = Gizmos.color;
        Gizmos.color = Color.red;
        Gizmos.matrix = transform.localToWorldMatrix;

        Bounds bounds = m_Renderer.localBounds;
        Gizmos.DrawWireCube(bounds.center, bounds.size);

        Gizmos.matrix = m;
        Gizmos.color = color;
    }
}
