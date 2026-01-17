using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LoginUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject loginPanel;
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public Button loginButton;
    public TextMeshProUGUI messageText;
    public GameObject loadingIndicator;

    [Header("Scene References")]
    public GameObject scenarioSelectionScreen;

    [Header("Visual Settings")]
    public Color errorColor = Color.red;
    public Color successColor = Color.green;
    public Color normalColor = Color.white;

    private AuthenticationManager authManager;
    private bool isProcessingLogin = false;

    void Start()
    {
        authManager = AuthenticationManager.Instance;

        if (loginButton != null)
            loginButton.onClick.AddListener(OnLoginButtonClicked);

        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);

        if (scenarioSelectionScreen != null)
            scenarioSelectionScreen.SetActive(false);

        ShowMessage("", normalColor);
    }

    public void OnLoginButtonClicked()
    {
        if (isProcessingLogin) return;

        string username = usernameInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(username))
        {
            ShowMessage("Please enter a username", errorColor);
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            ShowMessage("Please enter a password", errorColor);
            return;
        }

        StartCoroutine(ProcessLoginCoroutine(username, password));
    }

    IEnumerator ProcessLoginCoroutine(string username, string password)
    {
        isProcessingLogin = true;

        ShowMessage("Logging in...", normalColor);
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
        ShowMessage($"Welcome, {response.user.fullName}!", successColor);
        Invoke(nameof(TransitionToScenarioSelection), 1f);
    }

    void OnLoginFailure(string message)
    {
        Debug.Log($"Login failed: {message}");
        ShowMessage(message, errorColor);
        passwordInput.text = "";
        ShakeLoginPanel();
    }

    void TransitionToScenarioSelection()
    {
        loginPanel.SetActive(false);
        scenarioSelectionScreen.SetActive(true);

        var scenarioUI = scenarioSelectionScreen.GetComponent<ScenarioSelectionUI>();
        if (scenarioUI != null)
            scenarioUI.RefreshScenarioList();
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
}
