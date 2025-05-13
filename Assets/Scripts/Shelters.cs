using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;

public class Shelters : MonoBehaviour
{
    [Header("UI References")]
    public GameObject itemPrefab;
    public Transform listContent;
    public ScrollRect scrollRect;
    public GameObject locationPanel;
    public TextMeshProUGUI resultText;

    [Header("Google API")]
    public string apiKey;

    [Header("Test Settings")]
    public bool useTestLocation = false; 
    public Vector2 testLocation = new Vector2(31.5204f, 74.3587f);

    private float userLatitude;
    private float userLongitude;
    private Coroutine clearTextCoroutine;
    private bool locationInitialized = false;

    void Start()
    {
        if (Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            StartCoroutine(InitializeLocation());
        }
        else
        {
            Permission.RequestUserPermission(Permission.FineLocation);
        }
    }

    IEnumerator InitializeLocation()
    {
        if (useTestLocation)
        {
            userLatitude = testLocation.x;
            userLongitude = testLocation.y;
            SetResultText($"Using test location: {userLatitude}, {userLongitude}", 3f);
            locationInitialized = true;
            yield break;
        }

        if (!Input.location.isEnabledByUser)
        {
            SetResultText("Location services not enabled. Using default location.", 3f);
            userLatitude = testLocation.x;
            userLongitude = testLocation.y;
            locationInitialized = true;
            yield break;
        }

        Input.location.Start(5f, 5f);

        int maxWait = 10;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (maxWait < 1 || Input.location.status == LocationServiceStatus.Failed)
        {
            SetResultText("Location service failed. Using default location.", 3f);
            userLatitude = testLocation.x;
            userLongitude = testLocation.y;
        }
        else
        {
            userLatitude = Input.location.lastData.latitude;
            userLongitude = Input.location.lastData.longitude;
            SetResultText($"Using your location: {userLatitude}, {userLongitude}", 3f);
        }

        locationInitialized = true;
        Input.location.Stop();
    }

    public void OnFindNearbyButtonClicked()
    {
        if (!locationInitialized)
        {
            SetResultText("Location not ready yet. Please wait...", 3f);
            return;
        }

        Debug.Log("Location initialized successfully.");
        locationPanel.SetActive(true);
        StartCoroutine(FetchAndDisplayAllNearby());
    }

    IEnumerator FetchAndDisplayAllNearby()
    {
        foreach (Transform child in listContent)
        {
            Destroy(child.gameObject);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(listContent as RectTransform);
        yield return null;

        SetResultText("Searching for nearby clinics...", 0f);

        List<GooglePlaceResult> combinedResults = new List<GooglePlaceResult>();

        string url = $"https://maps.googleapis.com/maps/api/place/nearbysearch/json?location={userLatitude},{userLongitude}&radius=5000&type=veterinary_care&key={apiKey}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                GooglePlacesResponse response = JsonUtility.FromJson<GooglePlacesResponse>(request.downloadHandler.text);
                combinedResults.AddRange(response.results);
            }
            else
            {
                Debug.LogError("API Request Error: " + request.error);
                SetResultText("API Request Failed: " + request.error, 3f);
                yield break;
            }
        }

        if (combinedResults.Count > 0)
        {
            foreach (GooglePlaceResult place in combinedResults)
            {
                GameObject item = Instantiate(itemPrefab, listContent);
                
                item.SetActive(false);
                item.SetActive(true);
                
                TextMeshProUGUI itemText = item.GetComponentInChildren<TextMeshProUGUI>(true);
                RectTransform rectTransform = item.GetComponent<RectTransform>();
                
                if (itemText != null)
                {
                    string mapsLink = $"https://www.google.com/maps/search/?api=1&query={place.geometry.location.lat},{place.geometry.location.lng}&query_place_id={place.place_id}";
                    
                    itemText.text = $"<b>{place.name}</b>\n{place.vicinity}\n<color=blue><u>View on Maps</u></color>";
                    itemText.ForceMeshUpdate();

                    Button button = item.GetComponent<Button>();
                    if (button == null) button = item.AddComponent<Button>();
                    
                    button.transition = Selectable.Transition.None;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => {
                        Debug.Log("Opening: " + mapsLink);
                        Application.OpenURL(mapsLink);
                    });
                }
            }

            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
            LayoutRebuilder.ForceRebuildLayoutImmediate(listContent as RectTransform);
            SetResultText($"Found {combinedResults.Count} nearby clinics.", 3f);
        }
        else
        {
            SetResultText("No nearby veterinary clinics found.", 3f);
        }
    }

    private void SetResultText(string message, float clearAfterSeconds)
    {
        resultText.text = message;
        
        if (clearTextCoroutine != null)
        {
            StopCoroutine(clearTextCoroutine);
        }
        
        if (clearAfterSeconds > 0)
        {
            clearTextCoroutine = StartCoroutine(ClearResultTextAfterDelay(clearAfterSeconds));
        }
    }

    private IEnumerator ClearResultTextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        resultText.text = "";
        clearTextCoroutine = null;
    }
}

[System.Serializable]
public class GooglePlacesResponse
{
    public List<GooglePlaceResult> results;
}

[System.Serializable]
public class GooglePlaceResult
{
    public string name;
    public string vicinity;
    public string place_id;
    public Geometry geometry;
}

[System.Serializable]
public class Geometry
{
    public Location location;
}

[System.Serializable]
public class Location
{
    public float lat;
    public float lng;
}
