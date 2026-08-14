using UnityEngine;

[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
public sealed class HandBob : MonoBehaviour
{
    [Header("Walk Sway")]
    [SerializeField, Min(0.1f)] private float bobFrequency = 1.8f;
    [SerializeField, Min(0f)] private float horizontalAmount = 7f;
    [SerializeField, Min(0f)] private float verticalAmount = 4f;
    [SerializeField, Min(0f)] private float rotationAmount = 1.5f;

    [Header("Small Shake")]
    [SerializeField, Min(0f)] private float shakeAmount = 0.8f;
    [SerializeField, Min(0f)] private float shakeSpeed = 9f;
    [SerializeField, Min(0.01f)] private float blendSpeed = 6f;

    private RectTransform handRect;
    private PlayerController player;
    private Vector2 restingPosition;
    private Quaternion restingRotation;
    private float phase;
    private float movementBlend;

    private void Awake()
    {
        handRect = GetComponent<RectTransform>();
        restingPosition = handRect.anchoredPosition;
        restingRotation = handRect.localRotation;
    }

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.GetComponent<PlayerController>();
        }
    }

    private void LateUpdate()
    {
        bool isWalking = player != null
            && player.IsGrounded
            && player.HorizontalSpeed > 0.1f;

        float targetBlend = isWalking ? 1f : 0f;
        movementBlend = Mathf.MoveTowards(
            movementBlend,
            targetBlend,
            blendSpeed * Time.deltaTime);

        if (isWalking)
        {
            phase += bobFrequency * Mathf.PI * 2f * Time.deltaTime;
        }

        float horizontalSway = Mathf.Sin(phase) * horizontalAmount;
        float verticalSway = Mathf.Sin(phase * 2f) * verticalAmount;
        float rotationSway = Mathf.Sin(phase) * rotationAmount;

        float noiseTime = Time.time * shakeSpeed;
        float shakeX = (Mathf.PerlinNoise(noiseTime, 0.17f) - 0.5f) * 2f * shakeAmount;
        float shakeY = (Mathf.PerlinNoise(0.73f, noiseTime) - 0.5f) * 2f * shakeAmount;
        float shakeRotation = (Mathf.PerlinNoise(noiseTime, 1.31f) - 0.5f) * shakeAmount;

        Vector2 bobOffset = new(
            horizontalSway + shakeX,
            verticalSway + shakeY);

        handRect.anchoredPosition = restingPosition + bobOffset * movementBlend;
        handRect.localRotation = restingRotation
            * Quaternion.Euler(0f, 0f, (rotationSway + shakeRotation) * movementBlend);
    }

    private void OnDisable()
    {
        if (handRect == null)
        {
            return;
        }

        handRect.anchoredPosition = restingPosition;
        handRect.localRotation = restingRotation;
    }
}
