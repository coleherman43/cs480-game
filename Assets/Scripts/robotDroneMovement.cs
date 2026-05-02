using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class robotDroneMovement : MonoBehaviour
{
    public float hoverHeight = 2.25f;
    public float bobAmplitude = 0.2f;
    public float bobSpeed = 1.75f;
    public float moveDistanceCenter = 15f;
    public float moveDistanceVariation = 5f;
    public float moveSpeed = 8f;
    public float acceleration = 9f;
    public float deceleration = 12f;
    public float chaseSpeed = 10f;
    public float chaseTurnSpeed = 240f;
    public float bobDuration = 3f;
    public float tiltAngle = 16f;     // Forward pitch (degrees) while moving
    public float rotateSpeed = 80f;   // Degrees/sec for Y turn and pitch return

    [Header("Audio")]
    public AudioClip robotMovesClip;
    public AudioClip robotNoticesClip;
    public AudioClip robotLosesClip;

    [Header("Detection")]
    public Transform player;
    public float detectionRange = 50f;
    public float fovAngle = 60f;
    public LayerMask obstacleMask;

    enum DroneState { Bobbing, Settling, Moving, RotatingBack, Chasing }

    DroneState currentState;
    float stateTimer;
    float bobPhaseOffset;
    Vector3 moveTarget;
    bool playerInSight;

    float facingAngleY;
    float targetFacingAngleY;
    float currentPitch;
    float currentMoveSpeed;
    AudioSource audioSource;

    const float settleThreshold = 0.005f;
    const float angleThreshold = 0.5f;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        currentState = DroneState.Bobbing;
        stateTimer = 0f;
        bobPhaseOffset = 0f;
        facingAngleY = transform.eulerAngles.y;
        targetFacingAngleY = facingAngleY;
        currentPitch = 0f;
        currentMoveSpeed = 0f;
    }

    void Start()
    {
        transform.position = new Vector3(transform.position.x, hoverHeight, transform.position.z);
    }

    void Update()
    {
        CheckPlayerDetection();

        switch (currentState)
        {
            case DroneState.Bobbing:
                UpdateBobbing();
                break;
            case DroneState.Settling:
                UpdateSettling();
                break;
            case DroneState.Moving:
                UpdateMoving();
                break;
            case DroneState.RotatingBack:
                UpdateRotatingBack();
                break;
            case DroneState.Chasing:
                UpdateChasing();
                break;
        }
    }

    void UpdateBobbing()
    {
        float bobOffset = Mathf.Sin((Time.time - bobPhaseOffset) * bobSpeed) * bobAmplitude;
        transform.position = new Vector3(transform.position.x, hoverHeight + bobOffset, transform.position.z);

        facingAngleY = Mathf.MoveTowardsAngle(facingAngleY, targetFacingAngleY, rotateSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, facingAngleY, 0f);

        stateTimer += Time.deltaTime;
        if (stateTimer >= bobDuration)
        {
            stateTimer = 0f;
            currentState = DroneState.Settling;
        }
    }

    void UpdateSettling()
    {
        float newY = Mathf.MoveTowards(transform.position.y, hoverHeight, moveSpeed * 2f * Time.deltaTime);
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        facingAngleY = Mathf.MoveTowardsAngle(facingAngleY, targetFacingAngleY, rotateSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, facingAngleY, 0f);

        bool ySettled = Mathf.Abs(newY - hoverHeight) < settleThreshold;
        bool rotSettled = Mathf.Abs(Mathf.DeltaAngle(facingAngleY, targetFacingAngleY)) < angleThreshold;

        if (ySettled && rotSettled)
        {
            facingAngleY = targetFacingAngleY;
            transform.position = new Vector3(transform.position.x, hoverHeight, transform.position.z);
            transform.rotation = Quaternion.Euler(0f, facingAngleY, 0f);

            float legDistance = Mathf.Max(0.1f, Random.Range(moveDistanceCenter - moveDistanceVariation, moveDistanceCenter + moveDistanceVariation));
            Vector3 moveDir = Quaternion.Euler(0f, facingAngleY, 0f) * Vector3.forward;
            moveTarget = new Vector3(
                transform.position.x + moveDir.x * legDistance,
                hoverHeight,
                transform.position.z + moveDir.z * legDistance
            );

            currentMoveSpeed = 0f;
            PlaySfx(robotMovesClip);
            currentState = DroneState.Moving;
        }
    }

    void UpdateMoving()
    {
        // Face and pitch in the real travel direction for fast-forward movement feel.
        Vector3 flatToTarget = new Vector3(
            moveTarget.x - transform.position.x,
            0f,
            moveTarget.z - transform.position.z
        );
        float distanceToTarget = flatToTarget.magnitude;
        if (distanceToTarget > 0.0001f)
        {
            Vector3 moveDir = flatToTarget.normalized;
            facingAngleY = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;

            // Brake as we near destination so speed eases down instead of snapping to stop.
            float desiredSpeed = Mathf.Min(moveSpeed, distanceToTarget * deceleration);
            float speedStep = (desiredSpeed >= currentMoveSpeed ? acceleration : deceleration) * Time.deltaTime;
            currentMoveSpeed = Mathf.MoveTowards(currentMoveSpeed, desiredSpeed, speedStep);

            float stepDistance = Mathf.Min(currentMoveSpeed * Time.deltaTime, distanceToTarget);
            Vector3 nextPos = transform.position + moveDir * stepDistance;
            transform.position = new Vector3(nextPos.x, hoverHeight, nextPos.z);
        }

        currentPitch = Mathf.MoveTowards(currentPitch, tiltAngle, rotateSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(currentPitch, facingAngleY, 0f);

        Vector3 flatPos = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 flatTarget = new Vector3(moveTarget.x, 0f, moveTarget.z);
        if (Vector3.Distance(flatPos, flatTarget) < 0.01f)
        {
            currentMoveSpeed = 0f;
            currentState = DroneState.RotatingBack;
        }
    }

    void UpdateRotatingBack()
    {
        currentPitch = Mathf.MoveTowards(currentPitch, 0f, rotateSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(currentPitch, facingAngleY, 0f);

        if (Mathf.Abs(currentPitch) < 0.5f)
        {
            currentPitch = 0f;
            transform.rotation = Quaternion.Euler(0f, facingAngleY, 0f);

            float turn = Random.Range(60f, 300f);
            if (Random.value > 0.5f)
            {
                turn = -turn;
            }
            targetFacingAngleY = facingAngleY + turn;

            stateTimer = 0f;
            bobPhaseOffset = Time.time;
            currentState = DroneState.Bobbing;
        }
    }

    void UpdateChasing()
    {
        if (player == null)
        {
            ResumePatrolFromChase();
            return;
        }

        Vector3 toPlayer = new Vector3(
            player.position.x - transform.position.x,
            0f,
            player.position.z - transform.position.z
        );

        float flatDistance = toPlayer.magnitude;
        if (flatDistance > 0.001f)
        {
            Vector3 chaseDir = toPlayer.normalized;
            float targetYaw = Mathf.Atan2(chaseDir.x, chaseDir.z) * Mathf.Rad2Deg;
            facingAngleY = Mathf.MoveTowardsAngle(facingAngleY, targetYaw, chaseTurnSpeed * Time.deltaTime);

            currentMoveSpeed = Mathf.MoveTowards(currentMoveSpeed, chaseSpeed, acceleration * Time.deltaTime);
            float stepDistance = Mathf.Min(currentMoveSpeed * Time.deltaTime, flatDistance);

            Vector3 nextPos = transform.position + chaseDir * stepDistance;
            transform.position = new Vector3(nextPos.x, hoverHeight, nextPos.z);
        }
        else
        {
            currentMoveSpeed = Mathf.MoveTowards(currentMoveSpeed, 0f, deceleration * Time.deltaTime);
        }

        currentPitch = Mathf.MoveTowards(currentPitch, tiltAngle, rotateSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(currentPitch, facingAngleY, 0f);
    }

    void ResumePatrolFromChase()
    {
        currentMoveSpeed = 0f;
        targetFacingAngleY = facingAngleY;
        stateTimer = 0f;
        bobPhaseOffset = Time.time;
        currentState = DroneState.Bobbing;
    }

    void CheckPlayerDetection()
    {
        if (player == null)
        {
            return;
        }

        Vector3 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;
        float angle = Vector3.Angle(transform.forward, toPlayer);

        bool nowInSight = false;
        if (distance <= detectionRange && angle <= fovAngle * 0.5f)
        {
            if (!Physics.Raycast(transform.position, toPlayer.normalized, distance, obstacleMask))
            {
                nowInSight = true;
            }
        }

        if (nowInSight && !playerInSight)
        {
            PlaySfx(robotNoticesClip);
            Debug.Log("Drone spotted the player!");
            currentState = DroneState.Chasing;
        }
        else if (!nowInSight && playerInSight)
        {
            PlaySfx(robotLosesClip);
            Debug.Log("Drone lost sight of the player.");
            if (currentState == DroneState.Chasing)
            {
                ResumePatrolFromChase();
            }
        }

        playerInSight = nowInSight;
    }

    void PlaySfx(AudioClip clip)
    {
        if (clip == null || audioSource == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip);
    }
}
