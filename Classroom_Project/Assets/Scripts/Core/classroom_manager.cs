using UnityEngine;
using System.Collections;
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
    public List<StudentAgent> activeStudents = new List<StudentAgent>();

    [Header("Student Spawning")]
    [Tooltip("StudentSpawner component that handles spawning students. This is the active spawning system.")]
    public StudentSpawner studentSpawner;

    
    [Header("Teacher Interface")]
    public TeacherUI teacherUI;
    
    [Header("Camera Control")]
    [Tooltip("Camera controller for automatic focusing on eager students")]
    public CameraController cameraController;
    
    [Header("Session Tracking")]
    public SessionData currentSession;
    private float sessionStartTime;
    private int actionCount = 0;
    
    [Header("Performance Metrics")]
    public float overallClassEngagement = 0f;
    public int disruptionCount = 0;
    public int positiveInterventions = 0;
    public int negativeInterventions = 0;
    
    // Track which students are eager to answer
    private Dictionary<StudentAgent, bool> studentEagernessState = new Dictionary<StudentAgent, bool>();
    private StudentAgent currentlyFocusedStudent = null;

    void Start()
    {
        InitializeSession();
        
        // Ensure main camera is active from the start
        EnsureMainCameraActive();

        // Load scenario chosen in ScenarioSelectionUI
        string selectedScenario = PlayerPrefs.GetString("SelectedScenario", "");

        ScenarioLoader loader = FindObjectOfType<ScenarioLoader>();

        if (!string.IsNullOrEmpty(selectedScenario) && loader != null)
        {
            loader.LoadScenario(selectedScenario, 
                onSuccess: (loadedScenario) => {
                    LoadScenario(loadedScenario);
                },
                onError: (error) => {
                    Debug.LogError($"Failed to load scenario: {error}");
                    Debug.LogWarning("Falling back to inspector scenario");
                    LoadScenario(currentScenario);
                });
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
        
        // Monitor students for eagerness to answer
        if (cameraController != null && cameraController.autoFocusOnEagerStudents)
        {
            MonitorStudentEagerness();
        }
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

        // Ensure main camera is always active and disable all student cameras
        // Call immediately and also after a short delay to catch any late-instantiated cameras
        EnsureMainCameraActive();
        StartCoroutine(EnsureMainCameraActiveDelayed());

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
                // Improve mood for most of the class
                ImproveClassMood(0.75f, 1.5f); // 75% of students, +1.5 happiness
                // Trigger positive student responses
                TriggerPositiveStudentResponses(item);
                break;

            case BagItemType.Book:
                // Structure: call students to participate
                ExecuteClasswideAction(ActionType.CallToBoard, "Read together from book");
                break;

            case BagItemType.Music:
                // Calm: give break and improve mood
                ExecuteClasswideAction(ActionType.GiveBreak, "Calming music in background");
                // Improve mood for most of the class
                ImproveClassMood(0.75f, 1.5f); // 75% of students, +1.5 happiness
                // Trigger positive student responses
                TriggerPositiveStudentResponses(item);
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
                // Improve mood
                student.emotions.Happiness = Mathf.Clamp(student.emotions.Happiness + 1.5f, 1f, 10f);
                student.emotions.Boredom = Mathf.Clamp(student.emotions.Boredom - 2f, 1f, 10f);
                // Trigger positive response
                TriggerStudentPositiveResponse(student, item);
                break;

            case BagItemType.Book:
                // Structure: call student to participate
                actionType = ActionType.CallToBoard;
                context = $"Teacher used book to engage {student.studentName}";
                break;

            case BagItemType.Music:
                // Calm: give break and improve mood
                actionType = ActionType.GiveBreak;
                context = $"Calming music for {student.studentName}";
                // Improve mood
                student.emotions.Happiness = Mathf.Clamp(student.emotions.Happiness + 1.5f, 1f, 10f);
                student.emotions.Boredom = Mathf.Clamp(student.emotions.Boredom - 2f, 1f, 10f);
                // Trigger positive response
                TriggerStudentPositiveResponse(student, item);
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

        // Calculate average engagement (exclude students on break)
        int activeInClass = activeStudents.Count(s => s != null && !s.IsOnBreak());
        if (activeInClass > 0)
        {
            int listeningCount = activeStudents.Count(s => s != null && !s.IsOnBreak() && 
                (s.currentState == StudentState.Listening || s.currentState == StudentState.Engaged));
            overallClassEngagement = (float)listeningCount / activeInClass;
        }

        // Count disruptions (exclude students on break)
        disruptionCount = activeStudents.Count(s => s != null && !s.IsOnBreak() && 
            (s.currentState == StudentState.Arguing || s.currentState == StudentState.SideTalk));

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
    /// Give a student a break for specified duration (in minutes)
    /// </summary>
    public void GiveStudentBreak(StudentAgent student, float durationMinutes)
    {
        if (student == null)
        {
            Debug.LogWarning("Cannot give break to null student");
            return;
        }

        if (!activeStudents.Contains(student))
        {
            Debug.LogWarning($"Student {student.studentName} is not in active students list");
            return;
        }

        // Record the action
        TeacherAction action = new TeacherAction
        {
            Type = ActionType.GiveBreak,
            TargetStudentId = student.studentId,
            Context = $"Student {student.studentName} sent on break for {durationMinutes} minutes",
            Timestamp = Time.time - sessionStartTime
        };

        actionCount++;
        currentSession.teacherActions.Add(action);
        positiveInterventions++; // Break is considered a positive intervention

        // Start the break
        student.StartBreak(durationMinutes);

        // Start coroutine to handle break timer (since inactive GameObjects don't run Update)
        StartCoroutine(HandleStudentBreak(student, durationMinutes * 60f));

        Debug.Log($"Gave break to {student.studentName} for {durationMinutes} minutes");
    }

    /// <summary>
    /// Coroutine to handle student break timer and return student after duration
    /// </summary>
    IEnumerator HandleStudentBreak(StudentAgent student, float durationSeconds)
    {
        yield return new WaitForSeconds(durationSeconds);

        // Return student from break
        if (student != null)
        {
            student.ReturnFromBreak();
            
            // Show feedback in UI if available
            if (teacherUI != null)
            {
                teacherUI.ShowFeedback($"{student.studentName} חזר מההפסקה", Color.green);
            }

            Debug.Log($"{student.studentName} returned from break");
        }
    }

    /// <summary>
    /// Swap seats between two students or move a student to a seat spawn point
    /// </summary>
    public void SwapSeats(StudentAgent student1, StudentAgent student2, Transform seat1, Transform seat2)
    {
        // Case 1: Two students swapping seats
        if (student1 != null && student2 != null)
        {
            Vector3 tempPosition = student1.transform.position;
            Quaternion tempRotation = student1.transform.rotation;

            student1.transform.position = student2.transform.position;
            student1.transform.rotation = student2.transform.rotation;

            student2.transform.position = tempPosition;
            student2.transform.rotation = tempRotation;

            // Apply emotional effect for both students
            TeacherAction action1 = new TeacherAction
            {
                Type = ActionType.ChangeSeating,
                TargetStudentId = student1.studentId,
                Context = $"Swapped seat with {student2.studentName}"
            };
            student1.ReceiveTeacherAction(action1);

            TeacherAction action2 = new TeacherAction
            {
                Type = ActionType.ChangeSeating,
                TargetStudentId = student2.studentId,
                Context = $"Swapped seat with {student1.studentName}"
            };
            student2.ReceiveTeacherAction(action2);

            // Track the action
            actionCount++;
            currentSession.teacherActions.Add(action1);
            currentSession.teacherActions.Add(action2);

            Debug.Log($"Swapped seats between {student1.studentName} and {student2.studentName}");
            return;
        }

        // Case 2: Student to seat spawn point
        if (student1 != null && seat2 != null)
        {
            student1.transform.position = seat2.position;
            student1.transform.rotation = seat2.rotation;

            TeacherAction action = new TeacherAction
            {
                Type = ActionType.ChangeSeating,
                TargetStudentId = student1.studentId,
                Context = $"Moved to seat {seat2.name}"
            };
            student1.ReceiveTeacherAction(action);

            actionCount++;
            currentSession.teacherActions.Add(action);

            Debug.Log($"Moved {student1.studentName} to seat {seat2.name}");
            return;
        }

        if (student2 != null && seat1 != null)
        {
            student2.transform.position = seat1.position;
            student2.transform.rotation = seat1.rotation;

            TeacherAction action = new TeacherAction
            {
                Type = ActionType.ChangeSeating,
                TargetStudentId = student2.studentId,
                Context = $"Moved to seat {seat1.name}"
            };
            student2.ReceiveTeacherAction(action);

            actionCount++;
            currentSession.teacherActions.Add(action);

            Debug.Log($"Moved {student2.studentName} to seat {seat1.name}");
            return;
        }

        // Case 3: Two seat spawn points (move students to those seats)
        if (seat1 != null && seat2 != null)
        {
            // Find students closest to each seat and swap them
            StudentAgent closestToSeat1 = FindClosestStudentToSeat(seat1.position);
            StudentAgent closestToSeat2 = FindClosestStudentToSeat(seat2.position);

            if (closestToSeat1 != null && closestToSeat2 != null && closestToSeat1 != closestToSeat2)
            {
                Vector3 tempPosition = closestToSeat1.transform.position;
                Quaternion tempRotation = closestToSeat1.transform.rotation;

                closestToSeat1.transform.position = seat2.position;
                closestToSeat1.transform.rotation = seat2.rotation;

                closestToSeat2.transform.position = seat1.position;
                closestToSeat2.transform.rotation = seat1.rotation;

                // Apply emotional effect
                TeacherAction action1 = new TeacherAction
                {
                    Type = ActionType.ChangeSeating,
                    TargetStudentId = closestToSeat1.studentId,
                    Context = $"Moved to seat {seat2.name}"
                };
                closestToSeat1.ReceiveTeacherAction(action1);

                TeacherAction action2 = new TeacherAction
                {
                    Type = ActionType.ChangeSeating,
                    TargetStudentId = closestToSeat2.studentId,
                    Context = $"Moved to seat {seat1.name}"
                };
                closestToSeat2.ReceiveTeacherAction(action2);

                actionCount++;
                currentSession.teacherActions.Add(action1);
                currentSession.teacherActions.Add(action2);

                Debug.Log($"Swapped students at seats {seat1.name} and {seat2.name}");
            }
            else if (closestToSeat1 != null)
            {
                // Only one student found, move to the other seat
                closestToSeat1.transform.position = seat2.position;
                closestToSeat1.transform.rotation = seat2.rotation;

                TeacherAction action = new TeacherAction
                {
                    Type = ActionType.ChangeSeating,
                    TargetStudentId = closestToSeat1.studentId,
                    Context = $"Moved to seat {seat2.name}"
                };
                closestToSeat1.ReceiveTeacherAction(action);

                actionCount++;
                currentSession.teacherActions.Add(action);

                Debug.Log($"Moved {closestToSeat1.studentName} to seat {seat2.name}");
            }
            return;
        }

        Debug.LogWarning("Invalid seat swap parameters provided");
    }

    /// <summary>
    /// Find the student closest to a given seat position
    /// </summary>
    StudentAgent FindClosestStudentToSeat(Vector3 seatPosition)
    {
        StudentAgent closest = null;
        float minDistance = float.MaxValue;
        float maxDistance = 3f; // Maximum distance to consider a student as being at that seat

        foreach (StudentAgent student in activeStudents)
        {
            if (student == null) continue;

            float distance = Vector3.Distance(student.transform.position, seatPosition);
            if (distance < minDistance && distance < maxDistance)
            {
                minDistance = distance;
                closest = student;
            }
        }

        return closest;
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
    /// Monitor students for when they become eager to answer and focus camera on them
    /// </summary>
    void MonitorStudentEagerness()
    {
        // Only focus on one student at a time
        if (currentlyFocusedStudent != null)
            return;
        
        foreach (StudentAgent student in activeStudents)
        {
            if (student == null || student.IsOnBreak())
                continue;
            
            // Check if student has StudentQuestionResponder component
            StudentQuestionResponder responder = student.GetComponent<StudentQuestionResponder>();
            if (responder == null)
                continue;
            
            bool isEager = responder.HasAnswerReady();
            bool wasEager = studentEagernessState.ContainsKey(student) && studentEagernessState[student];
            
            // Update state
            studentEagernessState[student] = isEager;
            
            // If student just became eager (wasn't eager before, but is now)
            if (!wasEager && isEager)
            {
                // Focus camera on this student
                FocusCameraOnStudent(student);
                currentlyFocusedStudent = student;
                Debug.Log($"[ClassroomManager] Camera focusing on {student.studentName} who wants to answer");
                break; // Only focus on the first eager student
            }
            
            // If student is no longer eager, clear focus and return to manual control
            if (wasEager && !isEager && currentlyFocusedStudent == student)
            {
                if (cameraController != null)
                {
                    cameraController.StopFocusing();
                }
                currentlyFocusedStudent = null;
                Debug.Log($"[ClassroomManager] {student.studentName} is no longer eager, returning camera to manual control");
            }
        }
    }
    
    /// <summary>
    /// Focus camera on a specific student
    /// </summary>
    void FocusCameraOnStudent(StudentAgent student)
    {
        if (student == null || cameraController == null)
            return;
        
        cameraController.FocusOnStudent(student.transform, () => {
            Debug.Log($"[ClassroomManager] Camera finished focusing on {student.studentName}");
        });
    }
    
    /// <summary>
    /// Coroutine to ensure main camera is active after a delay (catches late-instantiated cameras)
    /// </summary>
    IEnumerator EnsureMainCameraActiveDelayed()
    {
        yield return new WaitForSeconds(0.1f);
        EnsureMainCameraActive();
    }
    
    /// <summary>
    /// Ensure main camera is active and disable all student cameras
    /// </summary>
    void EnsureMainCameraActive()
    {
        // Find and enable the main camera
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            // Try to find camera tagged as MainCamera
            GameObject mainCamObj = GameObject.FindGameObjectWithTag("MainCamera");
            if (mainCamObj != null)
            {
                mainCam = mainCamObj.GetComponent<Camera>();
            }
        }
        
        if (mainCam != null)
        {
            mainCam.enabled = true;
            mainCam.gameObject.SetActive(true);
            Debug.Log("[ClassroomManager] Main camera enabled and activated");
        }
        else
        {
            Debug.LogWarning("[ClassroomManager] Main camera not found!");
        }
        
        // Disable all student cameras
        Camera[] allCameras = FindObjectsOfType<Camera>();
        int disabledCount = 0;
        foreach (Camera cam in allCameras)
        {
            // Skip the main camera
            if (cam == mainCam || cam.tag == "MainCamera")
                continue;
            
            // Disable any camera that's not the main camera
            // This will catch student cameras and any other cameras
            cam.enabled = false;
            disabledCount++;
        }
        
        if (disabledCount > 0)
        {
            Debug.Log($"[ClassroomManager] Disabled {disabledCount} non-main camera(s)");
        }
    }
    
    /// <summary>
    /// Placeholder for database persistence
    /// </summary>
    void SaveSessionToDatabase(SessionReport report)
    {
        // TODO: Implement MongoDB integration
        Debug.Log("Session saved to database (placeholder)");
        
        // Also save to local session history for UI display
        if (report != null)
        {
            TeacherHomeSceneUI.SaveSessionToHistory(report);
        }
    }

    /// <summary>
    /// Improve mood for a percentage of the class
    /// </summary>
    void ImproveClassMood(float percentage, float happinessBoost)
    {
        if (activeStudents.Count == 0) return;

        // Calculate how many students to affect
        int studentsToAffect = Mathf.Max(1, Mathf.RoundToInt(activeStudents.Count * percentage));
        
        // Shuffle students to randomly select which ones get the mood boost
        List<StudentAgent> shuffledStudents = new List<StudentAgent>(activeStudents);
        for (int i = 0; i < shuffledStudents.Count; i++)
        {
            StudentAgent temp = shuffledStudents[i];
            int randomIndex = Random.Range(i, shuffledStudents.Count);
            shuffledStudents[i] = shuffledStudents[randomIndex];
            shuffledStudents[randomIndex] = temp;
        }

        // Apply mood improvement to selected students
        for (int i = 0; i < studentsToAffect && i < shuffledStudents.Count; i++)
        {
            StudentAgent student = shuffledStudents[i];
            if (student != null && !student.IsOnBreak())
            {
                student.emotions.Happiness = Mathf.Clamp(student.emotions.Happiness + happinessBoost, 1f, 10f);
                student.emotions.Boredom = Mathf.Clamp(student.emotions.Boredom - 2f, 1f, 10f);
                student.emotions.Frustration = Mathf.Clamp(student.emotions.Frustration - 1f, 1f, 10f);
            }
        }

        Debug.Log($"Improved mood for {studentsToAffect} students");
    }

    /// <summary>
    /// Trigger positive student responses for classwide bag items (Game, Music)
    /// </summary>
    void TriggerPositiveStudentResponses(BagItemType item)
    {
        if (activeStudents.Count == 0) return;

        // Hebrew positive response messages
        string[] positiveResponses = new string[]
        {
            "איזה כייף!",
            "המשחק הזה טוב!",
            "זה כיף!",
            "אני אוהב את זה!",
            "זה נהדר!",
            "כיף גדול!",
            "אני נהנה!",
            "זה מהנה!"
        };

        // Trigger responses for about 60-70% of students (randomly)
        int studentsToRespond = Mathf.Max(1, Mathf.RoundToInt(activeStudents.Count * Random.Range(0.6f, 0.7f)));
        
        // Shuffle students to randomly select which ones respond
        List<StudentAgent> shuffledStudents = new List<StudentAgent>(activeStudents);
        for (int i = 0; i < shuffledStudents.Count; i++)
        {
            StudentAgent temp = shuffledStudents[i];
            int randomIndex = Random.Range(i, shuffledStudents.Count);
            shuffledStudents[i] = shuffledStudents[randomIndex];
            shuffledStudents[randomIndex] = temp;
        }

        // Trigger responses with slight delays to make it feel natural
        for (int i = 0; i < studentsToRespond && i < shuffledStudents.Count; i++)
        {
            StudentAgent student = shuffledStudents[i];
            if (student != null && !student.IsOnBreak())
            {
                string response = positiveResponses[Random.Range(0, positiveResponses.Length)];
                float delay = Random.Range(0.2f, 1.5f); // Stagger responses
                StartCoroutine(ShowStudentResponseDelayed(student, response, delay));
            }
        }
    }

    /// <summary>
    /// Trigger a positive response from a single student
    /// </summary>
    void TriggerStudentPositiveResponse(StudentAgent student, BagItemType item)
    {
        if (student == null || student.IsOnBreak()) return;

        // Hebrew positive response messages
        string[] positiveResponses = new string[]
        {
            "איזה כייף!",
            "המשחק הזה טוב!",
            "זה כיף!",
            "אני אוהב את זה!",
            "זה נהדר!",
            "כיף גדול!",
            "אני נהנה!",
            "זה מהנה!"
        };

        string response = positiveResponses[Random.Range(0, positiveResponses.Length)];
        float delay = Random.Range(0.1f, 0.5f);
        StartCoroutine(ShowStudentResponseDelayed(student, response, delay));
    }

    /// <summary>
    /// Coroutine to show student response with a delay
    /// </summary>
    IEnumerator ShowStudentResponseDelayed(StudentAgent student, string response, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (student == null) yield break;

        // Find the response bubble component
        StudentResponseBubble responseBubble = student.GetComponentInChildren<StudentResponseBubble>();
        if (responseBubble == null)
        {
            responseBubble = student.GetComponent<StudentResponseBubble>();
        }

        if (responseBubble != null)
        {
            responseBubble.ShowResponse(response);
            Debug.Log($"{student.studentName} says: {response}");
        }
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