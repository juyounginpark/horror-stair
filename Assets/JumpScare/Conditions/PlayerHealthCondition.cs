using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Modular health condition for jumpscares.
/// Attach this component to player, enemies, or gimmick objects.
/// Triggers a jumpscare when health drops to or below the threshold (default: 0).
/// </summary>
public sealed class PlayerHealthCondition : MonoBehaviour, IJumpscareCondition
{
    [Header("Health Settings")]
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private float triggerThreshold = 0f;

    [Header("Behavior")]
    [Tooltip("If true, automatically searches for PlayerController in scene on trigger.")]
    [SerializeField] private bool autoFindPlayer = true;

    [Header("Events")]
    [SerializeField] private UnityEvent<float, float> onHealthChanged; // (current, max)

    public event Action<PlayerController> OnConditionMet;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthRatio => Mathf.Clamp01(currentHealth / Mathf.Max(1f, maxHealth));
    public bool IsConditionMet => currentHealth <= triggerThreshold;

    private bool hasFired;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Apply damage. If health drops below threshold, triggers jumpscare.
    /// </summary>
    public void TakeDamage(float damage, PlayerController player = null)
    {
        if (damage <= 0f || hasFired)
            return;

        SetHealth(currentHealth - damage, player);
    }

    /// <summary>
    /// Heal player health.
    /// </summary>
    public void Heal(float amount)
    {
        if (amount <= 0f)
            return;

        SetHealth(Mathf.Min(maxHealth, currentHealth + amount));
    }

    /// <summary>
    /// Set current health directly.
    /// </summary>
    public void SetHealth(float value, PlayerController player = null)
    {
        currentHealth = Mathf.Clamp(value, 0f, maxHealth);
        onHealthChanged?.Invoke(currentHealth, maxHealth);

        if (!hasFired && currentHealth <= triggerThreshold)
        {
            hasFired = true;
            if (player == null && autoFindPlayer)
                player = FindFirstObjectByType<PlayerController>();

            OnConditionMet?.Invoke(player);
        }
    }

    /// <summary>
    /// Reset health to maximum and clear fired state.
    /// </summary>
    public void ResetCondition()
    {
        hasFired = false;
        currentHealth = maxHealth;
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
