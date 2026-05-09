using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Assignables")]
    public Transform playerCam;
    public Transform orientation;
    public Rigidbody rb;

    [Header("LayerMasks")]
    public LayerMask groundMask;
    public LayerMask wallMask;

    [Header("Movement")]
    public float moveSpeed = 4500f;
    public float walkSpeed = 20f;
    public float sprintSpeed = 30f;
    public float crouchSpeed = 10f;
    public float airMultiplier = 0.5f;

    [Header("Jumping")]
    public float jumpForce = 550f;
    private float jumpCooldown = 0.25f;
    private bool readyToJump = true;

    [Header("Sliding")]
    public float slideForce = 400f;
    public float slideSlowdown = 0.2f;
    private bool sliding;
    private Vector3 slideDirection;

    [Header("WallRunning")]
    public float wallRunGravity = 1f;
    public float wallRunJumpForce = 600f;
    private bool wallRunning;
    private bool readyToWallrun = true;
    private Vector3 wallNormalVector;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    private bool grounded;
    private Vector3 normalVector;

    // Private state
    private float x, y;
    private bool jumping, crouching, sprinting;
    private bool cancellingGrounded;
    private float desiredX;
    private float xRotation;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        wallNormalVector = Vector3.up;
    }

    void Update()
    {
        Look();
    }

    void FixedUpdate()
    {
        Movement();
        WallRunning();
    }

    void OnMove(InputValue movementValue)
    {
        Vector2 movementvector = movementValue.Get<Vector2>();

        x = movementvector.x;
        y = movementvector.y;
    }

    void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            jumping = true;
        }
        else
        {
            jumping = false;
        }
    }

    void OnCrouch(InputValue value)
    {
        if (value.isPressed)
        {
            crouching = true;
            StartCrouch();
        }
        else
        {
            crouching = false;
            StopCrouch();
        }
    }

    void Look()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float mouseX = mouseDelta.x * 0.1f;
        float mouseY = mouseDelta.y * 0.1f;

        desiredX += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        orientation.localRotation = Quaternion.Euler(0f, desiredX, 0f);
        transform.rotation = Quaternion.Euler(0f, desiredX, 0f);
    }

    void StartCrouch()
    {
        transform.localScale = new Vector3(1f, 0.5f, 1f);
        transform.position = new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z);
        if (rb.linearVelocity.magnitude > 0.5f && grounded)
        {
            sliding = true;
            slideDirection = orientation.forward;
            rb.AddForce(slideDirection * slideForce);
        }
    }

    void StopCrouch()
    {
        transform.localScale = new Vector3(1f, 1f, 1f);
        transform.position = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
        sliding = false;
    }

    void Movement()
    {
        // Extra downward force for better feel
        rb.AddForce(Vector3.down * Time.deltaTime * 10f);

        Vector2 mag = FindVelRelativeToLook();
        float xMag = mag.x, yMag = mag.y;

        CounterMovement(x, y, mag);

        if (readyToJump && jumping) Jump();

        // Cap speed
        float maxSpeed = sprinting ? sprintSpeed : crouching ? crouchSpeed : walkSpeed;

        if (x > 0 && xMag > maxSpeed) x = 0;
        if (x < 0 && xMag < -maxSpeed) x = 0;
        if (y > 0 && yMag > maxSpeed) y = 0;
        if (y < 0 && yMag < -maxSpeed) y = 0;

        float multiplier = grounded ? 1f : airMultiplier;
        float forwardMultiplier = multiplier;

        if (sliding && grounded)
        {
            rb.AddForce(Vector3.down * Time.deltaTime * 3000f);
            rb.AddForce(moveSpeed * Time.deltaTime * -rb.linearVelocity.normalized * slideSlowdown);
            return;
        }

        if (wallRunning)
        {
            multiplier = 0.3f;
            forwardMultiplier = 0.3f;
        }

        rb.AddForce(orientation.forward * y * moveSpeed * Time.deltaTime * forwardMultiplier);
        rb.AddForce(orientation.right * x * moveSpeed * Time.deltaTime * multiplier);
    }

    void Jump()
    {
        if ((grounded || wallRunning) && readyToJump)
        {
            readyToJump = false;
            Vector3 vel = rb.linearVelocity;

            rb.AddForce(Vector2.up * jumpForce * 1.5f);
            rb.AddForce(normalVector * jumpForce * 0.5f);

            if (rb.linearVelocity.y < 0.5f)
                rb.linearVelocity = new Vector3(vel.x, 0f, vel.z);
            else if (rb.linearVelocity.y > 0f)
                rb.linearVelocity = new Vector3(vel.x, vel.y / 2f, vel.z);

            if (wallRunning)
            {
                rb.AddForce(wallNormalVector * wallRunJumpForce);
                wallRunning = false;
            }

            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    void ResetJump() => readyToJump = true;

    void WallRunning()
    {
        if (wallRunning)
        {
            rb.AddForce(-wallNormalVector * Time.deltaTime * moveSpeed);
            rb.AddForce(Vector3.up * Time.deltaTime * rb.mass * 100f * wallRunGravity);
        }
    }

    void StartWallRun(Vector3 normal)
    {
        if (!grounded && readyToWallrun)
        {
            wallNormalVector = normal;
            if (!wallRunning)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                rb.AddForce(Vector3.up * 20f, ForceMode.Impulse);
            }
            wallRunning = true;
        }
    }

    void CounterMovement(float x, float y, Vector2 mag)
    {
        if (!grounded || jumping) return;
        float threshold = 0.01f;
        float counterForce = 0.5f;

        if (Mathf.Abs(mag.x) > threshold && Mathf.Abs(x) < 0.05f || (mag.x < -threshold && x > 0) || (mag.x > threshold && x < 0))
            rb.AddForce(moveSpeed * orientation.right * Time.deltaTime * -mag.x * counterForce);

        if (Mathf.Abs(mag.y) > threshold && Mathf.Abs(y) < 0.05f || (mag.y < -threshold && y > 0) || (mag.y > threshold && y < 0))
            rb.AddForce(moveSpeed * orientation.forward * Time.deltaTime * -mag.y * counterForce);

        // Speed cap
        float speed = Mathf.Sqrt(rb.linearVelocity.x * rb.linearVelocity.x + rb.linearVelocity.z * rb.linearVelocity.z);
        if (speed > walkSpeed)
        {
            float yVel = rb.linearVelocity.y;
            Vector3 capped = rb.linearVelocity.normalized * walkSpeed;
            rb.linearVelocity = new Vector3(capped.x, yVel, capped.z);
        }
    }

    Vector2 FindVelRelativeToLook()
    {
        float lookAngle = orientation.eulerAngles.y;
        float velAngle = Mathf.Atan2(rb.linearVelocity.x, rb.linearVelocity.z) * Mathf.Rad2Deg;
        float delta = Mathf.DeltaAngle(lookAngle, velAngle);
        float perpDelta = 90f - delta;
        float speed = rb.linearVelocity.magnitude;
        return new Vector2(speed * Mathf.Cos(delta * Mathf.Deg2Rad), speed * Mathf.Cos(perpDelta * Mathf.Deg2Rad));
    }

    void OnCollisionStay(Collision other)
    {
        int layer = other.gameObject.layer;

        for (int i = 0; i < other.contactCount; i++)
        {
            Vector3 normal = other.contacts[i].normal;

            // Floor
            if (Vector3.Angle(Vector3.up, normal) < 35f)
            {
                grounded = true;
                normalVector = normal;
                if (wallRunning) wallRunning = false;
                cancellingGrounded = false;
                CancelInvoke(nameof(StopGrounded));
            }

            // Wall - check it's on the wall layer
            if (Mathf.Abs(90f - Vector3.Angle(Vector3.up, normal)) < 0.1f)
            {
                if (((1 << layer) & wallMask) != 0)
                    StartWallRun(normal);
            }
        }

        if (!cancellingGrounded)
        {
            cancellingGrounded = true;
            Invoke(nameof(StopGrounded), Time.deltaTime * 3f);
        }
    }

    void StopGrounded() => grounded = false;
}