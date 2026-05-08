using UnityEngine;
using System.Collections;

public class ComputerRoomPlayerMovement : MonoBehaviour
{
    public CharacterController controller;

    public bool inputLocked = false;

    public float walkSpeed = 5f;
    public float sprintSpeed = 9f;
    public float gravity = -9.81f;

    public float mouseSensitivity;

    private float verticalVelocity;
    // private float xRotation = 0f;

    public float minX = -60f;
    public float maxX = 60f;
    public Camera computerRoomCamera;
    float rotY = 0f;
    float rotX = 0f;

    void Start()
    {
        computerRoomCamera = GetComponentInChildren<Camera>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (inputLocked) 
        {
            return;
        }

        HandleMouseLook();
        HandleMovement();
    }

    void HandleMouseLook()
    {
        rotY += UnityEngine.InputSystem.Mouse.current.delta.x.ReadValue() * mouseSensitivity * Time.deltaTime;
        rotX += UnityEngine.InputSystem.Mouse.current.delta.y.ReadValue() * mouseSensitivity * Time.deltaTime;
        rotX = Mathf.Clamp(rotX, minX, maxX);
        transform.localEulerAngles = new Vector3(0, rotY, 0);
        computerRoomCamera.transform.localEulerAngles = new Vector3(-rotX, 0, 0);
    }

    void HandleMovement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;

        Vector3 move = transform.right * x + transform.forward * z;

        controller.Move(move * currentSpeed * Time.deltaTime);

        if (controller.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 gravityMove = Vector3.up * verticalVelocity;

        controller.Move(gravityMove * Time.deltaTime);
    }
}
