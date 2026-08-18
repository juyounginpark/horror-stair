using System.Collections;
using System.Collections.Generic;
using Action = System.Action;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class JumpscareSequence : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField, Min(0f)] private float flickerDuration = 2.2f;
    [SerializeField, Min(0.01f)] private float flickerMinInterval = 0.05f;
    [SerializeField, Min(0.01f)] private float flickerMaxInterval = 0.16f;
    [SerializeField, Min(0f)] private float darkPauseMin = 1f;
    [SerializeField, Min(0f)] private float darkPauseMax = 2f;
    [SerializeField, Min(0.01f)] private float faceRevealDuration = 0.22f;
    [SerializeField, Min(0.01f)] private float blackoutFadeDuration = 0.08f;
    [SerializeField] private float blackScreenDuration = 2f;
    [SerializeField] private float fadeOutDuration = 0.35f;

    [Header("Placement")]
    [SerializeField] private JumpscareAnchor jumpscareAnchor;
    [SerializeField, Min(1f)] private float startDistance = 30f;
    [SerializeField, Min(0.5f)] private float minSilhouetteDistance = 2.5f;
    [SerializeField, Min(0.05f)] private float sphereCastRadius = 0.25f;
    [SerializeField, Min(0.05f)] private float faceDistance = 0.65f;
    [SerializeField, Min(0f)] private float wallClearance = 0.75f;
    [SerializeField] private LayerMask obstacleLayers = ~0;
    [SerializeField, Min(0.1f)] private float surfaceSearchHeight = 5f;
    [SerializeField, Min(0.1f)] private float surfaceSearchDistance = 12f;
    [SerializeField, Min(1f)] private float faceScaleMultiplier = 1f;

    [Header("Sound")]
    [SerializeField] private AudioClip jumpscareSound;
    [SerializeField, Range(0f, 1f)] private float soundVolume = 1f;

    [Header("Visibility")]
    [SerializeField] private Color backgroundLightColor = new(1f, 0.06f, 0.02f);
    [SerializeField, Min(0f)] private float backgroundLightIntensity = 18f;
    [SerializeField, Min(0.1f)] private float backgroundLightRange = 15f;
    [SerializeField, Range(1f, 179f)] private float backgroundLightAngle = 140f;
    [SerializeField] private Color backdropColor = new(0.42f, 0.015f, 0.012f);
    [SerializeField] private Color silhouetteColor = Color.black;
    [SerializeField] private Color faceLightColor = new(1f, 0.18f, 0.12f);
    [SerializeField, Min(0f)] private float faceLightIntensity = 10f;
    [SerializeField, Min(0.1f)] private float faceLightRange = 8f;
    [SerializeField] private Color faceMonsterColor = new(0.8f, 0.12f, 0.08f);

    [Header("Optional replacement monster")]
    [SerializeField] private Animator animator;
    [SerializeField] private string animationTrigger = "Jumpscare";

    [Header("Testing")]
    [SerializeField] private bool testWithJKey = true;
    [SerializeField] private bool playOnStart;

    private AudioSource audioSource;
    private AudioClip impactClip;
    private Camera playerCamera;
    private bool isPlaying;
    private Vector3 homePosition;
    private Quaternion homeRotation;
    private Vector3 homeScale;
    private Vector3 sequenceScale;
    private Quaternion cameraRotationBeforeJumpscare;
    private PlayerController playerController;
    private bool playerControllerWasEnabled;
    private bool playerRigidbodyWasKinematic;
    private Behaviour cameraDriver;
    private bool cameraDriverWasEnabled;
    private Light[] playerLights;
    private bool[] playerLightsWereEnabled;
#if ENABLE_INPUT_SYSTEM
    private PlayerInput playerInput;
    private bool playerInputWasEnabled;
    private bool playerInputWasActive;
#endif

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 1f;
        audioSource.ignoreListenerPause = true;
        impactClip = CreateImpactClip();
        homePosition = transform.position;
        homeRotation = transform.rotation;
        homeScale = transform.localScale;

        Renderer monsterRenderer = GetComponent<Renderer>();
        if (monsterRenderer != null)
        {
            MaterialPropertyBlock block = new();
            monsterRenderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", new Color(0.12f, 0.015f, 0.015f));
            block.SetColor("_Color", new Color(0.12f, 0.015f, 0.015f));
            monsterRenderer.SetPropertyBlock(block);
        }
    }

    private void Update()
    {
        if (!testWithJKey || isPlaying)
            return;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame)
            Play();
#else
        if (Input.GetKeyDown(KeyCode.J))
            Play();
#endif
    }

    private void Start()
    {
        if (playOnStart)
            Play();
    }

    // Call this from enemy detection, damage, or game-over code.
    public void Play()
    {
        if (!isPlaying)
            StartCoroutine(PlaySequence(null, null, null, null));
    }

    public void Play(PlayerController target, Action blackoutAction = null, Action completed = null, JumpscareAnchor customAnchor = null)
    {
        if (!isPlaying)
            StartCoroutine(PlaySequence(target, blackoutAction, completed, customAnchor));
    }

    private IEnumerator PlaySequence(PlayerController target, Action blackoutAction, Action completed, JumpscareAnchor customAnchor)
    {
        isPlaying = true;
        playerCamera = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();

        if (playerCamera == null)
        {
            Debug.LogError("JumpscareSequence requires a camera.");
            isPlaying = false;
            yield break;
        }

        cameraRotationBeforeJumpscare = playerCamera.transform.rotation;
        LockPlayerControls(target);

        cameraDriver = playerCamera.GetComponent<PlayerCameraController>();
        if (cameraDriver == null)
            cameraDriver = playerCamera.GetComponent("CinemachineBrain") as Behaviour;
        if (cameraDriver != null)
        {
            cameraDriverWasEnabled = cameraDriver.enabled;
            cameraDriver.enabled = false;
        }

        // Find nearest jumpscare anchor if available
        JumpscareAnchor effectiveAnchor = customAnchor != null ? customAnchor : jumpscareAnchor;
        if (effectiveAnchor == null)
        {
            Vector3 searchPos = target != null ? target.transform.position : playerCamera.transform.position;
            effectiveAnchor = JumpscareAnchor.FindNearest(searchPos);
        }

        Vector3 cameraPosition;
        Vector3 startPosition;

        if (effectiveAnchor != null)
        {
            // Teleport player and camera to the dedicated anchor point with perfect ground snap
            float playerHalfHeight = 1f;
            if (playerController != null)
            {
                CapsuleCollider col = playerController.GetComponent<CapsuleCollider>();
                if (col != null)
                {
                    playerHalfHeight = (col.height * 0.5f - col.center.y) * playerController.transform.lossyScale.y;
                }
            }

            Vector3 anchorFloorPos = effectiveAnchor.transform.position;
            if (Physics.Raycast(
                    effectiveAnchor.transform.position + Vector3.up * 2f,
                    Vector3.down,
                    out RaycastHit anchorFloorHit,
                    5f,
                    obstacleLayers,
                    QueryTriggerInteraction.Ignore))
            {
                anchorFloorPos.y = anchorFloorHit.point.y + playerHalfHeight;
            }

            if (target != null)
            {
                target.TeleportTo(anchorFloorPos, effectiveAnchor.transform.rotation);
            }
            playerCamera.transform.rotation = effectiveAnchor.transform.rotation;
            cameraPosition = playerCamera.transform.position;

            if (effectiveAnchor.MonsterSpawnPoint != null)
            {
                startPosition = effectiveAnchor.MonsterSpawnPoint.position;
            }
            else
            {
                startPosition = effectiveAnchor.transform.position + effectiveAnchor.transform.forward * effectiveAnchor.MonsterDistance;
                if (Physics.Raycast(
                        startPosition + Vector3.up * surfaceSearchHeight,
                        Vector3.down,
                        out RaycastHit surface,
                        surfaceSearchDistance,
                        obstacleLayers,
                        QueryTriggerInteraction.Ignore))
                {
                    startPosition.y = surface.point.y;
                }
            }
        }
        else
        {
            cameraPosition = playerCamera.transform.position;
            startPosition = FindBestSilhouettePlacement(cameraPosition, out Vector3 startDirection);
        }

        sequenceScale = transform.localScale;
        transform.SetPositionAndRotation(
            startPosition,
            Quaternion.LookRotation(cameraPosition - startPosition, playerCamera.transform.up));

        CanvasGroup fade = CreateFadeOverlay();
        CanvasGroup redFlash = CreateRedFlashOverlay();
        Light backgroundLight = CreateBackgroundLight(startPosition);
        Renderer backgroundRenderer = CreateRedBackdrop(startPosition);
        Light faceLight = CreateFaceLight();
        faceLight.enabled = false;
        Renderer[] illuminatedRenderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < illuminatedRenderers.Length; i++)
        {
            if (illuminatedRenderers[i] is SkinnedMeshRenderer smr)
                smr.updateWhenOffscreen = true;
        }

        List<Material> temporaryMaterials = CreateMonsterMaterials(
            illuminatedRenderers,
            silhouetteColor,
            out Material[][] originalMaterials);

        Vector3 monsterCenter = transform.position + Vector3.up * 1.5f;
        Vector3 lookDirection = monsterCenter - cameraPosition;
        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            playerCamera.transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            if (playerController != null)
            {
                Vector3 bodyLook = lookDirection;
                bodyLook.y = 0f;
                if (bodyLook.sqrMagnitude > 0.0001f)
                    playerController.transform.rotation = Quaternion.LookRotation(bodyLook, Vector3.up);
            }
        }

        float elapsed = 0f;
        float nextFlicker = Random.Range(
            flickerMinInterval,
            Mathf.Max(flickerMinInterval, flickerMaxInterval));
        while (elapsed < flickerDuration)
        {
            if (elapsed >= nextFlicker)
            {
                backgroundLight.enabled = !backgroundLight.enabled;
                backgroundRenderer.enabled = backgroundLight.enabled;
                redFlash.alpha = backgroundLight.enabled ? 0.15f : 0f;
                nextFlicker = elapsed + Random.Range(
                    flickerMinInterval,
                    Mathf.Max(flickerMinInterval, flickerMaxInterval));
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        backgroundLight.enabled = false;
        backgroundRenderer.enabled = false;
        redFlash.alpha = 0f;
        fade.alpha = 1f;

        elapsed = 0f;
        float darkPause = Random.Range(darkPauseMin, Mathf.Max(darkPauseMin, darkPauseMax));
        while (elapsed < darkPause)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        PlaceFaceInFrontOfCamera(illuminatedRenderers);
        SetMonsterMaterialColor(temporaryMaterials, faceMonsterColor);

        if (animator != null && !string.IsNullOrEmpty(animationTrigger))
            animator.SetTrigger(animationTrigger);

        audioSource.Stop();
        audioSource.clip = jumpscareSound != null ? jumpscareSound : impactClip;
        audioSource.volume = soundVolume;
        audioSource.Play();
        faceLight.enabled = true;
        fade.alpha = 0f;

        elapsed = 0f;
        while (elapsed < faceRevealDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < blackoutFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fade.alpha = Mathf.Clamp01(elapsed / blackoutFadeDuration);
            yield return null;
        }

        fade.alpha = 1f;
        faceLight.enabled = false;
        blackoutAction?.Invoke();

        elapsed = 0f;
        while (elapsed < blackScreenDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        transform.SetPositionAndRotation(homePosition, homeRotation);
        transform.localScale = homeScale;
        playerCamera.transform.rotation = cameraRotationBeforeJumpscare;

        if (cameraDriver != null)
            cameraDriver.enabled = cameraDriverWasEnabled;

        RestorePlayerControls();

        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fade.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
            yield return null;
        }

        Destroy(fade.gameObject);
        if (redFlash != null)
            Destroy(redFlash.gameObject);
        Destroy(backgroundLight.gameObject);
        Destroy(backgroundRenderer.sharedMaterial);
        Destroy(backgroundRenderer.gameObject);
        Destroy(faceLight.gameObject);
        RestoreMonsterMaterials(illuminatedRenderers, originalMaterials, temporaryMaterials);
        isPlaying = false;
        completed?.Invoke();
    }

    private void LockPlayerControls(PlayerController target)
    {
        playerController = target != null ? target : Object.FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            playerControllerWasEnabled = playerController.enabled;
            playerController.enabled = false;

            Rigidbody body = playerController.GetComponent<Rigidbody>();
            if (body != null)
            {
                playerRigidbodyWasKinematic = body.isKinematic;
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }
                body.isKinematic = true;
            }

            playerLights = playerController.GetComponentsInChildren<Light>(true);
            playerLightsWereEnabled = new bool[playerLights.Length];
            for (int index = 0; index < playerLights.Length; index++)
            {
                playerLightsWereEnabled[index] = playerLights[index].enabled;
                playerLights[index].enabled = false;
            }
        }

#if ENABLE_INPUT_SYSTEM
        playerInput = Object.FindFirstObjectByType<PlayerInput>();
        if (playerInput != null)
        {
            playerInputWasEnabled = playerInput.enabled;
            playerInputWasActive = playerInput.inputIsActive;
            playerInput.DeactivateInput();
        }
#endif
    }

    private Vector3 FindBestSilhouettePlacement(Vector3 cameraPosition, out Vector3 chosenDirection)
    {
        Vector3 forward = playerCamera.transform.forward;
        forward.y = 0f;
        forward = forward.sqrMagnitude > 0.001f
            ? forward.normalized
            : playerController != null ? playerController.transform.forward : Vector3.forward;

        float[] testAngles = { 0f, 20f, -20f, 40f, -40f, 60f, -60f, 80f, -80f, 100f, -100f, 120f, -120f, 140f, -140f, 160f, -160f, 180f };

        Vector3 bestPosition = cameraPosition + forward * Mathf.Max(faceDistance + 1f, minSilhouetteDistance);
        Vector3 bestDir = forward;
        float bestScore = float.NegativeInfinity;
        bool foundValidPlacement = false;

        float defaultFloorY = playerController != null ? playerController.transform.position.y : cameraPosition.y - 1.5f;

        for (int i = 0; i < testAngles.Length; i++)
        {
            float angle = testAngles[i];
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * forward;

            // 1. Line of Sight Check: SphereCast forward to detect walls / obstacles
            float maxSightDistance = startDistance;
            if (Physics.SphereCast(
                    cameraPosition,
                    sphereCastRadius,
                    dir,
                    out RaycastHit hit,
                    startDistance,
                    obstacleLayers,
                    QueryTriggerInteraction.Ignore))
            {
                maxSightDistance = Mathf.Max(0.1f, hit.distance - wallClearance);
            }

            // 2. Floor Boundary check
            float boundaryDistance = FindFloorBoundaryDistance(cameraPosition, dir);
            float candidateDistance = Mathf.Min(maxSightDistance, boundaryDistance);

            // Need at least faceDistance + clearance
            if (candidateDistance < faceDistance + 0.2f)
                continue;

            // Target placement position
            Vector3 targetPos = cameraPosition + dir * candidateDistance;

            // 3. Find floor height at target position
            if (Physics.Raycast(
                    targetPos + Vector3.up * surfaceSearchHeight,
                    Vector3.down,
                    out RaycastHit surface,
                    surfaceSearchDistance,
                    obstacleLayers,
                    QueryTriggerInteraction.Ignore))
            {
                targetPos.y = surface.point.y;
            }
            else
            {
                targetPos.y = defaultFloorY;
            }

            // 4. Verify clear line of sight from camera to monster body midpoint
            Vector3 monsterMid = targetPos + Vector3.up * 1f;
            Vector3 toMid = monsterMid - cameraPosition;
            float midDist = toMid.magnitude;
            bool sightBlocked = false;
            if (midDist > 0.3f && Physics.SphereCast(
                    cameraPosition,
                    0.1f,
                    toMid.normalized,
                    out RaycastHit blockHit,
                    midDist - 0.2f,
                    obstacleLayers,
                    QueryTriggerInteraction.Ignore))
            {
                sightBlocked = true;
            }

            // Score evaluation: Prefer longer distance, unblocked line of sight, and smaller angle deviation
            float distScore = candidateDistance;
            if (sightBlocked)
                distScore *= 0.3f; // Heavy penalty if blocked

            // If it meets minSilhouetteDistance and is straight ahead (0 deg), strongly prefer
            float anglePenalty = Mathf.Abs(angle) * 0.015f;
            float finalScore = distScore - anglePenalty;

            if (!foundValidPlacement || finalScore > bestScore)
            {
                bestScore = finalScore;
                bestPosition = targetPos;
                bestDir = dir;
                foundValidPlacement = true;

                // If straight ahead has ample distance without block, lock it immediately
                if (Mathf.Abs(angle) < 0.01f && candidateDistance >= minSilhouetteDistance && !sightBlocked)
                {
                    break;
                }
            }
        }

        chosenDirection = bestDir;
        return bestPosition;
    }

    private float FindFloorBoundaryDistance(Vector3 origin, Vector3 direction)
    {
        StaircaseFloor selectedFloor = GetComponentInParent<StaircaseFloor>();
        Bounds selectedBounds = default;
        bool hasBounds = selectedFloor != null && TryGetFloorBounds(selectedFloor, out selectedBounds);

        if (!hasBounds)
        {
            float bestDistance = float.PositiveInfinity;
            foreach (StaircaseFloor floor in Object.FindObjectsByType<StaircaseFloor>(FindObjectsSortMode.None))
            {
                if (!TryGetFloorBounds(floor, out Bounds bounds))
                    continue;

                float distance = bounds.SqrDistance(origin);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    selectedBounds = bounds;
                    hasBounds = true;
                }
            }
        }

        if (hasBounds)
        {
            float xDistance = Mathf.Abs(direction.x) > 0.001f
                ? ((direction.x > 0f ? selectedBounds.max.x : selectedBounds.min.x) - origin.x) / direction.x
                : float.PositiveInfinity;
            float zDistance = Mathf.Abs(direction.z) > 0.001f
                ? ((direction.z > 0f ? selectedBounds.max.z : selectedBounds.min.z) - origin.z) / direction.z
                : float.PositiveInfinity;
            float boundaryDistance = Mathf.Min(xDistance, zDistance);
            if (boundaryDistance > 0f && !float.IsInfinity(boundaryDistance))
                return Mathf.Max(faceDistance + 0.25f, boundaryDistance - wallClearance);
        }

        return Mathf.Min(startDistance, 6f);
    }

    private bool TryGetFloorBounds(StaircaseFloor floor, out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        foreach (Collider floorCollider in floor.GetComponentsInChildren<Collider>(true))
        {
            if (floorCollider.isTrigger || floorCollider.transform.IsChildOf(transform))
                continue;

            if (!found)
            {
                bounds = floorCollider.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(floorCollider.bounds);
            }
        }

        return found;
    }

    private void RestorePlayerControls()
    {
        if (playerController != null)
        {
            playerController.enabled = playerControllerWasEnabled;
            Rigidbody body = playerController.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.isKinematic = playerRigidbodyWasKinematic;
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }
            }
        }

        if (playerLights != null)
        {
            for (int index = 0; index < playerLights.Length; index++)
            {
                if (playerLights[index] != null)
                    playerLights[index].enabled = playerLightsWereEnabled[index];
            }
        }

#if ENABLE_INPUT_SYSTEM
        if (playerInput != null)
        {
            playerInput.enabled = playerInputWasEnabled;
            if (playerInputWasActive)
                playerInput.ActivateInput();
        }
#endif
    }

    private static CanvasGroup CreateFadeOverlay()
    {
        GameObject canvasObject = new("JumpscareFade", typeof(Canvas), typeof(CanvasGroup));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        CanvasGroup group = canvasObject.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = true;

        GameObject imageObject = new("Black", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        imageObject.GetComponent<Image>().color = Color.black;

        return group;
    }

    private static CanvasGroup CreateRedFlashOverlay()
    {
        GameObject canvasObject = new("JumpscareRedFlash", typeof(Canvas), typeof(CanvasGroup));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue - 1;

        CanvasGroup group = canvasObject.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        GameObject imageObject = new("RedFlash", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        imageObject.GetComponent<Image>().color = new Color(0.9f, 0.05f, 0.02f, 1f);

        return group;
    }

    private Light CreateBackgroundLight(Vector3 monsterPosition)
    {
        GameObject lightObject = new("Jumpscare Background Light", typeof(Light));
        Vector3 monsterCenter = monsterPosition + Vector3.up * 1.5f;
        Vector3 lookDir = (monsterCenter - playerCamera.transform.position).normalized;
        if (lookDir.sqrMagnitude < 0.001f)
            lookDir = playerCamera.transform.forward;

        // Position light BEHIND monster and aim it TOWARDS the camera for backlight silhouette effect
        lightObject.transform.SetPositionAndRotation(
            monsterCenter + lookDir * 0.8f,
            Quaternion.LookRotation(-lookDir, Vector3.up));
        Light light = lightObject.GetComponent<Light>();
        light.type = LightType.Spot;
        light.color = backgroundLightColor;
        light.intensity = backgroundLightIntensity;
        light.range = backgroundLightRange;
        light.spotAngle = backgroundLightAngle;
        light.shadows = LightShadows.None;
        return light;
    }

    private Light CreateFaceLight()
    {
        GameObject lightObject = new("Jumpscare Face Light", typeof(Light));
        lightObject.transform.SetParent(playerCamera.transform, false);
        Light light = lightObject.GetComponent<Light>();
        light.type = LightType.Point;
        light.color = faceLightColor;
        light.intensity = faceLightIntensity;
        light.range = faceLightRange;
        light.shadows = LightShadows.None;
        return light;
    }

    private Renderer CreateRedBackdrop(Vector3 monsterPosition)
    {
        Vector3 monsterCenter = monsterPosition + Vector3.up * 1.5f;
        Vector3 lookDir = (monsterCenter - playerCamera.transform.position).normalized;
        if (lookDir.sqrMagnitude < 0.001f)
            lookDir = playerCamera.transform.forward;

        GameObject backdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
        backdrop.name = "Jumpscare Red Backdrop";
        Destroy(backdrop.GetComponent<Collider>());
        backdrop.transform.SetPositionAndRotation(
            monsterCenter + lookDir * 1.5f,
            Quaternion.LookRotation(-lookDir, Vector3.up));
        backdrop.transform.localScale = new Vector3(120f, 80f, 1f);

        Material material = new(Shader.Find("Universal Render Pipeline/Unlit"));
        material.SetColor("_BaseColor", backdropColor);
        material.SetFloat("_Cull", 0f);
        Renderer renderer = backdrop.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        return renderer;
    }

    private void PlaceFaceInFrontOfCamera(Renderer[] renderers)
    {
        transform.rotation = Quaternion.LookRotation(-playerCamera.transform.forward, Vector3.up);
        transform.localScale = sequenceScale * faceScaleMultiplier;

        Transform headBone = null;
        Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < allTransforms.Length; i++)
        {
            if (allTransforms[i].name.IndexOf("Head", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                allTransforms[i].name.IndexOf("Top", System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                headBone = allTransforms[i];
                break;
            }
        }

        Vector3 target = playerCamera.transform.position + playerCamera.transform.forward * faceDistance;

        if (headBone != null)
        {
            Vector3 offset = target - headBone.position;
            transform.position += offset;
        }
        else if (renderers != null && renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);

            Vector3 facePoint = new(
                bounds.center.x,
                bounds.max.y - bounds.size.y * 0.15f,
                bounds.center.z);
            transform.position += target - facePoint;
        }
    }

    private List<Material> CreateMonsterMaterials(
        Renderer[] renderers,
        Color color,
        out Material[][] originalMaterials)
    {
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        originalMaterials = new Material[renderers.Length][];
        List<Material> temporaryMaterials = new();

        for (int index = 0; index < renderers.Length; index++)
        {
            originalMaterials[index] = renderers[index].sharedMaterials;
            Material[] illuminatedMaterials = new Material[originalMaterials[index].Length];
            for (int materialIndex = 0; materialIndex < illuminatedMaterials.Length; materialIndex++)
            {
                Material original = originalMaterials[index][materialIndex];
                Material illuminated = new(unlitShader);
                illuminated.SetColor("_BaseColor", color);
                if (original != null && original.mainTexture != null)
                    illuminated.SetTexture("_BaseMap", original.mainTexture);

                illuminatedMaterials[materialIndex] = illuminated;
                temporaryMaterials.Add(illuminated);
            }

            renderers[index].sharedMaterials = illuminatedMaterials;
        }

        return temporaryMaterials;
    }

    private static void SetMonsterMaterialColor(List<Material> materials, Color color)
    {
        foreach (Material material in materials)
            material.SetColor("_BaseColor", color);
    }

    private static void RestoreMonsterMaterials(
        Renderer[] renderers,
        Material[][] originalMaterials,
        List<Material> temporaryMaterials)
    {
        for (int index = 0; index < renderers.Length; index++)
        {
            if (renderers[index] != null)
                renderers[index].sharedMaterials = originalMaterials[index];
        }

        foreach (Material material in temporaryMaterials)
            Destroy(material);
    }

    private static AudioClip CreateImpactClip()
    {
        const int sampleRate = 44100;
        const float length = 0.8f;
        int sampleCount = Mathf.RoundToInt(sampleRate * length);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float decay = Mathf.Exp(-3.2f * t);
            float rumble = Mathf.Sin(2f * Mathf.PI * (75f - 35f * t) * t);
            float shriek = Mathf.Sin(2f * Mathf.PI * (780f + 520f * t) * t);
            float noise = Random.Range(-1f, 1f);
            samples[i] = Mathf.Clamp((rumble * 0.55f + shriek * 0.2f + noise * 0.35f) * decay, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Generated Jumpscare Impact", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
