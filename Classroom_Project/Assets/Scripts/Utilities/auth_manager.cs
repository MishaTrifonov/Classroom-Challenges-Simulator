using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;

public class AuthenticationManager : MonoBehaviour
{
    [Header("Backend API (WebGL)")]
    [Tooltip("Example: https://your-vercel-app.vercel.app")]
    public string apiBaseUrl = "http://localhost:3000";

    [Header("Dev / Offline Mode")]
    public bool allowOfflineLogin = true;

    [Header("Current Session")]
    public UserModel currentUser;
    public bool isLoggedIn = false;

    // Singleton
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

    // ---------------------------
    // PUBLIC API (WebGL-safe)
    // ---------------------------

    /// <summary>
    /// Login via backend API. WebGL safe: coroutine + UnityWebRequest.
    /// </summary>
    public IEnumerator LoginCoroutine(
        string username,
        string password,
        Action<LoginResponse> onSuccess,
        Action<string> onError
    )
    {
        // Validate input early
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            onError?.Invoke("Username and password are required");
            yield break;
        }

        // Optional offline/dev admin
        if (allowOfflineLogin)
        {
            // dev backdoor: admin/admin
            if (username == "admin" && password == "admin")
            {
                var user = new UserModel
                {
                    username = "admin",
                    fullName = "Dev Admin",
                    role = UserRole.Administrator,
                    isActive = true,
                    sessionCount = 0,
                    allowedScenarios = Array.Empty<string>()
                };

                currentUser = user;
                isLoggedIn = true;

                onSuccess?.Invoke(new LoginResponse
                {
                    success = true,
                    message = "Admin dev login successful",
                    user = user,
                    sessionToken = "dev-token"
                });
                yield break;
            }

            // offline admin: admin/admin123 (when backend unavailable)
            // We'll still TRY backend first; fallback happens if request fails.
        }

        // Build request body
        var reqBody = new LoginRequest { username = username, password = password };
        string json = JsonUtility.ToJson(reqBody);

        // IMPORTANT: /api/login is your backend route
        string url = CombineUrl(apiBaseUrl, "/api/login");

        using (var req = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            // WebGL: ensure no stuck requests
            req.timeout = 15;

            yield return req.SendWebRequest();

            // Network / CORS / server down
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Login request failed: {req.error}\n{req.downloadHandler.text}");

                if (allowOfflineLogin && username == "admin" && password == "admin123")
                {
                    var offlineUser = new UserModel
                    {
                        username = "admin",
                        fullName = "Offline Admin",
                        role = UserRole.Administrator,
                        isActive = true,
                        sessionCount = 0,
                        allowedScenarios = Array.Empty<string>()
                    };

                    currentUser = offlineUser;
                    isLoggedIn = true;

                    onSuccess?.Invoke(new LoginResponse
                    {
                        success = true,
                        message = "Offline login successful (backend unavailable)",
                        user = offlineUser,
                        sessionToken = "offline-token"
                    });
                    yield break;
                }

                onError?.Invoke("Login failed (server unreachable or CORS blocked)");
                yield break;
            }

            // Parse JSON response
            // NOTE: JsonUtility needs matching field names (success/message/user/sessionToken)
            string respJson = req.downloadHandler.text;

            LoginResponse response;
            try
            {
                response = JsonUtility.FromJson<LoginResponse>(respJson);
            }
            catch
            {
                Debug.LogError($"Failed parsing login response: {respJson}");
                onError?.Invoke("Bad server response");
                yield break;
            }

            if (response == null)
            {
                onError?.Invoke("Empty server response");
                yield break;
            }

            if (!response.success)
            {
                onError?.Invoke(string.IsNullOrEmpty(response.message) ? "Invalid username or password" : response.message);
                yield break;
            }

            // Success
            currentUser = response.user;
            isLoggedIn = true;

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

    // ---------------------------
    // Helpers
    // ---------------------------

    static string CombineUrl(string baseUrl, string path)
    {
        if (string.IsNullOrEmpty(baseUrl)) return path ?? "";
        if (string.IsNullOrEmpty(path)) return baseUrl;

        if (baseUrl.EndsWith("/")) baseUrl = baseUrl.Substring(0, baseUrl.Length - 1);
        if (!path.StartsWith("/")) path = "/" + path;

        return baseUrl + path;
    }
}
