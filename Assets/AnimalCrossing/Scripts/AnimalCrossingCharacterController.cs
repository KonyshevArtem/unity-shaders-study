using UnityEngine;

public class AnimalCrossingCharacterController : MonoBehaviour
{
    [SerializeField] float m_Speed = 0.2f;

    void Update()
    {
        transform.position += new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")) * m_Speed * Time.deltaTime;
    }
}
