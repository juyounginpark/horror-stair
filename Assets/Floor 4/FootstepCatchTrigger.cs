using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class FootstepCatchTrigger : MonoBehaviour
{
    private BoxCollider trigger;
    private FootstepDecalTrail owner;

    public void Configure(FootstepDecalTrail chaseOwner)
    {
        owner = chaseOwner;
        trigger = GetComponent<BoxCollider>();
        trigger.isTrigger = true;
    }

    public void Disable()
    {
        if (trigger != null)
            trigger.enabled = false;

        enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null)
            owner.Catch(player);
    }
}
