using UnityEngine;

public class TestPlayerController : MonoBehaviour
{
    public float speed = 5f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        float moveX = Input.GetAxis("Horizontal"); // arrow keys / A,D
        float moveZ = Input.GetAxis("Vertical");   // arrow keys / W,S

        Vector3 movement = new Vector3(moveX, 0f, moveZ);
        rb.linearVelocity = new Vector3(movement.x * speed, rb.linearVelocity.y, movement.z * speed);
    }
}
