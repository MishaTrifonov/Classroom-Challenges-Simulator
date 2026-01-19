using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Central controller for classroom simulation.
/// Manages student spawning, teacher actions, scenario flow, and analytics.
/// </summary>
public class ClassroomManager : MonoBehaviour
{
    [Header("Scenario Configuration")]
    public ScenarioConfig currentScenario;
    
    [Header("Student Management")]
    public GameObject studentPrefab;
    public Transform studentSpawnParent;
    public List<StudentAgent> activeStudents = new List<StudentAgent>();

    [Header("Student Spawning")]
    public StudentSpawner studentSpawner;

    
    [Header("Teacher Interface")]
    public TeacherUI teacherUI;
    
    [Header("Session Tracking")]
    public SessionData currentSession;
    private float sessionStartTime;
    private int actionCount = 0;
    
    [Header("Performance Metrics")]
    public float overallClassEngagement = 0f;
    public int disruptionCount = 0;
    public int positiveInterventions = 0;
    public int negativeInterventions = 0;

    void Start()
    {
        InitializeSession();

        // Load scenario chosen in ScenarioSelectionUI
        string selectedScenario = PlayerPrefs.GetString("SelectedScenario", "");

        ScenarioLoader loader = FindObjectOfType<ScenarioLoader>();

        if (!string.IsNullOrEmpty(selectedScenario) && loader != null)
        {
            ScenarioConfig loadedScenario = loader.LoadScenario(selectedScenario);
            LoadScenario(loadedScenario);
        }
        else
        {
            Debug.LogWarning("No scenario selected, using inspector scenario");
            LoadScenario(currentScenario);
        }
    }


    void Update()
    {
        UpdateClassMetrics();
    }

    /// <summary>
    /// Initialize a new simulation session
    /// </summary>
    void InitializeSession()
    {
        sessionStartTime = Time.time;
        currentSession = new SessionData
        {
            sessionId = System.Guid.NewGuid().ToString(),
            startTime = System.DateTime.Now,
            teacherActions = new List<TeacherAction>()
        };

        Debug.Log($"Session initialized: {currentSession.sessionId}");
    }

    /// <summary>
    /// Load and instantiate a classroom scenario
    /// </summary>
    public void LoadScenario(ScenarioConfig scenario)
    {
        if (scenario == null)
        {
            Debug.LogError("No scenario configuration provided!");
            return;
        }

        currentScenario = scenario;
        
        ClearClassroom();

        if (studentSpawner == null)
        {
            Debug.LogError("StudentSpawner not assigned!");
            return;
        }

        activeStudents = studentSpawner.Spawn(scenario.studentProfiles);


        Debug.Log($"Loaded scenario: {scenario.scenarioName} with {activeStudents.Count} students");
    }

    /// <summary>
    /// Spawn student agents from profiles
    /// </summary>
   /* void SpawnStudents(List<StudentProfile> profiles)
    {
        if (studentPrefab == null || studentSpawnParent == null)
        {
            Debug.LogError("Student prefab or spawn parent not assigned!");
            return;
        }

        // Collect spawn points (children of studentSpawnParent)
        List<Transform> spawnPoints = new List<Transform>();
        for (int i = 0; i < studentSpawnParent.childCount; i++)
            spawnPoints.Add(studentSpawnParent.GetChild(i));

        if (spawnPoints.Count == 0)
        {
            Debug.LogError("No spawn points found under studentSpawnParent!");
            return;
        }

        // Optional: sort by name so SpawnPoint_01,02,03 order is stable
        spawnPoints = spawnPoints.OrderBy(t => t.name).ToList();

        int countToSpawn = Mathf.Min(profiles.Count, spawnPoints.Count);

        for (int i = 0; i < countToSpawn; i++)
        {
            StudentProfile profile = profiles[i];
            Transform point = spawnPoints[i];

            GameObject studentObj = Instantiate(studentPrefab, point.position, point.rotation, this.transform);
            StudentAgent student = studentObj.GetComponent<StudentAgent>();

            if (student != null)
            {
                student.studentId = profile.id;
                student.studentName = profile.name;
                student.extroversion = profile.extroversion;
                student.sensitivity = profile.sensitivity;
                student.rebelliousness = profile.rebelliousness;
                student.academicMotivation = profile.academicMotivation;

                student.emotions.Happiness = profile.initialHappiness;
                student.emotions.Boredom = profile.initialBoredom;

                activeStudents.Add(student);
            }
            }

        if (profiles.Count > spawnPoints.Count)
            Debug.LogWarning($"Not enough seats! Profiles: {profiles.Count}, SpawnPoints: {spawnPoints.Count}");
        }*/

    
    public void ExecuteBagItem(BagItemType item)
    {
        // Classwide effect using your existing system
        switch (item)
        {
            case BagItemType.Ruler:
                // "Strict": may raise attention but increases negative emotion a bit
                ExecuteClasswideAction(ActionType.Yell, "Teacher used ruler to regain attention");
                break;

            case BagItemType.Game:
                // Fun: reduces boredom / improves mood
                ExecuteClasswideAction(ActionType.GiveBreak, "Quick class game");
                ExecuteClasswideAction(ActionType.Praise, "Encouraged participation");
                break;

            case BagItemType.Book:
                // Structure: call students to participate
                ExecuteClasswideAction(ActionType.CallToBoard, "Read together from book");
                break;

            case BagItemType.Music:
                // Calm: give break
                ExecuteClasswideAction(ActionType.GiveBreak, "Calming music in background");
                break;
        }

        Debug.Log($"Bag item used: {item}");
    }

    /// <summary>
    /// Execute a bag item on a specific student instead of classwide
    /// </summary>
    public void ExecuteBagItemOnStudent(BagItemType item, StudentAgent student)
    {
        if (student == null)
        {
            Debug.LogWarning("Cannot execute bag item on null student");
            return;
        }

        // Convert bag item to appropriate teacher action for the student
        ActionType actionType;
        string context = "";

        switch (item)
        {
            case BagItemType.Ruler:
                // "Strict": may raise attention but increases negative emotion a bit
                actionType = ActionType.Yell;
                context = $"Teacher used ruler on {student.studentName}";
                break;

            case BagItemType.Game:
                // Fun: reduces boredom / improves mood - use praise and give break
                actionType = ActionType.Praise;
                context = $"Teacher engaged {student.studentName} with a game";
                // Also reduce boredom
                TeacherAction breakAction = new TeacherAction
                {
                    Type = ActionType.GiveBreak,
                    TargetStudentId = student.studentId,
                    Context = $"Quick game break for {student.studentName}"
                };
                student.ReceiveTeacherAction(breakAction);
                break;

            case BagItemType.Book:
                // Structure: call student to participate
                actionType = ActionType.CallToBoard;
                context = $"Teacher used book to engage {student.studentName}";
                break;

            case BagItemType.Music:
                // Calm: give break
                actionType = ActionType.GiveBreak;
                context = $"Calming music for {student.studentName}";
                break;

            default:
                actionType = ActionType.Praise;
                context = $"Bag item used on {student.studentName}";
                break;
        }

        // Execute the action on the specific student
        TeacherAction action = new TeacherAction
        {
            Type = actionType,
            TargetStudentId = student.studentId,
            Context = context
        };

        ExecuteTeacherAction(action);

        Debug.Log($"Bag item {item} used on student: {student.studentName}");
    }

    /// <summary>
    /// Calculate student seat position in classroom grid
    /// </summary>
    Vector3 CalculateStudentPosition(int index, int totalStudents)
    {
        int rows = Mathf.CeilToInt(Mathf.Sqrt(totalStudents));
        int cols = Mathf.CeilToInt((float)totalStudents / rows);
        
        int row = index / cols;
        int col = index % cols;
        
        float xSpacing = 2.5f;
        float zSpacing = 2.5f;
        
        return new Vector3(col * xSpacing, 0, row * zSpacing);
    }

    /// <summary>
    /// Remove all students from classroom
    /// </summary>
    void ClearClassroom()
    {
        foreach (StudentAgent student in activeStudents)
        {
            if (student != null)
                Destroy(student.gameObject);
        }
        activeStudents.Clear();
    }

    /// <summary>
    /// Execute a teacher action on target student(s)
    /// </summary>
    public void ExecuteTeacherAction(TeacherAction action)
    {
        actionCount++;
        action.Timestamp = Time.time - sessionStartTime;
        currentSession.teacherActions.Add(action);

        // Track intervention type
        if (action.Type == ActionType.Praise || action.Type == ActionType.PositiveReinforcement)
            positiveInterventions++;
        else if (action.Type == ActionType.Yell || action.Type == ActionType.RemoveFromClass)
            negativeInterventions++;

        // Find target student
        StudentAgent targetStudent = activeStudents.FirstOrDefault(s => s.studentId == action.TargetStudentId);
        
        if (targetStudent != null)
        {
            targetStudent.ReceiveTeacherAction(action);
            Debug.Log($"Teacher action executed: {action.Type} on {targetStudent.studentName}");
            
            // Update UI feedback
            if (teacherUI != null)
                teacherUI.ShowActionFeedback(action, targetStudent);
        }
        else
        {
            Debug.LogWarning($"Target student not found: {action.TargetStudentId}");
        }
    }

    /// <summary>
    /// Execute action on multiple students (e.g., whole class)
    /// </summary>
    public void ExecuteClasswideAction(ActionType actionType, string context)
    {
        foreach (StudentAgent student in activeStudents)
        {
            TeacherAction action = new TeacherAction
            {
                Type = actionType,
                TargetStudentId = student.studentId,
                Context = context
            };
            student.ReceiveTeacherAction(action);
        }

        Debug.Log($"Classwide action executed: {actionType}");
    }

    /// <summary>
    /// Update real-time classroom metrics
    /// </summary>
    void UpdateClassMetrics()
    {
        if (activeStudents.Count == 0) return;

        // Calculate average engagement
        int listeningCount = activeStudents.Count(s => s.currentState == StudentState.Listening || s.currentState == StudentState.Engaged);
        overallClassEngagement = (float)listeningCount / activeStudents.Count;

        // Count disruptions
        disruptionCount = activeStudents.Count(s => s.currentState == StudentState.Arguing || s.currentState == StudentState.SideTalk);

        // Update UI
        if (teacherUI != null)
        {
            teacherUI.UpdateMetrics(overallClassEngagement, disruptionCount);
        }
    }

    /// <summary>
    /// Get student by ID
    /// </summary>
    public StudentAgent GetStudentById(string id)
    {
        return activeStudents.FirstOrDefault(s => s.studentId == id);
    }

    /// <summary>
    /// Get students in specific state
    /// </summary>
    public List<StudentAgent> GetStudentsByState(StudentState state)
    {
        return activeStudents.Where(s => s.currentState == state).ToList();
    }

    /// <summary>
    /// End current session and generate report
    /// </summary>
    public SessionReport EndSession()
    {
        currentSession.endTime = System.DateTime.Now;
        currentSession.duration = Time.time - sessionStartTime;

        SessionReport report = new SessionReport
        {
            sessionData = currentSession,
            totalActions = actionCount,
            positiveActions = positiveInterventions,
            negativeActions = negativeInterventions,
            averageEngagement = overallClassEngagement,
            totalDisruptions = disruptionCount,
            score = CalculateSessionScore()
        };

        Debug.Log($"Session ended. Score: {report.score}");
        
        // Save to database (MongoDB integration would go here)
        SaveSessionToDatabase(report);

        return report;
    }

    /// <summary>
    /// Calculate performance score based on multiple factors
    /// </summary>
    float CalculateSessionScore()
    {
        float engagementScore = overallClassEngagement * 40f;
        float disruptionPenalty = Mathf.Max(0, 30f - disruptionCount * 2f);
        float interventionBalance = positiveInterventions > negativeInterventions ? 20f : 10f;
        float efficiencyBonus = actionCount < 50 ? 10f : 5f;

        return engagementScore + disruptionPenalty + interventionBalance + efficiencyBonus;
    }



    /// <summary>
    /// Placeholder for database persistence
    /// </summary>
    void SaveSessionToDatabase(SessionReport report)
    {
        // TODO: Implement MongoDB integration
        Debug.Log("Session saved to database (placeholder)");
        
        // Also save to local session history for UI display
        TeacherHomeSceneUI.SaveSessionToHistory(report);
    }
}

/// <summary>
/// Scenario configuration loaded from JSON
/// </summary>
[System.Serializable]
public class ScenarioConfig
{
    public string scenarioName;
    public string description;
    public string difficulty;
    public List<StudentProfile> studentProfiles;
}


/// <summary>
/// Individual student configuration profile
/// </summary>
[System.Serializable]
public class StudentProfile
{
    public string id;
    public string name;
    public float extroversion;
    public float sensitivity;
    public float rebelliousness;
    public float academicMotivation;
    public float initialHappiness = 5f;
    public float initialBoredom = 3f;
}

/// <summary>
/// Session data structure for analytics
/// </summary>
[System.Serializable]
public class SessionData
{
    public string sessionId;
    public System.DateTime startTime;
    public System.DateTime endTime;
    public float duration;
    public List<TeacherAction> teacherActions;
}

/// <summary>
/// Session performance report
/// </summary>
[System.Serializable]
public class SessionReport
{
    public SessionData sessionData;
    public int totalActions;
    public int positiveActions;
    public int negativeActions;
    public float averageEngagement;
    public int totalDisruptions;
    public float score;
}