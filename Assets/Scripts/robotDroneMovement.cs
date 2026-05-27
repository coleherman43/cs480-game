using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class robotDroneMovement : MonoBehaviour
{
    public float hoverHeight = 2.05f;
    public float hoverHeightCenter = 7.25f;
    public float hoverHeightVariance = 8.75f;
    public float bobAmplitude = 0.2f;
    public float bobSpeed = 1.75f;
    public float moveSpeed = 2.5f;
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
    public float detectionRange = 15f;
    public float fovAngle = 70f;
    public float wallDetectionRange = 5f;
    public LayerMask obstacleMask;

    [Header("Chase Lose Timer")]
    public float chaseLoseDuration = 5f;
    public bool reloadSceneOnLose = false;

    [Header("Coin Patrol")]
    public string coinObjectName = "Coin (Clone)";
    public float checkStopDistance = 10f;

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

    enum DroneState { Checking, Bobbing, Chasing }

    DroneState currentState;
    float stateTimer;
    float bobPhaseOffset;
    bool playerInSight;

    float facingAngleY;
    float targetFacingAngleY;
    float currentPitch;
    float currentMoveSpeed;
    float chaseTimerRemaining;
    bool playerLost;
    float currentTargetHoverHeight;
    float currentHoverY;
    List<Transform> knownCoins = new List<Transform>();
    float coinRefreshTimer;
    Transform currentCoin;
    AudioSource audioSource;
    MeshFilter fovConeMeshFilter;
    MeshRenderer fovConeMeshRenderer;
    Material fovConeMaterial;

    float lastConeRange;
    float lastConeAngle;
    int lastConeSegments;

    // Persistent bypass direction for the Checking state — recomputed only when
    // the stored path becomes blocked, preventing frame-to-frame oscillation.
    Vector3 checkingBypassDir;
    bool    checkingBypassActive;
    float GetHeightMin() => hoverHeightCenter - hoverHeightVariance;
    float GetHeightMax() => hoverHeightCenter + hoverHeightVariance;

    float ClampHeight(float y) => Mathf.Clamp(y, GetHeightMin(), GetHeightMax());

    float PickRandomHoverHeight() => Random.Range(GetHeightMin(), GetHeightMax());

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        currentState = DroneState.Checking;
        stateTimer = 0f;
        bobPhaseOffset = 0f;
        facingAngleY = transform.eulerAngles.y;
        targetFacingAngleY = facingAngleY;
        currentPitch = 0f;
        currentMoveSpeed = 0f;
        chaseTimerRemaining = chaseLoseDuration;
        playerLost = false;
        currentTargetHoverHeight = PickRandomHoverHeight();
        currentHoverY = ClampHeight(hoverHeight);

        RemoveLegacyFovLight();
        SetupFovCone();
    }

    void Start()
    {
        transform.position = new Vector3(transform.position.x, ClampHeight(hoverHeight), transform.position.z);
        currentHoverY = transform.position.y;
        FindAllCoins();
        currentCoin = PickRandomCoin();
        if (currentCoin != null)
            SetFacingToward(currentCoin.position);
    }

    void Update()
    {
        if (playerLost)
        {
            return;
        }

        CheckPlayerDetection();
        UpdateChaseLoseTimer();

        coinRefreshTimer -= Time.deltaTime;
        if (coinRefreshTimer <= 0f)
        {
            FindAllCoins();
            coinRefreshTimer = 5f;
        }

        switch (currentState)
        {
            case DroneState.Checking:
                UpdateChecking();
                break;
            case DroneState.Bobbing:
                UpdateCurrentHoverY();
                UpdateBobbing();
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

    void UpdateCurrentHoverY()
    {
        currentHoverY = Mathf.MoveTowards(currentHoverY, currentTargetHoverHeight, chaseVerticalSpeed * Time.deltaTime);
        currentHoverY = ClampHeight(currentHoverY);
        currentHoverY = ClampHeightToWalls(currentHoverY);
    }

    void UpdateBobbing()
    {
        float bobOffset = Mathf.Sin((Time.time - bobPhaseOffset) * bobSpeed) * bobAmplitude;
        float targetY = currentHoverY + bobOffset;
        float clampedY = ClampHeight(targetY);
        clampedY = ClampHeightToWalls(clampedY);
        transform.position = new Vector3(transform.position.x, clampedY, transform.position.z);

        if (currentCoin != null)
            SetFacingToward(currentCoin.position);

        facingAngleY = Mathf.MoveTowardsAngle(facingAngleY, targetFacingAngleY, rotateSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, facingAngleY, 0f);

        stateTimer += Time.deltaTime;
        if (stateTimer >= bobDuration)
        {
            currentCoin = PickRandomCoin();
            stateTimer = 0f;
            checkingBypassActive = false;
            currentState = DroneState.Checking;
        }
    }

    void UpdateChecking()
    {
        if (currentCoin == null)
        {
            currentCoin = PickRandomCoin();
            if (currentCoin == null) return;
        }

        // Skip coins that are above the drone's reachable height (e.g. sky coins).
        if (currentCoin.position.y > GetHeightMax() + 2f)
        {
            currentCoin = PickRandomCoin();
            checkingBypassActive = false;
            if (currentCoin == null) return;
        }

        Vector3 toTarget = currentCoin.position - transform.position;
        float dist = toTarget.magnitude;

        // Stop when within range and the coin is inside the FOV.
        if (dist <= checkStopDistance && IsCoinInFov(currentCoin))
        {
            currentMoveSpeed = 0f;
            currentPitch = 0f;
            checkingBypassActive = false;
            currentHoverY = transform.position.y;
            currentTargetHoverHeight = currentHoverY;
            bobPhaseOffset = Time.time;
            stateTimer = 0f;
            currentState = DroneState.Bobbing;
            return;
        }

        // Persistent steering: only recompute the bypass direction when the direct
        // path opens up (resume) or the current bypass becomes blocked.
        Vector3 desiredDir = toTarget.normalized;
        bool directClear = !Physics.Raycast(transform.position, desiredDir, wallDetectionRange, obstacleMask);

        Vector3 steerDir;
        if (directClear)
        {
            checkingBypassActive = false;
            steerDir = desiredDir;
        }
        else
        {
            if (!checkingBypassActive ||
                Physics.Raycast(transform.position, checkingBypassDir, 1.5f, obstacleMask))
            {
                checkingBypassDir    = ComputeSteeringDirection(desiredDir);
                checkingBypassActive = true;
            }
            steerDir = checkingBypassDir;
        }

        // Rotate horizontally to face the steering direction.
        Vector3 flatSteer = new Vector3(steerDir.x, 0f, steerDir.z);
        if (flatSteer.sqrMagnitude > 0.0001f)
        {
            float targetYaw = Mathf.Atan2(flatSteer.x, flatSteer.z) * Mathf.Rad2Deg;
            facingAngleY = Mathf.MoveTowardsAngle(facingAngleY, targetYaw, chaseTurnSpeed * Time.deltaTime);
        }

        // Pitch in the vertical direction of travel.
        float targetPitch = -Mathf.Asin(Mathf.Clamp(steerDir.y, -1f, 1f)) * Mathf.Rad2Deg;
        currentPitch = Mathf.MoveTowards(currentPitch, targetPitch, rotateSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(currentPitch, facingAngleY, 0f);

        // Accelerate.
        currentMoveSpeed = Mathf.MoveTowards(currentMoveSpeed, moveSpeed, acceleration * Time.deltaTime);
        Vector3 moveStep = steerDir * currentMoveSpeed * Time.deltaTime;

        // Horizontal movement: slide along walls instead of stopping dead.
        Vector3 hStep = new Vector3(moveStep.x, 0f, moveStep.z);
        if (hStep.magnitude > 0.0001f &&
            Physics.Raycast(transform.position, hStep.normalized, out RaycastHit hHit, hStep.magnitude + 0.15f, obstacleMask))
        {
            Vector3 wallNormal = new Vector3(hHit.normal.x, 0f, hHit.normal.z).normalized;
            hStep = Vector3.ProjectOnPlane(hStep, wallNormal);
            checkingBypassActive = false; // recompute next frame now that we hit a wall
        }

        // Vertical movement: standard height clamping.
        float nextY = Mathf.MoveTowards(transform.position.y,
            transform.position.y + moveStep.y, chaseVerticalSpeed * Time.deltaTime);
        nextY = ClampHeight(nextY);
        nextY = ClampHeightToWalls(nextY);

        transform.position = new Vector3(
            transform.position.x + hStep.x,
            nextY,
            transform.position.z + hStep.z);
    }

    // Probes yaw/pitch offsets from desiredDir and returns the clearest path most
    // aligned with the original target direction.
    Vector3 ComputeSteeringDirection(Vector3 desiredDir)
    {
        if (desiredDir.sqrMagnitude < 0.0001f) return Vector3.forward;

        // Fast path: desired direction is clear.
        if (!Physics.Raycast(transform.position, desiredDir, wallDetectionRange, obstacleMask))
            return desiredDir;

        Quaternion toDesired = Quaternion.LookRotation(desiredDir, Vector3.up);
        float[] yawOffsets   = {  0f,  45f, -45f,  90f, -90f, 135f, -135f, 180f };
        float[] pitchOffsets = {  0f,  30f, -30f,  60f, -60f };

        Vector3 bestDir   = -desiredDir;  // worst-case fallback
        float   bestScore = float.NegativeInfinity;

        foreach (float yaw in yawOffsets)
        {
            foreach (float pitch in pitchOffsets)
            {
                Vector3 candidate = toDesired * Quaternion.Euler(-pitch, yaw, 0f) * Vector3.forward;
                candidate.Normalize();
                if (!Physics.Raycast(transform.position, candidate, wallDetectionRange, obstacleMask))
                {
                    // Small continuity bias toward the current facing direction prevents
                    // oscillation between symmetric alternatives (e.g. ±45°).
                    float score = Vector3.Dot(candidate, desiredDir)
                        + 0.1f * Vector3.Dot(candidate, transform.forward);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestDir   = candidate;
                    }
                }
            }
        }

        return bestDir;
    }

    bool IsCoinInFov(Transform coin)
    {
        if (coin == null) return false;
        Vector3 toCoin   = coin.position - transform.position;
        Vector3 flatCoin = new Vector3(toCoin.x, 0f, toCoin.z);
        Vector3 flatFwd  = new Vector3(transform.forward.x, 0f, transform.forward.z);
        if (flatCoin.sqrMagnitude < 0.0001f || flatFwd.sqrMagnitude < 0.0001f) return true;
        return Vector3.Angle(flatFwd, flatCoin) <= fovAngle * 0.5f;
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
            Vector3 chaseNextPos = transform.position + chaseDir * step;
            float chaseNewY = Mathf.MoveTowards(transform.position.y, chaseNextPos.y, chaseVerticalSpeed * Time.deltaTime);
            chaseNewY = ClampHeightToWalls(chaseNewY);
            transform.position = new Vector3(chaseNextPos.x, chaseNewY, chaseNextPos.z);
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
        currentCoin = PickRandomCoin();
        stateTimer = 0f;
        currentState = DroneState.Checking;
    }

    void FindAllCoins()
    {
        knownCoins.Clear();
        // Match pre-placed coins ("Coin (18)", "Coin (39)"…) as well as
        // spawner clones ("Coin (Clone)") by checking for the base name.
        string baseName = coinObjectName.Replace(" (Clone)", "").Trim();
        GameObject[] all = Object.FindObjectsOfType<GameObject>();
        for (int i = 0; i < all.Length; i++)
        {
            string n = all[i].name;
            if (n == baseName || n.StartsWith(baseName + " ("))
                knownCoins.Add(all[i].transform);
        }
    }

    Transform PickRandomCoin()
    {
        // Remove any coins that have been destroyed.
        for (int i = knownCoins.Count - 1; i >= 0; i--)
        {
            if (knownCoins[i] == null)
                knownCoins.RemoveAt(i);
        }
        if (knownCoins.Count == 0)
        {
            FindAllCoins();
            if (knownCoins.Count == 0) return null;
        }
        if (knownCoins.Count == 1) return knownCoins[0];

        // Prefer a coin other than the one we just visited.
        int start = Random.Range(0, knownCoins.Count);
        for (int i = 0; i < knownCoins.Count; i++)
        {
            Transform candidate = knownCoins[(start + i) % knownCoins.Count];
            if (candidate != currentCoin) return candidate;
        }
        return knownCoins[start];
    }

    void SetFacingToward(Vector3 worldPos)
    {
        Vector3 dir = worldPos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
            targetFacingAngleY = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
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
        //Debug.Log("Player caught: detected by drone for 5 seconds. Add future game over logic here.");
        GameEvents.gameOver?.Invoke(false);    
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
