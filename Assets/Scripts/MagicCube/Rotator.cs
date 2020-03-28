using UnityEngine;

public class Rotator : MonoBehaviour
{
    private Vector2 lastMouseCoord;
    
    private void Awake()
    {
        lastMouseCoord = Input.mousePosition;
    }

    void Update()
    {
        Vector2 mouseCoord = Input.mousePosition;

        transform.RotateAround(transform.position, -Camera.main.transform.up, mouseCoord.x - lastMouseCoord.x);
        transform.RotateAround(transform.position, Camera.main.transform.right, mouseCoord.y - lastMouseCoord.y);

        lastMouseCoord = mouseCoord;
    }
}
