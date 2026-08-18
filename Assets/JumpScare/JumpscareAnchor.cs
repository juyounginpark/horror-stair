using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class JumpscareAnchor : MonoBehaviour
{
    private static readonly List<JumpscareAnchor> ActiveAnchors = new();

    [Tooltip("Optional specific spawn transform for the monster silhouette. If null, uses front direction.")]
    [SerializeField] private Transform monsterSpawnPoint;

    [Tooltip("Distance in front of the anchor to spawn the monster if monsterSpawnPoint is null.")]
    [SerializeField, Min(1f)] private float monsterDistance = 3.5f;

    public Transform MonsterSpawnPoint => monsterSpawnPoint;
    public float MonsterDistance => monsterDistance;

    private void OnEnable()
    {
        if (!ActiveAnchors.Contains(this))
            ActiveAnchors.Add(this);
    }

    private void OnDisable()
    {
        ActiveAnchors.Remove(this);
    }

    public static JumpscareAnchor FindNearest(Vector3 position)
    {
        JumpscareAnchor bestAnchor = null;
        float bestSqrDistance = float.PositiveInfinity;

        // 1. Check registered active anchors
        for (int i = 0; i < ActiveAnchors.Count; i++)
        {
            JumpscareAnchor anchor = ActiveAnchors[i];
            if (anchor == null || !anchor.isActiveAndEnabled)
                continue;

            float sqrDist = (anchor.transform.position - position).sqrMagnitude;
            if (sqrDist < bestSqrDistance)
            {
                bestSqrDistance = sqrDist;
                bestAnchor = anchor;
            }
        }

        if (bestAnchor != null)
            return bestAnchor;

        // 2. Fallback: Find objects with JumpscareAnchor in scene
        JumpscareAnchor[] foundAnchors = Object.FindObjectsByType<JumpscareAnchor>(FindObjectsSortMode.None);
        for (int i = 0; i < foundAnchors.Length; i++)
        {
            JumpscareAnchor anchor = foundAnchors[i];
            if (anchor == null || !anchor.isActiveAndEnabled)
                continue;

            float sqrDist = (anchor.transform.position - position).sqrMagnitude;
            if (sqrDist < bestSqrDistance)
            {
                bestSqrDistance = sqrDist;
                bestAnchor = anchor;
            }
        }

        return bestAnchor;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.4f);
        Gizmos.DrawRay(transform.position, transform.forward * 1.5f);

        if (monsterSpawnPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(monsterSpawnPoint.position, 0.4f);
            Gizmos.DrawLine(transform.position, monsterSpawnPoint.position);
        }
        else
        {
            Gizmos.color = Color.red;
            Vector3 spawnTarget = transform.position + transform.forward * monsterDistance;
            Gizmos.DrawWireSphere(spawnTarget, 0.4f);
            Gizmos.DrawLine(transform.position, spawnTarget);
        }
    }
}
