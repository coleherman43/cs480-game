using UnityEngine;

public class DynamicFOV : MonoBehaviour
{
    public Camera playerCamera;

    [Header("FOV Settings")]
    public float normalFOV = 75f;
    public float sprintFOV = 90f;
    public float slideFOV = 100f;

    [Header("Transition")]
    public float smoothSpeed = 8f;

    private float targetFOV;

    public PlayerMovement pMov;
    void Start()
    {
        targetFOV = normalFOV;
    }

    void Update()
    {
        // Replace these with your actual movement checks
        if (pMov.isSliding)
        {
            targetFOV = slideFOV;
        }
        else if (pMov.isSprinting)
        {
            targetFOV = sprintFOV;
        }
        else
        {
            targetFOV = normalFOV;
        }

        // Smooth transition
        playerCamera.fieldOfView = Mathf.Lerp(
            playerCamera.fieldOfView,
            targetFOV,
            Time.deltaTime * smoothSpeed
        );
    }

}