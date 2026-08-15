using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class MonsterWatcherEncounter : MonoBehaviour
{
    [Header("Monster")]
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField, Min(0.1f)] private float moveSpeed = 2.5f;
    [SerializeField, Min(0f)] private float spawnDistanceBelowFloor = 2f;
    [SerializeField] private Vector3 lookTargetOffset = new(0f, 1.3f, 0f);
    [SerializeField, Min(0f)] private float turnSpeed = 360f;

    [Header("Clear Condition")]
    [SerializeField, Min(0.1f)] private float requiredLightOffDuration = 5f;

    [Header("Surface Detection")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField, Min(0.1f)] private float surfaceSearchHeight = 4f;
    [SerializeField, Min(0.1f)] private float surfaceSearchDistance = 10f;

    private PlayerController player;
    private PlayerLightController lightController;
    private GameObject monsterInstance;
    private readonly List<Vector3> chasePath = new();
    private float lightOffTime;
    private int pathIndex;
    private bool active;
    private bool cleared;

    public bool IsActive => active;
    public bool IsCleared => cleared;
    public float LightOffTime => lightOffTime;
    public GameObject MonsterInstance => monsterInstance;

    private void Awake()
    {
        BoxCollider trigger = GetComponent<BoxCollider>();
        trigger.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController enteringPlayer = other.GetComponentInParent<PlayerController>();
        if (enteringPlayer != null)
            Begin(enteringPlayer);
    }

    private void Update()
    {
        if (!active || player == null || monsterInstance == null)
            return;

        FacePlayer();

        if (lightController == null || lightController.IsLightOn)
        {
            lightOffTime = 0f;
            MoveMonster(moveSpeed * Time.deltaTime);
            return;
        }

        lightOffTime += Time.deltaTime;
        if (lightOffTime >= requiredLightOffDuration)
            Clear();
    }

    public void Begin(PlayerController target)
    {
        if (active || cleared || target == null || monsterPrefab == null)
            return;

        if (!BuildChasePath())
            return;

        lightController = FindFirstObjectByType<PlayerLightController>();
        if (lightController == null)
        {
            Debug.LogError("Monster watcher requires a PlayerLightController.", this);
            return;
        }

        player = target;
        lightOffTime = 0f;
        pathIndex = 1;
        monsterInstance = Instantiate(monsterPrefab, chasePath[0], Quaternion.identity, transform);
        monsterInstance.name = "Monster Silhouette";
        monsterInstance.GetComponent<MonsterContactTrigger>().Configure(this);
        active = true;
        FacePlayer(true);
    }

    public void Catch(PlayerController caughtPlayer)
    {
        if (!active || caughtPlayer != player)
            return;

        MonsterJumpscare gameOver = monsterInstance.GetComponent<MonsterJumpscare>();
        if (gameOver != null)
            gameOver.Play(caughtPlayer);
        else
            caughtPlayer.ReturnToStart();

        StopEncounter();
    }

    private void Clear()
    {
        cleared = true;
        StopEncounter();
    }

    private void StopEncounter()
    {
        active = false;
        player = null;
        lightOffTime = 0f;
        pathIndex = 0;
        chasePath.Clear();

        if (monsterInstance != null)
            Destroy(monsterInstance);

        monsterInstance = null;
    }

    private bool BuildChasePath()
    {
        StaircaseFloor floor = GetComponentInParent<StaircaseFloor>();
        Checker[] checkers = floor != null
            ? floor.GetComponentsInChildren<Checker>(true)
            : System.Array.Empty<Checker>();

        if (checkers.Length == 0)
        {
            Debug.LogError("Monster watcher requires at least one Checker.", this);
            return false;
        }

        System.Array.Sort(checkers, (left, right) =>
            left.transform.position.y.CompareTo(right.transform.position.y));

        chasePath.Clear();
        chasePath.Add(GetSpawnPosition(checkers));

        for (int index = 0; index < checkers.Length; index++)
        {
            Vector3 current = checkers[index].transform.position;
            chasePath.Add(current);

            if (index + 1 < checkers.Length)
                chasePath.Add(GetLandingCorner(current, checkers[index + 1].transform.position));
        }

        chasePath.Add(transform.position);
        return true;
    }

    private Vector3 GetSpawnPosition(Checker[] checkers)
    {
        Vector3 position = checkers[0].transform.position;
        if (checkers.Length > 1)
        {
            Vector3 upstairs = checkers[1].transform.position - position;
            upstairs.y = 0f;
            if (upstairs.sqrMagnitude > 0.001f)
                position -= upstairs.normalized * spawnDistanceBelowFloor;
        }

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

    private void MoveMonster(float distance)
    {
        while (distance > 0f && pathIndex < chasePath.Count)
        {
            Vector3 target = chasePath[pathIndex];
            float remaining = Vector3.Distance(monsterInstance.transform.position, target);
            if (remaining <= distance)
            {
                monsterInstance.transform.position = target;
                distance -= remaining;
                pathIndex++;
            }
            else
            {
                monsterInstance.transform.position = Vector3.MoveTowards(
                    monsterInstance.transform.position,
                    target,
                    distance);
                distance = 0f;
            }
        }

        if (distance > 0f)
        {
            monsterInstance.transform.position = Vector3.MoveTowards(
                monsterInstance.transform.position,
                player.transform.position,
                distance);
        }
    }

    private static Vector3 GetLandingCorner(Vector3 current, Vector3 next)
    {
        float height = (current.y + next.y) * 0.5f;
        return Mathf.Abs(next.x - current.x) > Mathf.Abs(next.z - current.z)
            ? new Vector3(next.x, height, current.z)
            : new Vector3(current.x, height, next.z);
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
