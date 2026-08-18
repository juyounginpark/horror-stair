using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Modular timeout condition for jumpscares.
/// Attach to puzzles, timed rooms, or gimmick objects.
/// Triggers a jumpscare if the timer expires before being stopped/disarmed.
/// </summary>
public sealed class TimeoutCondition : MonoBehaviour, IJumpscareCondition
{
    [Header("Timer Settings")]
    [SerializeField, Min(0.1f)] private float timeLimit = 15f;
    [SerializeField] private bool startOnEnable = false;
    [SerializeField] private bool autoFindPlayer = true;

    [Header("Events")]
    [SerializeField] private UnityEvent<float, float> onTimerTick; // (remaining, total)
    [SerializeField] private UnityEvent onTimeout;

    public event Action<PlayerController> OnConditionMet;

    public float TimeLimit => timeLimit;
    public float TimeRemaining => Mathf.Max(0f, currentTimer);
    public float Progress => Mathf.Clamp01(1f - (currentTimer / Mathf.Max(0.01f, timeLimit)));
    public bool IsRunning => isRunning;
    public bool IsConditionMet => hasTimedOut;

    private float currentTimer;
    private bool isRunning;
    private bool hasTimedOut;

    private void Awake()
    {
        currentTimer = timeLimit;
    }

    private void OnEnable()
    {
        if (startOnEnable && !hasTimedOut)
            StartTimer();
    }

    private void Update()
    {
        if (!isRunning || hasTimedOut)
            return;

        currentTimer -= Time.deltaTime;
        onTimerTick?.Invoke(currentTimer, timeLimit);

        if (currentTimer <= 0f)
        {
            hasTimedOut = true;
            isRunning = false;
            onTimeout?.Invoke();

            PlayerController player = autoFindPlayer ? FindFirstObjectByType<PlayerController>() : null;
            OnConditionMet?.Invoke(player);
        }
    }

    /// <summary>
    /// Start or resume the timer countdown.
    /// </summary>
    public void StartTimer()
    {
        if (hasTimedOut)
            return;

        isRunning = true;
    }

    /// <summary>
    /// Stop / Disarm the timer (e.g. puzzle solved).
    /// </summary>
    public void StopTimer()
    {
        isRunning = false;
    }

    /// <summary>
    /// Add or subtract extra time.
    /// </summary>
    public void AddTime(float extraSeconds)
    {
        currentTimer = Mathf.Clamp(currentTimer + extraSeconds, 0f, timeLimit * 2f);
        onTimerTick?.Invoke(currentTimer, timeLimit);
    }

    /// <summary>
    /// Reset the timer back to its initial limit.
    /// </summary>
    public void ResetCondition()
    {
        isRunning = false;
        hasTimedOut = false;
        currentTimer = timeLimit;
        onTimerTick?.Invoke(currentTimer, timeLimit);
    }
}
