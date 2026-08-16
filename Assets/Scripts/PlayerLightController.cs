using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Light))]
[DisallowMultipleComponent]
public sealed class PlayerLightController : MonoBehaviour
{
    [Header("Flicker")]
    [SerializeField, Min(0.1f)] private float minimumFlickerInterval = 5f;
    [SerializeField, Min(0.1f)] private float maximumFlickerInterval = 14f;
    [SerializeField, Range(1, 5)] private int maximumFlickerCount = 3;

    [Header("Range Pulse")]
    [SerializeField, Min(0f)] private float maximumRangeVariation = 3f;
    [SerializeField, Min(0.05f)] private float rangeSmoothTime = 0.45f;
    [SerializeField, Min(0.05f)] private float minimumRangeChangeInterval = 0.35f;
    [SerializeField, Min(0.05f)] private float maximumRangeChangeInterval = 1.2f;

    private Light playerLight;
    private bool isLightOn;
    private float nextFlickerTime;
    private Coroutine flickerRoutine;
    private float baseRange;
    private float targetRange;
    private float rangeVelocity;
    private float nextRangeChangeTime;

    public bool IsLightOn => isLightOn;
    public event System.Action<bool> LightStateChanged;

    private void Awake()
    {
        playerLight = GetComponent<Light>();
        isLightOn = playerLight.enabled;
        baseRange = playerLight.range;
        targetRange = baseRange;
        ScheduleNextFlicker();
        ScheduleNextRange();
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            ToggleLight();
        }

        if (isLightOn && flickerRoutine == null && Time.time >= nextFlickerTime)
        {
            flickerRoutine = StartCoroutine(Flicker());
        }

        UpdateLightRange();
    }

    private void ToggleLight()
    {
        SetLight(!isLightOn);
    }

    public void SetLight(bool lightOn)
    {
        if (isLightOn == lightOn)
        {
            playerLight.enabled = lightOn;
            return;
        }

        isLightOn = lightOn;

        if (flickerRoutine != null)
        {
            StopCoroutine(flickerRoutine);
            flickerRoutine = null;
        }

        playerLight.enabled = isLightOn;
        LightStateChanged?.Invoke(isLightOn);
        if (isLightOn)
        {
            ScheduleNextFlicker();
        }
    }

    private IEnumerator Flicker()
    {
        int flickerCount = UnityEngine.Random.Range(1, maximumFlickerCount + 1);

        for (int index = 0; index < flickerCount; index++)
        {
            playerLight.enabled = false;
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.035f, 0.11f));

            playerLight.enabled = true;
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.04f, 0.14f));
        }

        playerLight.enabled = isLightOn;
        flickerRoutine = null;
        ScheduleNextFlicker();
    }

    private void ScheduleNextFlicker()
    {
        float minimum = Mathf.Min(minimumFlickerInterval, maximumFlickerInterval);
        float maximum = Mathf.Max(minimumFlickerInterval, maximumFlickerInterval);
        nextFlickerTime = Time.time + UnityEngine.Random.Range(minimum, maximum);
    }

    private void UpdateLightRange()
    {
        if (Time.time >= nextRangeChangeTime)
        {
            float minimumRange = Mathf.Max(0.01f, baseRange - maximumRangeVariation);
            float maximumRange = baseRange + maximumRangeVariation;
            targetRange = UnityEngine.Random.Range(minimumRange, maximumRange);
            ScheduleNextRange();
        }

        playerLight.range = Mathf.SmoothDamp(
            playerLight.range,
            targetRange,
            ref rangeVelocity,
            rangeSmoothTime);
    }

    private void ScheduleNextRange()
    {
        float minimum = Mathf.Min(minimumRangeChangeInterval, maximumRangeChangeInterval);
        float maximum = Mathf.Max(minimumRangeChangeInterval, maximumRangeChangeInterval);
        nextRangeChangeTime = Time.time + UnityEngine.Random.Range(minimum, maximum);
    }
}
