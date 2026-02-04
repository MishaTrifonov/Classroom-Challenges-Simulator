using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// Main UI for Teacher Home Scene with dashboard, session management, and feedback panels.
/// Improved version with better layout for Create Scenario panel
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

    [Header("Teacher Assignment")]
    [Tooltip("Input field to search/select a teacher (username/full name)")]
    public TMP_InputField teacherSearchInput;

    [Tooltip("Dropdown showing matching teachers while typing")]
    public TMP_Dropdown teacherResultsDropdown;

    [Tooltip("Optional: shows the selected teacher")]
    public TextMeshProUGUI selectedTeacherText;

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
    [Tooltip("Reference to ScenarioSelectionUI component")]
    public ScenarioSelectionUI scenarioSelectionUI;

    [Tooltip("Panel containing the scenario selection UI")]
    public GameObject scenarioSelectionPanel;

    [Header("Dashboard Info")]
    [Tooltip("Text displaying user information")]
    public TextMeshProUGUI userInfoText;

    [Tooltip("Text displaying total sessions completed")]
    public TextMeshProUGUI totalSessionsText;
    
    [Tooltip("Button to logout and return to login screen")]
    public Button logoutButton;

    private AuthenticationManager authManager;
    private List<StudentProfileData> currentStudents = new List<StudentProfileData>();
    private StudentProfile editingStudent = null;
    private GameObject editingStudentObject = null;

    private Coroutine teacherSearchCoroutine;
    private readonly List<UserModel> teacherSearchResults = new List<UserModel>();
    private string selectedTeacherUsername;
    private bool suppressTeacherSearch;

    void Awake()
    {
        // Hide panels initially
        if (scenarioSelectionPanel != null)
            scenarioSelectionPanel.SetActive(false);
    }

    void Start()
    {
        authManager = AuthenticationManager.Instance;

        // Find ScenarioSelectionUI if not assigned
        if (scenarioSelectionUI == null)
        {
            scenarioSelectionUI = FindObjectOfType<ScenarioSelectionUI>();
        }

        // Initialize student profiles container layout
        InitializeStudentProfilesContainer();

        // Setup button listeners
        SetupButtonListeners();

        // Initialize panels
        InitializePanels();

        // Setup teacher selector listeners (if assigned)
        InitializeTeacherSelector();

        // Update dashboard
        UpdateDashboardInfo();
        UpdateCreateScenarioButtonVisibility();

        // Override scenario selection logout button
        StartCoroutine(OverrideScenarioSelectionLogoutButton());
    }

    void InitializeTeacherSelector()
    {
        if (teacherSearchInput != null)
        {
            teacherSearchInput.onValueChanged.RemoveListener(OnTeacherSearchChanged);
            teacherSearchInput.onValueChanged.AddListener(OnTeacherSearchChanged);
        }

        if (teacherResultsDropdown != null)
        {
            teacherResultsDropdown.onValueChanged.RemoveListener(OnTeacherDropdownChanged);
            teacherResultsDropdown.onValueChanged.AddListener(OnTeacherDropdownChanged);
            teacherResultsDropdown.gameObject.SetActive(false);
        }

        ClearTeacherSelection();
    }

    void ClearTeacherSelection()
    {
        selectedTeacherUsername = null;
        teacherSearchResults.Clear();

        if (selectedTeacherText != null)
        {
            selectedTeacherText.text = "";
        }

        if (teacherResultsDropdown != null)
        {
            teacherResultsDropdown.ClearOptions();
            teacherResultsDropdown.gameObject.SetActive(false);
        }
    }

    void OnTeacherSearchChanged(string input)
    {
        if (suppressTeacherSearch)
            return;

        selectedTeacherUsername = null;
        if (selectedTeacherText != null)
            selectedTeacherText.text = "";

        if (teacherSearchCoroutine != null)
        {
            StopCoroutine(teacherSearchCoroutine);
            teacherSearchCoroutine = null;
        }

        if (authManager == null)
            return;

        if (string.IsNullOrWhiteSpace(input))
        {
            if (teacherResultsDropdown != null)
                teacherResultsDropdown.gameObject.SetActive(false);
            return;
        }

        teacherSearchCoroutine = StartCoroutine(FetchTeacherSuggestionsAfterDelay(input.Trim()));
    }

    IEnumerator FetchTeacherSuggestionsAfterDelay(string search)
    {
        yield return new WaitForSeconds(0.25f);

        bool done = false;
        bool ok = false;
        string errorMessage = "";
        UserModel[] users = null;

        // Role filtering is optional; pass null to avoid role-mapping mismatches.
        yield return authManager.FetchUsersCoroutine(
            search,
            null,
            20,
            (resp) =>
            {
                users = resp != null ? resp.users : null;
                ok = true;
                done = true;
            },
            (err) =>
            {
                errorMessage = err;
                ok = false;
                done = true;
            }
        );

        yield return new WaitUntil(() => done);

        if (!ok)
        {
            Debug.LogWarning($"Teacher search failed: {errorMessage}");
            if (teacherResultsDropdown != null)
                teacherResultsDropdown.gameObject.SetActive(false);
            yield break;
        }

        teacherSearchResults.Clear();
        if (users != null)
        {
            for (int i = 0; i < users.Length; i++)
            {
                var u = users[i];
                if (u == null) continue;
                if (u.isActive == false) continue;
                if (string.IsNullOrWhiteSpace(u.username)) continue;
                teacherSearchResults.Add(u);
            }
        }

        if (teacherResultsDropdown == null)
            yield break;

        teacherResultsDropdown.ClearOptions();

        if (teacherSearchResults.Count == 0)
        {
            teacherResultsDropdown.gameObject.SetActive(false);
            yield break;
        }

        var options = new List<TMP_Dropdown.OptionData>(teacherSearchResults.Count);
        for (int i = 0; i < teacherSearchResults.Count; i++)
        {
            var u = teacherSearchResults[i];
            string display = !string.IsNullOrWhiteSpace(u.fullName)
                ? u.fullName
                : u.username;
            options.Add(new TMP_Dropdown.OptionData(display));
        }

        teacherResultsDropdown.AddOptions(options);
        teacherResultsDropdown.value = 0;
        teacherResultsDropdown.RefreshShownValue();
        teacherResultsDropdown.gameObject.SetActive(true);
    }

    void OnTeacherDropdownChanged(int index)
    {
        if (index < 0 || index >= teacherSearchResults.Count)
            return;

        var selected = teacherSearchResults[index];
        selectedTeacherUsername = selected.username;

        if (teacherSearchInput != null)
        {
            suppressTeacherSearch = true;
            teacherSearchInput.text = !string.IsNullOrWhiteSpace(selected.fullName) ? selected.fullName : selected.username;
            suppressTeacherSearch = false;
        }


        if (selectedTeacherText != null)
        {
            string label = !string.IsNullOrWhiteSpace(selected.fullName)
                ? $"מורה שנבחר/ה: {selected.fullName}"
                : $"מורה שנבחר/ה: {selected.username}";
            selectedTeacherText.text = label;
            selectedTeacherText.isRightToLeftText = true;
            selectedTeacherText.alignment = TextAlignmentOptions.MidlineRight;
        }

        if (teacherResultsDropdown != null)
            teacherResultsDropdown.gameObject.SetActive(false);
    }

    void SetupButtonListeners()
    {
        if (logoutButton != null)
            logoutButton.onClick.AddListener(Logout);

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
            addStudentButton.onClick.AddListener(OpenAddStudentPopup);

        if (confirmAddStudentButton != null)
            confirmAddStudentButton.onClick.AddListener(ConfirmAddStudent);

        if (cancelAddStudentButton != null)
            cancelAddStudentButton.onClick.AddListener(CloseStudentEditorPopup);

        if (saveScenarioButton != null)
            saveScenarioButton.onClick.AddListener(SaveScenario);
    }

    void InitializePanels()
    {
        if (dashboardPanel != null)
            dashboardPanel.SetActive(true);

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
    }

    void InitializeStudentProfilesContainer()
    {
        if (studentProfilesContainer != null)
        {
            var containerRect = studentProfilesContainer.GetComponent<RectTransform>();
            if (containerRect != null)
            {
                containerRect.anchorMin = new Vector2(0, 1);
                containerRect.anchorMax = new Vector2(1, 1);
                containerRect.pivot = new Vector2(0.5f, 1);
            }

            var gridLayout = studentProfilesContainer.GetComponent<GridLayoutGroup>();
            if (gridLayout == null)
            {
                gridLayout = studentProfilesContainer.gameObject.AddComponent<GridLayoutGroup>();
            }

            gridLayout.childAlignment = TextAnchor.UpperCenter;
            gridLayout.padding = new RectOffset(10, 10, 10, 10);
            gridLayout.cellSize = new Vector2(350, 300);

            var sizeFitter = studentProfilesContainer.GetComponent<ContentSizeFitter>();
            if (sizeFitter == null)
            {
                sizeFitter = studentProfilesContainer.gameObject.AddComponent<ContentSizeFitter>();
            }
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    void UpdateCreateScenarioButtonVisibility()
    {
        if (createScenarioButton == null)
            return;

        bool hasPermission = authManager != null &&
                            authManager.currentUser != null &&
                            authManager.currentUser.role >= UserRole.Instructor;

        createScenarioButton.gameObject.SetActive(hasPermission);
    }

    System.Collections.IEnumerator OverrideScenarioSelectionLogoutButton()
    {
        yield return null;

        if (scenarioSelectionUI != null && scenarioSelectionUI.logoutButton != null)
        {
            scenarioSelectionUI.logoutButton.onClick.RemoveAllListeners();
            scenarioSelectionUI.logoutButton.onClick.AddListener(CloseScenarioSelectionPanel);
        }
    }

    void ShowScenarioSelection()
    {
        if (scenarioSelectionUI == null)
        {
            Debug.LogError("ScenarioSelectionUI is not assigned!");
            return;
        }

        if (dashboardPanel != null)
            dashboardPanel.SetActive(false);

        if (scenarioSelectionPanel != null)
            scenarioSelectionPanel.SetActive(true);

        scenarioSelectionUI.RefreshScenarioList();
    }

    void CloseScenarioSelectionPanel()
    {
        if (scenarioSelectionPanel != null)
            scenarioSelectionPanel.SetActive(false);

        if (dashboardPanel != null)
            dashboardPanel.SetActive(true);
    }

    void ShowSupervisorFeedback()
    {
        if (supervisorFeedbackPanel == null)
            return;

        supervisorFeedbackPanel.SetActive(true);

        if (supervisorFeedbackText != null)
        {
            string feedback = LoadSupervisorFeedback();
            supervisorFeedbackText.text = string.IsNullOrEmpty(feedback) ? "אין משוב זמין כרגע." : feedback;
        }
    }

    void CloseSupervisorFeedbackPanel()
    {
        if (supervisorFeedbackPanel != null)
            supervisorFeedbackPanel.SetActive(false);
    }

    string LoadSupervisorFeedback()
    {
        return PlayerPrefs.GetString("SupervisorFeedback", "");
    }

    void ShowSessionHistory()
    {
        if (sessionHistoryPanel == null)
            return;

        sessionHistoryPanel.SetActive(true);

        if (sessionHistoryContainer != null)
        {
            foreach (Transform child in sessionHistoryContainer)
            {
                Destroy(child.gameObject);
            }
        }

        List<SessionHistoryEntry> history = LoadSessionHistoryData();
        DisplaySessionHistory(history);
    }

    void CloseSessionHistoryPanel()
    {
        if (sessionHistoryPanel != null)
            sessionHistoryPanel.SetActive(false);
    }

    void ShowCreateScenario()
    {
        if (createScenarioPanel == null)
            return;

        if (authManager == null || authManager.currentUser == null ||
            authManager.currentUser.role < UserRole.Instructor)
        {
            if (creationStatusText != null)
                creationStatusText.text = "אין לך הרשאה ליצור תרחישים.";
            return;
        }

        createScenarioPanel.SetActive(true);

        // Clear form
        if (scenarioNameInput != null)
            scenarioNameInput.text = "";

        if (scenarioDescriptionInput != null)
            scenarioDescriptionInput.text = "";

        if (difficultyDropdown != null)
            difficultyDropdown.value = 0;

        if (creationStatusText != null)
            creationStatusText.text = "";

        // Clear teacher selection
        if (teacherSearchInput != null)
        {
            suppressTeacherSearch = true;
            teacherSearchInput.text = "";
            suppressTeacherSearch = false;
        }
        ClearTeacherSelection();

        // Clear students
        ClearStudentList();
    }

    void CloseCreateScenarioPanel()
    {
        if (createScenarioPanel != null)
            createScenarioPanel.SetActive(false);

        if (dashboardPanel != null)
            dashboardPanel.SetActive(true);

        ClearStudentList();
    }

    void ClearStudentList()
    {
        currentStudents.Clear();

        if (studentProfilesContainer != null)
        {
            foreach (Transform child in studentProfilesContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }

    void OpenAddStudentPopup()
    {
        if (studentEditorPopup == null || studentEditorProfile == null)
        {
            Debug.LogError("Student editor popup or profile is not assigned!");
            return;
        }

        editingStudent = null;
        editingStudentObject = null;

        studentEditorProfile.ResetToDefaults();
        studentEditorPopup.SetActive(true);
    }

    void EditStudent(GameObject studentItem)
    {
        if (studentEditorPopup == null || studentEditorProfile == null)
        {
            Debug.LogError("Student editor popup or profile is not assigned!");
            return;
        }

        // Find the student data
        var displayComponent = studentItem.GetComponent<StudentProfileDisplay>();
        if (displayComponent != null && displayComponent.profileData != null)
        {
            editingStudent = displayComponent.profileData.profile;
            editingStudentObject = studentItem;

            studentEditorProfile.LoadProfile(editingStudent);
            studentEditorPopup.SetActive(true);
        }
    }

    void ConfirmAddStudent()
    {
        if (studentEditorProfile == null)
        {
            Debug.LogError("Student editor profile is not assigned!");
            return;
        }

        StudentProfile profile = studentEditorProfile.GetStudentProfile();

        if (string.IsNullOrWhiteSpace(profile.name))
        {
            if (creationStatusText != null)
                creationStatusText.text = "שם התלמיד לא יכול להיות ריק!";
            return;
        }

        if (editingStudent != null && editingStudentObject != null)
        {
            // Editing existing student
            UpdateStudentDisplay(editingStudentObject, profile);

            // Update in list
            var displayComponent = editingStudentObject.GetComponent<StudentProfileDisplay>();
            if (displayComponent != null && displayComponent.profileData != null)
            {
                displayComponent.profileData.profile = profile;
            }
        }
        else
        {
            // Adding new student
            profile.id = System.Guid.NewGuid().ToString();

            StudentProfileData profileData = new StudentProfileData { profile = profile };
            currentStudents.Add(profileData);

            GameObject displayItem = CreateStudentDisplayItem(profile);
            displayItem.transform.SetParent(studentProfilesContainer, false);

            var displayComponent = displayItem.GetComponent<StudentProfileDisplay>();
            if (displayComponent == null)
                displayComponent = displayItem.AddComponent<StudentProfileDisplay>();

            displayComponent.profileData = profileData;
            profileData.displayItem = displayItem;
        }

        CloseStudentEditorPopup();

        if (creationStatusText != null)
            creationStatusText.text = $"תלמיד '{profile.name}' {(editingStudent != null ? "עודכן" : "נוסף")} בהצלחה!";
    }

    void CloseStudentEditorPopup()
    {
        if (studentEditorPopup != null)
            studentEditorPopup.SetActive(false);

        editingStudent = null;
        editingStudentObject = null;
    }

    void RemoveStudent(GameObject studentItem)
    {
        var displayComponent = studentItem.GetComponent<StudentProfileDisplay>();
        if (displayComponent != null && displayComponent.profileData != null)
        {
            currentStudents.Remove(displayComponent.profileData);
        }

        Destroy(studentItem);
    }

    /// <summary>
    /// Create improved student display item with better spacing and layout
    /// </summary>
    GameObject CreateStudentDisplayItem(StudentProfile profile)
    {
        GameObject itemObj = new GameObject($"Student_{profile.name}");

        var rectTransform = itemObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.pivot = new Vector2(0.5f, 1);
        rectTransform.sizeDelta = new Vector2(0, 100); // Full width, fixed height

        var image = itemObj.AddComponent<Image>();
        image.color = new Color(0.18f, 0.18f, 0.22f, 1f);

        var layoutElement = itemObj.AddComponent<LayoutElement>();
        layoutElement.minHeight = 100;
        layoutElement.preferredHeight = 100;
        layoutElement.flexibleWidth = 1;

        // Main horizontal layout
        var horizontalLayout = itemObj.AddComponent<HorizontalLayoutGroup>();
        horizontalLayout.padding = new RectOffset(12, 12, 10, 10);
        horizontalLayout.spacing = 15;
        horizontalLayout.childControlWidth = false;
        horizontalLayout.childControlHeight = false;
        horizontalLayout.childForceExpandWidth = false;
        horizontalLayout.childForceExpandHeight = false;
        horizontalLayout.childAlignment = TextAnchor.MiddleLeft;

        // Info container (vertical layout for name and stats)
        GameObject infoContainer = new GameObject("InfoContainer");
        infoContainer.transform.SetParent(itemObj.transform, false);

        var infoLayout = infoContainer.AddComponent<VerticalLayoutGroup>();
        infoLayout.spacing = 6;
        infoLayout.childControlHeight = false;
        infoLayout.childControlWidth = false;
        infoLayout.childForceExpandHeight = false;
        infoLayout.childForceExpandWidth = false;

        var infoLayoutElement = infoContainer.AddComponent<LayoutElement>();
        infoLayoutElement.flexibleWidth = 1;

        var infoRect = infoContainer.GetComponent<RectTransform>();
        infoRect.sizeDelta = new Vector2(200, 100);

        // Name text
        GameObject nameObj = new GameObject("NameText");
        nameObj.transform.SetParent(infoContainer.transform, false);
        var nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = $"<b>{profile.name}</b>";
        nameText.fontSize = 20;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = new Color(1f, 1f, 1f, 1f);
        nameText.alignment = TextAlignmentOptions.MidlineRight;
        nameText.isRightToLeftText = true;
        nameText.overflowMode = TextOverflowModes.Ellipsis;

        var nameLayoutElement = nameObj.AddComponent<LayoutElement>();
        nameLayoutElement.preferredHeight = 30;

        // Stats text (single line)
        GameObject statsObj = new GameObject("StatsText");
        statsObj.transform.SetParent(infoContainer.transform, false);
        var statsText = statsObj.AddComponent<TextMeshProUGUI>();
        statsText.text = $"<size=18>מוחצנות: <b>{profile.extroversion:F1}</b>  |  " +
                            $"רגישות: <b>{profile.sensitivity:F1}</b>  |  " +
                            $"מרדנות: <b>{profile.rebelliousness:F1}</b>  |  " +
                            $"מוטיבציה: <b>{profile.academicMotivation:F1}</b>  |  " +
                            $"אושר: <b>{profile.initialHappiness:F1}</b>  |  " +
                            $"שעמום: <b>{profile.initialBoredom:F1}</b></size>";
        statsText.fontSize = 18;
        statsText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        statsText.alignment = TextAlignmentOptions.MidlineRight;
        statsText.isRightToLeftText = true;
        statsText.overflowMode = TextOverflowModes.Overflow;
        statsText.enableWordWrapping = true;

        var statsLayoutElement = statsObj.AddComponent<LayoutElement>();
        statsLayoutElement.minHeight = 100;
        statsLayoutElement.minWidth = 250;
        statsLayoutElement.flexibleHeight = 0;

        var statsRect = statsObj.GetComponent<RectTransform>();
        statsRect.sizeDelta = new Vector2(250, 100);

        // Buttons container
        GameObject buttonsContainer = new GameObject("ButtonsContainer");
        buttonsContainer.transform.SetParent(itemObj.transform, false);
        buttonsContainer.transform.SetAsFirstSibling();

        var buttonsLayout = buttonsContainer.AddComponent<HorizontalLayoutGroup>();
        buttonsLayout.spacing = 6;
        buttonsLayout.childControlHeight = false;
        buttonsLayout.childControlWidth = false;
        buttonsLayout.childForceExpandHeight = false;
        buttonsLayout.childForceExpandWidth = false;

        var buttonsLayoutElement = buttonsContainer.AddComponent<LayoutElement>();
        buttonsLayoutElement.minWidth = 130;
        buttonsLayoutElement.preferredWidth = 130;

        // Remove button (first in RTL layout)
        CreateButton(buttonsContainer.transform, "הסרה", new Color(0.85f, 0.25f, 0.25f, 1f),
            () => RemoveStudent(itemObj), 60, 30);

        // Edit button (second in RTL layout)
        CreateButton(buttonsContainer.transform, "עריכה", new Color(0.25f, 0.5f, 0.85f, 1f),
            () => EditStudent(itemObj), 60, 30);

        return itemObj;
    }

    /// <summary>
    /// Create a button with text
    /// </summary>
    GameObject CreateButton(Transform parent, string buttonText, Color color, UnityEngine.Events.UnityAction onClick, float width = 80, float height = 35)
    {
        GameObject buttonObj = new GameObject($"{buttonText}Button");
        buttonObj.transform.SetParent(parent, false);

        var rectTransform = buttonObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(width, height);

        var layoutElement = buttonObj.AddComponent<LayoutElement>();
        layoutElement.minHeight = height;
        layoutElement.preferredHeight = height;
        layoutElement.minWidth = width;
        layoutElement.preferredWidth = width;

        var image = buttonObj.AddComponent<Image>();
        image.color = color;

        var button = buttonObj.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        // Hover effect
        var colors = button.colors;
        colors.highlightedColor = new Color(color.r * 1.2f, color.g * 1.2f, color.b * 1.2f, 1f);
        colors.pressedColor = new Color(color.r * 0.8f, color.g * 0.8f, color.b * 0.8f, 1f);
        button.colors = colors;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = buttonText;
        text.fontSize = 11;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.isRightToLeftText = true;
        text.overflowMode = TextOverflowModes.Ellipsis;

        return buttonObj;
    }

    /// <summary>
    /// Update an existing student display with new data
    /// </summary>
    void UpdateStudentDisplay(GameObject studentItem, StudentProfile profile)
    {
        var nameText = studentItem.transform.Find("InfoContainer/NameText")?.GetComponent<TextMeshProUGUI>();
        if (nameText != null)
        {
            nameText.text = $"<b>{profile.name}</b>";
            nameText.alignment = TextAlignmentOptions.MidlineRight;
            nameText.isRightToLeftText = true;
        }

        var statsText = studentItem.transform.Find("InfoContainer/StatsText")?.GetComponent<TextMeshProUGUI>();
        if (statsText != null)
        {
            statsText.text = $"<size=18>מוחצנות: <b>{profile.extroversion:F1}</b>  |  " +
                            $"רגישות: <b>{profile.sensitivity:F1}</b>  |  " +
                            $"מרדנות: <b>{profile.rebelliousness:F1}</b>  |  " +
                            $"מוטיבציה: <b>{profile.academicMotivation:F1}</b>  |  " +
                            $"אושר: <b>{profile.initialHappiness:F1}</b>  |  " +
                            $"שעמום: <b>{profile.initialBoredom:F1}</b></size>";
            statsText.alignment = TextAlignmentOptions.MidlineRight;
            statsText.isRightToLeftText = true;
        }
    }

    void SaveScenario()
    {
        if (scenarioNameInput == null || scenarioDescriptionInput == null || difficultyDropdown == null)
        {
            Debug.LogError("Scenario input fields are not properly assigned!");
            return;
        }

        // If teacher assignment UI exists, require selecting a teacher from the results.
        if (teacherSearchInput != null)
        {
            if (string.IsNullOrWhiteSpace(selectedTeacherUsername))
            {
                if (creationStatusText != null)
                    creationStatusText.text = "יש לבחור מורה מהרשימה לפני שמירה.";
                return;
            }
        }

        string scenarioName = scenarioNameInput.text.Trim();
        string scenarioDescription = scenarioDescriptionInput.text.Trim();
        string difficulty = difficultyDropdown.options[difficultyDropdown.value].text;

        if (string.IsNullOrWhiteSpace(scenarioName))
        {
            if (creationStatusText != null)
                creationStatusText.text = "שם התרחיש לא יכול להיות ריק!";
            return;
        }

        if (currentStudents.Count == 0)
        {
            if (creationStatusText != null)
                creationStatusText.text = "יש להוסיף לפחות תלמיד אחד!";
            return;
        }

        ScenarioConfig scenario = new ScenarioConfig
        {
            scenarioName = scenarioName,
            description = scenarioDescription,
            difficulty = difficulty,
            assignedTeacherUsername = selectedTeacherUsername,
            studentProfiles = new List<StudentProfile>()
        };

        foreach (var studentData in currentStudents)
        {
            scenario.studentProfiles.Add(studentData.profile);
        }

        StartCoroutine(SaveScenarioCoroutine(scenario));
    }

    IEnumerator SaveScenarioCoroutine(ScenarioConfig scenario)
    {
        if (authManager == null)
        {
            if (creationStatusText != null)
                creationStatusText.text = "Error: Authentication manager not found.";
            yield break;
        }

        string fileName = $"{scenario.scenarioName.Replace(" ", "_")}.json";

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

        if (saveScenarioButton != null)
            saveScenarioButton.interactable = true;

        if (saveSuccess)
        {
            if (creationStatusText != null)
                creationStatusText.text = $"✓ Scenario '{scenario.scenarioName}' saved successfully!";

            Debug.Log($"Scenario saved to server: {fileName}");
            Invoke(nameof(CloseCreateScenarioPanel), 2f);

            // Add scenario to teacher's allowedScenarios
            if (!string.IsNullOrWhiteSpace(selectedTeacherUsername) && !string.IsNullOrWhiteSpace(scenario.scenarioName))
            {
                StartCoroutine(AddScenarioToTeacherAllowedScenarios(selectedTeacherUsername, scenario.scenarioName));
            }
        }
        else
        {
            if (creationStatusText != null)
                creationStatusText.text = $"Error: {errorMessage}";

            Debug.LogError($"Failed to save scenario: {errorMessage}");
        }
    }

    // Call backend to add scenario to teacher's allowedScenarios
    IEnumerator AddScenarioToTeacherAllowedScenarios(string teacherUsername, string scenarioName)
    {
        if (authManager == null)
            yield break;

        string url = $"{authManager.apiBaseUrl.TrimEnd('/')}/api/users/add-allowed-scenario";
        var reqBody = new AddAllowedScenarioRequest { username = teacherUsername, scenarioName = scenarioName };
        string json = JsonUtility.ToJson(reqBody);

        using (var req = new UnityEngine.Networking.UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 15;

            yield return req.SendWebRequest();

            if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"Failed to update teacher allowedScenarios: {req.error}");
                yield break;
            }

            string respJson = req.downloadHandler.text;
            Debug.Log($"Teacher allowedScenarios update response: {respJson}");
        }
    }

    [System.Serializable]
    public class AddAllowedScenarioRequest
    {
        public string username;
        public string scenarioName;
    }

    void DisplaySessionHistory(List<SessionHistoryEntry> history)
    {
        if (history == null || history.Count == 0)
        {
            if (noHistoryText != null)
                noHistoryText.gameObject.SetActive(true);
            return;
        }

        if (noHistoryText != null)
            noHistoryText.gameObject.SetActive(false);

        for (int i = history.Count - 1; i >= 0; i--)
        {
            CreateSessionHistoryItem(history[i]);
        }

        Canvas.ForceUpdateCanvases();
    }

    void CreateSessionHistoryItem(SessionHistoryEntry entry)
    {
        GameObject itemObj;

        if (sessionHistoryItemPrefab != null)
        {
            itemObj = Instantiate(sessionHistoryItemPrefab, sessionHistoryContainer);
        }
        else
        {
            itemObj = new GameObject($"SessionHistoryItem_{entry.sessionId}");
            itemObj.transform.SetParent(sessionHistoryContainer, false);

            var rectTransform = itemObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0, 120);

            var image = itemObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            var layoutElement = itemObj.AddComponent<LayoutElement>();
            layoutElement.minHeight = 120;
            layoutElement.preferredHeight = 120;

            var textObj = new GameObject("SessionInfo");
            textObj.transform.SetParent(itemObj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = new Vector2(10, 10);
            textRect.offsetMax = new Vector2(-10, -10);

            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = FormatSessionEntry(entry);
            text.fontSize = 14;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.TopLeft;
        }
    }

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

    string FormatDuration(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60);
        int secs = Mathf.FloorToInt(seconds % 60);
        return $"{minutes}m {secs}s";
    }

    List<SessionHistoryEntry> LoadSessionHistoryData()
    {
        List<SessionHistoryEntry> history = new List<SessionHistoryEntry>();
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

    void UpdateDashboardInfo()
    {
        if (userInfoText != null && authManager != null && authManager.currentUser != null)
        {
            var user = authManager.currentUser;
            userInfoText.text = $"ברוך הבא, {user.fullName}\nתפקיד: {GetRoleHebrew(user.role)}";
        }

        if (totalSessionsText != null)
        {
            int totalSessions = PlayerPrefs.GetInt("SessionHistoryCount", 0);
            totalSessionsText.text = $"סך שיעורים: {totalSessions}";
        }
    }

    public static void SaveSessionToHistory(SessionReport report, string feedback = "")
    {
        if (report == null || report.sessionData == null)
            return;

        int count = PlayerPrefs.GetInt("SessionHistoryCount", 0);

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

        string key = $"SessionHistory_{count}";
        string json = JsonUtility.ToJson(entry);
        PlayerPrefs.SetString(key, json);

        count++;
        PlayerPrefs.SetInt("SessionHistoryCount", count);
        PlayerPrefs.Save();
    }

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

    /// <summary>
    /// Logout and return to login screen
    /// </summary>
    void Logout()
    {
        Debug.Log("Logout button clicked");

        // Clear user session
        if (authManager != null)
        {
            authManager.Logout();
        }
        
        // Load login scene
        SceneManager.LoadScene("LoginScene");
    }
}

[System.Serializable]
public class StudentProfileData
{
    public StudentProfile profile;
    public GameObject displayItem;
}

[System.Serializable]
public class SessionHistoryEntry
{
    public string sessionId;
    public string dateString;
    public float duration;
    public float score;
    public float engagement;
    public int totalActions;
    public int disruptions;
    public string feedback;

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
            dateString = value.ToString("O");
        }
    }
}

public class StudentProfileDisplay : MonoBehaviour
{
    public StudentProfileData profileData;
}