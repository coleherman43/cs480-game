using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class robotDroneMovement : MonoBehaviour
{
    public float hoverHeight = 2.05f;
    public float hoverHeightCenter = 7.25f;
    public float hoverHeightVariance = 7.75f;
    public float bobAmplitude = 0.2f;
    public float bobSpeed = 1.75f;
    public float moveDistanceCenter = 15f;
    public float moveDistanceVariation = 5f;
    public float moveSpeed = 8f;
    public float acceleration = 9f;
    public float deceleration = 12f;
    public float chaseSpeed = 10f;
    public float chaseTurnSpeed = 240f;
    public float chaseVerticalSpeed = 5f;
    public bool matchPlayerSpeedWhileChasing = true;
    public float minChaseSpeed = 3f;
    public float bobDuration = 3f;
    public float tiltAngle = 16f;     // Downward pitch magnitude (degrees) while moving
    public float rotateSpeed = 80f;   // Degrees/sec for Y turn and pitch return

    [Header("Audio")]
    public AudioClip robotMovesClip;
    public AudioClip robotNoticesClip;
    public AudioClip robotLosesClip;

    [Header("Detection")]
    public Transform player;
    public float detectionRange = 50f;
    public float fovAngle = 60f;
    public float wallDetectionRange = 10f;
    public LayerMask obstacleMask;

    [Header("Chase Lose Timer")]
    public float chaseLoseDuration = 5f;
    public bool reloadSceneOnLose = false;

    [Header("HUD")]
    public bool showChaseHud = true;

    [Header("FOV Cone Visual")]
    public bool showFovCone = true;
    public Color fovConeColor = new Color(0.2f, 0.6f, 1f, 0.22f);
    public float fovConeLengthMultiplier = 1f;
    public int fovConeSegments = 24;
    public bool anchorConeToDroneFront = true;
    public float fovConeYOffset = 0f;
    public float fovConeForwardOffset = 0.05f;
    public float sightedAlphaBoost = 0.12f;

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
    float chaseTimerRemaining;
    bool playerLost;
    float currentTargetHoverHeight;
    AudioSource audioSource;
    MeshFilter fovConeMeshFilter;
    MeshRenderer fovConeMeshRenderer;
    Material fovConeMaterial;

    float lastConeRange;
    float lastConeAngle;
    int lastConeSegments;

    const float settleThreshold = 0.005f;
    const float angleThreshold = 0.5f;

    float GetHeightMin() => hoverHeightCenter - hoverHeightVariance;
    float GetHeightMax() => hoverHeightCenter + hoverHeightVariance;

    float ClampHeight(float y) => Mathf.Clamp(y, GetHeightMin(), GetHeightMax());

    float PickRandomHoverHeight() => Random.Range(GetHeightMin(), GetHeightMax());

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
        chaseTimerRemaining = chaseLoseDuration;
        playerLost = false;
        currentTargetHoverHeight = PickRandomHoverHeight();

        RemoveLegacyFovLight();
        SetupFovCone();
    }

    void Start()
    {
        transform.position = new Vector3(transform.position.x, ClampHeight(hoverHeight), transform.position.z);
    }

    void Update()
    {
        if (playerLost)
        {
            return;
        }

        CheckPlayerDetection();
        UpdateChaseLoseTimer();

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

        UpdateFovCone();
    }

    void RemoveLegacyFovLight()
    {
        Transform existing = transform.Find("DroneFovLight");
        if (existing != null)
        {
            Destroy(existing.gameObject);
        }
    }

    void SetupFovCone()
    {
        Transform existing = transform.Find("DroneFovCone");
        if (existing != null)
        {
            fovConeMeshFilter = existing.GetComponent<MeshFilter>();
            fovConeMeshRenderer = existing.GetComponent<MeshRenderer>();
        }

        if (fovConeMeshFilter == null || fovConeMeshRenderer == null)
        {
            GameObject coneObject = new GameObject("DroneFovCone");
            coneObject.transform.SetParent(transform, false);
            fovConeMeshFilter = coneObject.AddComponent<MeshFilter>();
            fovConeMeshRenderer = coneObject.AddComponent<MeshRenderer>();
        }

        fovConeMaterial = CreateConeMaterial();
        if (fovConeMaterial != null)
        {
            fovConeMeshRenderer.sharedMaterial = fovConeMaterial;
        }

        lastConeRange = -1f;
        lastConeAngle = -1f;
        lastConeSegments = -1;
        RebuildFovConeMesh();
    }

    Material CreateConeMaterial()
    {
        Shader coneShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (coneShader == null)
        {
            coneShader = Shader.Find("Unlit/Color");
        }
        if (coneShader == null)
        {
            coneShader = Shader.Find("Sprites/Default");
        }
        if (coneShader == null)
        {
            return null;
        }

        Material material = new Material(coneShader);
        material.renderQueue = 3000;
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }
        if (material.HasProperty("_Cull"))
        {
            material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
        }

        ApplyConeColor(material, fovConeColor);
        return material;
    }

    void ApplyConeColor(Material material, Color color)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }

    void RebuildFovConeMesh()
    {
        if (fovConeMeshFilter == null)
        {
            return;
        }

        float coneRange = Mathf.Max(0.1f, detectionRange * Mathf.Max(0.01f, fovConeLengthMultiplier));
        float coneAngle = Mathf.Clamp(fovAngle, 1f, 179f);
        int segments = Mathf.Clamp(fovConeSegments, 8, 64);

        if (Mathf.Approximately(coneRange, lastConeRange) && Mathf.Approximately(coneAngle, lastConeAngle) && segments == lastConeSegments)
        {
            return;
        }

        float radius = Mathf.Tan(0.5f * coneAngle * Mathf.Deg2Rad) * coneRange;

        Vector3[] vertices = new Vector3[segments + 2];
        int apexIndex = 0;
        int baseCenterIndex = segments + 1;
        vertices[apexIndex] = Vector3.zero;

        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / segments;
            float theta = t * Mathf.PI * 2f;
            float x = Mathf.Cos(theta) * radius;
            float y = Mathf.Sin(theta) * radius;
            vertices[i + 1] = new Vector3(x, y, coneRange);
        }
        vertices[baseCenterIndex] = new Vector3(0f, 0f, coneRange);

        int[] triangles = new int[segments * 12];
        int tri = 0;
        for (int i = 0; i < segments; i++)
        {
            int current = i + 1;
            int next = ((i + 1) % segments) + 1;

            // Cone sides (double-sided).
            triangles[tri++] = apexIndex;
            triangles[tri++] = current;
            triangles[tri++] = next;
            triangles[tri++] = apexIndex;
            triangles[tri++] = next;
            triangles[tri++] = current;

            // Cone base cap (double-sided).
            triangles[tri++] = baseCenterIndex;
            triangles[tri++] = next;
            triangles[tri++] = current;
            triangles[tri++] = baseCenterIndex;
            triangles[tri++] = current;
            triangles[tri++] = next;
        }

        Mesh coneMesh = new Mesh();
        coneMesh.name = "DroneFovConeMesh";
        coneMesh.vertices = vertices;
        coneMesh.triangles = triangles;
        coneMesh.RecalculateNormals();
        coneMesh.RecalculateBounds();

        fovConeMeshFilter.sharedMesh = coneMesh;
        lastConeRange = coneRange;
        lastConeAngle = coneAngle;
        lastConeSegments = segments;
    }

    void UpdateFovCone()
    {
        if (fovConeMeshFilter == null || fovConeMeshRenderer == null)
        {
            return;
        }

        RebuildFovConeMesh();

        Transform coneTransform = fovConeMeshFilter.transform;
        coneTransform.localPosition = GetFovConeLocalAnchor();
        coneTransform.localRotation = Quaternion.identity;
        fovConeMeshRenderer.enabled = showFovCone;

        Color visibleColor = fovConeColor;
        if (playerInSight)
        {
            visibleColor.a = Mathf.Clamp01(visibleColor.a + sightedAlphaBoost);
        }

        ApplyConeColor(fovConeMaterial, visibleColor);
    }

    Vector3 GetFovConeLocalAnchor()
    {
        if (!anchorConeToDroneFront)
        {
            return new Vector3(0f, fovConeYOffset, fovConeForwardOffset);
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        bool hasBounds = false;
        Bounds localBounds = new Bounds(Vector3.zero, Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer == fovConeMeshRenderer)
            {
                continue;
            }

            Bounds worldBounds = renderer.bounds;
            Vector3 min = worldBounds.min;
            Vector3 max = worldBounds.max;

            Vector3[] corners = new Vector3[8]
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, max.y, max.z)
            };

            for (int c = 0; c < corners.Length; c++)
            {
                Vector3 localCorner = transform.InverseTransformPoint(corners[c]);
                if (!hasBounds)
                {
                    localBounds = new Bounds(localCorner, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    localBounds.Encapsulate(localCorner);
                }
            }
        }

        if (!hasBounds)
        {
            return new Vector3(0f, fovConeYOffset, fovConeForwardOffset);
        }

        return new Vector3(
            localBounds.center.x,
            localBounds.center.y + fovConeYOffset,
            localBounds.max.z + fovConeForwardOffset
        );
    }

    void OnDestroy()
    {
        if (fovConeMaterial != null)
        {
            Destroy(fovConeMaterial);
        }
    }

    void UpdateChaseLoseTimer()
    {
        if (player == null)
        {
            chaseTimerRemaining = chaseLoseDuration;
            return;
        }

        if (playerInSight)
        {
            chaseTimerRemaining -= Time.deltaTime;
            if (chaseTimerRemaining <= 0f)
            {
                chaseTimerRemaining = 0f;
                TriggerPlayerLoss();
            }
        }
        else
        {
            chaseTimerRemaining = chaseLoseDuration;
        }
    }

    void UpdateBobbing()
    {
        float bobOffset = Mathf.Sin((Time.time - bobPhaseOffset) * bobSpeed) * bobAmplitude;
        float targetY = currentTargetHoverHeight + bobOffset;
        float clampedY = ClampHeight(targetY);
        clampedY = ClampHeightToWalls(clampedY);
        transform.position = new Vector3(transform.position.x, clampedY, transform.position.z);

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
        float newY = Mathf.MoveTowards(transform.position.y, currentTargetHoverHeight, moveSpeed * 2f * Time.deltaTime);
        newY = ClampHeight(newY);
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        facingAngleY = Mathf.MoveTowardsAngle(facingAngleY, targetFacingAngleY, rotateSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, facingAngleY, 0f);

        bool ySettled = Mathf.Abs(newY - currentTargetHoverHeight) < settleThreshold;
        bool rotSettled = Mathf.Abs(Mathf.DeltaAngle(facingAngleY, targetFacingAngleY)) < angleThreshold;

        if (ySettled && rotSettled)
        {
            facingAngleY = targetFacingAngleY;
            transform.position = new Vector3(transform.position.x, ClampHeight(currentTargetHoverHeight), transform.position.z);
            transform.rotation = Quaternion.Euler(0f, facingAngleY, 0f);

            Vector3 settleDir = Quaternion.Euler(0f, facingAngleY, 0f) * Vector3.forward;
            if (IsWallAheadInDirection(settleDir, wallDetectionRange))
            {
                float redirectTurn = Random.Range(60f, 300f);
                if (Random.value > 0.5f) redirectTurn = -redirectTurn;
                targetFacingAngleY = facingAngleY + redirectTurn;
                stateTimer = 0f;
                bobPhaseOffset = Time.time;
                currentState = DroneState.Bobbing;
                return;
            }

            float legDistance = Mathf.Max(0.1f, Random.Range(moveDistanceCenter - moveDistanceVariation, moveDistanceCenter + moveDistanceVariation));
            Vector3 moveDir = Quaternion.Euler(0f, facingAngleY, 0f) * Vector3.forward;
            moveTarget = new Vector3(
                transform.position.x + moveDir.x * legDistance,
                ClampHeight(currentTargetHoverHeight),
                transform.position.z + moveDir.z * legDistance
            );

            currentMoveSpeed = 0f;
            PlaySfx(robotMovesClip);
            currentState = DroneState.Moving;
        }
    }

    void UpdateMoving()
    {
        if (IsWallAheadInDirection(transform.forward, wallDetectionRange))
        {
            currentMoveSpeed = 0f;
            currentTargetHoverHeight = PickRandomHoverHeight();
            stateTimer = 0f;
            bobPhaseOffset = Time.time;
            currentState = DroneState.Bobbing;
            return;
        }

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
            nextPos.y = ClampHeight(currentTargetHoverHeight);
            transform.position = nextPos;
        }

        float targetPitch = -Mathf.Abs(tiltAngle);
        currentPitch = Mathf.MoveTowards(currentPitch, targetPitch, rotateSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(currentPitch, facingAngleY, 0f);

        Vector3 flatPos = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 flatTarget = new Vector3(moveTarget.x, 0f, moveTarget.z);
        if (Vector3.Distance(flatPos, flatTarget) < 0.01f)
        {
            currentMoveSpeed = 0f;
            currentTargetHoverHeight = PickRandomHoverHeight();
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

            float candidateAngle = facingAngleY;
            int maxAttempts = 8;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                float turn = Random.Range(60f, 300f);
                if (Random.value > 0.5f) turn = -turn;
                candidateAngle = facingAngleY + turn;
                Vector3 candidateDir = Quaternion.Euler(0f, candidateAngle, 0f) * Vector3.forward;
                if (!IsWallAheadInDirection(candidateDir, wallDetectionRange))
                    break;
            }
            targetFacingAngleY = candidateAngle;

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

        Vector3 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;

        if (distance > 0.001f)
        {
            Vector3 chaseDir = toPlayer.normalized;

            // Face the player both horizontally and vertically.
            float targetYaw   = Mathf.Atan2(chaseDir.x, chaseDir.z) * Mathf.Rad2Deg;
            float targetPitch = -Mathf.Asin(Mathf.Clamp(chaseDir.y, -1f, 1f)) * Mathf.Rad2Deg;
            facingAngleY = Mathf.MoveTowardsAngle(facingAngleY, targetYaw, chaseTurnSpeed * Time.deltaTime);
            currentPitch = Mathf.MoveTowards(currentPitch, targetPitch, rotateSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(currentPitch, facingAngleY, 0f);

            // Move directly toward the player.
            float targetChaseSpeed = chaseSpeed;
            if (matchPlayerSpeedWhileChasing)
            {
                targetChaseSpeed = Mathf.Max(minChaseSpeed, GetPlayerFlatSpeed());
            }

            currentMoveSpeed = Mathf.MoveTowards(currentMoveSpeed, targetChaseSpeed, acceleration * Time.deltaTime);
            float step = Mathf.Min(currentMoveSpeed * Time.deltaTime, distance);
            transform.position += chaseDir * step;
        }
        else
        {
            currentMoveSpeed = Mathf.MoveTowards(currentMoveSpeed, 0f, deceleration * Time.deltaTime);
        }
    }

    float GetPlayerFlatSpeed()
    {
        if (player == null)
        {
            return chaseSpeed;
        }

        PlayerController playerController = player.GetComponent<PlayerController>();

        chaseSpeed = playerController.runSpeed;

        return chaseSpeed;
    }

    void ResumePatrolFromChase()
    {
        currentMoveSpeed = 0f;
        targetFacingAngleY = facingAngleY;
        stateTimer = 0f;
        bobPhaseOffset = Time.time;
        currentState = DroneState.Bobbing;
    }

    bool IsWallAheadInDirection(Vector3 direction, float distance)
    {
        if (direction.sqrMagnitude < 0.0001f) return false;
        return Physics.Raycast(transform.position, direction.normalized, distance, obstacleMask);
    }

    float ClampHeightToWalls(float desiredY)
    {
        float currentY = transform.position.y;
        float dy = desiredY - currentY;
        if (Mathf.Abs(dy) < 0.0001f) return desiredY;
        Vector3 dir = dy > 0f ? Vector3.up : Vector3.down;
        if (Physics.Raycast(transform.position, dir, out RaycastHit hit, Mathf.Abs(dy) + 0.05f, obstacleMask))
        {
            return currentY + dir.y * Mathf.Max(0f, hit.distance - 0.05f);
        }
        return desiredY;
    }

    void CheckPlayerDetection()
    {
        if (player == null)
        {
            return;
        }

        Vector3 toPlayer = player.position - transform.position;
        Vector3 flatToPlayer = new Vector3(toPlayer.x, 0f, toPlayer.z);
        Vector3 flatForward = new Vector3(transform.forward.x, 0f, transform.forward.z);
        float distance = toPlayer.magnitude;
        float angle = flatToPlayer.sqrMagnitude < 0.0001f || flatForward.sqrMagnitude < 0.0001f
            ? 0f
            : Vector3.Angle(flatForward, flatToPlayer);

        bool nowInSight = false;
        if (distance <= detectionRange && angle <= fovAngle * 0.5f)
        {
            nowInSight = true;
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

    void TriggerPlayerLoss()
    {
        if (playerLost)
        {
            return;
        }

        playerLost = true;
        Debug.Log("Player caught: detected by drone for 5 seconds. Add future game over logic here.");
    }

    void OnGUI()
    {
        if (!showChaseHud || !playerInSight)
        {
            return;
        }

        float timerPercent = Mathf.Clamp01(1f - (chaseTimerRemaining / Mathf.Max(0.01f, chaseLoseDuration)));
        float barWidth = Mathf.Min(420f, Screen.width - 24f);
        Rect dangerBarRect = new Rect((Screen.width - barWidth) * 0.5f, 24f, barWidth, 16f);
        GUI.color = new Color(0.08f, 0.08f, 0.08f, 0.8f);
        GUI.DrawTexture(dangerBarRect, Texture2D.whiteTexture);

        Rect fillRect = dangerBarRect;
        fillRect.width = dangerBarRect.width * timerPercent;
        GUI.color = Color.Lerp(new Color(0.96f, 0.89f, 0.24f, 1f), new Color(0.95f, 0.18f, 0.16f, 1f), timerPercent);
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);

        GUI.color = Color.white;
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
