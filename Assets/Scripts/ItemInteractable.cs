using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class ItemInteractable : MonoBehaviour
{
    private const string AlertObjectName = "Alert - Item";

    [Header("Interaction")]
    [SerializeField] private GameObject itemRoot;
    [SerializeField, Min(0.1f)] private float interactionDistance = 5f;

    [Header("Outline")]
    [SerializeField] private Color outlineColor = Color.red;
    [SerializeField, Range(0f, 10f)] private float outlineWidth = 4f;

    private Camera playerCamera;
    private Renderer[] itemRenderers;
    private bool hasCollider;
    private Outline itemOutline;

    private static ItemInteractable focusedItem;
    private static GameObject alertItemUI;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        focusedItem = null;
        alertItemUI = null;
    }

    private void Start()
    {
        if (itemRoot == null)
        {
            itemRoot = gameObject;
        }

        itemRenderers = itemRoot.GetComponentsInChildren<Renderer>(true);
        hasCollider = itemRoot.GetComponentInChildren<Collider>(true) != null;

        itemOutline = itemRoot.GetComponent<Outline>();
        if (itemOutline == null)
        {
            itemOutline = itemRoot.AddComponent<Outline>();
        }

        itemOutline.OutlineMode = Outline.Mode.OutlineVisible;
        itemOutline.OutlineColor = outlineColor;
        itemOutline.OutlineWidth = outlineWidth;
        itemOutline.enabled = false;

        FindAlertUIIfNeeded();
    }

    private void Update()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        bool isFocused = IsAimedAtItem();

        if (itemOutline != null)
        {
            itemOutline.enabled = isFocused;
        }

        SetFocused(isFocused);

        Keyboard keyboard = Keyboard.current;
        if (isFocused && keyboard != null && keyboard.eKey.wasPressedThisFrame)
        {
            SetFocused(false);
            itemRoot.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (itemOutline != null)
        {
            itemOutline.enabled = false;
        }

        SetFocused(false);
    }

    private void OnDestroy()
    {
        SetFocused(false);
    }

    private bool IsAimedAtItem()
    {
        if (playerCamera == null || itemRoot == null)
        {
            return false;
        }

        Ray centerRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        if (Physics.Raycast(
                centerRay,
                out RaycastHit hit,
                interactionDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
        {
            return hit.transform == itemRoot.transform
                || hit.transform.IsChildOf(itemRoot.transform);
        }

        // A Collider gives the most accurate result. Renderer bounds are a fallback
        // so the script also works immediately on a simple mesh-only object.
        if (hasCollider || itemRenderers == null)
        {
            return false;
        }

        foreach (Renderer itemRenderer in itemRenderers)
        {
            if (itemRenderer == null || !itemRenderer.enabled)
            {
                continue;
            }

            if (Vector3.Distance(playerCamera.transform.position, itemRenderer.bounds.center)
                    <= interactionDistance
                && RendererContainsScreenCenter(itemRenderer, playerCamera))
            {
                return true;
            }
        }

        return false;
    }

    private void SetFocused(bool isFocused)
    {
        FindAlertUIIfNeeded();

        if (isFocused)
        {
            focusedItem = this;
            if (alertItemUI != null && !alertItemUI.activeSelf)
            {
                alertItemUI.SetActive(true);
            }

            return;
        }

        if (focusedItem != this)
        {
            return;
        }

        focusedItem = null;
        if (alertItemUI != null && alertItemUI.activeSelf)
        {
            alertItemUI.SetActive(false);
        }
    }

    private static void FindAlertUIIfNeeded()
    {
        if (alertItemUI != null)
        {
            return;
        }

        Transform[] sceneTransforms = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Transform sceneTransform in sceneTransforms)
        {
            if (sceneTransform.name == AlertObjectName)
            {
                alertItemUI = sceneTransform.gameObject;
                return;
            }
        }
    }

    private static bool RendererContainsScreenCenter(Renderer targetRenderer, Camera targetCamera)
    {
        Bounds bounds = targetRenderer.bounds;
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;
        float minimumX = float.PositiveInfinity;
        float maximumX = float.NegativeInfinity;
        float minimumY = float.PositiveInfinity;
        float maximumY = float.NegativeInfinity;
        bool hasPointInFrontOfCamera = false;

        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                    Vector3 viewportPoint = targetCamera.WorldToViewportPoint(corner);
                    if (viewportPoint.z <= 0f)
                    {
                        continue;
                    }

                    hasPointInFrontOfCamera = true;
                    minimumX = Mathf.Min(minimumX, viewportPoint.x);
                    maximumX = Mathf.Max(maximumX, viewportPoint.x);
                    minimumY = Mathf.Min(minimumY, viewportPoint.y);
                    maximumY = Mathf.Max(maximumY, viewportPoint.y);
                }
            }
        }

        return hasPointInFrontOfCamera
            && minimumX <= 0.5f && maximumX >= 0.5f
            && minimumY <= 0.5f && maximumY >= 0.5f;
    }
}
