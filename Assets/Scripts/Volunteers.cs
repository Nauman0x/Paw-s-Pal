using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class Volunteers : MonoBehaviour
{
    [Header("UI References")]
    public GameObject volunteersPanel;
    public ScrollRect volunteersScrollView;
    public GameObject volunteerItemPrefab;
    public Transform volunteersContent;
    public TextMeshProUGUI volunteersStatusText;
    public float itemSpacing = 10f;

    [Header("Google Sheets API")]
    public string apiKey;
    public string spreadsheetID;
    public string sheetName = "Volunteers";
    public string range = "A2:C";

    private Coroutine fetchCoroutine;
    private readonly float statusMessageDuration = 3f;

    void Start()
    {
        InitializeUI();
    }

    private void InitializeUI()
    {
        var layoutGroup = volunteersContent.GetComponent<VerticalLayoutGroup>() ??
                          volunteersContent.gameObject.AddComponent<VerticalLayoutGroup>();

        layoutGroup.spacing = itemSpacing;
        layoutGroup.childAlignment = TextAnchor.UpperLeft;
        layoutGroup.childControlHeight = false;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childForceExpandWidth = true;

        var sizeFitter = volunteersContent.GetComponent<ContentSizeFitter>() ??
                         volunteersContent.gameObject.AddComponent<ContentSizeFitter>();

        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        if (volunteersScrollView != null)
        {
            volunteersScrollView.content = volunteersContent.GetComponent<RectTransform>();
            volunteersScrollView.viewport = volunteersScrollView.GetComponent<RectTransform>();
            volunteersScrollView.horizontal = false;
            volunteersScrollView.vertical = true;
        }
    }

    public void ToggleVolunteersPanel()
    {
        volunteersPanel.SetActive(!volunteersPanel.activeSelf);

        if (volunteersPanel.activeSelf)
        {
            FetchVolunteersData();
        }
        else if (fetchCoroutine != null)
        {
            StopCoroutine(fetchCoroutine);
        }
    }

    public void FetchVolunteersData()
    {
        if (fetchCoroutine != null)
        {
            StopCoroutine(fetchCoroutine);
        }
        fetchCoroutine = StartCoroutine(FetchVolunteersFromGoogleSheets());
    }

    IEnumerator FetchVolunteersFromGoogleSheets()
    {
        ClearVolunteersList();
        SetStatusMessage("Loading volunteers...", false);

        string url = $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetID}/values/{sheetName}!{range}?key={apiKey}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                ProcessAPIResponse(request.downloadHandler.text);
            }
            else
            {
                HandleRequestError(request);
            }
        }
    }

    private void ProcessAPIResponse(string rawResponse)
    {
        Debug.Log("Raw API Response: " + rawResponse);

        var allMatches = Regex.Matches(rawResponse, @"\[\s*""(.*?)"",\s*""(.*?)"",\s*""(.*?)""\s*\]");
        List<List<string>> volunteerData = new List<List<string>>();

        foreach (Match match in allMatches)
        {
            string name = match.Groups[1].Value;
            string contact = match.Groups[2].Value;
            string status = match.Groups[3].Value;
            volunteerData.Add(new List<string> { name, contact, status });
        }

        if (volunteerData.Count > 0)
        {
            StartCoroutine(DisplayVolunteersCoroutine(volunteerData));
        }
        else
        {
            SetStatusMessage("No volunteer data found", true);
        }
    }

    IEnumerator DisplayVolunteersCoroutine(List<List<string>> volunteersData)
    {
        yield return null;

        foreach (var volunteer in volunteersData)
        {
            CreateVolunteerUIItem(volunteer[0], volunteer[1], volunteer[2]);
            yield return null;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(volunteersContent as RectTransform);
        Canvas.ForceUpdateCanvases();

        if (volunteersScrollView != null)
        {
            volunteersScrollView.verticalNormalizedPosition = 1;
        }

        SetStatusMessage($"Loaded {volunteersData.Count} volunteers", true);
    }

    private void CreateVolunteerUIItem(string name, string contact, string status)
    {
        if (volunteerItemPrefab == null || volunteersContent == null)
        {
            Debug.LogError("UI references not set!");
            return;
        }

        GameObject newItem = Instantiate(volunteerItemPrefab, volunteersContent);
        newItem.SetActive(true);

        var rectTransform = newItem.GetComponent<RectTransform>();
        rectTransform.pivot = new Vector2(0, 1);
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.sizeDelta = new Vector2(0, 80f);

        var layoutElement = newItem.GetComponent<LayoutElement>() ?? newItem.AddComponent<LayoutElement>();
        layoutElement.minHeight = 5f;
        layoutElement.preferredHeight = -1;

        TMP_Text textComponent = newItem.GetComponentInChildren<TMP_Text>();
        if (textComponent != null)
        {
            string cleanedStatus = status.Trim().ToLower();
            string statusColor = cleanedStatus == "available" ? "#00FF00" : "#FF0000";

            textComponent.text = $"<b>{name.Trim()}</b>\n" +
                                 $"<size=80%>Contact: {contact.Trim()}</size>\n" +
                                 $"<size=80%>Status: <color={statusColor}>{status.Trim()}</color></size>";

            var textRect = textComponent.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.offsetMin = new Vector2(10, 5);
            textRect.offsetMax = new Vector2(-10, -5);
        }

        if (!string.IsNullOrEmpty(contact))
        {
            SetupContactButton(newItem, contact);
        }
    }

    private void SetupContactButton(GameObject item, string contact)
    {
        Button button = item.GetComponent<Button>() ?? item.AddComponent<Button>();

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.9f);
        button.colors = colors;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            string phoneNumber = Regex.Replace(contact, @"[^\d+]", "");
            if (!string.IsNullOrEmpty(phoneNumber))
            {
#if UNITY_ANDROID || UNITY_IOS
                Application.OpenURL("tel:" + phoneNumber);
#else
                Debug.Log($"Would call: {phoneNumber}");
#endif
            }
        });
    }

    private void HandleRequestError(UnityWebRequest request)
    {
        string errorMessage = request.error;

        if (request.responseCode == 403) errorMessage = "API access denied";
        if (request.responseCode == 404) errorMessage = "Spreadsheet not found";

        Debug.LogError($"API Error: {errorMessage}");
        SetStatusMessage(errorMessage, true);
    }

    private void ClearVolunteersList()
    {
        foreach (Transform child in volunteersContent)
        {
            Destroy(child.gameObject);
        }
    }

    private void SetStatusMessage(string message, bool autoClear)
    {
        if (volunteersStatusText != null)
        {
            volunteersStatusText.text = message;
            if (autoClear) Invoke(nameof(ClearStatusMessage), statusMessageDuration);
        }
    }

    private void ClearStatusMessage()
    {
        if (volunteersStatusText != null)
        {
            volunteersStatusText.text = "";
        }
    }
}
