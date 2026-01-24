using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// Main UI for Teacher Home Scene with dashboard, session management, and feedback panels.
/// </summary>
public class TeacherHomeSceneUI : MonoBehaviour
{
    [Header("Main Dashboard Panel")]
    [Tooltip("Main panel containing the dashboard")]
    public GameObject dashboardPanel;

    [Header("Navigation Buttons")]
    [Tooltip("Button to start a new session")]
    public Button startSessionButton;

    [Tooltip("Button to view supervisor feedback")]
    public Button supervisorFeedbackButton;

    [Tooltip("Button to view session history")]
    public Button sessionHistoryButton;

    [Tooltip("Button to create a new scenario (role >= 1 required)")]
    public Button createScenarioButton;

    [Header("Supervisor Feedback Panel")]
    [Tooltip("Panel displaying supervisor feedback")]
    public GameObject supervisorFeedbackPanel;

    [Tooltip("Text displaying supervisor feedback content")]
    public TextMeshProUGUI supervisorFeedbackText;

    [Tooltip("Button to close supervisor feedback panel")]
    public Button closeSupervisorFeedbackButton;

    [Header("Session History Panel")]
    [Tooltip("Panel displaying session history")]
    public GameObject sessionHistoryPanel;

    [Tooltip("Container for session history items (should be Content of ScrollRect)")]
    public Transform sessionHistoryContainer;

    [Tooltip("Prefab for session history item (optional)")]
    public GameObject sessionHistoryItemPrefab;

    [Tooltip("Button to close session history panel")]
    public Button closeSessionHistoryButton;

    [Tooltip("Text displayed when no session history exists")]
    public TextMeshProUGUI noHistoryText;

    [Header("Create Scenario Panel")]
    [Tooltip("Panel for creating new scenarios")]
    public GameObject createScenarioPanel;

    [Tooltip("Button to close create scenario panel")]
    public Button closeCreateScenarioButton;

    [Tooltip("Input field for scenario name")]
    public TMP_InputField scenarioNameInput;

    [Tooltip("Input field for scenario description")]
    public TMP_InputField scenarioDescriptionInput;

    [Tooltip("Dropdown for scenario difficulty")]
    public TMP_Dropdown difficultyDropdown;

    [Tooltip("Button to add a new student")]
    public Button addStudentButton;

    [Tooltip("Container for student profile entries")]
    public Transform studentProfilesContainer;

    [Tooltip("Prefab for student profile entry")]
    public GameObject studentProfilePrefab;

    [Tooltip("Button to save the scenario")]
    public Button saveScenarioButton;

    [Tooltip("Text to display creation status messages")]
    public TextMeshProUGUI creationStatusText;

    [Header("Student Editor Popup")]
    [Tooltip("Popup panel for editing individual student profiles")]
    public GameObject studentEditorPopup;

    [Tooltip("Reference to the StudentProfileEntry component in the popup")]
    public StudentProfileEntry studentEditorProfile;

    [Tooltip("Button to confirm and add the student")]
    public Button confirmAddStudentButton;

    [Tooltip("Button to cancel student creation")]
    public Button cancelAddStudentButton;

    [Header("Scenario Selection")]
    [Tooltip("Reference to ScenarioSelectionUI component (should be on a GameObject in the scene)")]
    public ScenarioSelectionUI scenarioSelectionUI;

    [Tooltip("Panel containing the scenario selection UI (should be assigned to ScenarioSelectionUI)")]
    public GameObject scenarioSelectionPanel;

    [Header("Dashboard Info")]
    [Tooltip("Text displaying user information")]
    public TextMeshProUGUI userInfoText;

    [Tooltip("Text displaying total sessions completed")]
    public TextMeshProUGUI totalSessionsText;

    private AuthenticationManager authManager;
    private List<StudentProfileEntry> currentStudentProfiles = new List<StudentProfileEntry>();

    void Awake()
    {
        // Hide scenario selection panel initially (before ScenarioSelectionUI initializes)
        if (scenarioSelectionPanel != null)
            scenarioSelectionPanel.SetActive(false);
    }

    void Start()
    {
        // Get references
        authManager = AuthenticationManager.Instance;

        // Find ScenarioSelectionUI if not assigned
        if (scenarioSelectionUI == null)
        {
            scenarioSelectionUI = FindObjectOfType<ScenarioSelectionUI>();
            if (scenarioSelectionUI == null)
            {
                Debug.LogWarning("ScenarioSelectionUI not found in scene. Please add ScenarioSelectionUI component to a GameObject in the scene.");
            }
        }

        // Setup button listeners
        if (startSessionButton != null)
            startSessionButton.onClick.AddListener(ShowScenarioSelection);

        if (supervisorFeedbackButton != null)
            supervisorFeedbackButton.onClick.AddListener(ShowSupervisorFeedback);

        if (sessionHistoryButton != null)
            sessionHistoryButton.onClick.AddListener(ShowSessionHistory);

        if (createScenarioButton != null)
            createScenarioButton.onClick.AddListener(ShowCreateScenario);

        if (closeSupervisorFeedbackButton != null)
            closeSupervisorFeedbackButton.onClick.AddListener(CloseSupervisorFeedbackPanel);

        if (closeSessionHistoryButton != null)
            closeSessionHistoryButton.onClick.AddListener(CloseSessionHistoryPanel);

        if (closeCreateScenarioButton != null)
            closeCreateScenarioButton.onClick.AddListener(CloseCreateScenarioPanel);

        if (addStudentButton != null)
        {
            addStudentButton.onClick.AddListener(OpenStudentEditorPopup);
            Debug.Log("Add Student Button listener added");
        }
        else
        {
            Debug.LogWarning("addStudentButton is not assigned in Inspector!");
        }

        if (confirmAddStudentButton != null)
        {
            confirmAddStudentButton.onClick.AddListener(ConfirmAddStudent);
            Debug.Log("Confirm Add Student Button listener added");
        }

        if (cancelAddStudentButton != null)
        {
            cancelAddStudentButton.onClick.AddListener(CloseStudentEditorPopup);
            Debug.Log("Cancel Add Student Button listener added");
        }

        if (saveScenarioButton != null)
        {
            saveScenarioButton.onClick.AddListener(SaveScenario);
            Debug.Log("Save Scenario Button listener added");
        }
        else
        {
            Debug.LogWarning("saveScenarioButton is not assigned in Inspector!");
        }

        // Initialize panels
        if (dashboardPanel != null)
            dashboardPanel.SetActive(true);

        // Ensure scenario selection panel is disabled at start
        if (scenarioSelectionPanel != null)
            scenarioSelectionPanel.SetActive(false);

        if (supervisorFeedbackPanel != null)
            supervisorFeedbackPanel.SetActive(false);

        if (sessionHistoryPanel != null)
            sessionHistoryPanel.SetActive(false);

        if (createScenarioPanel != null)
            createScenarioPanel.SetActive(false);

        if (studentEditorPopup != null)
            studentEditorPopup.SetActive(false);

        // Update dashboard info and check permissions
        UpdateDashboardInfo();
        UpdateCreateScenarioButtonVisibility();

        // Override logout button behavior in ScenarioSelectionUI to go back to dashboard instead
        // We need to do this after ScenarioSelectionUI initializes, so use a coroutine
        StartCoroutine(OverrideScenarioSelectionLogoutButton());
    }

    /// <summary>
    /// Update visibility of Create Scenario button based on user role
    /// </summary>
    void UpdateCreateScenarioButtonVisibility()
    {
        if (createScenarioButton == null)
            return;

        // Check if user has role >= 1 (Instructor or Administrator)
        bool hasPermission = authManager != null &&
                            authManager.currentUser != null &&
                            (int)authManager.currentUser.role >= 1;

        createScenarioButton.gameObject.SetActive(hasPermission);
    }

    /// <summary>
    /// Show create scenario panel
    /// </summary>
    void ShowCreateScenario()
    {
        // Verify permissions again
        if (authManager == null || authManager.currentUser == null || (int)authManager.currentUser.role < 1)
        {
            Debug.LogWarning("User does not have permission to create scenarios. Role must be >= 1.");
            return;
        }

        if (createScenarioPanel != null)
        {
            createScenarioPanel.SetActive(true);
            InitializeCreateScenarioPanel();
        }

        // Hide dashboard while showing create scenario
        if (dashboardPanel != null)
            dashboardPanel.SetActive(false);
    }

    /// <summary>
    /// Initialize the create scenario panel with default values
    /// </summary>
    void InitializeCreateScenarioPanel()
    {
        // Clear previous data
        currentStudentProfiles.Clear();

        // Clear student profiles container
        if (studentProfilesContainer != null)
        {
            // Clear student profiles container
            foreach (Transform child in studentProfilesContainer)
            {
                Destroy(child.gameObject);
            }

            // Ensure VerticalLayoutGroup exists
            var verticalLayout = studentProfilesContainer.GetComponent<VerticalLayoutGroup>();
            if (verticalLayout == null)
            {
                verticalLayout = studentProfilesContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            }
            verticalLayout.childControlWidth = true;
            verticalLayout.childControlHeight = false;
            verticalLayout.childForceExpandWidth = true;
            verticalLayout.childForceExpandHeight = false;
            verticalLayout.spacing = 10;
            verticalLayout.padding = new RectOffset(10, 10, 10, 10);

            // Ensure ContentSizeFitter exists
            var contentSizeFitter = studentProfilesContainer.GetComponent<ContentSizeFitter>();
            if (contentSizeFitter == null)
            {
                contentSizeFitter = studentProfilesContainer.gameObject.AddComponent<ContentSizeFitter>();
            }
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        // Reset input fields
        if (scenarioNameInput != null)
            scenarioNameInput.text = "";

        if (scenarioDescriptionInput != null)
            scenarioDescriptionInput.text = "";

        // Setup difficulty dropdown
        if (difficultyDropdown != null)
        {
            difficultyDropdown.ClearOptions();
            difficultyDropdown.AddOptions(new List<string> { "Easy", "Medium", "Hard" });
            difficultyDropdown.value = 1; // Default to Medium
        }

        // Clear status text
        if (creationStatusText != null)
            creationStatusText.text = "";

        // Add one default student profile
        // AddStudentProfile();
    }

    /// <summary>
    /// Open the student editor popup to create a new student
    /// </summary>
    void OpenStudentEditorPopup()
    {
        Debug.Log("OpenStudentEditorPopup called");

        if (studentEditorPopup == null)
        {
            Debug.LogError("studentEditorPopup is not assigned!");
            return;
        }

        if (studentEditorProfile == null)
        {
            Debug.LogError("studentEditorProfile is not assigned!");
            return;
        }

        // Reset the editor to default values
        studentEditorProfile.ResetToDefaults();

        // Set a temporary student ID (will be finalized when confirmed)
        studentEditorProfile.SetStudentId($"student_{currentStudentProfiles.Count + 1:D3}");

        // Show the popup
        studentEditorPopup.SetActive(true);

        Debug.Log("Student editor popup opened");
    }

    /// <summary>
    /// Confirm and add the student from the editor popup
    /// </summary>
    void ConfirmAddStudent()
    {
        Debug.Log("ConfirmAddStudent called");

        if (studentEditorProfile == null || studentProfilesContainer == null)
        {
            Debug.LogError("Missing required references!");
            return;
        }

        // Get the student profile data from the editor
        StudentProfile profileData = studentEditorProfile.GetStudentProfile();

        // Create a display item for the student list
        GameObject displayItem = CreateStudentDisplayItem(profileData);
        displayItem.transform.SetParent(studentProfilesContainer, false);

        // Store the profile data in the display item
        var displayComponent = displayItem.AddComponent<StudentProfileDisplay>();
        displayComponent.profileData = profileData;
        currentStudentProfiles.Add(studentEditorProfile); // Keep reference for saving

        Debug.Log($"Student '{profileData.name}' added successfully. Total students: {currentStudentProfiles.Count}");

        // Close the popup
        CloseStudentEditorPopup();
    }

    /// <summary>
    /// Close the student editor popup without adding
    /// </summary>
    void CloseStudentEditorPopup()
    {
        if (studentEditorPopup != null)
            studentEditorPopup.SetActive(false);

        Debug.Log("Student editor popup closed");
    }

    /// <summary>
    /// Create a simple display item for the student list
    /// </summary>
    GameObject CreateStudentDisplayItem(StudentProfile profile)
    {
        GameObject itemObj = new GameObject($"Student_{profile.name}");

        var rectTransform = itemObj.AddComponent<RectTransform>();
        // Set anchors to stretch horizontally
        rectTransform.anchorMin = new Vector2(0, 0.5f);
        rectTransform.anchorMax = new Vector2(1, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(0, 180); // Width will stretch, height is 180

        var image = itemObj.AddComponent<Image>();
        image.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

        var layoutElement = itemObj.AddComponent<LayoutElement>();
        layoutElement.minHeight = 180;
        layoutElement.preferredHeight = 180;
        layoutElement.flexibleWidth = 1; // Allow it to expand horizontally

        // Create horizontal layout for content
        var horizontalLayout = itemObj.AddComponent<HorizontalLayoutGroup>();
        horizontalLayout.padding = new RectOffset(10, 10, 10, 10);
        horizontalLayout.spacing = 10;
        horizontalLayout.childControlWidth = true;
        horizontalLayout.childControlHeight = true;
        horizontalLayout.childForceExpandWidth = false;
        horizontalLayout.childForceExpandHeight = true;

        // Student info text
        GameObject textObj = new GameObject("InfoText");
        textObj.transform.SetParent(itemObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        var textLayoutElement = textObj.AddComponent<LayoutElement>();
        textLayoutElement.flexibleWidth = 1;

        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = $"<b>{profile.name}</b>\n" +
                   $"Extro: {profile.extroversion:F1} | Sens: {profile.sensitivity:F1} | " +
                   $"Rebel: {profile.rebelliousness:F1} | Acad: {profile.academicMotivation:F1}";
        text.fontSize = 12;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.MidlineLeft;

        // Edit button
        GameObject editButtonObj = new GameObject("EditButton");
        editButtonObj.transform.SetParent(itemObj.transform, false);
        var editButtonRect = editButtonObj.AddComponent<RectTransform>();
        editButtonRect.sizeDelta = new Vector2(60, 0);
        var editButtonLayoutElement = editButtonObj.AddComponent<LayoutElement>();
        editButtonLayoutElement.minWidth = 60;
        editButtonLayoutElement.preferredWidth = 60;

        var editButtonImage = editButtonObj.AddComponent<Image>();
        editButtonImage.color = new Color(0.2f, 0.4f, 0.8f, 1f);

        var editButton = editButtonObj.AddComponent<Button>();
        editButton.onClick.AddListener(() => EditStudent(itemObj));

        GameObject editTextObj = new GameObject("Text");
        editTextObj.transform.SetParent(editButtonObj.transform, false);
        var editTextRect = editTextObj.AddComponent<RectTransform>();
        editTextRect.anchorMin = Vector2.zero;
        editTextRect.anchorMax = Vector2.one;
        editTextRect.sizeDelta = Vector2.zero;
        var editText = editTextObj.AddComponent<TextMeshProUGUI>();
        editText.text = "Edit";
        editText.fontSize = 12;
        editText.color = Color.white;
        editText.alignment = TextAlignmentOptions.Center;

        // Remove button
        GameObject removeButtonObj = new GameObject("RemoveButton");
        removeButtonObj.transform.SetParent(itemObj.transform, false);
        var removeButtonRect = removeButtonObj.AddComponent<RectTransform>();
        removeButtonRect.sizeDelta = new Vector2(60, 0);
        var removeButtonLayoutElement = removeButtonObj.AddComponent<LayoutElement>();
        removeButtonLayoutElement.minWidth = 60;
        removeButtonLayoutElement.preferredWidth = 60;

        var removeButtonImage = removeButtonObj.AddComponent<Image>();
        removeButtonImage.color = new Color(0.8f, 0.2f, 0.2f, 1f);

        var removeButton = removeButtonObj.AddComponent<Button>();
        removeButton.onClick.AddListener(() => RemoveStudent(itemObj));

        GameObject removeTextObj = new GameObject("Text");
        removeTextObj.transform.SetParent(removeButtonObj.transform, false);
        var removeTextRect = removeTextObj.AddComponent<RectTransform>();
        removeTextRect.anchorMin = Vector2.zero;
        removeTextRect.anchorMax = Vector2.one;
        removeTextRect.sizeDelta = Vector2.zero;
        var removeText = removeTextObj.AddComponent<TextMeshProUGUI>();
        removeText.text = "Remove";
        removeText.fontSize = 12;
        removeText.color = Color.white;
        removeText.alignment = TextAlignmentOptions.Center;

        return itemObj;
    }

    /// <summary>
    /// Edit an existing student
    /// </summary>
    void EditStudent(GameObject studentItem)
    {
        var displayComponent = studentItem.GetComponent<StudentProfileDisplay>();
        if (displayComponent == null || studentEditorProfile == null)
            return;

        // Load the student data into the editor
        studentEditorProfile.LoadProfile(displayComponent.profileData);

        // Store reference to the item being edited
        studentEditorProfile.editingItemReference = studentItem;

        // Open the popup
        studentEditorPopup.SetActive(true);

        Debug.Log($"Editing student: {displayComponent.profileData.name}");
    }

    /// <summary>
    /// Remove a student from the list
    /// </summary>
    void RemoveStudent(GameObject studentItem)
    {
        Debug.Log($"Removing student: {studentItem.name}");
        Destroy(studentItem);
    }

    /// <summary>
    /// Add a new student profile entry (OLD METHOD - kept for backwards compatibility)
    /// </summary>
    void AddStudentProfile()
    {
        Debug.Log("AddStudentProfile called");

        if (studentProfilesContainer == null)
        {
            Debug.LogError("studentProfilesContainer is null!");
            return;
        }

        Debug.Log("Creating student profile...");
        GameObject profileObj;

        if (studentProfilePrefab != null)
        {
            Debug.Log("Using prefab");
            profileObj = Instantiate(studentProfilePrefab, studentProfilesContainer);
        }
        else
        {
            Debug.Log("Creating programmatically (no prefab assigned)");
            // Create a simple student profile UI programmatically
            profileObj = CreateStudentProfileUI();
        }

        Debug.Log($"Profile object created: {profileObj.name}");

        var profileEntry = profileObj.GetComponent<StudentProfileEntry>();
        if (profileEntry == null)
        {
            Debug.Log("Adding StudentProfileEntry component");
            profileEntry = profileObj.AddComponent<StudentProfileEntry>();
        }

        currentStudentProfiles.Add(profileEntry);
        Debug.Log($"Total student profiles: {currentStudentProfiles.Count}");

        // Set student ID
        profileEntry.SetStudentId($"student_{currentStudentProfiles.Count:D3}");

        Debug.Log("Student profile added successfully");
    }

    /// <summary>
    /// Create student profile UI programmatically if no prefab is provided
    /// </summary>
    GameObject CreateStudentProfileUI()
    {
        GameObject profileObj = new GameObject("StudentProfile");
        profileObj.transform.SetParent(studentProfilesContainer, false);

        var rectTransform = profileObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(0, 200);

        var image = profileObj.AddComponent<Image>();
        image.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

        var layoutElement = profileObj.AddComponent<UnityEngine.UI.LayoutElement>();
        layoutElement.minHeight = 200;
        layoutElement.preferredHeight = 200;

        // Add StudentProfileEntry component
        var entry = profileObj.AddComponent<StudentProfileEntry>();
        entry.CreateDefaultUI(profileObj.transform);

        return profileObj;
    }

    /// <summary>
    /// Save the scenario
    /// </summary>
    void SaveScenario()
    {
        if (creationStatusText != null)
            creationStatusText.text = "Saving scenario...";

        // Validate inputs
        if (string.IsNullOrWhiteSpace(scenarioNameInput.text))
        {
            if (creationStatusText != null)
                creationStatusText.text = "Error: Scenario name is required.";
            return;
        }

        if (currentStudentProfiles.Count == 0)
        {
            if (creationStatusText != null)
                creationStatusText.text = "Error: At least one student profile is required.";
            return;
        }

        // Build scenario data
        ScenarioConfig scenario = new ScenarioConfig
        {
            scenarioName = scenarioNameInput.text,
            description = scenarioDescriptionInput.text,
            difficulty = difficultyDropdown.options[difficultyDropdown.value].text,
            studentProfiles = new List<StudentProfile>()
        };

        // Collect student profiles from display items
        if (studentProfilesContainer != null)
        {
            foreach (Transform child in studentProfilesContainer)
            {
                var displayComponent = child.GetComponent<StudentProfileDisplay>();
                if (displayComponent != null && displayComponent.profileData != null)
                {
                    scenario.studentProfiles.Add(displayComponent.profileData);
                }
            }
        }

        // Generate filename
        string fileName = $"scenario_{scenario.scenarioName.Replace(" ", "_").ToLower()}.json";

        // Save to server using AuthenticationManager
        StartCoroutine(SaveScenarioCoroutine(fileName, scenario));
    }

    /// <summary>
    /// Coroutine to save scenario to server
    /// </summary>
    IEnumerator SaveScenarioCoroutine(string fileName, ScenarioConfig scenario)
    {
        if (authManager == null)
        {
            if (creationStatusText != null)
                creationStatusText.text = "Error: Authentication manager not found.";
            yield break;
        }

        // Disable save button while saving
        if (saveScenarioButton != null)
            saveScenarioButton.interactable = false;

        bool saveComplete = false;
        bool saveSuccess = false;
        string errorMessage = "";

        yield return authManager.SaveScenarioCoroutine(
            fileName,
            scenario,
            (response) =>
            {
                saveComplete = true;
                saveSuccess = true;
            },
            (error) =>
            {
                saveComplete = true;
                saveSuccess = false;
                errorMessage = error;
            }
        );

        yield return new WaitUntil(() => saveComplete);

        // Re-enable save button
        if (saveScenarioButton != null)
            saveScenarioButton.interactable = true;

        if (saveSuccess)
        {
            if (creationStatusText != null)
                creationStatusText.text = $"✓ Scenario '{scenario.scenarioName}' saved successfully to server!";

            Debug.Log($"Scenario saved to server: {fileName}");

            // Clear the form after 2 seconds
            Invoke(nameof(CloseCreateScenarioPanel), 2f);
        }
        else
        {
            if (creationStatusText != null)
                creationStatusText.text = $"Error: {errorMessage}";

            Debug.LogError($"Failed to save scenario: {errorMessage}");
        }
    }

    /// <summary>
    /// Close create scenario panel and return to dashboard
    /// </summary>
    void CloseCreateScenarioPanel()
    {
        if (createScenarioPanel != null)
            createScenarioPanel.SetActive(false);

        if (dashboardPanel != null)
            dashboardPanel.SetActive(true);
    }

    /// <summary>
    /// Override the logout button in ScenarioSelectionUI to go back to dashboard
    /// </summary>
    System.Collections.IEnumerator OverrideScenarioSelectionLogoutButton()
    {
        // Wait a frame to ensure ScenarioSelectionUI has initialized
        yield return null;

        if (scenarioSelectionUI != null && scenarioSelectionUI.logoutButton != null)
        {
            // Remove existing listeners and add our own to go back to dashboard
            scenarioSelectionUI.logoutButton.onClick.RemoveAllListeners();
            scenarioSelectionUI.logoutButton.onClick.AddListener(CloseScenarioSelectionPanel);
        }
    }

    /// <summary>
    /// Show scenario selection panel using ScenarioSelectionUI
    /// </summary>
    void ShowScenarioSelection()
    {
        Debug.Log("ShowScenarioSelection called");

        if (scenarioSelectionUI == null)
        {
            Debug.LogError("ScenarioSelectionUI is not assigned! Please assign it in the Inspector.");
            return;
        }

        // Ensure ScenarioLoader exists in the scene
        ScenarioLoader loader = FindObjectOfType<ScenarioLoader>();
        if (loader == null)
        {
            Debug.LogWarning("ScenarioLoader not found in scene. Creating one automatically...");
            GameObject loaderObj = new GameObject("ScenarioLoader");
            loader = loaderObj.AddComponent<ScenarioLoader>();
            Debug.Log("ScenarioLoader created successfully.");
        }
        else
        {
            Debug.Log("ScenarioLoader found in scene.");
        }

        // Hide dashboard first
        if (dashboardPanel != null)
        {
            dashboardPanel.SetActive(false);
            Debug.Log("Dashboard panel hidden");
        }

        // Show the scenario selection panel
        if (scenarioSelectionPanel != null)
        {
            scenarioSelectionPanel.SetActive(true);
            Debug.Log("Scenario selection panel shown");
        }
        else
        {
            Debug.LogError("scenarioSelectionPanel is null! Please assign it in the Inspector.");
        }

        // Ensure ScenarioSelectionUI has all its references initialized
        // Refresh the scenario list - this will use the ScenarioLoader
        if (scenarioSelectionUI != null)
        {
            Debug.Log("Refreshing scenario list...");
            scenarioSelectionUI.RefreshScenarioList();
        }
    }

    /// <summary>
    /// Close scenario selection panel and return to dashboard
    /// </summary>
    void CloseScenarioSelectionPanel()
    {
        // Hide scenario selection panel
        if (scenarioSelectionPanel != null)
            scenarioSelectionPanel.SetActive(false);

        // Show dashboard again
        if (dashboardPanel != null)
            dashboardPanel.SetActive(true);
    }

    /// <summary>
    /// Display supervisor feedback panel
    /// </summary>
    void ShowSupervisorFeedback()
    {
        if (supervisorFeedbackPanel != null)
        {
            supervisorFeedbackPanel.SetActive(true);

            // Load and display supervisor feedback
            LoadSupervisorFeedback();
        }

        // Hide dashboard while showing feedback
        if (dashboardPanel != null)
            dashboardPanel.SetActive(false);
    }

    /// <summary>
    /// Close supervisor feedback panel and return to dashboard
    /// </summary>
    void CloseSupervisorFeedbackPanel()
    {
        if (supervisorFeedbackPanel != null)
            supervisorFeedbackPanel.SetActive(false);

        if (dashboardPanel != null)
            dashboardPanel.SetActive(true);
    }

    /// <summary>
    /// Display session history panel
    /// </summary>
    void ShowSessionHistory()
    {
        if (sessionHistoryPanel != null)
        {
            sessionHistoryPanel.SetActive(true);
            LoadSessionHistory();
        }

        // Hide dashboard while showing history
        if (dashboardPanel != null)
            dashboardPanel.SetActive(false);
    }

    /// <summary>
    /// Close session history panel and return to dashboard
    /// </summary>
    void CloseSessionHistoryPanel()
    {
        if (sessionHistoryPanel != null)
            sessionHistoryPanel.SetActive(false);

        if (dashboardPanel != null)
            dashboardPanel.SetActive(true);
    }

    /// <summary>
    /// Load and display supervisor feedback
    /// </summary>
    void LoadSupervisorFeedback()
    {
        if (supervisorFeedbackText == null)
            return;

        // Load supervisor feedback from storage
        // For now, using placeholder data - can be replaced with actual database calls
        string feedback = PlayerPrefs.GetString("SupervisorFeedback", "");

        if (string.IsNullOrEmpty(feedback))
        {
            feedback = "עדיין אין משוב ממפקח.\n\n" +
                       "המפקח שלך יספק משוב כאן לאחר סקירת שיעורי ההדרכה שלך.";
        }

        supervisorFeedbackText.text = feedback;
    }

    /// <summary>
    /// Load and display session history with feedback
    /// </summary>
    void LoadSessionHistory()
    {
        if (sessionHistoryContainer == null)
            return;

        // Clear existing items
        foreach (Transform child in sessionHistoryContainer)
        {
            Destroy(child.gameObject);
        }

        // Load session history
        List<SessionHistoryEntry> history = LoadSessionHistoryData();

        if (history == null || history.Count == 0)
        {
            if (noHistoryText != null)
                noHistoryText.gameObject.SetActive(true);
            return;
        }

        if (noHistoryText != null)
            noHistoryText.gameObject.SetActive(false);

        // Display each session in history
        for (int i = history.Count - 1; i >= 0; i--) // Show most recent first
        {
            CreateSessionHistoryItem(history[i]);
        }

        // Force layout update
        Canvas.ForceUpdateCanvases();
    }

    /// <summary>
    /// Create a UI item for a session history entry
    /// </summary>
    void CreateSessionHistoryItem(SessionHistoryEntry entry)
    {
        GameObject itemObj;

        if (sessionHistoryItemPrefab != null)
        {
            itemObj = Instantiate(sessionHistoryItemPrefab, sessionHistoryContainer);
        }
        else
        {
            // Create a simple UI item programmatically
            itemObj = new GameObject($"SessionHistoryItem_{entry.sessionId}");
            itemObj.transform.SetParent(sessionHistoryContainer, false);

            var rectTransform = itemObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0, 120);

            var image = itemObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Add layout element
            var layoutElement = itemObj.AddComponent<UnityEngine.UI.LayoutElement>();
            layoutElement.minHeight = 120;
            layoutElement.preferredHeight = 120;

            // Create text for session info
            var textObj = new GameObject("SessionInfo");
            textObj.transform.SetParent(itemObj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            textRect.offsetMin = new Vector2(10, 10);
            textRect.offsetMax = new Vector2(-10, -10);

            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = FormatSessionEntry(entry);
            text.fontSize = 14;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.TopLeft;
        }
    }

    /// <summary>
    /// Format session entry for display
    /// </summary>
    string FormatSessionEntry(SessionHistoryEntry entry)
    {
        string dateStr = entry.date.ToString("MM/dd/yyyy HH:mm");
        string durationStr = FormatDuration(entry.duration);

        return $"Session: {entry.sessionId}\n" +
               $"Date: {dateStr}\n" +
               $"Duration: {durationStr}\n" +
               $"Score: {entry.score:F1}/100\n" +
               $"Feedback: {entry.feedback}";
    }

    /// <summary>
    /// Format duration in seconds to readable format
    /// </summary>
    string FormatDuration(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60);
        int secs = Mathf.FloorToInt(seconds % 60);
        return $"{minutes}m {secs}s";
    }

    /// <summary>
    /// Load session history data from storage
    /// </summary>
    List<SessionHistoryEntry> LoadSessionHistoryData()
    {
        List<SessionHistoryEntry> history = new List<SessionHistoryEntry>();

        // Load from PlayerPrefs (can be replaced with database calls)
        int count = PlayerPrefs.GetInt("SessionHistoryCount", 0);

        for (int i = 0; i < count; i++)
        {
            string key = $"SessionHistory_{i}";
            string data = PlayerPrefs.GetString(key, "");

            if (!string.IsNullOrEmpty(data))
            {
                try
                {
                    SessionHistoryEntry entry = JsonUtility.FromJson<SessionHistoryEntry>(data);
                    if (entry != null)
                        history.Add(entry);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to parse session history entry: {e.Message}");
                }
            }
        }

        return history;
    }

    /// <summary>
    /// Update dashboard information
    /// </summary>
    void UpdateDashboardInfo()
    {
        // Update user info
        if (userInfoText != null && authManager != null && authManager.currentUser != null)
        {
            var user = authManager.currentUser;
            userInfoText.text = $"ברוך הבא, {user.fullName}\nתפקיד: {GetRoleHebrew(user.role)}";
        }

        // Update total sessions
        if (totalSessionsText != null)
        {
            int totalSessions = PlayerPrefs.GetInt("SessionHistoryCount", 0);
            totalSessionsText.text = $"סך שיעורים: {totalSessions}";
        }
    }

    /// <summary>
    /// Called when a session ends to save session history
    /// This can be called from ClassroomManager when a session ends
    /// </summary>
    public static void SaveSessionToHistory(SessionReport report, string feedback = "")
    {
        if (report == null || report.sessionData == null)
            return;

        // Load existing history
        int count = PlayerPrefs.GetInt("SessionHistoryCount", 0);

        // Create history entry
        SessionHistoryEntry entry = new SessionHistoryEntry
        {
            sessionId = report.sessionData.sessionId,
            date = report.sessionData.endTime,
            duration = report.sessionData.duration,
            score = report.score,
            engagement = report.averageEngagement,
            totalActions = report.totalActions,
            disruptions = report.totalDisruptions,
            feedback = string.IsNullOrEmpty(feedback) ? "לא סופק משוב." : feedback
        };

        // Save to PlayerPrefs
        string key = $"SessionHistory_{count}";
        string json = JsonUtility.ToJson(entry);
        PlayerPrefs.SetString(key, json);

        // Update count
        count++;
        PlayerPrefs.SetInt("SessionHistoryCount", count);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Get Hebrew translation for user role
    /// </summary>
    string GetRoleHebrew(UserRole role)
    {
        switch (role)
        {
            case UserRole.Student: return "סטודנט";
            case UserRole.Instructor: return "מדריך";
            case UserRole.Administrator: return "מנהל מערכת";
            default: return role.ToString();
        }
    }
}

/// <summary>
/// Data structure for session history entries
/// </summary>
[System.Serializable]
public class SessionHistoryEntry
{
    public string sessionId;
    public string dateString; // Stored as ISO string for JSON serialization
    public float duration;
    public float score;
    public float engagement;
    public int totalActions;
    public int disruptions;
    public string feedback;

    // Property to get/set DateTime
    public DateTime date
    {
        get
        {
            if (string.IsNullOrEmpty(dateString))
                return DateTime.Now;
            try
            {
                return DateTime.Parse(dateString);
            }
            catch
            {
                return DateTime.Now;
            }
        }
        set
        {
            dateString = value.ToString("O"); // ISO 8601 format
        }
    }
}

/// <summary>
/// Component to store student profile data in display items
/// </summary>
public class StudentProfileDisplay : MonoBehaviour
{
    public StudentProfile profileData;
}