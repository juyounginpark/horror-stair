using UnityEngine;

[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public sealed class Checker : MonoBehaviour
{
    public enum Area
    {
        Area1 = 1,
        Area2 = 2,
        Area3 = 3,
        Area4 = 4
    }

    [SerializeField] private Area area = Area.Area1;
    [SerializeField] private bool readAreaFromTag = true;

    public Area CurrentArea => area;

    private void Awake()
    {
        if (readAreaFromTag && TryReadAreaFromHierarchy(out Area detectedArea))
        {
            area = detectedArea;
        }

        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() == null)
        {
            return;
        }

        StaircaseFloor floor = GetComponentInParent<StaircaseFloor>();
        if (floor != null)
        {
            floor.AreaReached(area);
        }
    }

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private bool TryReadAreaFromHierarchy(out Area detectedArea)
    {
        for (Transform current = transform; current != null; current = current.parent)
        {
            if (int.TryParse(current.tag, out int tagNumber) && tagNumber is >= 1 and <= 4)
            {
                detectedArea = (Area)tagNumber;
                return true;
            }

            foreach (char character in current.name)
            {
                if (character is >= '1' and <= '4')
                {
                    detectedArea = (Area)(character - '0');
                    return true;
                }
            }
        }

        detectedArea = area;
        return false;
    }
}
