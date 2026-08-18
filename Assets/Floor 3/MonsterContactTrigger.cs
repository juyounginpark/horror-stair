using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class MonsterContactTrigger : MonoBehaviour
{
    private MonsterWatcherEncounter owner;

    public void Configure(MonsterWatcherEncounter encounter)
    {
        owner = encounter;
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null && owner != null)
            owner.Catch(player);
    }
}
