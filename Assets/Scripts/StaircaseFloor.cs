using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class StaircaseFloor : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float floorHeight = 20f;
    [SerializeField, Min(1)] private int floorsToKeepEachSide = 4;

    private static readonly Dictionary<int, StaircaseFloor> Floors = new();
    private static readonly Dictionary<string, StaircaseFloor> FloorPrefabs = new(
        System.StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<int> ShownDialogues = new();
    private static bool hasOrigin;
    private static float originHeight;

    private int floorIndex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Floors.Clear();
        FloorPrefabs.Clear();
        ShownDialogues.Clear();
        hasOrigin = false;
        originHeight = 0f;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RefreshFloorPrefabCatalog()
    {
        FloorPrefabs.Clear();

        GameObject[] floorObjects = Resources.LoadAll<GameObject>("Floors");
        foreach (GameObject floorObject in floorObjects)
        {
            StaircaseFloor floorPrefab = floorObject.GetComponent<StaircaseFloor>();
            if (floorPrefab == null || !floorObject.name.StartsWith("Floor "))
            {
                continue;
            }

            FloorPrefabs[floorObject.name.Trim()] = floorPrefab;
        }

        Debug.Log($"Refreshed Floor prefab catalog: {FloorPrefabs.Count} prefabs found.");
    }

    private void Awake()
    {
        if (!hasOrigin)
        {
            originHeight = transform.position.y;
            hasOrigin = true;
        }

        floorIndex = Mathf.RoundToInt((transform.position.y - originHeight) / floorHeight);

        Floors[floorIndex] = this;
    }

    private IEnumerator Start()
    {
        if (floorIndex == 0)
        {
            yield return StageSheetLoader.WaitUntilReady();
            if (ReplaceOriginFloorIfNeeded())
            {
                yield break;
            }

            EnsureUpcomingFloors();
        }
    }

    private void OnDestroy()
    {
        if (Floors.TryGetValue(floorIndex, out StaircaseFloor registeredFloor) && registeredFloor == this)
        {
            Floors.Remove(floorIndex);
        }
    }

    public void AreaReached(Checker.Area area)
    {
        TryShowDialogue(area);

        switch (area)
        {
            case Checker.Area.Area1:
            case Checker.Area.Area2:
                EnsureUpcomingFloors();
                RemoveDistantFloors(floorIndex);
                break;
        }
    }

    private void TryShowDialogue(Checker.Area area)
    {
        int dialogueNumber = 5 - (int)area;
        int stageNumber = Mathf.Max(1, 1 - floorIndex);
        int dialogueKey = stageNumber * 10 + dialogueNumber;

        if (ShownDialogues.Contains(dialogueKey))
        {
            return;
        }

        string dialogue = StageSheetLoader.GetStageDialogue(stageNumber, dialogueNumber);
        if (string.IsNullOrWhiteSpace(dialogue))
        {
            return;
        }

        if (DialogueUIController.Show(dialogue))
        {
            ShownDialogues.Add(dialogueKey);
        }
    }

    private void EnsureUpcomingFloors()
    {
        // Keep the next four descending stages ready using their CSV Stage Type.
        EnsureFloors(-1, 4);
    }

    private void EnsureFloors(int direction, int count)
    {
        for (int step = 1; step <= count; step++)
        {
            EnsureFloor(floorIndex + direction * step);
        }
    }

    private void EnsureFloor(int targetIndex)
    {
        if (Floors.ContainsKey(targetIndex))
        {
            return;
        }

        Vector3 targetPosition = transform.position;
        targetPosition.y += (targetIndex - floorIndex) * floorHeight;

        int stageNumber = Mathf.Max(1, 1 - targetIndex);
        string requestedType = StageSheetLoader.GetStageType(stageNumber);
        StaircaseFloor selectedPrefab = GetFloorPrefab(requestedType, out string resolvedType);

        StaircaseFloor newFloor = Instantiate(selectedPrefab, targetPosition, transform.rotation);
        newFloor.name = $"Floor {resolvedType} - Stage {stageNumber}";
    }

    private StaircaseFloor GetFloorPrefab(string requestedType, out string resolvedType)
    {
        string normalizedType = string.IsNullOrWhiteSpace(requestedType)
            ? "1"
            : requestedType.Trim();
        string expectedPrefabName = $"Floor {normalizedType}";

        if (FloorPrefabs.TryGetValue(expectedPrefabName, out StaircaseFloor matchedPrefab))
        {
            resolvedType = normalizedType;
            return matchedPrefab;
        }

        if (FloorPrefabs.TryGetValue("Floor 1", out StaircaseFloor defaultPrefab))
        {
            resolvedType = "1";
            Debug.LogWarning(
                $"{expectedPrefabName} was not found. Using Floor 1 instead.",
                this);
            return defaultPrefab;
        }

        resolvedType = "1";
        Debug.LogError(
            $"No Floor 1 prefab was found in Resources/Floors. Using the current floor.",
            this);
        return this;
    }

    private bool ReplaceOriginFloorIfNeeded()
    {
        int stageNumber = Mathf.Max(1, 1 - floorIndex);
        string requestedType = StageSheetLoader.GetStageType(stageNumber);
        StaircaseFloor selectedPrefab = GetFloorPrefab(requestedType, out string resolvedType);
        string expectedName = $"Floor {resolvedType} - Stage {stageNumber}";

        if (name.Equals(expectedName, System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        StaircaseFloor replacement = Instantiate(
            selectedPrefab,
            transform.position,
            transform.rotation);
        replacement.floorIndex = floorIndex;
        replacement.name = expectedName;
        Destroy(gameObject);
        return true;
    }

    private void RemoveDistantFloors(int centerIndex)
    {
        List<StaircaseFloor> floorsToRemove = new();

        foreach (KeyValuePair<int, StaircaseFloor> floor in Floors)
        {
            if (Mathf.Abs(floor.Key - centerIndex) > floorsToKeepEachSide)
            {
                floorsToRemove.Add(floor.Value);
            }
        }

        foreach (StaircaseFloor floor in floorsToRemove)
        {
            if (floor != null)
            {
                Destroy(floor.gameObject);
            }
        }
    }
}
