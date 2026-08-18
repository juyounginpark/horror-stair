using System;
using UnityEngine;

/// <summary>
/// Interface for modular jumpscare trigger conditions.
/// Implement this on any component to create custom triggers (e.g. Health, Zone, Timeout, Gimmick).
/// </summary>
public interface IJumpscareCondition
{
    /// <summary>
    /// Event fired when the condition is met. Passes the triggering player if known.
    /// </summary>
    event Action<PlayerController> OnConditionMet;

    /// <summary>
    /// Whether the condition is currently satisfied.
    /// </summary>
    bool IsConditionMet { get; }

    /// <summary>
    /// Reset the condition state (for reusable triggers).
    /// </summary>
    void ResetCondition();
}
