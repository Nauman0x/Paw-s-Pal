using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.IO;
using System.Text;
using TMPro;  // Add this line for TextMeshPro support

[Serializable]
public class GeminiResponse
{
    public Candidate[] candidates;
}

[Serializable]
public class Candidate
{
    public Content content;
}

[Serializable]
public class Content
{
    public Part[] parts;
}

[Serializable]
public class Part
{
    public string text;
}

public class ChatBot : MonoBehaviour
{
    public Button uploadButton;
    public Button sendButton;
    public Button firstAidButton;
    public TMP_InputField injuryInput;  // Updated from InputField to TMP_InputField
    public Transform chatContent;
    public GameObject userMessagePrefab;
    public GameObject botMessagePrefab;
    public ScrollRect chatScrollRect;
    public GameObject chatPanel;

    private string base64Image = "";
    [Header("Google API")]
    public string apiKey;

    void Start()
    {
        uploadButton.onClick.AddListener(PickImage);
        sendButton.onClick.AddListener(() => StartCoroutine(SendToGemini()));
        uploadButton.onClick.AddListener(ShowChatPanel);
        firstAidButton.onClick.AddListener(ShowChatPanel);
    }

    void ShowChatPanel()
    {
        chatPanel.SetActive(true);
    }

    void PickImage()
    {
        NativeGallery.GetImageFromGallery((path) =>
        {
            if (path != null)
            {
                byte[] imageBytes = File.ReadAllBytes(path);
                base64Image = Convert.ToBase64String(imageBytes);
                AddMessage("Image uploaded.", true);
            }
            else
            {
                AddMessage("No image selected.", true);
            }
        }, "Select Animal Image", "image/*");
    }

    IEnumerator SendToGemini()
    {
        if (string.IsNullOrEmpty(base64Image))
        {
            AddMessage("Please upload an image first.", true);
            yield break;
        }

        string injuryText = injuryInput.text;  // This remains the same, now working with TMP_InputField
        if (string.IsNullOrEmpty(injuryText))
        {
            AddMessage("Please enter injury details.", true);
            yield break;
        }

        AddMessage(injuryText, true);

        string prompt = $"Provide veterinary first aid for this animal with: {injuryText}. Be concise (max 150 words).";

        string jsonData = $@"
        {{
          ""contents"": [{{
            ""parts"": [
              {{
                ""inline_data"": {{
                  ""mime_type"": ""image/png"",
                  ""data"": ""{base64Image}""
                }}
              }},
              {{
                ""text"": ""{prompt}""
              }}
            ]
          }}]
        }}";

        string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                string extractedText = ExtractGeminiResponse(response);
                AddMessage(FormatMarkdownToRichText(extractedText), false);
            }
            else
            {
                AddMessage("Error: " + request.error, false);
            }
        }
    }

    void AddMessage(string text, bool isUser)
    {
        GameObject prefab = isUser ? userMessagePrefab : botMessagePrefab;
        GameObject messageObj = Instantiate(prefab, chatContent);
        TMP_Text messageText = messageObj.GetComponentInChildren<TMP_Text>();

        if (messageText != null)
        {
            messageText.text = FormatMarkdownToRichText(text);
            messageText.enableWordWrapping = true;
            messageText.overflowMode = TextOverflowModes.Overflow;

            ContentSizeFitter contentSizeFitter = messageObj.GetComponent<ContentSizeFitter>();
            if (contentSizeFitter == null)
            {
                contentSizeFitter = messageObj.AddComponent<ContentSizeFitter>();
            }
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
        else
        {
            Debug.LogWarning("Message prefab is missing TMP_Text component!");
        }

        Canvas.ForceUpdateCanvases();
        chatScrollRect.verticalNormalizedPosition = 0f;
    }

    string ExtractGeminiResponse(string json)
    {
        try
        {
            GeminiResponse geminiResponse = JsonUtility.FromJson<GeminiResponse>(json);
            if (geminiResponse.candidates != null &&
                geminiResponse.candidates.Length > 0 &&
                geminiResponse.candidates[0].content.parts != null &&
                geminiResponse.candidates[0].content.parts.Length > 0)
            {
                return geminiResponse.candidates[0].content.parts[0].text;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("Failed to parse Gemini response: " + e.Message);
        }
        return "Could not parse response.";
    }

    string FormatMarkdownToRichText(string text)
    {
        while (text.Contains("**"))
        {
            int start = text.IndexOf("**");
            int end = text.IndexOf("**", start + 2);
            if (end == -1) break;

            string boldText = text.Substring(start + 2, end - start - 2);
            text = text.Substring(0, start) + "<b>" + boldText + "</b>" + text.Substring(end + 2);
        }

        return text;
    }
}
