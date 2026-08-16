using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class MonsterWatcherEncounter : MonoBehaviour
{
    private enum EncounterState
    {
        Inactive,
        WaitingForLight,
        Watching,
        Leaving
    }

    [Header("Monster")]
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField, Min(0.1f)] private float exitSpeed = 8f;
    [SerializeField, Min(0.5f)] private float frontSpawnDistance = 2.2f;
    [SerializeField] private Vector3 lookTargetOffset = new(0f, 1.3f, 0f);
    [SerializeField, Min(0f)] private float turnSpeed = 360f;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float monsterAppearanceDelay = 0.3f;
    [SerializeField, Min(0.1f)] private float lightOnDeadline = 0.5f;
    [SerializeField, Min(0.1f)] private float stationaryClearDuration = 2f;

    [Header("Surface Detection")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField, Min(0.1f)] private float surfaceSearchHeight = 4f;
    [SerializeField, Min(0.1f)] private float surfaceSearchDistance = 10f;

    [Header("Entry Sound")]
    [SerializeField] private AudioClip entrySound;
    [SerializeField, Range(0f, 1f)] private float entrySoundVolume = 1f;

    private PlayerController player;
    private PlayerLightController lightController;
    private Camera playerCamera;
    private Transform viewTransform;
    private GameObject monsterInstance;
    private MonsterProceduralAnimator monsterAnimator;
    private Renderer[] monsterRenderers;
    private readonly Plane[] frustumPlanes = new Plane[6];
    private readonly List<Vector3> exitPath = new();
    private Quaternion watchRotation;
    private Vector3 exitDirection;
    private float stateTime;
    private float lightOffTime;
    private int pathIndex;
    private bool active;
    private bool cleared;
    private bool lightWasRelit;
    private EncounterState state;

    public bool IsActive => active;
    public bool IsCleared => cleared;
    public bool IsWatching => state == EncounterState.Watching;
    public bool IsLeaving => state == EncounterState.Leaving;
    public float LightOffTime => lightWasRelit ? 0f : lightOffTime;
    public GameObject MonsterInstance => monsterInstance;

    private void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnDestroy()
    {
        UnsubscribeFromLight();
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

        stateTime += Time.deltaTime;

        if (state == EncounterState.WaitingForLight && stateTime >= monsterAppearanceDelay)
            ShowMonsterInFront();

        if (!lightWasRelit)
        {
            lightOffTime += Time.deltaTime;
            if (lightOffTime >= lightOnDeadline)
            {
                Catch(player);
                return;
            }
        }

        if (state == EncounterState.WaitingForLight)
            return;

        if (monsterInstance == null)
            return;

        if (state == EncounterState.Watching)
        {
            FacePlayer();

            if (HasPlayerMoved())
                Catch(player);
            else if (stateTime >= stationaryClearDuration)
                StartLeaving();

            return;
        }

        if (state == EncounterState.Leaving)
        {
            MoveMonsterAlongStairs(exitSpeed * Time.deltaTime);
            if (!IsMonsterVisible())
                Clear();
        }
    }

    public void Begin(PlayerController target)
    {
        if (active || cleared || target == null || monsterPrefab == null)
            return;

        lightController = target.GetComponentInChildren<PlayerLightController>(true);
        if (lightController == null)
        {
            Debug.LogError("Monster watcher requires a PlayerLightController under Player.", this);
            return;
        }

        if (entrySound != null)
            AudioSource.PlayClipAtPoint(entrySound, target.transform.position, entrySoundVolume);

        player = target;
        playerCamera = target.GetComponentInChildren<Camera>(true);
        viewTransform = playerCamera != null ? playerCamera.transform : target.transform;
        stateTime = 0f;
        lightOffTime = 0f;
        lightWasRelit = false;
        state = EncounterState.WaitingForLight;
        active = true;
        lightController.LightStateChanged += HandleLightStateChanged;
        lightController.SetLight(false);
    }

    public void Catch(PlayerController caughtPlayer)
    {
        if (!active || caughtPlayer != player)
            return;

        MonsterJumpscare gameOver = monsterInstance != null
            ? monsterInstance.GetComponent<MonsterJumpscare>()
            : null;

        if (gameOver != null)
            gameOver.Play(caughtPlayer);
        else
            caughtPlayer.ReturnToStart();

        StopEncounter();
    }

    private void HandleLightStateChanged(bool isLightOn)
    {
        if (!active)
            return;

        if (isLightOn)
            lightWasRelit = true;
        else if (state == EncounterState.Watching)
            Catch(player);
    }

    private void ShowMonsterInFront()
    {
        Vector3 forward = viewTransform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            forward = player.transform.forward;

        Vector3 position = PlaceOnSurface(player.transform.position + forward.normalized * frontSpawnDistance);
        SpawnMonster(position);
        BuildExitPath(position);

        watchRotation = viewTransform.rotation;
        stateTime = 0f;
        state = EncounterState.Watching;
        FacePlayer(true);
    }

    private void StartLeaving()
    {
        foreach (Collider monsterCollider in monsterInstance.GetComponentsInChildren<Collider>())
            monsterCollider.enabled = false;

        if (monsterAnimator != null)
            monsterAnimator.SetRunning(true);

        pathIndex = 0;
        stateTime = 0f;
        state = EncounterState.Leaving;
    }

    private void SpawnMonster(Vector3 position)
    {
        monsterInstance = Instantiate(monsterPrefab, position, Quaternion.identity, transform);
        monsterInstance.name = "Character Monster 03";
        monsterRenderers = monsterInstance.GetComponentsInChildren<Renderer>();
        monsterAnimator = monsterInstance.GetComponentInChildren<MonsterProceduralAnimator>(true);
        if (monsterAnimator != null)
            monsterAnimator.SetRunning(false);

        MonsterContactTrigger contact = monsterInstance.GetComponent<MonsterContactTrigger>();
        if (contact != null)
            contact.Configure(this);
    }

    private bool HasPlayerMoved()
    {
        return player.HasMovementInput
            || Quaternion.Angle(watchRotation, viewTransform.rotation) > 0.5f;
    }

    private void Clear()
    {
        cleared = true;
        StopEncounter();
    }

    private void StopEncounter()
    {
        UnsubscribeFromLight();
        active = false;
        player = null;
        lightController = null;
        playerCamera = null;
        viewTransform = null;
        stateTime = 0f;
        lightOffTime = 0f;
        pathIndex = 0;
        lightWasRelit = false;
        state = EncounterState.Inactive;
        exitPath.Clear();

        if (monsterInstance != null)
            Destroy(monsterInstance);

        monsterInstance = null;
        monsterRenderers = null;
        monsterAnimator = null;
    }

    private void UnsubscribeFromLight()
    {
        if (lightController != null)
            lightController.LightStateChanged -= HandleLightStateChanged;
    }

    private Vector3 PlaceOnSurface(Vector3 position)
    {
        if (Physics.Raycast(
                position + Vector3.up * surfaceSearchHeight,
                Vector3.down,
                out RaycastHit hit,
                surfaceSearchDistance,
                groundLayers,
                QueryTriggerInteraction.Ignore))
        {
            position = hit.point;
        }

        return position;
    }

    private void BuildExitPath(Vector3 start)
    {
        StaircaseFloor floor = GetComponentInParent<StaircaseFloor>();
        Checker[] checkers = floor != null
            ? floor.GetComponentsInChildren<Checker>(true)
            : System.Array.Empty<Checker>();

        System.Array.Sort(checkers, (left, right) =>
            right.transform.position.y.CompareTo(left.transform.position.y));

        exitPath.Clear();
        for (int index = 0; index < checkers.Length; index++)
        {
            Vector3 current = checkers[index].transform.position;
            if (current.y > start.y + 0.5f)
                continue;

            exitPath.Add(current);
            if (index + 1 < checkers.Length)
                exitPath.Add(GetLandingCorner(current, checkers[index + 1].transform.position));
        }

        exitDirection = exitPath.Count > 1
            ? (exitPath[^1] - exitPath[^2]).normalized
            : viewTransform.forward;
        pathIndex = 0;
    }

    private void MoveMonsterAlongStairs(float distance)
    {
        while (distance > 0f && pathIndex < exitPath.Count)
        {
            Vector3 target = exitPath[pathIndex];
            Vector3 offset = target - monsterInstance.transform.position;
            float remaining = offset.magnitude;
            if (remaining <= distance)
            {
                monsterInstance.transform.position = target;
                distance -= remaining;
                pathIndex++;
                continue;
            }

            exitDirection = offset.normalized;
            FaceDirection(exitDirection);
            monsterInstance.transform.position += exitDirection * distance;
            return;
        }

        if (distance > 0f)
        {
            FaceDirection(exitDirection);
            monsterInstance.transform.position += exitDirection * distance;
        }
    }

    private static Vector3 GetLandingCorner(Vector3 current, Vector3 next)
    {
        float height = (current.y + next.y) * 0.5f;
        return Mathf.Abs(next.x - current.x) > Mathf.Abs(next.z - current.z)
            ? new Vector3(next.x, height, current.z)
            : new Vector3(current.x, height, next.z);
    }

    private bool IsMonsterVisible()
    {
        if (playerCamera == null || monsterRenderers == null)
            return false;

        GeometryUtility.CalculateFrustumPlanes(playerCamera, frustumPlanes);
        foreach (Renderer monsterRenderer in monsterRenderers)
        {
            if (monsterRenderer != null
                && monsterRenderer.enabled
                && GeometryUtility.TestPlanesAABB(frustumPlanes, monsterRenderer.bounds))
            {
                return true;
            }
        }

        return false;
    }

    private void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            return;

        monsterInstance.transform.rotation = Quaternion.RotateTowards(
            monsterInstance.transform.rotation,
            Quaternion.LookRotation(direction, Vector3.up),
            turnSpeed * Time.deltaTime);
    }

    private void FacePlayer(bool immediate = false)
    {
        Vector3 direction = player.transform.position + lookTargetOffset - monsterInstance.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        monsterInstance.transform.rotation = immediate || turnSpeed <= 0f
            ? targetRotation
            : Quaternion.RotateTowards(
                monsterInstance.transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime);
    }
}
