using System.Collections;
using TMPro;
using UnityEngine;

public sealed class DialogueUIController : MonoBehaviour
{
    private const float DisplayDuration = 3f;

    private static DialogueUIController instance;

    private TMP_Text dialogueText;
    private GameObject dialogueRoot;
    private Coroutine hideCoroutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateController()
    {
        GameObject controllerObject = new GameObject("Dialogue UI Controller");
        controllerObject.AddComponent<DialogueUIController>();
    }

    public static bool Show(string dialogue)
    {
        if (instance == null || instance.dialogueText == null || instance.dialogueRoot == null)
        {
            Debug.LogWarning("Dialogue UI를 찾지 못했습니다. Dialogue 이름의 TMP 텍스트를 확인해 주세요.");
            return false;
        }

        instance.ShowInternal(dialogue);
        return true;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        FindDialogueUI();
        if (dialogueRoot != null)
        {
            dialogueRoot.SetActive(false);
        }
    }

    private void FindDialogueUI()
    {
        TMP_Text[] texts = FindObjectsByType<TMP_Text>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (TMP_Text text in texts)
        {
            Transform current = text.transform;
            while (current != null)
            {
                string objectName = current.name.ToLowerInvariant();
                if (objectName.Contains("dialogue") || objectName.Contains("dialouge"))
                {
                    dialogueText = text;
                    dialogueRoot = current.gameObject;
                    return;
                }

                current = current.parent;
            }
        }
    }

    private void ShowInternal(string dialogue)
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        dialogueText.text = dialogue.Trim();
        dialogueRoot.SetActive(true);
        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(DisplayDuration);
        dialogueRoot.SetActive(false);
        hideCoroutine = null;
    }
}
