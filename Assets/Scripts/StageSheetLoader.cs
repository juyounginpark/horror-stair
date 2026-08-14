using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public sealed class StageSheetLoader : MonoBehaviour
{
    private const string CsvUrl =
        "https://docs.google.com/spreadsheets/d/110N_N_0JaqK8GTqtuKMfl6Z0PXhLerWw-Dva0nGKKwA/export?format=csv&gid=0";

    private static readonly Dictionary<int, string> StageTypes = new();
    private static readonly Dictionary<int, string[]> StageDialogues = new();

    public static bool IsReady { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        StageTypes.Clear();
        StageDialogues.Clear();
        IsReady = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateLoader()
    {
        GameObject loaderObject = new GameObject("Stage Sheet Loader");
        DontDestroyOnLoad(loaderObject);
        loaderObject.AddComponent<StageSheetLoader>();
    }

    public static string GetStageType(int stageNumber)
    {
        return StageTypes.TryGetValue(stageNumber, out string stageType)
            && !string.IsNullOrWhiteSpace(stageType)
            ? stageType.Trim()
            : "1";
    }

    public static string GetStageDialogue(int stageNumber, int dialogueNumber)
    {
        if (dialogueNumber is < 1 or > 4
            || !StageDialogues.TryGetValue(stageNumber, out string[] dialogues))
        {
            return string.Empty;
        }

        return dialogues[dialogueNumber - 1];
    }

    public static IEnumerator WaitUntilReady()
    {
        while (!IsReady)
        {
            yield return null;
        }
    }

    private IEnumerator Start()
    {
        using UnityWebRequest request = UnityWebRequest.Get(CsvUrl);
        request.timeout = 10;
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            ParseCsv(request.downloadHandler.text);
            Debug.Log($"Loaded {StageTypes.Count} stages from Google Sheets.");
        }
        else
        {
            Debug.LogWarning(
                $"Could not load the stage sheet. Floor Type 1 will be used as fallback. " +
                $"Reason: {request.error}");
        }

        IsReady = true;
    }

    private static void ParseCsv(string csvText)
    {
        StageTypes.Clear();
        StageDialogues.Clear();

        List<List<string>> rows = ParseCsvRows(csvText);
        if (rows.Count == 0)
        {
            return;
        }

        List<string> headers = rows[0];
        int stageNumberColumn = FindColumn(headers, "Stage Num");
        int stageTypeColumn = FindColumn(headers, "Stage Type");
        int[] dialogueColumns = new int[4];
        for (int index = 0; index < dialogueColumns.Length; index++)
        {
            int dialogueNumber = index + 1;
            dialogueColumns[index] = FindColumn(
                headers,
                $"Dialouge {dialogueNumber}",
                $"Dialogue {dialogueNumber}");
        }

        if (stageNumberColumn < 0)
        {
            Debug.LogWarning("The stage sheet does not contain a Stage Num column.");
            return;
        }

        for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            List<string> columns = rows[rowIndex];
            if (!int.TryParse(GetColumn(columns, stageNumberColumn), out int stageNumber))
            {
                continue;
            }

            string stageType = GetColumn(columns, stageTypeColumn);
            StageTypes[stageNumber] = string.IsNullOrWhiteSpace(stageType)
                ? "1"
                : stageType;

            string[] dialogues = new string[4];
            for (int index = 0; index < dialogues.Length; index++)
            {
                dialogues[index] = GetColumn(columns, dialogueColumns[index]);
            }

            StageDialogues[stageNumber] = dialogues;
        }
    }

    private static List<List<string>> ParseCsvRows(string csvText)
    {
        List<List<string>> rows = new();
        List<string> row = new();
        StringBuilder field = new();
        bool insideQuotes = false;

        for (int index = 0; index < csvText.Length; index++)
        {
            char character = csvText[index];
            if (character == '"')
            {
                if (insideQuotes && index + 1 < csvText.Length && csvText[index + 1] == '"')
                {
                    field.Append('"');
                    index++;
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }

                continue;
            }

            if (!insideQuotes && character == ',')
            {
                row.Add(field.ToString().Trim());
                field.Clear();
                continue;
            }

            if (!insideQuotes && (character == '\n' || character == '\r'))
            {
                if (character == '\r' && index + 1 < csvText.Length && csvText[index + 1] == '\n')
                {
                    index++;
                }

                row.Add(field.ToString().Trim());
                field.Clear();
                if (row.Count > 1 || !string.IsNullOrWhiteSpace(row[0]))
                {
                    rows.Add(row);
                }

                row = new List<string>();
                continue;
            }

            field.Append(character);
        }

        row.Add(field.ToString().Trim());
        if (row.Count > 1 || !string.IsNullOrWhiteSpace(row[0]))
        {
            rows.Add(row);
        }

        return rows;
    }

    private static int FindColumn(List<string> headers, params string[] names)
    {
        for (int index = 0; index < headers.Count; index++)
        {
            foreach (string name in names)
            {
                string header = headers[index].Trim().TrimStart('\uFEFF');
                if (header.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return index;
                }
            }
        }

        return -1;
    }

    private static string GetColumn(List<string> columns, int index)
    {
        return index >= 0 && index < columns.Count
            ? columns[index].Trim()
            : string.Empty;
    }
}
