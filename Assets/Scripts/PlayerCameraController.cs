using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerCameraController : MonoBehaviour
{
    [Header("Mouse Look")]
    [SerializeField, Min(0f)] private float mouseSensitivity = 0.15f;
    [SerializeField] private float minimumPitch = -75f;
    [SerializeField] private float maximumPitch = 75f;

    [Header("Camera Zoom")]
    [SerializeField, Min(0f)] private float firstPersonDistance = 0.05f;
    [SerializeField, Min(0f)] private float thirdPersonDistance = 4f;
    [SerializeField, Min(0f)] private float eyeHeight = 1f;
    [SerializeField, Min(0f)] private float zoomSmoothTime = 0.15f;

    private Transform player;
    private Rigidbody playerBody;
    private Renderer playerRenderer;
    private float yaw;
    private float pitch;
    private float currentDistance;
    private float targetDistance;
    private float zoomVelocity;

    private void Awake()
    {
        player = transform.parent;
        if (player == null)
        {
            Debug.LogError("PlayerCamera must be a child of Player.", this);
            enabled = false;
            return;
        }

        playerBody = player.GetComponent<Rigidbody>();
        playerRenderer = player.GetComponent<Renderer>();
        yaw = player.eulerAngles.y;
        pitch = NormalizeAngle(transform.localEulerAngles.x);
        currentDistance = firstPersonDistance;
        targetDistance = firstPersonDistance;

        if (playerRenderer != null)
        {
            playerRenderer.enabled = false;
        }

        LockCursor();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            LockCursor();
        }

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity * 10f;
            pitch = Mathf.Clamp(
                pitch - Input.GetAxis("Mouse Y") * mouseSensitivity * 10f,
                minimumPitch,
                maximumPitch);
        }

        float scroll = Input.mouseScrollDelta.y;
        if (scroll > 0f)
        {
            targetDistance = firstPersonDistance;
        }
        else if (scroll < 0f)
        {
            targetDistance = thirdPersonDistance;
        }
    }

    private void FixedUpdate()
    {
        Quaternion targetRotation = Quaternion.Euler(0f, yaw, 0f);
        if (playerBody != null)
        {
            playerBody.MoveRotation(targetRotation);
        }
        else
        {
            player.rotation = targetRotation;
        }
    }

    private void LateUpdate()
    {
        currentDistance = Mathf.SmoothDamp(
            currentDistance,
            targetDistance,
            ref zoomVelocity,
            zoomSmoothTime);

        Quaternion cameraRotation = Quaternion.Euler(pitch, 0f, 0f);
        Vector3 pivot = Vector3.up * eyeHeight;
        transform.SetLocalPositionAndRotation(
            pivot + cameraRotation * Vector3.back * currentDistance,
            cameraRotation);

        if (playerRenderer != null)
        {
            playerRenderer.enabled = currentDistance > 0.3f;
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            LockCursor();
        }
    }

    private static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private static float NormalizeAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }
}
