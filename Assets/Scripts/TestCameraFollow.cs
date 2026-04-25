using UnityEngine;

public class TestCameraFollow : MonoBehaviour
{
    public Transform target;

    public Vector3 offset = new Vector3(0, 5, -8);
    
    void LateUpdate()
    {
        transform.position = target.position + offset;
    }
}
