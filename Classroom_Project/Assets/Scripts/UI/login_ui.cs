using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class LoginUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject loginPanel;
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public Button loginButton;
    public Button showPasswordButton;
    public TextMeshProUGUI messageText;
    public GameObject loadingIndicator;
    
    [Header("Password Toggle Button")]
    [Tooltip("Optional: Text component to show 'Show'/'Hide' text")]
    public TextMeshProUGUI showPasswordButtonText;
    [Tooltip("Optional: Image component to show eye icon")]
    public UnityEngine.UI.Image showPasswordButtonImage;
    [Tooltip("Optional: Sprite for when password is hidden (eye closed)")]
    public Sprite eyeClosedSprite;
    [Tooltip("Optional: Sprite for when password is visible (eye open)")]
    public Sprite eyeOpenSprite;

    [Header("Scene Settings")]
    [Tooltip("Name of the teacher home scene to load after login")]
    public string teacherHomeSceneName = "TeacherHomeScreen";

    [Header("Visual Settings")]
    public Color errorColor = Color.red;
    public Color successColor = Color.green;
    public Color normalColor = Color.white;

    private AuthenticationManager authManager;
    private bool isProcessingLogin = false;
    private bool isPasswordVisible = false;

    void Start()
    {
        authManager = AuthenticationManager.Instance;

        // Set password field to hide characters by default
        if (passwordInput != null)
        {
            passwordInput.contentType = TMP_InputField.ContentType.Password;
            passwordInput.ForceLabelUpdate();
        }

        // Initialize button appearance
        UpdatePasswordButtonAppearance();

        if (loginButton != null)
            loginButton.onClick.AddListener(OnLoginButtonClicked);

        if (showPasswordButton != null)
            showPasswordButton.onClick.AddListener(TogglePasswordVisibility);

        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);

        ShowMessage("", normalColor);
    }

    public void OnLoginButtonClicked()
    {
        if (isProcessingLogin) return;

        string username = usernameInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(username))
        {
            ShowMessage("אנא הזן שם משתמש", errorColor);
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            ShowMessage("אנא הזן סיסמה", errorColor);
            return;
        }

        StartCoroutine(ProcessLoginCoroutine(username, password));
    }

    IEnumerator ProcessLoginCoroutine(string username, string password)
    {
        isProcessingLogin = true;

        ShowMessage("מתחבר...", normalColor);
        if (loadingIndicator != null) loadingIndicator.SetActive(true);
        if (loginButton != null) loginButton.interactable = false;

        yield return StartCoroutine(authManager.LoginCoroutine(
            username,
            password,
            OnLoginSuccess,
            OnLoginFailure
        ));

        if (loadingIndicator != null) loadingIndicator.SetActive(false);
        if (loginButton != null) loginButton.interactable = true;

        isProcessingLogin = false;
    }

    void OnLoginSuccess(LoginResponse response)
    {
        Debug.Log($"Login successful! Welcome {response.user.fullName}");
        ShowMessage($"ברוך הבא, {response.user.fullName}!", successColor);
        Invoke(nameof(TransitionToTeacherHome), 1f);
    }

    void OnLoginFailure(string message)
    {
        Debug.Log($"Login failed: {message}");
        ShowMessage(message, errorColor);
        passwordInput.text = "";
        ShakeLoginPanel();
    }

    void TransitionToTeacherHome()
    {
        // Load the Teacher Home Screen scene
        SceneManager.LoadScene(teacherHomeSceneName);
    }

    void ShowMessage(string message, Color color)
    {
        if (messageText != null)
        {
            messageText.text = message;
            messageText.color = color;
        }
    }

    void ShakeLoginPanel()
    {
        if (loginPanel != null)
            StartCoroutine(ShakeCoroutine(loginPanel.transform));
    }

    IEnumerator ShakeCoroutine(Transform target)
    {
        Vector3 originalPos = target.localPosition;
        float elapsed = 0f;

        while (elapsed < 0.5f)
        {
            target.localPosition = originalPos + Vector3.right * Random.Range(-10f, 10f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        target.localPosition = originalPos;
    }

    public void TogglePasswordVisibility()
    {
        if (passwordInput == null) return;

        isPasswordVisible = !isPasswordVisible;

        if (isPasswordVisible)
        {
            // Show password as plain text
            passwordInput.contentType = TMP_InputField.ContentType.Standard;
        }
        else
        {
            // Hide password with asterisks/dots
            passwordInput.contentType = TMP_InputField.ContentType.Password;
        }

        // Force the input field to update its display
        passwordInput.ForceLabelUpdate();
        
        // Update button appearance
        UpdatePasswordButtonAppearance();
    }

    void UpdatePasswordButtonAppearance()
    {
        // Update button text if available
        if (showPasswordButtonText != null)
        {
            showPasswordButtonText.text = isPasswordVisible ? "הסתר" : "הצג";
        }

        // Update button image/sprite if available
        if (showPasswordButtonImage != null)
        {
            if (isPasswordVisible && eyeOpenSprite != null)
            {
                showPasswordButtonImage.sprite = eyeOpenSprite;
            }
            else if (!isPasswordVisible && eyeClosedSprite != null)
            {
                showPasswordButtonImage.sprite = eyeClosedSprite;
            }
        }
    }
}
