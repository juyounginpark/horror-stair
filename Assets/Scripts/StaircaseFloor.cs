using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class StaircaseFloor : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float floorHeight = 20f;
    [SerializeField, Min(1)] private int floorsToKeepEachSide = 3;
    [SerializeField] private StaircaseFloor floorEndPrefab;
    [SerializeField] private bool isFloorEnd;

    private static readonly Dictionary<int, StaircaseFloor> Floors = new();
    private static StaircaseFloor floorEndInstance;
    private static bool hasOrigin;
    private static float originHeight;

    private int floorIndex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Floors.Clear();
        floorEndInstance = null;
        hasOrigin = false;
        originHeight = 0f;
    }

    private void Awake()
    {
        if (!hasOrigin)
        {
            originHeight = transform.position.y;
            hasOrigin = true;
        }

        floorIndex = Mathf.RoundToInt((transform.position.y - originHeight) / floorHeight);

        if (isFloorEnd)
        {
            floorEndInstance = this;
        }
        else
        {
            Floors[floorIndex] = this;
        }
    }

    private void Start()
    {
        if (!isFloorEnd && floorIndex == 0)
        {
            MaintainEndingBelow();
        }
    }

    private void OnDestroy()
    {
        if (isFloorEnd && floorEndInstance == this)
        {
            floorEndInstance = null;
        }
        else if (Floors.TryGetValue(floorIndex, out StaircaseFloor registeredFloor) && registeredFloor == this)
        {
            Floors.Remove(floorIndex);
        }
    }

    public void AreaReached(Checker.Area area)
    {
        if (isFloorEnd)
        {
            return;
        }

        switch (area)
        {
            case Checker.Area.Area1:
            case Checker.Area.Area2:
                MaintainEndingBelow();
                RemoveDistantFloors(floorIndex);
                break;
        }
    }

    private void MaintainEndingBelow()
    {
        // Move the unreachable ending first, then fill the three playable floors.
        EnsureFloorEnd(floorIndex - 4);
        EnsureFloors(-1, 3);
    }

    private void EnsureFloorEnd(int targetIndex)
    {
        Vector3 targetPosition = transform.position;
        targetPosition.y += (targetIndex - floorIndex) * floorHeight;

        if (floorEndInstance != null)
        {
            floorEndInstance.floorIndex = targetIndex;
            floorEndInstance.transform.SetPositionAndRotation(targetPosition, transform.rotation);
            floorEndInstance.name = $"Floor End ({targetIndex:+0;-0;0})";
            return;
        }

        if (floorEndPrefab == null)
        {
            Debug.LogError("Floor End prefab is not assigned.", this);
            return;
        }

        floorEndInstance = Instantiate(floorEndPrefab, targetPosition, transform.rotation);
        floorEndInstance.floorIndex = targetIndex;
        floorEndInstance.name = $"Floor End ({targetIndex:+0;-0;0})";
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

        StaircaseFloor newFloor = Instantiate(this, targetPosition, transform.rotation);
        newFloor.name = $"Floor ({targetIndex:+0;-0;0})";
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
