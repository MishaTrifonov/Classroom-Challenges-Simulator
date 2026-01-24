using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class LoadScenarioJson : MonoBehaviour
{
    public IEnumerator LoadJson(string fileName)
    {
        // לדוגמה: fileName = "Scenarios/scenario1.json"
        string url = $"{Application.streamingAssetsPath}/{fileName}";

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to load JSON: {url}\n{req.error}");
                yield break;
            }

            string json = req.downloadHandler.text;
            Debug.Log("Loaded JSON length: " + json.Length);
            // פה תעשי Deserialize...
        }
    }
}
