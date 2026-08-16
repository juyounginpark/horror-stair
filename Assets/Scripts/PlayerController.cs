using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public sealed class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [SerializeField, Min(0f)] private float jumpForce = 5f;

    [Header("Sprint")]
    [SerializeField, Min(1f)] private float sprintSpeedMultiplier = 1.6f;
    [SerializeField, Min(0.01f)] private float maximumStamina = 1f;
    [SerializeField, Min(0f)] private float staminaDrainPerSecond = 0.22f;
    [SerializeField, Min(0f)] private float staminaRecoveryPerSecond = 0.18f;
    [SerializeField, Range(0f, 1f)] private float minimumSprintStartRatio = 0.2f;
    [SerializeField, Range(0f, 1f)] private float exhaustedSlowdownRatio = 0.3f;
    [SerializeField, Range(0f, 1f)] private float exhaustedMoveSpeedMultiplier = 0.5f;
    [SerializeField] private Slider staminaSlider;

    [Header("Ground Check")]
    [SerializeField, Range(0f, 1f)] private float minimumGroundNormalY = 0.6f;
    [SerializeField, Min(0f)] private float groundedGraceTime = 0.1f;

    private Rigidbody body;
    private CapsuleCollider capsuleCollider;
    private PhysicsMaterial noFrictionMaterial;
    private PhysicsMaterial idleGroundMaterial;
    private Vector2 moveInput;
    private bool jumpRequested;
    private bool isSprinting;
    private float currentStamina;
    private float lastGroundedTime = float.NegativeInfinity;
    private static bool hasStartPose;
    private static Vector3 startPosition;
    private static Quaternion startRotation;

    public bool IsGrounded => Time.time - lastGroundedTime <= groundedGraceTime;
    public bool IsSprinting => isSprinting;
    public bool HasMovementInput => moveInput.sqrMagnitude > 0.0001f || jumpRequested;
    public float StaminaRatio => maximumStamina <= 0f
        ? 0f
        : currentStamina / maximumStamina;

    public float HorizontalSpeed => body == null
        ? 0f
        : new Vector2(body.linearVelocity.x, body.linearVelocity.z).magnitude;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStartPose()
    {
        hasStartPose = false;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        body.useGravity = true;
        body.constraints = (body.constraints
            | RigidbodyConstraints.FreezeRotationX
            | RigidbodyConstraints.FreezeRotationZ)
            & ~RigidbodyConstraints.FreezeRotationY;
        body.maxLinearVelocity = 50f;
        body.maxAngularVelocity = 50f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        currentStamina = maximumStamina;

        if (!hasStartPose)
        {
            startPosition = transform.position;
            startRotation = transform.rotation;
            hasStartPose = true;
        }

        noFrictionMaterial = new PhysicsMaterial("Player No Friction")
        {
            hideFlags = HideFlags.HideAndDontSave,
            dynamicFriction = 0f,
            staticFriction = 0f,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };

        idleGroundMaterial = new PhysicsMaterial("Player Ground Grip")
        {
            hideFlags = HideFlags.HideAndDontSave,
            dynamicFriction = 1f,
            staticFriction = 1f,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Maximum,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };

        capsuleCollider.material = noFrictionMaterial;
    }

    private void Start()
    {
        FindStaminaSliderIfNeeded();
        UpdateStaminaUI(false);
    }

    private void OnDestroy()
    {
        if (noFrictionMaterial != null)
        {
            Destroy(noFrictionMaterial);
        }

        if (idleGroundMaterial != null)
        {
            Destroy(idleGroundMaterial);
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            moveInput = Vector2.zero;
            UpdateStamina(false);
            return;
        }

        moveInput = new Vector2(
            ReadAxis(keyboard.aKey.isPressed, keyboard.dKey.isPressed),
            ReadAxis(keyboard.sKey.isPressed, keyboard.wKey.isPressed));
        moveInput = Vector2.ClampMagnitude(moveInput, 1f);

        bool shiftHeld = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        UpdateStamina(shiftHeld);

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            jumpRequested = true;
        }
    }

    private void FixedUpdate()
    {
        bool wantsToMove = moveInput.sqrMagnitude > 0.0001f;
        bool isJumping = jumpRequested && IsGrounded;
        capsuleCollider.material = IsGrounded && !wantsToMove && !isJumping
            ? idleGroundMaterial
            : noFrictionMaterial;

        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
        Vector3 velocity = body.linearVelocity;
        float currentMoveSpeed = moveSpeed;
        if (isSprinting)
        {
            currentMoveSpeed *= sprintSpeedMultiplier;
        }
        else if (StaminaRatio <= exhaustedSlowdownRatio)
        {
            currentMoveSpeed *= exhaustedMoveSpeedMultiplier;
        }
        velocity.x = moveDirection.x * currentMoveSpeed;
        velocity.z = moveDirection.z * currentMoveSpeed;

        if (jumpRequested && IsGrounded)
        {
            velocity.y = jumpForce;
            lastGroundedTime = float.NegativeInfinity;
        }

        body.linearVelocity = velocity;
        jumpRequested = false;
    }

    private void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y >= minimumGroundNormalY)
            {
                lastGroundedTime = Time.time;
                return;
            }
        }
    }

    private static float ReadAxis(bool negativePressed, bool positivePressed)
    {
        return (positivePressed ? 1f : 0f) - (negativePressed ? 1f : 0f);
    }

    public void ReturnToStart()
    {
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.position = startPosition;
        body.rotation = startRotation;
        transform.SetPositionAndRotation(startPosition, startRotation);
    }

    private void UpdateStamina(bool shiftHeld)
    {
        if (shiftHeld)
        {
            float staminaRatio = currentStamina / maximumStamina;
            if (!isSprinting && staminaRatio > minimumSprintStartRatio)
            {
                isSprinting = true;
            }

            if (isSprinting)
            {
                currentStamina = Mathf.Max(
                    0f,
                    currentStamina - staminaDrainPerSecond * Time.deltaTime);

                if (currentStamina <= 0f)
                {
                    isSprinting = false;
                }
            }
        }
        else
        {
            isSprinting = false;
            currentStamina = Mathf.MoveTowards(
                currentStamina,
                maximumStamina,
                staminaRecoveryPerSecond * Time.deltaTime);
        }

        UpdateStaminaUI(shiftHeld);
    }

    private void FindStaminaSliderIfNeeded()
    {
        if (staminaSlider != null)
        {
            return;
        }

        Slider[] sliders = FindObjectsByType<Slider>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Slider slider in sliders)
        {
            if (slider.name.Equals("stamina", System.StringComparison.OrdinalIgnoreCase))
            {
                staminaSlider = slider;
                break;
            }
        }

        if (staminaSlider != null)
        {
            staminaSlider.minValue = 0f;
            staminaSlider.maxValue = maximumStamina;
            staminaSlider.interactable = false;
        }
    }

    private void UpdateStaminaUI(bool shiftHeld)
    {
        FindStaminaSliderIfNeeded();
        if (staminaSlider == null)
        {
            return;
        }

        staminaSlider.value = currentStamina;

        bool isFullyRecovered = currentStamina >= maximumStamina - 0.0001f;
        bool shouldShow = shiftHeld || !isFullyRecovered;
        if (staminaSlider.gameObject.activeSelf != shouldShow)
        {
            staminaSlider.gameObject.SetActive(shouldShow);
        }
    }
}
