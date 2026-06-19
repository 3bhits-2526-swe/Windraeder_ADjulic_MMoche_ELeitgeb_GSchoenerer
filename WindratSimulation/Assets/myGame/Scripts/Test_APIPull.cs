using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class WindyDataFetcher : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI weatherText;

    [Header("API Settings")]
    private string apiKey = "sEokVTuSMAVYCmaAaejEW9v6iL9tn665";
    private string url = "https://api.windy.com/api/point-forecast/v2";

    // Beispiel-Koordinaten (Berlin)
    [SerializeField] private double latitude = 52.5200;
    [SerializeField] private double longitude = 13.4050;

    void Start()
    {
        if (weatherText == null)
        {
            Debug.LogError("[Windy] Bitte ziehe ein TextMeshPro-Feld in den Inspector!");
            return;
        }
        
        StartCoroutine(FetchWindDataLoop());
    }

    IEnumerator FetchWindDataLoop()
    {
        while (true)
        {
            Debug.Log("[Windy] Aktualisiere Winddaten...");

            // Hier erzwingen wir die "InvariantCulture" (Punkt statt Komma bei den Koordinaten)
            string latString = latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string lonString = longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);

            // JSON-Payload sauber zusammenbauen
            string jsonPayload = "{" +
                $"\"key\":\"{apiKey}\"," +
                $"\"lat\":{latString}," +
                $"\"lon\":{lonString}," +
                "\"model\":\"gfs\"," +
                "\"parameters\":[\"wind\"]," +
                "\"levels\":[\"surface\"]" +
            "}";
            
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    VerarbeiteDaten(request.downloadHandler.text);
                }
                else
                {
                    Debug.LogError($"[Windy Serverantwort Fehler]: {request.downloadHandler.text}");
                    weatherText.text = $"Fehler: {request.responseCode}";
                }
            }

            yield return new WaitForSeconds(5.0f); 
        }
    }

    void VerarbeiteDaten(string json)
    {
        try
        {
            string uSuchBegriff = "\"wind_u-surface\":[";
            string vSuchBegriff = "\"wind_v-surface\":[";

            int uStart = json.IndexOf(uSuchBegriff);
            int vStart = json.IndexOf(vSuchBegriff);

            if (uStart != -1 && vStart != -1)
            {
                // 1. U-Vektor extrahieren
                uStart += uSuchBegriff.Length;
                int uEnde = json.IndexOf(",", uStart);
                if (uEnde == -1) uEnde = json.IndexOf("]", uStart);
                string uString = json.Substring(uStart, uEnde - uStart);
                float windU = float.Parse(uString, System.Globalization.CultureInfo.InvariantCulture);

                // 2. V-Vektor extrahieren
                vStart += vSuchBegriff.Length;
                int vEnde = json.IndexOf(",", vStart);
                if (vEnde == -1) vEnde = json.IndexOf("]", vStart);
                string vString = json.Substring(vStart, vEnde - vStart);
                float windV = float.Parse(vString, System.Globalization.CultureInfo.InvariantCulture);

                // 3. Echte Windgeschwindigkeit berechnen
                float windMetersPerSecond = Mathf.Sqrt((windU * windU) + (windV * windV));
                float windKmH = windMetersPerSecond * 3.6f;

                weatherText.text = $"Windstärke:\n{windKmH:F1} km/h";
            }
            else
            {
                weatherText.text = "Keine Winddaten im JSON.";
                Debug.LogWarning("Das empfangene JSON hatte nicht das richtige Format: " + json);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Windy] Fehler beim Parsen der Daten: {e.Message}");
            weatherText.text = "Fehler beim Lesen.";
        }
    }
}