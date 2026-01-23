using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;

/// <summary>
/// Handles loading classroom scenarios from the database server.
/// Scenarios define student profiles, classroom settings, and initial conditions.
/// </summary>
public class ScenarioLoader : MonoBehaviour
{
    [Header("Backend API")]
    public string apiBaseUrl = "https://backend-for-project.onrender.com";
    
    [Header("Scenario Settings")]
    public string defaultScenarioFileName = "scenario_basic_classroom.json";
    
    [Header("Available Scenarios")]
    public List<string> availableScenarios = new List<string>();
    
    private string scenarioFolderPath;
    private bool isFetchingFromServer = false;

    void Awake()
    {
        // Set the path where scenarios are stored (for local fallback)
        // StreamingAssets is a special Unity folder for external files
        scenarioFolderPath = Path.Combine(Application.streamingAssetsPath, "Scenarios");
        
        // Start fetching available scenarios from server
        StartCoroutine(FetchAvailableScenariosCoroutine());
    }

    /// <summary>
    /// Fetch list of available scenarios from the server
    /// </summary>
    public IEnumerator FetchAvailableScenariosCoroutine(
        Action<List<string>> onSuccess = null,
        Action<string> onError = null)
    {
        isFetchingFromServer = true;
        availableScenarios.Clear();
        Debug.Log("Fetching available scenarios from server...");
        
        string url = CombineUrl(apiBaseUrl, "/api/scenarios");
        Debug.Log(url);
        
        using (var req = new UnityWebRequest(url, "GET"))
        {
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 15;
            
            yield return req.SendWebRequest();
            Debug.Log($"Fetch scenarios request completed with result: {req.result}");
            
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to fetch scenarios from server: {req.error}");
                onError?.Invoke("Failed to connect to server");
                
                // Fallback to local scenarios
                DiscoverLocalScenarios();
                isFetchingFromServer = false;
                yield break;
            }
            
            string respJson = req.downloadHandler.text;
            Debug.Log($"Scenarios response: {respJson}");
            
            try
            {
                ScenarioListResponse response = JsonUtility.FromJson<ScenarioListResponse>(respJson);
                
                if (response.success && response.scenarios != null)
                {
                    availableScenarios = new List<string>(response.scenarios);
                    Debug.Log($"Successfully fetched {availableScenarios.Count} scenarios from server");
                    onSuccess?.Invoke(availableScenarios);
                }
                else
                {
                    Debug.LogWarning("Server returned unsuccessful response");
                    onError?.Invoke(response.message ?? "Failed to fetch scenarios");
                    DiscoverLocalScenarios();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse scenarios response: {e.Message}");
                onError?.Invoke("Invalid server response");
                DiscoverLocalScenarios();
            }
        }
        
        isFetchingFromServer = false;
    }

    /// <summary>
    /// Find all JSON scenario files in the local Scenarios folder (fallback)
    /// </summary>
    void DiscoverLocalScenarios()
    {
        availableScenarios.Clear();
        
        // Check if the folder exists
        if (!Directory.Exists(scenarioFolderPath))
        {
            Debug.LogWarning($"Scenarios folder not found at: {scenarioFolderPath}");
            Debug.Log("Creating Scenarios folder...");
            Directory.CreateDirectory(scenarioFolderPath);
            return;
        }

        // Find all .json files
        string[] files = Directory.GetFiles(scenarioFolderPath, "*.json");
        
        foreach (string filePath in files)
        {
            string fileName = Path.GetFileName(filePath);
            availableScenarios.Add(fileName);
            Debug.Log($"Found local scenario: {fileName}");
        }
        
        if (availableScenarios.Count == 0)
        {
            Debug.LogWarning("No scenario files found in local Scenarios folder!");
        }
    }

    /// <summary>
    /// Load a specific scenario by filename from the server
    /// </summary>
    public void LoadScenario(string fileName, Action<ScenarioConfig> onSuccess, Action<string> onError)
    {
        StartCoroutine(LoadScenarioCoroutine(fileName, onSuccess, onError));
    }

    /// <summary>
    /// Load a specific scenario by filename from the server
    /// </summary>
    public IEnumerator LoadScenarioCoroutine(
        string fileName,
        Action<ScenarioConfig> onSuccess,
        Action<string> onError)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            onError?.Invoke("Scenario filename is required");
            yield break;
        }
        
        string url = CombineUrl(apiBaseUrl, $"/api/scenarios/{fileName}");
        Debug.Log(url);
        
        using (var req = new UnityWebRequest(url, "GET"))
        {
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 15;
            
            yield return req.SendWebRequest();
            
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to load scenario from server: {req.error}");
                
                // Fallback to local file
                ScenarioConfig localScenario = LoadScenarioFromLocalFile(fileName);
                if (localScenario != null)
                {
                    onSuccess?.Invoke(localScenario);
                }
                else
                {
                    onError?.Invoke("Failed to load scenario from server and local fallback");
                }
                yield break;
            }
            
            string respJson = req.downloadHandler.text;
            Debug.Log($"Scenario data received: {respJson.Substring(0, Mathf.Min(200, respJson.Length))}...");
            
            try
            {
                ScenarioResponse response = JsonUtility.FromJson<ScenarioResponse>(respJson);
                
                if (response.success && response.scenario != null)
                {
                    Debug.Log($"Successfully loaded scenario: {response.scenario.scenarioName}");
                    Debug.Log($"Students: {response.scenario.studentProfiles.Count}");
                    onSuccess?.Invoke(response.scenario);
                }
                else
                {
                    Debug.LogWarning("Server returned unsuccessful response");
                    onError?.Invoke(response.message ?? "Failed to load scenario");
                    
                    // Try local fallback
                    ScenarioConfig localScenario = LoadScenarioFromLocalFile(fileName);
                    if (localScenario != null)
                    {
                        onSuccess?.Invoke(localScenario);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse scenario: {e.Message}");
                onError?.Invoke("Invalid scenario data from server");
                
                // Try local fallback
                ScenarioConfig localScenario = LoadScenarioFromLocalFile(fileName);
                if (localScenario != null)
                {
                    onSuccess?.Invoke(localScenario);
                }
            }
        }
    }

    /// <summary>
    /// Load a scenario from local file (fallback method)
    /// </summary>
    ScenarioConfig LoadScenarioFromLocalFile(string fileName)
    {
        string fullPath = Path.Combine(scenarioFolderPath, fileName);
        
        // Check if file exists
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"Local scenario file not found: {fullPath}");
            return CreateDefaultScenario();
        }

        try
        {
            // Read the JSON file
            string jsonContent = File.ReadAllText(fullPath);
            
            // Convert JSON to ScenarioConfig object
            ScenarioConfig scenario = JsonUtility.FromJson<ScenarioConfig>(jsonContent);
            
            Debug.Log($"Successfully loaded local scenario: {scenario.scenarioName}");
            Debug.Log($"Students: {scenario.studentProfiles.Count}");
            
            return scenario;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading local scenario: {e.Message}");
            return CreateDefaultScenario();
        }
    }

    /// <summary>
    /// Load the default scenario from the server
    /// </summary>
    public void LoadDefaultScenario(Action<ScenarioConfig> onSuccess, Action<string> onError)
    {
        StartCoroutine(LoadScenarioCoroutine(defaultScenarioFileName, onSuccess, onError));
    }

    /// <summary>
    /// Create a minimal fallback scenario if loading fails
    /// </summary>
    ScenarioConfig CreateDefaultScenario()
    {
        Debug.Log("Creating default fallback scenario...");
        
        ScenarioConfig scenario = new ScenarioConfig
        {
            scenarioName = "Default Classroom",
            description = "Basic classroom with 5 students",
            difficulty = "Easy",
            studentProfiles = new List<StudentProfile>()
        };

        // // Create 5 basic students
        // for (int i = 1; i <= 5; i++)
        // {
        //     scenario.studentProfiles.Add(new StudentProfile
        //     {
        //         id = $"student_{i:000}",
        //         name = $"Student {i}",
        //         extroversion = Random.Range(0.3f, 0.8f),
        //         sensitivity = Random.Range(0.3f, 0.7f),
        //         rebelliousness = Random.Range(0.2f, 0.6f),
        //         academicMotivation = Random.Range(0.4f, 0.9f),
        //         initialHappiness = 5f,
        //         initialBoredom = 3f
        //     });
        // }

        return scenario;
    }

    /// <summary>
    /// Save a scenario to the server (useful for creating new scenarios)
    /// </summary>
    public void SaveScenario(ScenarioConfig scenario, string fileName, Action onSuccess = null, Action<string> onError = null)
    {
        StartCoroutine(SaveScenarioCoroutine(scenario, fileName, onSuccess, onError));
    }

    /// <summary>
    /// Save a scenario to the server
    /// </summary>
    public IEnumerator SaveScenarioCoroutine(
        ScenarioConfig scenario,
        string fileName,
        Action onSuccess,
        Action<string> onError)
    {
        if (scenario == null || string.IsNullOrEmpty(fileName))
        {
            onError?.Invoke("Invalid scenario or filename");
            yield break;
        }
        
        var reqBody = new SaveScenarioRequest
        {
            fileName = fileName,
            scenario = scenario
        };
        
        string json = JsonUtility.ToJson(reqBody, true);
        string url = CombineUrl(apiBaseUrl, "/api/scenarios");
        
        using (var req = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 15;
            
            yield return req.SendWebRequest();
            
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to save scenario to server: {req.error}");
                
                // Fallback to local save
                SaveScenarioToLocalFile(scenario, fileName);
                onError?.Invoke("Failed to save to server, saved locally instead");
                yield break;
            }
            
            string respJson = req.downloadHandler.text;
            Debug.Log($"Save response: {respJson}");
            
            SaveScenarioResponse response;
            try
            {
                response = JsonUtility.FromJson<SaveScenarioResponse>(respJson);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse save response: {e.Message}");
                onError?.Invoke("Invalid server response");
                
                // Fallback to local save
                SaveScenarioToLocalFile(scenario, fileName);
                yield break;
            }
            
            if (response.success)
            {
                Debug.Log($"Scenario saved successfully: {fileName}");
                
                // Refresh available scenarios list
                yield return StartCoroutine(FetchAvailableScenariosCoroutine());
                onSuccess?.Invoke();
            }
            else
            {
                Debug.LogWarning("Server returned unsuccessful response");
                onError?.Invoke(response.message ?? "Failed to save scenario");
                
                // Fallback to local save
                SaveScenarioToLocalFile(scenario, fileName);
            }
        }
    }

    /// <summary>
    /// Save a scenario to local JSON file (fallback)
    /// </summary>
    void SaveScenarioToLocalFile(ScenarioConfig scenario, string fileName)
    {
        string fullPath = Path.Combine(scenarioFolderPath, fileName);
        
        try
        {
            // Convert scenario to JSON
            string jsonContent = JsonUtility.ToJson(scenario, true); // true = pretty print
            
            // Write to file
            File.WriteAllText(fullPath, jsonContent);
            
            Debug.Log($"Scenario saved locally: {fullPath}");
            
            // Refresh available scenarios list
            DiscoverLocalScenarios();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving local scenario: {e.Message}");
        }
    }

    /// <summary>
    /// Get list of all available scenario names
    /// </summary>
    public List<string> GetAvailableScenarios()
    {
        return new List<string>(availableScenarios);
    }

    /// <summary>
    /// Helper method to combine URL parts
    /// </summary>
    static string CombineUrl(string baseUrl, string path)
    {
        if (string.IsNullOrEmpty(baseUrl)) return path ?? "";
        if (string.IsNullOrEmpty(path)) return baseUrl;
        if (baseUrl.EndsWith("/")) baseUrl = baseUrl.Substring(0, baseUrl.Length - 1);
        if (!path.StartsWith("/")) path = "/" + path;
        return baseUrl + path;
    }
}

// ============================================================================
// API Request/Response Models
// ============================================================================

[System.Serializable]
public class ScenarioListResponse
{
    public bool success;
    public string message;
    public string[] scenarios;
}

[System.Serializable]
public class ScenarioResponse
{
    public bool success;
    public string message;
    public ScenarioConfig scenario;
}

[System.Serializable]
public class SaveScenarioRequest
{
    public string fileName;
    public ScenarioConfig scenario;
}

[System.Serializable]
public class SaveScenarioResponse
{
    public bool success;
    public string message;
}