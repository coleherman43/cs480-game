using UnityEngine;

public class robotDroneMovement : MonoBehaviour
{
    public float hoverHeight = 2.25f;
    public float bobAmplitude = 0.3f;
    public float bobSpeed = 1.75f;
    public float moveDistance = 2f;
    public float moveSpeed = 0.4f;

    Vector3 basePosition;

    void Awake()
    {
        basePosition = transform.position;
    }

    void Start()
    {
        ApplyHoverPosition();
    }

    void Update()
    {
        ApplyHoverPosition();
    }

    void ApplyHoverPosition()
    {
        float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        float xOffset = Mathf.Sin(Time.time * moveSpeed) * moveDistance;

        transform.position = new Vector3(
            basePosition.x + xOffset,
            hoverHeight + bobOffset,
            basePosition.z
        );
    }
}

