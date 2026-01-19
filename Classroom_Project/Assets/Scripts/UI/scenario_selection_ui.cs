
// filepath: c:\Users\micha\Classroom_Project\Classroom-Challenges-Simulator\Classroom_Project\Assets\Scripts\UI\scenario_selection_ui.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// Displays available scenarios and allows user to select one.
/// Shows scenario details like difficulty, description, and student count.
/// All scenario buttons are positioned at (0,0) with layout handled by container.
/// Multiple scenarios are displayed in a scrollable list.
/// </summary>
public class ScenarioSelectionUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Panel containing the scenario list")]
    public GameObject scenarioSelectionPanel;

    [Tooltip("Container where scenario buttons will be created (should be the Content of a ScrollRect)")]
    public Transform scenarioListContainer;

    [Tooltip("Prefab for scenario button")]
    public GameObject scenarioButtonPrefab;

    [Tooltip("Text showing user info")]
    public TextMeshProUGUI userInfoText;

    [Tooltip("Panel showing selected scenario details")]
    public GameObject scenarioDetailsPanel;

    [Tooltip("Details text elements")]
    public TextMeshProUGUI scenarioNameText;
    public TextMeshProUGUI scenarioDescriptionText;
    public TextMeshProUGUI scenarioDifficultyText;
    public TextMeshProUGUI scenarioStudentCountText;

    [Tooltip("Start simulation button")]
    public Button startSimulationButton;

    [Tooltip("Logout button")]
    public Button logoutButton;

    [Header("Scene Settings")]
    [Tooltip("Name of the simulation scene to load")]
    public string simulationSceneName = "MainClassroom";

    [Header("List Settings")]
    [Tooltip("Spacing between scenario buttons in the list")]
    public float buttonSpacing = 10f;

    [Tooltip("Padding for the list container (Left, Right, Top, Bottom)")]
    public int paddingLeft = 10;
    public int paddingRight = 10;
    public int paddingTop = 10;
    public int paddingBottom = 10;

    // References
    private AuthenticationManager authManager;
    private ScenarioLoader scenarioLoader;
    private List<string> availableScenarios = new List<string>();
    private string selectedScenarioName;
    private ScenarioConfig selectedScenario;
    private Button lastSelectedButton;

    // NOTE: no constructors, no RectOffset field initializers anywhere

    void Start()
    {
        // Get references
        authManager = AuthenticationManager.Instance;
        scenarioLoader = FindObjectOfType<ScenarioLoader>();

        if (authManager == null)
        {
            Debug.LogWarning("AuthenticationManager not found! Scenario access will not be restricted.");
            // Continue without authentication - all scenarios will be accessible
        }

        if (scenarioLoader == null)
        {
            Debug.LogWarning("ScenarioLoader not found in Start. Will try to find it later when needed.");
            // Don't return - continue initialization
        }

        // Set up button listeners
        if (startSimulationButton != null)
        {
            startSimulationButton.onClick.AddListener(OnStartSimulationClicked);
            startSimulationButton.interactable = false; // Disabled until scenario selected
        }

        if (logoutButton != null)
        {
            logoutButton.onClick.AddListener(OnLogoutClicked);
        }

        // Hide details panel initially
        if (scenarioDetailsPanel != null)
            scenarioDetailsPanel.SetActive(false);

        // Ensure the list container has proper layout components
        EnsureLayoutComponents();

        // Display user info
        UpdateUserInfo();

        // Load scenario list
        RefreshScenarioList();
    }

    /// <summary>
    /// Ensures the list container has the necessary layout components for proper list display
    /// </summary>
    void EnsureLayoutComponents()
    {
        if (scenarioListContainer == null)
        {
            Debug.LogWarning("Scenario list container is not assigned.");
            return;
        }

        var containerGO = scenarioListContainer.gameObject;

        // Vertical layout to stack buttons
        var layoutGroup = containerGO.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = containerGO.AddComponent<VerticalLayoutGroup>();
        }

        layoutGroup.childControlHeight = false;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.spacing = buttonSpacing;
        // RectOffset created here (in Start), which is allowed
        layoutGroup.padding = new RectOffset(paddingLeft, paddingRight, paddingTop, paddingBottom);
        layoutGroup.childAlignment = TextAnchor.UpperCenter;

        // Content size fitter so content height grows with children
        var sizeFitter = containerGO.GetComponent<ContentSizeFitter>();
        if (sizeFitter == null)
        {
            sizeFitter = containerGO.AddComponent<ContentSizeFitter>();
        }
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
    }

    void UpdateUserInfo()
    {
        if (userInfoText != null && authManager != null && authManager.currentUser != null)
        {
            var user = authManager.currentUser;
            //userInfoText.text = $"Welcome, {user.FullName}\nRole: {user.Role}\nSessions: {user.SessionCount}";
            userInfoText.text = $"Welcome, {user.fullName}\nRole: {user.role}\nSessions: {user.sessionCount}";

        }
    }

    /// <summary>
    /// Load and display all available scenarios as a vertical list
    /// </summary>
    public void RefreshScenarioList()
    {
        if (scenarioListContainer == null)
            return;

        // Ensure we have a scenarioLoader reference
        if (scenarioLoader == null)
        {
            scenarioLoader = FindObjectOfType<ScenarioLoader>();
            if (scenarioLoader == null)
            {
                Debug.LogError("ScenarioLoader not found! Cannot refresh scenario list. Please add ScenarioLoader component to the scene.");
                return;
            }
        }

        Debug.Log("Refreshing scenario list...");

        // Clear existing buttons
        foreach (Transform child in scenarioListContainer)
        {
            Destroy(child.gameObject);
        }

        lastSelectedButton = null;
        selectedScenario = null;
        selectedScenarioName = null;
        if (startSimulationButton != null)
            startSimulationButton.interactable = false;

        // Get available scenarios
        availableScenarios = scenarioLoader.GetAvailableScenarios();

        if (availableScenarios == null || availableScenarios.Count == 0)
        {
            Debug.LogWarning("No scenarios found!");
            CreateNoScenariosMessage();
            return;
        }

        int index = 0;
        foreach (var scenarioFileName in availableScenarios)
        {
            CreateScenarioButton(scenarioFileName, index);
            index++;
        }

        // Force Unity UI to re-layout
        Canvas.ForceUpdateCanvases();
        var containerRect = scenarioListContainer as RectTransform;
        if (containerRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
        }
    }

    /// <summary>
    /// Create a button for a scenario; its local position is set to (0,0)
    /// and the VerticalLayoutGroup on the container arranges it in a list.
    /// </summary>
    void CreateScenarioButton(string scenarioFileName, int index)
    {
        // If no auth manager, allow access to all scenarios
        bool canAccess = authManager == null || authManager.CanAccessScenario(scenarioFileName);

        GameObject buttonObj;

        if (scenarioButtonPrefab != null)
        {
            buttonObj = Instantiate(scenarioButtonPrefab, scenarioListContainer);
        }
        else
        {
            buttonObj = new GameObject($"Button_{scenarioFileName}_{index}");
            buttonObj.transform.SetParent(scenarioListContainer, false);

            var rectTransform = buttonObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(300, 60);

            var image = buttonObj.AddComponent<Image>();
            image.color = canAccess ? Color.white : Color.gray;

            buttonObj.AddComponent<Button>();

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.black;

            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            var le = buttonObj.AddComponent<LayoutElement>();
            le.minHeight = 60;
            le.preferredHeight = 60;
        }

        // Ensure RectTransform and reset local position to (0,0)
        var btnRect = buttonObj.GetComponent<RectTransform>();
        if (btnRect != null)
        {
            btnRect.anchoredPosition = Vector2.zero;
            btnRect.localScale = Vector3.one;
        }

        // Ensure LayoutElement exists
        var layoutElement = buttonObj.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = buttonObj.AddComponent<LayoutElement>();
            layoutElement.minHeight = 60;
            layoutElement.preferredHeight = 60;
        }

        var btn = buttonObj.GetComponent<Button>();
        var btnText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

        if (btnText != null)
        {
            string displayName = scenarioFileName
                .Replace(".json", "")
                .Replace("scenario_", "")
                .Replace("_", " ");
            btnText.text = displayName;
        }

        btn.interactable = canAccess;

        string fileName = scenarioFileName;
        btn.onClick.AddListener(() => OnScenarioButtonClicked(fileName, btn));

        if (!canAccess && btnText != null)
        {
            btnText.text += " 🔒";
        }
    }

    void CreateNoScenariosMessage()
    {
        var textObj = new GameObject("NoScenariosMessage");
        textObj.transform.SetParent(scenarioListContainer, false);

        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "No scenarios found.\nPlease add scenario files to StreamingAssets/Scenarios/";
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 18;
        text.color = Color.red;

        var rect = text.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, 100);
        rect.anchoredPosition = Vector2.zero;

        var layoutElement = textObj.AddComponent<LayoutElement>();
        layoutElement.minHeight = 100;
    }

    void OnScenarioButtonClicked(string scenarioFileName, Button clickedButton)
    {
        // Unhighlight previous
        if (lastSelectedButton != null)
        {
            var colors = lastSelectedButton.colors;
            colors.normalColor = Color.white;
            lastSelectedButton.colors = colors;
        }

        // Highlight current
        if (clickedButton != null)
        {
            var colors = clickedButton.colors;
            colors.normalColor = Color.cyan;
            clickedButton.colors = colors;
            lastSelectedButton = clickedButton;
        }

        selectedScenarioName = scenarioFileName;
        selectedScenario = scenarioLoader.LoadScenario(scenarioFileName);

        if (selectedScenario == null)
        {
            Debug.LogError($"Failed to load scenario: {scenarioFileName}");
            return;
        }

        DisplayScenarioDetails(selectedScenario);

        if (startSimulationButton != null)
            startSimulationButton.interactable = true;
    }

    void DisplayScenarioDetails(ScenarioConfig scenario)
    {
        if (scenarioDetailsPanel != null)
            scenarioDetailsPanel.SetActive(true);

        if (scenarioNameText != null)
            scenarioNameText.text = scenario.scenarioName;

        if (scenarioDescriptionText != null)
            scenarioDescriptionText.text = scenario.description;

        if (scenarioDifficultyText != null)
        {
            scenarioDifficultyText.text = $"Difficulty: {scenario.difficulty}";
            switch (scenario.difficulty.ToLower())
            {
                case "easy":
                    scenarioDifficultyText.color = Color.green;
                    break;
                case "medium":
                    scenarioDifficultyText.color = Color.yellow;
                    break;
                case "hard":
                    scenarioDifficultyText.color = Color.red;
                    break;
            }
        }

        if (scenarioStudentCountText != null)
        {
            int studentCount = scenario.studentProfiles?.Count ?? 0;
            scenarioStudentCountText.text = $"Students: {studentCount}";
        }
    }

    void OnStartSimulationClicked()
    {
        if (selectedScenario == null)
        {
            Debug.LogError("No scenario selected!");
            return;
        }

        PlayerPrefs.SetString("SelectedScenario", selectedScenarioName);
        PlayerPrefs.Save();

        SceneManager.LoadScene(simulationSceneName);
    }

    void OnLogoutClicked()
    {
        if (authManager != null)
            authManager.Logout();

        if (scenarioSelectionPanel != null)
            scenarioSelectionPanel.SetActive(false);

        var loginUI = FindObjectOfType<LoginUI>();
        if (loginUI != null && loginUI.loginPanel != null)
            loginUI.loginPanel.SetActive(true);
    }
}