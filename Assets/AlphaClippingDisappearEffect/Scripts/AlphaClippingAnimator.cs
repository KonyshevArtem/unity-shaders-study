using UnityEngine;

public class AlphaClippingAnimator : MonoBehaviour
{
    [SerializeField] private AlphaClippingController m_Controller;
    [SerializeField] private float m_Speed; 
    
    void Update()
    {
        m_Controller.Cutoff = (m_Controller.Cutoff + m_Speed * Time.deltaTime) % 1;
    }
}
