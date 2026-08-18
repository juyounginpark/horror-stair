using UnityEngine;

[DisallowMultipleComponent]
public sealed class MonsterJumpscare : MonoBehaviour
{
    [SerializeField] private JumpscareAnchor jumpscareAnchor;
    [SerializeField] private bool returnPlayerToStart = true;
    [SerializeField] private bool destroyMonsterAfterPlay = true;

    public void Play(PlayerController player, JumpscareAnchor customAnchor = null)
    {
        if (player == null)
            return;

        transform.SetParent(null, true);
        JumpscareSequence sequence = GetComponent<JumpscareSequence>();
        if (sequence == null)
            sequence = gameObject.AddComponent<JumpscareSequence>();

        JumpscareAnchor anchorToUse = customAnchor != null ? customAnchor : jumpscareAnchor;

        sequence.Play(
            player,
            returnPlayerToStart ? player.ReturnToStart : null,
            destroyMonsterAfterPlay ? () => Destroy(gameObject) : null,
            anchorToUse);
    }
}
