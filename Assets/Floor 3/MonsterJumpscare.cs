using UnityEngine;

[DisallowMultipleComponent]
public sealed class MonsterJumpscare : MonoBehaviour
{
    public void Play(PlayerController player)
    {
        if (player != null)
            player.ReturnToStart();
    }
}
