using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    CharacterController controller;
    Vector3 move;
    Vector3 input;
    Vector3 Yvelocity;
    Vector3 forwardDirection;
    float speed;
    public float runSpeed;
    public float airSpeed;
    public float crouchSpeed;
    public float sprintSpeed;
    public Transform groundCheck;
    int jumpCharges;
    public LayerMask groundMask;
    bool isGrounded;
    bool isCrouching;
    bool isSprinting;
    bool isSliding;
    public float jumpHeight;
    float startHeight;
    float crouchHeight = 0.5f;
    float gravity;
    public float normalGravity;
    Vector3 crouchingCenter = new Vector3(0, 0.5f, 0);
    Vector3 standingCenter = new Vector3(0, 0, 0);
    float slideTimer;
    public float maxSlideTimer;
    public float slideSpeedIncrease;
    public float slideSpeedDecrease;
    


    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<CharacterController>();
        startHeight = transform.localScale.y;
    }

    void HandleInput()
    {
        float x = Keyboard.current.dKey.isPressed ? 1f :
                Keyboard.current.aKey.isPressed ? -1f : 0f;
        float z = Keyboard.current.wKey.isPressed ? 1f :
                Keyboard.current.sKey.isPressed ? -1f : 0f;
        input = new Vector3(x, 0f, z);
        input = transform.TransformDirection(input);
        input = Vector3.ClampMagnitude(input, 1f);

        if (Keyboard.current.spaceKey.wasPressedThisFrame && jumpCharges > 0)
        {
            Jump();
        }
        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            Crouch();
        }
        if (Keyboard.current.cKey.wasReleasedThisFrame)
        {
            ExitCrouch();
        }
        if (Keyboard.current.leftShiftKey.wasPressedThisFrame && isGrounded)
        {
            isSprinting = true;
        }
        if (Keyboard.current.leftShiftKey.wasReleasedThisFrame)
        {
            isSprinting = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        HandleInput();
        if (isGrounded && !isSliding)
        {
            GroundedMovement();
        }
        else if (!isGrounded)
        {
            AirMovement();
        }
        else if (isSliding)
        {
            SlideMovement();
            DecreaseSpeed(slideSpeedDecrease);
            slideTimer -= 1f * Time.deltaTime;
            if (slideTimer < 0)
            {
                isSliding = false;
            }
        }

        CheckGround();
        controller.Move(move * Time.deltaTime);
        ApplyGravity();
    }

    void GroundedMovement()
    {
        speed = isSprinting ? sprintSpeed : isCrouching ? crouchSpeed : runSpeed;
        if (input.x != 0)
        {
            move.x += input.x * speed;
        }
        else
        {
            move.x = 0;
        }
        if (input.z != 0)
        {
            move.z += input.z * speed;
        }
        else
        {
            move.z = 0;
        }
        move = Vector3.ClampMagnitude(move, speed);
    }

    void AirMovement()
    {
        move.x += input.x * airSpeed;
        move.z += input.z * airSpeed;
        move = Vector3.ClampMagnitude(move, speed);
    }

    void CheckGround()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, 0.2f, groundMask);
        if (isGrounded)
        {
            jumpCharges = 1;
        }
    }

    void ApplyGravity()
    {
        gravity = normalGravity;
        Yvelocity.y += gravity * Time.deltaTime;
        controller.Move(Yvelocity * Time.deltaTime);
    }

    void Jump()
    {
        Yvelocity.y = Mathf.Sqrt(jumpHeight * -2f * normalGravity);
    }

    void Crouch()
    {
        controller.height = crouchHeight;
        controller.center = crouchingCenter;
        transform.localScale = new Vector3(transform.localScale.x, crouchHeight, transform.localScale.z);
        isCrouching = true;
        if (speed > runSpeed)
        {
            isSliding = true;
            forwardDirection = transform.forward;
            if (isGrounded)
            {
                IncreaseSpeed(slideSpeedIncrease);
            }
            slideTimer = maxSlideTimer;
        }
    }

    void ExitCrouch()
    {
        controller.height = (startHeight * 2);
        controller.center = standingCenter;
        transform.localScale = new Vector3(transform.localScale.x, startHeight, transform.localScale.z);
        isCrouching = false;
        isSliding = false;
    }

    void IncreaseSpeed(float speedIncrease)
    {
        speed += speedIncrease;
    }

    void DecreaseSpeed(float speedDecrease)
    {
        speed -= speedDecrease * Time.deltaTime;
    }

    void SlideMovement()
    {
        move += forwardDirection;
        move = Vector3.ClampMagnitude(move, speed);
    }
}
