using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;

public class AuthenticationManager : MonoBehaviour
{
    [Header("Backend API")]
    public string apiBaseUrl = "https://backend-for-project.onrender.com/";

    [Header("Current Session")]
    public UserModel currentUser;
    public bool isLoggedIn = false;

    public static AuthenticationManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public IEnumerator LoginCoroutine(
        string username,
        string password,
        Action<LoginResponse> onSuccess,
        Action<string> onError
    )
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            onError?.Invoke("Username and password are required");
            yield break;
        }

        var reqBody = new LoginRequest { username = username, password = password };
        string json = JsonUtility.ToJson(reqBody);
        string url = CombineUrl(apiBaseUrl, "/api/login");

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
                Debug.LogError($"Login failed: {req.error}");
                onError?.Invoke("Login failed (server unreachable)");
                yield break;
            }

            string respJson = req.downloadHandler.text;
            Debug.Log($"Server response: {respJson}");

            LoginResponse response;
            try
            {
                response = JsonUtility.FromJson<LoginResponse>(respJson);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed parsing response: {e.Message}");
                onError?.Invoke("Bad server response");
                yield break;
            }

            if (!response.success)
            {
                onError?.Invoke(response.message ?? "Login failed");
                yield break;
            }

            currentUser = response.user;
            isLoggedIn = true;
            Debug.Log($"Login successful: {currentUser.username}");
            onSuccess?.Invoke(response);
        }
    }

    public IEnumerator RegisterCoroutine(
        string username,
        string password,
        string email,
        string fullName,
        UserRole role,
        Action<RegisterResponse> onSuccess,
        Action<string> onError
    )
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            onError?.Invoke("Username and password are required");
            yield break;
        }

        var reqBody = new RegisterRequest
        {
            username = username,
            password = password,
            email = email,
            fullName = fullName,
            role = (int)role
        };

        string json = JsonUtility.ToJson(reqBody);
        string url = CombineUrl(apiBaseUrl, "/api/register");

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
                Debug.LogError($"Registration failed: {req.error}");
                onError?.Invoke("Registration failed (server unreachable)");
                yield break;
            }

            string respJson = req.downloadHandler.text;
            Debug.Log($"Registration response: {respJson}");

            RegisterResponse response;
            try
            {
                response = JsonUtility.FromJson<RegisterResponse>(respJson);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed parsing response: {e.Message}");
                onError?.Invoke("Bad server response");
                yield break;
            }

            if (!response.success)
            {
                onError?.Invoke(response.message ?? "Registration failed");
                yield break;
            }

            Debug.Log($"Registration successful: {username}");
            onSuccess?.Invoke(response);
        }
    }

    public IEnumerator SaveScenarioCoroutine(
        string fileName,
        ScenarioConfig scenario,
        Action<SaveScenarioResponse> onSuccess,
        Action<string> onError
    )
    {
        if (string.IsNullOrEmpty(fileName) || scenario == null)
        {
            onError?.Invoke("Scenario filename and data are required");
            yield break;
        }

        var reqBody = new SaveScenarioRequest
        {
            fileName = fileName,
            scenario = scenario
        };

        string json = JsonUtility.ToJson(reqBody);
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
                Debug.LogError($"Save scenario failed: {req.error}");
                onError?.Invoke("Failed to save scenario (server unreachable)");
                yield break;
            }

            string respJson = req.downloadHandler.text;
            Debug.Log($"Save scenario response: {respJson}");

            SaveScenarioResponse response;
            try
            {
                response = JsonUtility.FromJson<SaveScenarioResponse>(respJson);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed parsing response: {e.Message}");
                onError?.Invoke("Bad server response");
                yield break;
            }

            if (!response.success)
            {
                onError?.Invoke(response.message ?? "Failed to save scenario");
                yield break;
            }

            Debug.Log($"Scenario saved successfully: {fileName}");
            onSuccess?.Invoke(response);
        }
    }

    public void Logout()
    {
        currentUser = null;
        isLoggedIn = false;
        Debug.Log("User logged out");
    }

    public bool CanAccessScenario(string scenarioName)
    {
        if (!isLoggedIn || currentUser == null) return false;
        return currentUser.CanAccessScenario(scenarioName);
    }

    static string CombineUrl(string baseUrl, string path)
    {
        if (string.IsNullOrEmpty(baseUrl)) return path ?? "";
        if (string.IsNullOrEmpty(path)) return baseUrl;
        if (baseUrl.EndsWith("/")) baseUrl = baseUrl.Substring(0, baseUrl.Length - 1);
        if (!path.StartsWith("/")) path = "/" + path;
        return baseUrl + path;
    }
}



[System.Serializable]
public class LoginRequest
{
    public string username;
    public string password;
}


[System.Serializable]
public class LoginResponse
{
    public bool success;
    public string message;
    public UserModel user;
    public string sessionToken;
}

[System.Serializable]
public class RegisterRequest
{
    public string username;
    public string password;
    public string email;
    public string fullName;
    public int role;
}

[System.Serializable]
public class RegisterResponse
{
    public bool success;
    public string message;
}

