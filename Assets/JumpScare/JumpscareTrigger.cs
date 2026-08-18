using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Modular jumpscare trigger component.
/// Attach to any object (traps, gimmick controllers, zone triggers, enemies).
/// Automatically listens to any IJumpscareCondition components on the same object,
/// or can be triggered via code (Trigger) or UnityEvents.
/// </summary>
[DisallowMultipleComponent]
public sealed class JumpscareTrigger : MonoBehaviour
{
    [Header("Monster & Anchor Setup")]
    [Tooltip("Optional custom monster prefab. If empty, automatically uses the default monster.")]
    [SerializeField] private GameObject monsterPrefab;

    [Tooltip("Optional explicit anchor for cinematic positioning. If empty, automatically finds the nearest anchor in scene.")]
    [SerializeField] private JumpscareAnchor customAnchor;

    [Header("Options")]
    [Tooltip("Whether to return the player to the floor 1 start position after the jumpscare.")]
    [SerializeField] private bool returnPlayerToStart = true;

    [Tooltip("Whether this trigger can only fire once.")]
    [SerializeField] private bool oneShot = true;

    [Tooltip("Cooldown between triggers if not one-shot.")]
    [SerializeField, Min(0f)] private float cooldown = 3f;

    [Header("Events")]
    [SerializeField] private UnityEvent onJumpscareTriggered;

    private bool hasTriggered;
    private float lastTriggerTime = -999f;
    private readonly List<IJumpscareCondition> conditions = new();

    private void Awake()
    {
        CacheConditions();
    }

    private void OnEnable()
    {
        SubscribeConditions();
    }

    private void OnDisable()
    {
        UnsubscribeConditions();
    }

    private void CacheConditions()
    {
        conditions.Clear();
        GetComponents(conditions);
    }

    private void SubscribeConditions()
    {
        CacheConditions();
        for (int i = 0; i < conditions.Count; i++)
        {
            conditions[i].OnConditionMet += HandleConditionMet;
        }
    }

    private void UnsubscribeConditions()
    {
        for (int i = 0; i < conditions.Count; i++)
        {
            conditions[i].OnConditionMet -= HandleConditionMet;
        }
    }

    private void HandleConditionMet(PlayerController player)
    {
        Trigger(player);
    }

    /// <summary>
    /// Trigger the jumpscare sequence.
    /// Can be called manually from code, animation events, or UnityEvents.
    /// </summary>
    public void Trigger(PlayerController player = null)
    {
        if (!enabled)
            return;

        if (oneShot && hasTriggered)
            return;

        if (Time.time < lastTriggerTime + cooldown)
            return;

        if (player == null)
            player = FindFirstObjectByType<PlayerController>();

        if (player == null)
        {
            Debug.LogWarning("[JumpscareTrigger] Cannot execute jumpscare: No PlayerController found in scene.");
            return;
        }

        hasTriggered = true;
        lastTriggerTime = Time.time;
        onJumpscareTriggered?.Invoke();

        ExecuteJumpscare(player);
    }

    private void ExecuteJumpscare(PlayerController player)
    {
        GameObject spawnedMonster = null;

        if (monsterPrefab != null)
        {
            spawnedMonster = Instantiate(monsterPrefab);
        }
        else
        {
            // Default monster fallback from Assets/Resources or Floor 3
            GameObject defaultPrefab = Resources.Load<GameObject>("Floors/Floor 3");
            if (defaultPrefab == null)
            {
                // Try finding any monster prefab in scene / assets
                MonsterJumpscare existingInScene = FindFirstObjectByType<MonsterJumpscare>(FindObjectsInactive.Include);
                if (existingInScene != null)
                {
                    existingInScene.Play(player, customAnchor);
                    return;
                }
            }
            else
            {
                spawnedMonster = Instantiate(defaultPrefab);
            }
        }

        if (spawnedMonster != null)
        {
            spawnedMonster.name = "Jumpscare Monster (Triggered)";
            JumpscareSequence sequence = spawnedMonster.GetComponentInChildren<JumpscareSequence>(true);
            if (sequence == null)
                sequence = spawnedMonster.AddComponent<JumpscareSequence>();

            sequence.Play(
                player,
                returnPlayerToStart ? player.ReturnToStart : null,
                () => Destroy(spawnedMonster),
                customAnchor);
        }
    }

    /// <summary>
    /// Global static helper to easily trigger a jumpscare from anywhere in one line.
    /// </summary>
    public static void TriggerGlobal(PlayerController player = null, JumpscareAnchor anchor = null)
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerController>();

        if (player == null)
        {
            Debug.LogWarning("[JumpscareTrigger] TriggerGlobal failed: No player found.");
            return;
        }

        MonsterJumpscare jumpscare = FindFirstObjectByType<MonsterJumpscare>(FindObjectsInactive.Include);
        if (jumpscare != null)
        {
            jumpscare.Play(player, anchor);
            return;
        }

        // Create temporary trigger to execute
        GameObject tempObj = new("Temp_JumpscareTrigger");
        JumpscareTrigger trigger = tempObj.AddComponent<JumpscareTrigger>();
        trigger.customAnchor = anchor;
        trigger.Trigger(player);
        Destroy(tempObj, 5f);
    }

    /// <summary>
    /// Reset the trigger state (if one-shot is disabled or for game restarts).
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
        lastTriggerTime = -999f;
        for (int i = 0; i < conditions.Count; i++)
        {
            conditions[i].ResetCondition();
        }
    }
}
