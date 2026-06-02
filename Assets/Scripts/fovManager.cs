using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SpeedFOV : MonoBehaviour
{
    [Header("References")]
    public Rigidbody playerRb;

    [Header("FOV Settings")]
    public float baseFOV = 90f;
    public float speedFactor = 0.75f;
    public float maxBonusFOV = 20f;

    [Header("Speed Averaging")]
    [Range(1, 30)]
    public int averageFrames = 5;

    private Camera cam;

    private float[] speedSamples;
    private int sampleIndex;

    void Start()
    {
        cam = GetComponent<Camera>();

        speedSamples = new float[averageFrames];

        cam.fieldOfView = baseFOV;
    }

    void LateUpdate()
    {
        if (playerRb == null)
            return;

        // Horizontal velocity only
        Vector3 vel = playerRb.linearVelocity;
        vel.y = 0f;

        float currentSpeed = vel.magnitude;

        // Store sample
        speedSamples[sampleIndex] = currentSpeed;
        sampleIndex = (sampleIndex + 1) % averageFrames;

        // Running average
        float averageSpeed = 0f;
        for (int i = 0; i < speedSamples.Length; i++)
        {
            averageSpeed += speedSamples[i];
        }
        averageSpeed /= speedSamples.Length;

        cam.fieldOfView = baseFOV +
                          Mathf.Clamp(averageSpeed * speedFactor,
                                      0f,
                                      maxBonusFOV);
    }
}