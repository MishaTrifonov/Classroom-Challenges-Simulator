using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages game initialization, scene loading, and main menu flow.
/// This is the "master controller" that starts everything up.
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [Header("Core References")]
    [Tooltip("Drag the ClassroomManager GameObject here")]
    public ClassroomManager classroomManager;
    
    [Tooltip("Drag the ScenarioLoader GameObject here")]
    public ScenarioLoader scenarioLoader;
    
    [Tooltip("Drag the TeacherUI GameObject here")]
    public TeacherUI teacherUI;
    
    [Header("Startup Settings")]
    [Tooltip("Should the simulation start automatically?")]
    public bool autoStart = false; // Changed to false - now requires login
    
    [Tooltip("Which scenario to load on start (if autoStart is true)")]
    public string startupScenarioName = "scenario_basic_classroom.json";
    
    [Tooltip("Should we check for login before starting?")]
    public bool requireLogin = true;
    
    [Header("Debug Options")]
    public bool showDebugInfo = true;
    public KeyCode restartKey = KeyCode.R;
    public KeyCode quitKey = KeyCode.Escape;

    private bool isInitialized = false;

    void Start()
    {
        InitializeGame();
    }

    void Update()
    {
        // Debug controls
        if (Input.GetKeyDown(restartKey))
        {
            RestartSimulation();
        }
        
        if (Input.GetKeyDown(quitKey))
        {
            QuitSimulation();
        }
    }

    /// <summary>
    /// Initialize all game systems
    /// </summary>
    void InitializeGame()
    {
        Debug.Log("=== Classroom Simulator Initializing ===");
        
        // Validate references
        if (!ValidateReferences())
        {
            Debug.LogError("Initialization failed: Missing required references!");
            return;
        }

        // Connect systems
        ConnectSystems();

        // CRITICAL: Give ScenarioLoader time to initialize before loading scenario
        // Check if we're coming from login with a selected scenario
        if (PlayerPrefs.HasKey("SelectedScenario"))
        {
            string selectedScenario = PlayerPrefs.GetString("SelectedScenario");
            Debug.Log($"Found selected scenario in PlayerPrefs: {selectedScenario}");
            
            // Use Invoke to delay loading slightly so ScenarioLoader finishes its Start()
            Invoke(nameof(LoadScenarioFromPlayerPrefs), 0.5f);
        }
        // Otherwise, check if auto-start is enabled and login not required
        else if (autoStart && !requireLogin)
        {
            Debug.Log("Auto-start enabled, loading default scenario...");
            Invoke(nameof(LoadDefaultScenario), 0.5f);
        }
        else
        {
            Debug.Log("Waiting for scenario selection from login...");
        }

        isInitialized = true;
        Debug.Log("=== Initialization Complete ===");
    }
    
    /// <summary>
    /// Load scenario from PlayerPrefs (called after delay)
    /// </summary>
    void LoadScenarioFromPlayerPrefs()
    {
        string selectedScenario = PlayerPrefs.GetString("SelectedScenario");
        Debug.Log($"Loading scenario from PlayerPrefs: {selectedScenario}");
        LoadAndStartScenario(selectedScenario);
        PlayerPrefs.DeleteKey("SelectedScenario"); // Clear after loading
    }
    
    /// <summary>
    /// Load default scenario (called after delay)
    /// </summary>
    void LoadDefaultScenario()
    {
        LoadAndStartScenario(startupScenarioName);
    }

    /// <summary>
    /// Check that all required components are assigned
    /// </summary>
    bool ValidateReferences()
    {
        bool valid = true;

        if (classroomManager == null)
        {
            Debug.LogError("ClassroomManager not assigned!");
            classroomManager = FindObjectOfType<ClassroomManager>();
            if (classroomManager != null)
                Debug.Log("Found ClassroomManager automatically");
            else
                valid = false;
        }

        if (scenarioLoader == null)
        {
            Debug.LogError("ScenarioLoader not assigned!");
            scenarioLoader = FindObjectOfType<ScenarioLoader>();
            if (scenarioLoader != null)
                Debug.Log("Found ScenarioLoader automatically");
            else
                valid = false;
        }

        if (teacherUI == null)
        {
            Debug.LogError("TeacherUI not assigned!");
            teacherUI = FindObjectOfType<TeacherUI>();
            if (teacherUI != null)
                Debug.Log("Found TeacherUI automatically");
            else
                valid = false;
        }

        return valid;
    }

    /// <summary>
    /// Connect all systems together
    /// </summary>
    void ConnectSystems()
    {
        // Connect UI to ClassroomManager
        if (teacherUI != null && classroomManager != null)
        {
            teacherUI.classroomManager = classroomManager;
            Debug.Log("Connected TeacherUI to ClassroomManager");
        }

        // Connect ClassroomManager to UI
        if (classroomManager != null && teacherUI != null)
        {
            classroomManager.teacherUI = teacherUI;
            Debug.Log("Connected ClassroomManager to TeacherUI");
        }
    }

    /// <summary>
    /// Load a scenario and start the simulation
    /// </summary>
    public void LoadAndStartScenario(string scenarioFileName)
    {
        Debug.Log($"=== LoadAndStartScenario called with: {scenarioFileName} ===");

        if (scenarioLoader == null)
        {
            Debug.LogError("ScenarioLoader is NULL! Cannot load scenario.");
            return;
        }

        // Load scenario from JSON
        ScenarioConfig scenario = scenarioLoader.LoadScenario(scenarioFileName);

        if (scenario == null)
        {
            Debug.LogError($"Failed to load scenario: {scenarioFileName}");
            Debug.LogError("Check that the JSON file exists in StreamingAssets/Scenarios/");
            return;
        }

        if (scenario.studentProfiles == null || scenario.studentProfiles.Count == 0)
        {
            Debug.LogError($"Scenario '{scenario.scenarioName}' has NO STUDENTS!");
            Debug.LogError("Check the JSON file - studentProfiles array might be empty.");
            return;
        }

        Debug.Log($"Scenario loaded successfully: {scenario.scenarioName}");
        Debug.Log($"Student count in scenario: {scenario.studentProfiles.Count}");

        // Pass scenario to classroom manager
        if (classroomManager == null)
        {
            Debug.LogError("ClassroomManager is NULL! Cannot spawn students.");
            return;
        }

        classroomManager.LoadScenario(scenario);

        Debug.Log($"Scenario '{scenario.scenarioName}' started with {scenario.studentProfiles.Count} students!");
    }

    /// <summary>
    /// Restart the current simulation
    /// </summary>
    public void RestartSimulation()
    {
        Debug.Log("Restarting simulation...");
        
        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// End simulation and return to menu (or quit)
    /// </summary>
    public void QuitSimulation()
    {
        Debug.Log("Quitting simulation...");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    /// <summary>
    /// Display debug information on screen
    /// </summary>
    void OnGUI()
    {
        if (!showDebugInfo) return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 14;
        style.normal.textColor = Color.white;

        // Draw black background box
        GUI.Box(new Rect(10, 10, 300, 120), "");

        // Draw debug info
        GUI.Label(new Rect(20, 20, 280, 25), $"FPS: {(int)(1f / Time.deltaTime)}", style);
        GUI.Label(new Rect(20, 45, 280, 25), $"Students: {classroomManager?.activeStudents.Count ?? 0}", style);
        GUI.Label(new Rect(20, 70, 280, 25), $"Press {restartKey} to restart", style);
        GUI.Label(new Rect(20, 95, 280, 25), $"Press {quitKey} to quit", style);
    }
}

/// <summary>
/// Simple main menu UI (optional, for later)
/// </summary>
public class MainMenu : MonoBehaviour
{
    public GameInitializer gameInitializer;
    public GameObject menuPanel;
    public GameObject gamePanel;

    void Start()
    {
        ShowMenu();
    }

    public void ShowMenu()
    {
        if (menuPanel != null) menuPanel.SetActive(true);
        if (gamePanel != null) gamePanel.SetActive(false);
        Time.timeScale = 0; // Pause game
    }

    public void StartGame(string scenarioName)
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (gamePanel != null) gamePanel.SetActive(true);
        Time.timeScale = 1; // Resume game
        
        gameInitializer.LoadAndStartScenario(scenarioName);
    }

    public void QuitGame()
    {
        gameInitializer.QuitSimulation();
    }
}