using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Central coordinator for ML-Agent integration with the classroom simulation.
/// Manages the interaction between ML models, student agents, and the UI system.
/// Provides classroom context to ML models and handles spontaneous interaction display.
/// </summary>
public class MLAgentClassroomCoordinator : MonoBehaviour
{
    [Header("Configuration")]
    public MLAgentConfig config;

    [Header("Component References")]
    [Tooltip("Reference to the state decision model")]
    public MLAgentStateDecisionModel stateDecisionModel;

    [Tooltip("Reference to the interaction generator")]
    public MLAgentInteractionGenerator interactionGenerator;

    [Header("Classroom References")]
    [Tooltip("Reference to the ClassroomManager (auto-found if null)")]
    public MonoBehaviour classroomManager;

    [Header("Settings")]
    [Tooltip("Enable ML-Agent driven FSM transitions")]
    public bool enableMLAgentFSM = true;

    [Tooltip("Enable ML-Agent spontaneous interactions")]
    public bool enableSpontaneousInteractions = true;

    [Tooltip("Interval for checking spontaneous interactions (seconds)")]
    public float interactionCheckInterval = 3f;

    [Tooltip("Maximum simultaneous interactions shown")]
    public int maxSimultaneousInteractions = 3;

    [Header("Runtime State")]
    [SerializeField] private string currentLessonTopic = "";
    [SerializeField] private float lessonStartTime;
    [SerializeField] private float lastTeacherActionTime;
    [SerializeField] private string lastTeacherActionType;

    // Internal state
    private List<StudentAgent> students = new List<StudentAgent>();
    private Queue<SpontaneousInteraction> interactionQueue = new Queue<SpontaneousInteraction>();
    private List<SpontaneousInteraction> activeInteractions = new List<SpontaneousInteraction>();
    private bool isInitialized = false;
    private Coroutine interactionCheckCoroutine;

    /// <summary>
    /// Event fired when a new interaction should be displayed
    /// </summary>
    public event Action<SpontaneousInteraction> OnInteractionDisplay;

    /// <summary>
    /// Event fired when an interaction should be hidden
    /// </summary>
    public event Action<SpontaneousInteraction> OnInteractionHide;

    /// <summary>
    /// Event fired when a student's state changes via ML-Agent
    /// </summary>
    public event Action<StudentAgent, StudentState, StudentState> OnMLAgentStateChange;

    private void Awake()
    {
        // Auto-create components if not assigned
        if (stateDecisionModel == null)
        {
            stateDecisionModel = gameObject.AddComponent<MLAgentStateDecisionModel>();
        }

        if (interactionGenerator == null)
        {
            interactionGenerator = gameObject.AddComponent<MLAgentInteractionGenerator>();
        }
    }

    private void Start()
    {
        Initialize();
    }

    /// <summary>
    /// Initialize the coordinator
    /// </summary>
    public void Initialize()
    {
        if (isInitialized)
            return;

        // Share config with child components
        if (config != null)
        {
            stateDecisionModel.config = config;
            interactionGenerator.config = config;
        }
        else
        {
            config = MLAgentConfig.CreateDefaultConfig();
            stateDecisionModel.config = config;
            interactionGenerator.config = config;
        }

        // Initialize components
        stateDecisionModel.Initialize();
        interactionGenerator.Initialize();

        // Subscribe to events
        stateDecisionModel.OnStateDecisionMade += HandleStateDecision;
        interactionGenerator.OnInteractionGenerated += HandleInteractionGenerated;

        // Find classroom manager if not set
        if (classroomManager == null)
        {
            classroomManager = FindObjectOfType<ClassroomManager>();
        }

        // Mark lesson start
        lessonStartTime = Time.time;

        // Start interaction check loop
        if (enableSpontaneousInteractions)
        {
            interactionCheckCoroutine = StartCoroutine(InteractionCheckLoop());
        }

        isInitialized = true;
        Debug.Log("[MLAgentCoordinator] Initialized successfully");
    }

    private void OnDestroy()
    {
        if (stateDecisionModel != null)
            stateDecisionModel.OnStateDecisionMade -= HandleStateDecision;

        if (interactionGenerator != null)
            interactionGenerator.OnInteractionGenerated -= HandleInteractionGenerated;

        if (interactionCheckCoroutine != null)
            StopCoroutine(interactionCheckCoroutine);
    }

    /// <summary>
    /// Register a student with the coordinator
    /// </summary>
    public void RegisterStudent(StudentAgent student)
    {
        if (!students.Contains(student))
        {
            students.Add(student);
            if (config.debugLogging)
                Debug.Log($"[MLAgentCoordinator] Registered student: {student.studentName}");
        }
    }

    /// <summary>
    /// Unregister a student from the coordinator
    /// </summary>
    public void UnregisterStudent(StudentAgent student)
    {
        students.Remove(student);
    }

    /// <summary>
    /// Set the current lesson topic for context
    /// </summary>
    public void SetLessonTopic(string topic)
    {
        currentLessonTopic = topic;
    }

    /// <summary>
    /// Notify coordinator of a teacher action (for context tracking)
    /// </summary>
    public void NotifyTeacherAction(string actionType, string targetStudentId = null)
    {
        lastTeacherActionTime = Time.time;
        lastTeacherActionType = actionType;
    }

    /// <summary>
    /// Request ML-Agent state transition evaluation for a student.
    /// Called from StudentAgent.EvaluateStateTransition()
    /// </summary>
    public IEnumerator RequestStateTransition(
        StudentAgent student,
        Action<StateDecisionResult> onComplete)
    {
        if (!enableMLAgentFSM || stateDecisionModel == null)
        {
            onComplete?.Invoke(StateDecisionResult.NoChange(student.currentState));
            yield break;
        }

        var context = BuildClassroomContext(student);
        yield return StartCoroutine(stateDecisionModel.RequestStateDecision(student, context, onComplete));
    }

    /// <summary>
    /// Build classroom context for ML model
    /// </summary>
    private ClassroomContext BuildClassroomContext(StudentAgent forStudent)
    {
        var context = new ClassroomContext
        {
            lessonTopic = currentLessonTopic,
            lessonProgress = GetLessonProgress(),
            timeSinceLastTeacherAction = Time.time - lastTeacherActionTime,
            averageClassEngagement = CalculateClassEngagement(),
            nearbyStudentStates = GetNearbyStudentStates(forStudent),
            totalStudents = students.Count,
            disruptedStudentsCount = CountDisruptedStudents()
        };

        return context;
    }

    /// <summary>
    /// Calculate lesson progress (0-1)
    /// </summary>
    private float GetLessonProgress()
    {
        // Assume 45 minute lessons
        float lessonDuration = 45f * 60f;
        float elapsed = Time.time - lessonStartTime;
        return Mathf.Clamp01(elapsed / lessonDuration);
    }

    /// <summary>
    /// Calculate average class engagement
    /// </summary>
    private float CalculateClassEngagement()
    {
        if (students.Count == 0)
            return 0.5f;

        int engagedCount = 0;
        foreach (var student in students)
        {
            if (student != null && student.gameObject.activeInHierarchy)
            {
                if (student.currentState == StudentState.Listening ||
                    student.currentState == StudentState.Engaged)
                {
                    engagedCount++;
                }
            }
        }

        return (float)engagedCount / students.Count;
    }

    /// <summary>
    /// Get states of nearby students
    /// </summary>
    private List<string> GetNearbyStudentStates(StudentAgent forStudent)
    {
        var states = new List<string>();
        float nearbyRadius = forStudent.influenceRadius;

        foreach (var other in students)
        {
            if (other != forStudent && other != null && other.gameObject.activeInHierarchy)
            {
                float distance = Vector3.Distance(forStudent.transform.position, other.transform.position);
                if (distance <= nearbyRadius)
                {
                    states.Add(other.currentState.ToString());
                }
            }
        }

        // Limit to configured max
        if (config.includeNearbyStudentsContext && states.Count > config.maxNearbyStudentsInContext)
        {
            states = states.Take(config.maxNearbyStudentsInContext).ToList();
        }

        return states;
    }

    /// <summary>
    /// Count students in disruptive states
    /// </summary>
    private int CountDisruptedStudents()
    {
        int count = 0;
        foreach (var student in students)
        {
            if (student != null && student.gameObject.activeInHierarchy)
            {
                if (student.currentState == StudentState.Distracted ||
                    student.currentState == StudentState.SideTalk ||
                    student.currentState == StudentState.Arguing)
                {
                    count++;
                }
            }
        }
        return count;
    }

    /// <summary>
    /// Main loop for checking and generating spontaneous interactions
    /// </summary>
    private IEnumerator InteractionCheckLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(interactionCheckInterval);

            if (!enableSpontaneousInteractions)
                continue;

            // Clean up finished interactions
            CleanupFinishedInteractions();

            // Check if we can show more interactions
            if (activeInteractions.Count >= maxSimultaneousInteractions)
                continue;

            // Try to generate interaction for random eligible student
            var eligibleStudents = GetEligibleStudentsForInteraction();
            if (eligibleStudents.Count > 0)
            {
                var student = eligibleStudents[UnityEngine.Random.Range(0, eligibleStudents.Count)];
                var context = BuildClassroomContext(student);

                yield return StartCoroutine(interactionGenerator.TryGenerateInteraction(
                    student, context, (interaction) =>
                    {
                        if (interaction != null)
                        {
                            QueueInteraction(interaction);
                        }
                    }));
            }

            // Process queued interactions
            ProcessInteractionQueue();
        }
    }

    /// <summary>
    /// Get students eligible for spontaneous interactions
    /// </summary>
    private List<StudentAgent> GetEligibleStudentsForInteraction()
    {
        var eligible = new List<StudentAgent>();

        foreach (var student in students)
        {
            if (student == null || !student.gameObject.activeInHierarchy)
                continue;

            // Students on break can't interact
            if (student.IsOnBreak())
                continue;

            // Check if student already has active interaction
            bool hasActiveInteraction = activeInteractions.Any(i => i.studentId == student.studentId);
            if (hasActiveInteraction)
                continue;

            eligible.Add(student);
        }

        return eligible;
    }

    /// <summary>
    /// Queue an interaction for display
    /// </summary>
    private void QueueInteraction(SpontaneousInteraction interaction)
    {
        interactionQueue.Enqueue(interaction);
    }

    /// <summary>
    /// Process queued interactions
    /// </summary>
    private void ProcessInteractionQueue()
    {
        while (interactionQueue.Count > 0 && activeInteractions.Count < maxSimultaneousInteractions)
        {
            var interaction = interactionQueue.Dequeue();
            ShowInteraction(interaction);
        }
    }

    /// <summary>
    /// Show an interaction (display bubble, etc.)
    /// </summary>
    private void ShowInteraction(SpontaneousInteraction interaction)
    {
        activeInteractions.Add(interaction);

        // Find the student and show bubble
        var student = students.FirstOrDefault(s => s.studentId == interaction.studentId);
        if (student != null)
        {
            var bubble = student.GetComponent<StudentResponseBubble>();
            if (bubble == null)
                bubble = student.GetComponentInChildren<StudentResponseBubble>();

            if (bubble != null)
            {
                // Show as eager bubble for most types, or answer for questions/confusion
                if (interaction.type == InteractionType.Question ||
                    interaction.type == InteractionType.Confusion ||
                    interaction.type == InteractionType.HelpRequest)
                {
                    bubble.ShowResponse(interaction.content);
                }
                else
                {
                    bubble.ShowEagerBubble(interaction.content);
                }
            }
        }

        // Fire event
        OnInteractionDisplay?.Invoke(interaction);

        // Schedule hide
        StartCoroutine(HideInteractionAfterDelay(interaction, interaction.displayDuration));

        if (config.debugLogging)
        {
            Debug.Log($"[MLAgentCoordinator] Showing interaction: {interaction.studentName} - {interaction.type}: {interaction.content}");
        }
    }

    /// <summary>
    /// Hide interaction after delay
    /// </summary>
    private IEnumerator HideInteractionAfterDelay(SpontaneousInteraction interaction, float delay)
    {
        yield return new WaitForSeconds(delay);

        HideInteraction(interaction);
    }

    /// <summary>
    /// Hide an interaction
    /// </summary>
    private void HideInteraction(SpontaneousInteraction interaction)
    {
        activeInteractions.Remove(interaction);

        // Find the student and hide bubble
        var student = students.FirstOrDefault(s => s.studentId == interaction.studentId);
        if (student != null)
        {
            var bubble = student.GetComponent<StudentResponseBubble>();
            if (bubble == null)
                bubble = student.GetComponentInChildren<StudentResponseBubble>();

            if (bubble != null)
            {
                bubble.HideBubble();
            }
        }

        // Mark interaction finished in generator
        interactionGenerator.MarkInteractionFinished(interaction.studentId);

        // Fire event
        OnInteractionHide?.Invoke(interaction);
    }

    /// <summary>
    /// Clean up expired interactions
    /// </summary>
    private void CleanupFinishedInteractions()
    {
        var toRemove = new List<SpontaneousInteraction>();

        foreach (var interaction in activeInteractions)
        {
            float elapsed = Time.time - interaction.timestamp;
            if (elapsed > interaction.displayDuration + 1f)
            {
                toRemove.Add(interaction);
            }
        }

        foreach (var interaction in toRemove)
        {
            activeInteractions.Remove(interaction);
            interactionGenerator.MarkInteractionFinished(interaction.studentId);
        }
    }

    /// <summary>
    /// Handle state decision events
    /// </summary>
    private void HandleStateDecision(string studentId, StudentState newState, float confidence)
    {
        var student = students.FirstOrDefault(s => s.studentId == studentId);
        if (student != null && student.currentState != newState)
        {
            OnMLAgentStateChange?.Invoke(student, student.currentState, newState);
        }
    }

    /// <summary>
    /// Handle interaction generated events
    /// </summary>
    private void HandleInteractionGenerated(SpontaneousInteraction interaction)
    {
        // Interaction already queued in TryGenerateInteraction
    }

    /// <summary>
    /// Force trigger an interaction for a specific student
    /// </summary>
    public void TriggerInteraction(StudentAgent student, InteractionType type)
    {
        if (student == null)
            return;

        var context = BuildClassroomContext(student);
        StartCoroutine(interactionGenerator.ForceGenerateInteraction(student, context, type, (interaction) =>
        {
            if (interaction != null)
            {
                ShowInteraction(interaction);
            }
        }));
    }

    /// <summary>
    /// Get all active interactions
    /// </summary>
    public List<SpontaneousInteraction> GetActiveInteractions()
    {
        return new List<SpontaneousInteraction>(activeInteractions);
    }

    /// <summary>
    /// Get interactions requiring teacher response
    /// </summary>
    public List<SpontaneousInteraction> GetPendingResponseInteractions()
    {
        return activeInteractions.Where(i => i.requiresResponse).ToList();
    }

    /// <summary>
    /// Mark an interaction as responded to
    /// </summary>
    public void MarkInteractionResponded(string interactionId)
    {
        var interaction = activeInteractions.FirstOrDefault(i => i.interactionId == interactionId);
        if (interaction != null)
        {
            interaction.requiresResponse = false;
        }
    }

    /// <summary>
    /// Enable/disable ML-Agent FSM
    /// </summary>
    public void SetMLAgentFSMEnabled(bool enabled)
    {
        enableMLAgentFSM = enabled;
        if (stateDecisionModel != null)
            stateDecisionModel.useMLAgent = enabled;
    }

    /// <summary>
    /// Enable/disable spontaneous interactions
    /// </summary>
    public void SetSpontaneousInteractionsEnabled(bool enabled)
    {
        enableSpontaneousInteractions = enabled;
        if (interactionGenerator != null)
            interactionGenerator.useMLAgent = enabled;
    }

    /// <summary>
    /// Get statistics about ML-Agent operations
    /// </summary>
    public MLAgentStats GetStats()
    {
        return new MLAgentStats
        {
            registeredStudents = students.Count,
            activeInteractions = activeInteractions.Count,
            queuedInteractions = interactionQueue.Count,
            engagementRate = CalculateClassEngagement(),
            disruptionRate = (float)CountDisruptedStudents() / Mathf.Max(1, students.Count),
            lessonProgress = GetLessonProgress()
        };
    }
}

/// <summary>
/// Statistics about ML-Agent operations
/// </summary>
[Serializable]
public class MLAgentStats
{
    public int registeredStudents;
    public int activeInteractions;
    public int queuedInteractions;
    public float engagementRate;
    public float disruptionRate;
    public float lessonProgress;
}
