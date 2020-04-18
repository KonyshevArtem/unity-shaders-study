using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SimpleController : MonoBehaviour
{
    [SerializeField] private Camera camera;
    [SerializeField] private float speed;

    private new Rigidbody rigidbody;

    void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
    }

    void Update()
    {
        ControlLocomotion();
        ControlCamera();
    }

    void ControlLocomotion()
    {
        Vector3 velocity =
            (transform.right * Input.GetAxis("Horizontal") + transform.forward * Input.GetAxis("Vertical")) * speed;
        velocity.y = rigidbody.velocity.y;
        rigidbody.velocity = velocity;
    }

    void ControlCamera()
    {
        transform.Rotate(Vector3.up, Input.GetAxis("Mouse X"));

        float cameraAngle = Vector3.SignedAngle(transform.forward, camera.transform.forward, transform.right);
        cameraAngle = Mathf.Clamp(cameraAngle - Input.GetAxis("Mouse Y"), -80, 80);
        camera.transform.localRotation = Quaternion.AngleAxis(cameraAngle, Vector3.right);
    }
}