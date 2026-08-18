using System;
using UnityEngine;

/// <summary>
/// Modular zone trigger condition for jumpscares.
/// Attach to any GameObject with a Collider (will automatically set isTrigger = true).
/// Triggers a jumpscare when the player enters or stays in the trigger area.
/// </summary>
[RequireComponent(typeof(Collider))]
public sealed class ZoneTriggerCondition : MonoBehaviour, IJumpscareCondition
{
    [Header("Zone Trigger Settings")]
    [Tooltip("If true, triggers immediately when player enters the collider.")]
    [SerializeField] private bool triggerOnEnter = true;

    [Tooltip("If > 0, player must stay inside the zone for this duration before triggering.")]
    [SerializeField, Min(0f)] private float requiredStayDuration = 0f;

    [Tooltip("Tag to filter triggering objects (default: Player).")]
    [SerializeField] private string targetTag = "Player";

    public event Action<PlayerController> OnConditionMet;

    public bool IsConditionMet => isTriggered;

    private bool isTriggered;
    private float stayTimer;
    private PlayerController playerInside;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isTriggered)
            return;

        if (!IsPlayer(other, out PlayerController player))
            return;

        playerInside = player;
        stayTimer = 0f;

        if (triggerOnEnter && requiredStayDuration <= 0f)
        {
            FireTrigger(player);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (isTriggered || requiredStayDuration <= 0f)
            return;

        if (playerInside == null && !IsPlayer(other, out playerInside))
            return;

        stayTimer += Time.deltaTime;
        if (stayTimer >= requiredStayDuration)
        {
            FireTrigger(playerInside);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other, out _))
        {
            playerInside = null;
            stayTimer = 0f;
        }
    }

    private bool IsPlayer(Collider other, out PlayerController player)
    {
        player = other.GetComponentInParent<PlayerController>();
        if (player != null)
            return true;

        if (!string.IsNullOrEmpty(targetTag) && other.CompareTag(targetTag))
        {
            player = other.GetComponent<PlayerController>() ?? FindFirstObjectByType<PlayerController>();
            return true;
        }

        return false;
    }

    private void FireTrigger(PlayerController player)
    {
        isTriggered = true;
        OnConditionMet?.Invoke(player);
    }

    public void ResetCondition()
    {
        isTriggered = false;
        stayTimer = 0f;
        playerInside = null;
    }
}
