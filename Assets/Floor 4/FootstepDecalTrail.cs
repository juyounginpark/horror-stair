using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class FootstepDecalTrail : MonoBehaviour
{
    [Header("Decal Chase")]
    [SerializeField] private GameObject footprintPrefab;
    [SerializeField, Min(0.05f)] private float spawnInterval = 0.32f;
    [SerializeField, Min(0.1f)] private float chaseSpeed = 3.5f;
    [SerializeField, Min(0f)] private float startDelay = 2f;
    [SerializeField, Min(0.1f)] private float escapeVerticalDistance = 10f;
    [SerializeField, Min(0f)] private float floorEndOffset = 4f;
    [SerializeField, Min(0f)] private float floorEndHeightOffset = 2f;
    [SerializeField, Min(0f)] private float sideOffset = 0.16f;

    [Header("Catch Trigger")]
    [SerializeField] private GameObject jumpscareMonsterPrefab;
    [SerializeField] private JumpscareAnchor jumpscarePlayerAnchor;
    [SerializeField, Min(0.1f)] private float catchTriggerWidth = 6f;
    [SerializeField, Min(0.1f)] private float catchTriggerHeight = 3f;
    [SerializeField, Min(0.1f)] private float catchTriggerDepth = 1.8f;

    [Header("Surface Detection")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField, Min(0.1f)] private float surfaceSearchHeight = 3f;
    [SerializeField, Min(0.1f)] private float surfaceSearchDistance = 8f;
    [SerializeField, Min(0f)] private float surfaceOffset = 0.02f;

    [Header("Sound")]
    [SerializeField] private AudioClip footstepSound;
    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.8f;
    [SerializeField, Min(0.1f)] private float soundMinDistance = 1.5f;
    [SerializeField, Min(0.1f)] private float soundMaxDistance = 45f;

    [Header("Entry Sound")]
    [SerializeField] private AudioClip entrySound;
    [SerializeField, Range(0f, 1f)] private float entrySoundVolume = 1f;

    private readonly List<Vector3> path = new();
    private AudioClip fallbackSound;
    private Transform spawnedRoot;
    private PlayerController player;
    private Vector3 chasePosition;
    private float activationHeight;
    private float delayRemaining;
    private float spawnTimer;
    private int pathIndex;
    private bool leftFoot;
    private bool active;
    private bool cleared;

    public bool IsActive => active;
    public bool IsCleared => cleared;
    public Vector3 ChasePosition => chasePosition;

    private void Awake()
    {
        BoxCollider trigger = GetComponent<BoxCollider>();
        trigger.isTrigger = true;

        fallbackSound = CreateFootstepSound();
    }

    private void OnDestroy()
    {
        if (fallbackSound != null)
            Destroy(fallbackSound);
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController enteringPlayer = other.GetComponentInParent<PlayerController>();
        if (enteringPlayer != null)
            Begin(enteringPlayer);
    }

    private void Update()
    {
        if (!active || player == null)
            return;

        if (player.transform.position.y >= activationHeight + escapeVerticalDistance)
        {
            cleared = true;
            StopChase();
            return;
        }

        if (delayRemaining > 0f)
        {
            delayRemaining -= Time.deltaTime;
            return;
        }

        MoveChasePoint(chaseSpeed * Time.deltaTime);

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnFootprint();
            spawnTimer = spawnInterval;
        }

    }

    public void Begin(PlayerController target)
    {
        if (active || cleared || target == null)
            return;

        if (!BuildFloorPath())
            return;

        if (entrySound != null)
            AudioSource.PlayClipAtPoint(entrySound, target.transform.position, entrySoundVolume);

        player = target;
        activationHeight = target.transform.position.y;
        delayRemaining = startDelay;
        spawnTimer = 0f;
        pathIndex = 1;
        leftFoot = true;
        chasePosition = path[0];

        DisableSpawnedColliders();

        spawnedRoot = new GameObject("Spawned Footprints").transform;
        spawnedRoot.SetParent(transform, true);
        active = true;
    }

    public void Catch(PlayerController caughtPlayer)
    {
        if (!active || caughtPlayer != player)
            return;

        if (jumpscareMonsterPrefab != null)
        {
            GameObject monster = Instantiate(
                jumpscareMonsterPrefab,
                caughtPlayer.transform.position,
                Quaternion.identity,
                transform);
            MonsterJumpscare jumpscare = monster.GetComponent<MonsterJumpscare>();
            if (jumpscare == null)
                jumpscare = monster.AddComponent<MonsterJumpscare>();
            jumpscare.Play(caughtPlayer, jumpscarePlayerAnchor);
        }
        else
        {
            caughtPlayer.ReturnToStart();
        }

        StopChase();
    }

    public void StopChase()
    {
        DisableSpawnedColliders();
        active = false;
        path.Clear();
        player = null;
        spawnedRoot = null;
    }

    private void DisableSpawnedColliders()
    {
        if (spawnedRoot == null)
            return;

        foreach (FootstepCatchTrigger catchTrigger in spawnedRoot.GetComponentsInChildren<FootstepCatchTrigger>())
            catchTrigger.Disable();
    }

    private bool BuildFloorPath()
    {
        StaircaseFloor floor = GetComponentInParent<StaircaseFloor>();
        Checker[] checkers = floor != null
            ? floor.GetComponentsInChildren<Checker>(true)
            : System.Array.Empty<Checker>();

        if (checkers.Length < 2)
        {
            Debug.LogError("Footstep chase requires at least two Checker objects.", this);
            return false;
        }

        System.Array.Sort(
            checkers,
            (left, right) => left.transform.position.y.CompareTo(right.transform.position.y));

        path.Clear();
        for (int index = 0; index < checkers.Length; index++)
        {
            Vector3 current = checkers[index].transform.position;
            path.Add(current);

            if (index + 1 < checkers.Length)
                path.Add(GetLandingCorner(current, checkers[index + 1].transform.position));
        }

        path.Add(transform.position);

        Vector3 firstDirection = path[1] - path[0];
        firstDirection.y = 0f;
        firstDirection.Normalize();
        path.Insert(
            0,
            path[0] - firstDirection * floorEndOffset - Vector3.up * floorEndHeightOffset);
        return true;
    }

    private static Vector3 GetLandingCorner(Vector3 current, Vector3 next)
    {
        float height = (current.y + next.y) * 0.5f;
        return Mathf.Abs(next.x - current.x) > Mathf.Abs(next.z - current.z)
            ? new Vector3(next.x, height, current.z)
            : new Vector3(current.x, height, next.z);
    }

    private void MoveChasePoint(float distance)
    {
        while (distance > 0f && pathIndex < path.Count)
        {
            Vector3 target = path[pathIndex];
            float remaining = Vector3.Distance(chasePosition, target);
            if (remaining <= distance)
            {
                chasePosition = target;
                distance -= remaining;
                pathIndex++;
            }
            else
            {
                chasePosition = Vector3.MoveTowards(chasePosition, target, distance);
                distance = 0f;
            }
        }
    }

    private void SpawnFootprint()
    {
        if (footprintPrefab == null || spawnedRoot == null)
            return;

        Vector3 forward = GetPathDirection();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 target = chasePosition + right * (leftFoot ? -sideOffset : sideOffset);
        Vector3 point = target;
        Vector3 normal = Vector3.up;

        if (Physics.Raycast(
                target + Vector3.up * surfaceSearchHeight,
                Vector3.down,
                out RaycastHit hit,
                surfaceSearchDistance,
                groundLayers,
                QueryTriggerInteraction.Ignore))
        {
            point = hit.point;
            normal = hit.normal;
        }

        Vector3 surfaceForward = Vector3.ProjectOnPlane(forward, normal).normalized;
        if (surfaceForward.sqrMagnitude < 0.001f)
            surfaceForward = Vector3.forward;

        GameObject footprint = Instantiate(
            footprintPrefab,
            point + normal * surfaceOffset,
            Quaternion.LookRotation(-normal, surfaceForward),
            spawnedRoot);
        footprint.name = leftFoot ? "Footprint L" : "Footprint R";

        DecalProjector decal = footprint.GetComponent<DecalProjector>();
        if (decal != null)
        {
            decal.uvScale = new Vector2(leftFoot ? 1f : -1f, 1f);
            decal.uvBias = leftFoot ? Vector2.zero : new Vector2(1f, 0f);
        }

        AddCatchTrigger(footprint, point, forward);

        if (fallbackSound == null)
            fallbackSound = CreateFootstepSound();

        AudioSource source = footprint.GetComponent<AudioSource>();
        if (source == null)
            source = footprint.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.dopplerLevel = 0f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = soundMinDistance;
        source.maxDistance = Mathf.Max(soundMinDistance, soundMaxDistance);
        source.PlayOneShot(footstepSound != null ? footstepSound : fallbackSound, soundVolume);
        leftFoot = !leftFoot;
    }

    private void AddCatchTrigger(GameObject footprint, Vector3 point, Vector3 forward)
    {
        GameObject triggerObject = new("Catch Trigger");
        triggerObject.transform.SetParent(footprint.transform, true);
        triggerObject.transform.SetPositionAndRotation(
            point + Vector3.up * (catchTriggerHeight * 0.5f),
            Quaternion.LookRotation(forward, Vector3.up));

        BoxCollider trigger = triggerObject.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(
            catchTriggerWidth,
            catchTriggerHeight,
            Mathf.Max(catchTriggerDepth, chaseSpeed * spawnInterval + 0.25f));

        triggerObject.AddComponent<FootstepCatchTrigger>().Configure(this);
    }

    private Vector3 GetPathDirection()
    {
        Vector3 direction = pathIndex < path.Count
            ? path[pathIndex] - chasePosition
            : player.transform.position - chasePosition;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
    }

    private static AudioClip CreateFootstepSound()
    {
        const int sampleRate = 22050;
        const float length = 0.2f;
        int sampleCount = Mathf.RoundToInt(sampleRate * length);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float decay = Mathf.Exp(-24f * time);
            float thud = Mathf.Sin(2f * Mathf.PI * 85f * time) * 0.65f;
            float grit = Random.Range(-1f, 1f) * 0.25f;
            samples[i] = Mathf.Clamp((thud + grit) * decay, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Generated Footstep", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
