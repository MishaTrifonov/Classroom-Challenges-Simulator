using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Core student behavior agent using Finite State Machine.
/// Manages behavioral states and transitions based on emotional vectors.
/// Supports ML-Agent driven state transitions for enhanced realism.
/// </summary>
public class StudentAgent : MonoBehaviour
{
    [Header("Identity")]
    public string studentId;
    public string studentName;

    [Header("Emotional State")]
    public EmotionVector emotions = new EmotionVector();

    [Header("Behavioral Traits")]
    [Range(0f, 1f)] public float extroversion = 0.5f;
    [Range(0f, 1f)] public float sensitivity = 0.5f;
    [Range(0f, 1f)] public float rebelliousness = 0.3f;
    [Range(0f, 1f)] public float academicMotivation = 0.6f;

    [Header("Current State")]
    public StudentState currentState = StudentState.Listening;
    private StudentState previousState;

    [Header("ML-Agent Integration")]
    [Tooltip("Reference to ML-Agent coordinator (auto-found if null)")]
    public MLAgentClassroomCoordinator mlAgentCoordinator;

    [Tooltip("Use ML-Agent for state transitions instead of rule-based")]
    public bool useMLAgentTransitions = true;

    [Tooltip("Minimum confidence threshold to accept ML-Agent decisions")]
    [Range(0f, 1f)] public float mlAgentConfidenceThreshold = 0.3f;

    // ML-Agent state tracking
    private bool isRequestingMLAgentDecision = false;
    private float lastMLAgentRequestTime = 0f;
    private const float ML_AGENT_REQUEST_COOLDOWN = 1f;

    [Header("Proximity Settings")]
    public float influenceRadius = 3f;
    private List<StudentAgent> nearbyStudents = new List<StudentAgent>();

    [Header("Animation & Visual")]
    public Animator animator;
    public Renderer studentRenderer;
    private Color originalColor;
    private StudentReactionAnimator reactionAnimator;

    [Header("Movement")]
    public NavMeshAgent navAgent;
    public float walkSpeed = 2f;
    public Transform seatPosition; // Original seat position to return to
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isMoving = false;
    private bool hasAnswer = false; // Track if student has an answer ready

    [Header("Behavior Timing")]
    private float stateDuration = 0f;
    private float nextStateCheckTime = 0f;
    public float stateCheckInterval = 2f;

    [Header("Break Status")]
    private bool isOnBreak = false;
    private float breakStartTime = 0f;
    private float breakDurationSeconds = 0f;
    private Vector3 positionBeforeBreak;
    private Quaternion rotationBeforeBreak;
    private bool wasActiveBeforeBreak = true;

    void Start()
    {
        // Auto-assign renderer if not set
        if (studentRenderer == null)
        {
            studentRenderer = GetComponent<Renderer>();
            if (studentRenderer == null)
            {
                studentRenderer = GetComponentInChildren<Renderer>();
            }
        }

        if (studentRenderer != null)
        {
            originalColor = studentRenderer.material.color;
            // Ensure renderer is enabled
            studentRenderer.enabled = true;
        }
        else
        {
            Debug.LogWarning($"[StudentAgent] {studentName}: No renderer found! Student may be invisible.");
        }

        // Get StudentReactionAnimator component
        reactionAnimator = GetComponent<StudentReactionAnimator>();
        if (reactionAnimator == null)
        {
            reactionAnimator = GetComponentInChildren<StudentReactionAnimator>();
        }
        if (reactionAnimator == null)
        {
            Debug.LogWarning($"[StudentAgent] {studentName}: StudentReactionAnimator component not found! Student reactions may not work. Please add StudentReactionAnimator component to the student GameObject.");
        }

        // Initialize NavMeshAgent if not assigned
        if (navAgent == null)
            navAgent = GetComponent<NavMeshAgent>();

        if (navAgent != null)
        {
            navAgent.speed = walkSpeed;
            navAgent.enabled = true;

            // Ensure collider is enabled for click detection (NavMeshAgent doesn't disable it, but we verify)
            Collider col = GetComponent<Collider>();
            if (col != null)
                col.enabled = true;
        }


        // Store original position (seat position)
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        if (seatPosition == null)
        {
            // Create a reference point at current position
            GameObject seatObj = new GameObject($"Seat_{studentName}");
            seatObj.transform.position = originalPosition;
            seatObj.transform.rotation = originalRotation;
            seatPosition = seatObj.transform;
        }

        UpdateNearbyStudents();
        InvokeRepeating(nameof(UpdateNearbyStudents), 0f, 5f);

        // Initialize ML-Agent integration
        InitializeMLAgentIntegration();

        // Test response bubble
        GetComponent<StudentResponseBubble>()
            ?.ShowEagerBubble("בדיקה");
    }

    /// <summary>
    /// Initialize ML-Agent integration by finding coordinator and registering
    /// </summary>
    private void InitializeMLAgentIntegration()
    {
        // Find ML-Agent coordinator if not assigned
        if (mlAgentCoordinator == null)
        {
            mlAgentCoordinator = FindObjectOfType<MLAgentClassroomCoordinator>();
        }

        // Register with coordinator
        if (mlAgentCoordinator != null)
        {
            mlAgentCoordinator.RegisterStudent(this);
            Debug.Log($"[StudentAgent] {studentName}: Registered with ML-Agent coordinator");
        }
        else if (useMLAgentTransitions)
        {
            Debug.LogWarning($"[StudentAgent] {studentName}: ML-Agent transitions enabled but no coordinator found. Using rule-based fallback.");
        }
    }

    private void OnDestroy()
    {
        // Unregister from ML-Agent coordinator
        if (mlAgentCoordinator != null)
        {
            mlAgentCoordinator.UnregisterStudent(this);
        }
    }

    void Update()
    {
        // Note: Break status is checked by ClassroomManager via coroutine
        // because inactive GameObjects don't run Update()

        // Apply natural emotional decay
        emotions.Decay(Time.deltaTime);

        // Update state duration
        stateDuration += Time.deltaTime;

        // Periodic state evaluation
        if (Time.time >= nextStateCheckTime)
        {
            EvaluateStateTransition();
            nextStateCheckTime = Time.time + stateCheckInterval;
        }

        // Execute current state behavior
        ExecuteStateBehavior();

        // Check for crying when sadness is maxed
        if (emotions.Sadness >= 10f)
        {
            TriggerCryingAnimation();
        }
        else
        {
            StopCryingAnimation();
        }

        // Update movement animations
        UpdateMovementAnimations();

        // Update visual feedback
        UpdateVisualFeedback();
    }

    /// <summary>
    /// Evaluate whether to transition to a new behavioral state
    /// based on current emotions, context, and ML-Agent decisions
    /// </summary>
    void EvaluateStateTransition()
    {
        // Check if ML-Agent integration is available and enabled
        if (useMLAgentTransitions && mlAgentCoordinator != null && !isRequestingMLAgentDecision)
        {
            // Check cooldown to avoid overwhelming the ML-Agent
            if (Time.time - lastMLAgentRequestTime >= ML_AGENT_REQUEST_COOLDOWN)
            {
                StartCoroutine(EvaluateStateTransitionWithMLAgent());
                return;
            }
        }

        // Fall back to rule-based state transition
        EvaluateStateTransitionRuleBased();
    }

    /// <summary>
    /// Evaluate state transition using ML-Agent model
    /// </summary>
    private IEnumerator EvaluateStateTransitionWithMLAgent()
    {
        isRequestingMLAgentDecision = true;
        lastMLAgentRequestTime = Time.time;

        StateDecisionResult result = null;

        yield return StartCoroutine(mlAgentCoordinator.RequestStateTransition(this, (r) =>
        {
            result = r;
        }));

        isRequestingMLAgentDecision = false;

        if (result != null && result.success && result.shouldTransition)
        {
            // Check confidence threshold
            if (result.confidence >= mlAgentConfidenceThreshold)
            {
                if (result.recommendedState != currentState)
                {
                    Debug.Log($"[StudentAgent] {studentName}: ML-Agent transition " +
                              $"{currentState} -> {result.recommendedState} (conf: {result.confidence:F2})");
                    TransitionToState(result.recommendedState);
                }
            }
            else
            {
                // Low confidence - fall back to rule-based
                EvaluateStateTransitionRuleBased();
            }
        }
        else if (result == null || !result.success)
        {
            // ML-Agent failed - fall back to rule-based
            EvaluateStateTransitionRuleBased();
        }
    }

    /// <summary>
    /// Rule-based state transition evaluation (original logic)
    /// Used as fallback when ML-Agent is unavailable or returns low confidence
    /// </summary>
    void EvaluateStateTransitionRuleBased()
    {
        StudentState newState = currentState;
        float transitionProbability = 0f;

        // ANGER-DRIVEN TRANSITIONS
        if (emotions.Anger >= 7f)
        {
            transitionProbability = (emotions.Anger - 6f) / 4f * rebelliousness;
            if (Random.value < transitionProbability)
            {
                newState = StudentState.Arguing;
            }
        }

        // BOREDOM-DRIVEN TRANSITIONS
        else if (emotions.Boredom >= 7f)
        {
            if (currentState != StudentState.Distracted && currentState != StudentState.SideTalk)
            {
                newState = Random.value < 0.6f ? StudentState.Distracted : StudentState.SideTalk;
            }
        }

        // SADNESS-DRIVEN TRANSITIONS
        else if (emotions.Sadness >= 6f)
        {
            newState = StudentState.Withdrawn;
        }

        // FRUSTRATION-DRIVEN TRANSITIONS
        else if (emotions.Frustration >= 6f)
        {
            if (currentState == StudentState.Listening)
            {
                newState = Random.value < 0.5f ? StudentState.SideTalk : StudentState.Distracted;
            }
        }

        // POSITIVE STATE TRANSITIONS
        else if (emotions.Happiness >= 7f && emotions.Boredom < 4f)
        {
            if (Random.value < academicMotivation)
            {
                newState = StudentState.Engaged;
            }
        }

        // DEFAULT RETURN TO LISTENING
        else if (emotions.GetOverallMood() > 3f && currentState != StudentState.Listening)
        {
            if (Random.value < 0.3f)
            {
                newState = StudentState.Listening;
            }
        }

        if (newState != currentState)
        {
            TransitionToState(newState);
        }
    }

    /// <summary>
    /// Enable or disable ML-Agent driven state transitions
    /// </summary>
    public void SetMLAgentTransitionsEnabled(bool enabled)
    {
        useMLAgentTransitions = enabled;
    }

    /// <summary>
    /// Trigger a spontaneous interaction via ML-Agent (if available)
    /// </summary>
    public void TriggerSpontaneousInteraction(InteractionType type)
    {
        if (mlAgentCoordinator != null)
        {
            mlAgentCoordinator.TriggerInteraction(this, type);
        }
    }

    /// <summary>
    /// Execute state-specific behaviors
    /// </summary>
    void ExecuteStateBehavior()
    {
        switch (currentState)
        {
            case StudentState.Listening:
                // Calm, attentive posture
                if (animator != null)
                    animator.SetBool("IsListening", true);
                break;

            case StudentState.Engaged:
                // Active participation - check if student has answer and wants to raise hand
                if (hasAnswer && Random.value < 0.005f) // 0.5% chance per frame to raise hand when engaged
                {
                    RaiseHand();
                }
                
                // Occasionally get an answer ready (student thinks of answer)
                if (Random.value < 0.001f && academicMotivation > 0.5f)
                {
                    hasAnswer = true;
                }
                break;

            case StudentState.Distracted:
                // Looking around, fidgeting
                emotions.ApplyTrigger(EmotionalTrigger.LongPassiveActivity);
                break;

            case StudentState.SideTalk:
                // Talking to neighbors
                PropagateEmotionToNearby(0.1f);
                break;

            case StudentState.Arguing:
                // Confrontational behavior
                if (stateDuration > 5f) // After 5 seconds, may escalate
                {
                    TriggerDisruptiveEvent();
                }
                break;

            case StudentState.Withdrawn:
                // Silent, avoiding eye contact
                emotions.Boredom += 0.01f * Time.deltaTime;
                break;
        }
    }

    /// <summary>
    /// Change to a new behavioral state
    /// </summary>
    public void TransitionToState(StudentState newState)
    {
        previousState = currentState;
        currentState = newState;
        stateDuration = 0f;

        Debug.Log($"{studentName} transitioned from {previousState} to {newState} | {emotions}");

        // Trigger state-specific entry actions
        OnStateEnter(newState);
    }

    void OnStateEnter(StudentState state)
    {
        if (animator != null)
        {
            // Reset all state bools
            animator.SetBool("IsListening", false);
            animator.SetBool("IsEngaged", false);
            animator.SetBool("IsDistracted", false);
            animator.SetBool("IsTalking", false);
            animator.SetBool("IsArguing", false);
            animator.SetBool("IsWithdrawn", false);

            // Set new state
            switch (state)
            {
                case StudentState.Listening:
                    animator.SetBool("IsListening", true);
                    break;
                case StudentState.Engaged:
                    animator.SetBool("IsEngaged", true);
                    break;
                case StudentState.Distracted:
                    animator.SetBool("IsDistracted", true);
                    break;
                case StudentState.SideTalk:
                    animator.SetBool("IsTalking", true);
                    break;
                case StudentState.Arguing:
                    animator.SetBool("IsArguing", true);
                    break;
                case StudentState.Withdrawn:
                    animator.SetBool("IsWithdrawn", true);
                    break;
            }
        }
    }

    /// <summary>
    /// Respond to a teacher action directed at this student
    /// </summary>
    public void ReceiveTeacherAction(TeacherAction action)
    {
        float intensityModifier = 1f + sensitivity;
        emotions.ApplyTeacherAction(action, intensityModifier);

        // Trigger reaction animation if StudentReactionAnimator is available
        if (reactionAnimator != null)
        {
            reactionAnimator.ReactToTeacherAction(action.Type);
        }

        // Immediate behavioral response
        switch (action.Type)
        {
            case ActionType.Yell:
                if (rebelliousness > 0.7f)
                    TransitionToState(StudentState.Arguing);
                else
                    TransitionToState(StudentState.Withdrawn);
                break;

            case ActionType.Praise:
                TransitionToState(StudentState.Engaged);
                break;

            case ActionType.CallToBoard:
                TransitionToState(StudentState.Engaged);
                // Walk to board when called
                WalkToBoard();
                break;

            case ActionType.RemoveFromClass:
                // Handle permanent removal (not break)
                gameObject.SetActive(false);
                break;
        }

        // Propagate emotional effect to nearby students
        PropagateEmotionToNearby(0.3f);
    }

    /// <summary>
    /// Find students within influence radius
    /// </summary>
    void UpdateNearbyStudents()
    {
        nearbyStudents.Clear();
        StudentAgent[] allStudents = FindObjectsOfType<StudentAgent>();

        foreach (StudentAgent other in allStudents)
        {
            if (other != this && Vector3.Distance(transform.position, other.transform.position) <= influenceRadius)
            {
                nearbyStudents.Add(other);
            }
        }
    }

    /// <summary>
    /// Spread emotional effects to nearby students
    /// </summary>
    void PropagateEmotionToNearby(float intensity)
    {
        foreach (StudentAgent nearby in nearbyStudents)
        {
            // Contagious emotions
            nearby.emotions.Frustration += emotions.Frustration * 0.1f * intensity;
            nearby.emotions.Boredom += emotions.Boredom * 0.05f * intensity;
        }
    }

    /// <summary>
    /// Trigger crying animation when sadness reaches maximum (10)
    /// </summary>
    void TriggerCryingAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("IsCrying", true);
        }
    }

    /// <summary>
    /// Stop crying animation when sadness decreases
    /// </summary>
    void StopCryingAnimation()
    {
        if (animator != null && emotions.Sadness < 10f)
        {
            animator.SetBool("IsCrying", false);
        }
    }

    /// <summary>
    /// Raise hand animation - triggered when student has an answer
    /// </summary>
    public void RaiseHand()
    {
        Debug.Log($"[StudentAgent] {studentName} raised hand");
        
        if (animator != null)
        {
            // Check if the trigger parameter exists
            bool hasParameter = false;
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == "RaiseHand" && param.type == AnimatorControllerParameterType.Trigger)
                {
                    hasParameter = true;
                    break;
                }
            }
            
            if (hasParameter)
            {
                animator.SetTrigger("RaiseHand");
                Debug.Log($"[StudentAgent] RaiseHand trigger set successfully for {studentName}");
            }
            else
            {
                Debug.LogWarning($"[StudentAgent] WARNING: 'RaiseHand' trigger parameter not found in Animator Controller for {studentName}. " +
                               $"Please add a Trigger parameter named 'RaiseHand' to the Animator Controller. " +
                               $"See RAISE_HAND_ANIMATION_SETUP.md for setup instructions.");
            }
        }
        else
        {
            Debug.LogWarning($"[StudentAgent] WARNING: Animator component is null for {studentName}. Cannot trigger raise hand animation.");
        }
        
        // Also try using StudentReactionAnimator if available (preferred method)
        if (reactionAnimator != null)
        {
            reactionAnimator.RaiseHand();
        }
        
        hasAnswer = false; // Reset after raising hand
    }

    void TriggerDisruptiveEvent()
    {
        Debug.Log($"{studentName} is causing major disruption!");
        // Notify classroom manager
    }

    /// <summary>
    /// Update visual feedback based on emotional state
    /// </summary>
    void UpdateVisualFeedback()
    {
        if (studentRenderer == null) return;

        // Color code by emotional state
        Color targetColor = originalColor;

        if (emotions.Anger >= 7f)
            targetColor = Color.Lerp(originalColor, Color.red, 0.5f);
        else if (emotions.Sadness >= 7f)
            targetColor = Color.Lerp(originalColor, Color.blue, 0.5f);
        else if (emotions.Happiness >= 7f)
            targetColor = Color.Lerp(originalColor, Color.green, 0.5f);
        else if (emotions.Boredom >= 7f)
            targetColor = Color.Lerp(originalColor, Color.grey, 0.5f);

        studentRenderer.material.color = Color.Lerp(studentRenderer.material.color, targetColor, Time.deltaTime * 2f);
    }

    /// <summary>
    /// Update walking animation based on movement state
    /// </summary>
    void UpdateMovementAnimations()
    {
        if (animator == null || navAgent == null) return;

        // Check if student is moving
        bool isMovingNow = navAgent.enabled && navAgent.isOnNavMesh && navAgent.remainingDistance > 0.1f;
        
        if (isMoving != isMovingNow)
        {
            isMoving = isMovingNow;
            animator.SetBool("IsWalking", isMoving);
        }

        // Update speed parameter if animator has it
        if (isMoving && navAgent.velocity.magnitude > 0.1f)
        {
            animator.SetFloat("WalkSpeed", navAgent.velocity.magnitude / walkSpeed);
        }
    }

    /// <summary>
    /// Walk to the board when teacher calls student
    /// </summary>
    public void WalkToBoard()
    {
        if (navAgent == null || !navAgent.isOnNavMesh)
        {
            Debug.LogWarning($"{studentName} cannot walk - NavMeshAgent not available");
            return;
        }

        // Find board position
        GameObject board = GameObject.FindGameObjectWithTag("Board");
        if (board == null)
        {
            // Try finding by name
            board = GameObject.Find("board") ?? GameObject.Find("Board");
        }

        if (board != null)
        {
            Vector3 boardPosition = board.transform.position;
            // Position in front of board (adjust offset as needed)
            Vector3 targetPosition = boardPosition + board.transform.forward * 1.5f;
            targetPosition.y = transform.position.y; // Keep same height

            StartCoroutine(MoveToPosition(targetPosition));
        }
        else
        {
            Debug.LogWarning($"Board not found for {studentName} to walk to");
        }
    }

    /// <summary>
    /// Walk to a specific position (for teacher commands)
    /// </summary>
    public void WalkToPosition(Vector3 targetPosition)
    {
        if (navAgent == null || !navAgent.isOnNavMesh)
        {
            Debug.LogWarning($"{studentName} cannot walk - NavMeshAgent not available");
            return;
        }

        StartCoroutine(MoveToPosition(targetPosition));
    }

    /// <summary>
    /// Coroutine to handle movement to a target position
    /// </summary>
    IEnumerator MoveToPosition(Vector3 targetPosition)
    {
        if (navAgent == null || !navAgent.isOnNavMesh)
            yield break;

        navAgent.enabled = true;
        
        // Ensure collider remains enabled during movement for click detection
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = true;
        
        navAgent.SetDestination(targetPosition);

        // Wait until student reaches destination
        while (navAgent.enabled && navAgent.isOnNavMesh && navAgent.remainingDistance > 0.2f)
        {
            yield return null;
        }

        // Stop moving animation when reached
        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
        }

        // Face the board/teacher
        if (navAgent.enabled)
        {
            Vector3 lookDirection = targetPosition - transform.position;
            if (lookDirection.magnitude > 0.1f)
            {
                lookDirection.y = 0;
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }

        isMoving = false;
    }

    /// <summary>
    /// Return to seat after being at board
    /// </summary>
    public void ReturnToSeat()
    {
        if (seatPosition != null)
        {
            WalkToPosition(seatPosition.position);
            StartCoroutine(ReturnToSeatRotation());
        }
    }

    /// <summary>
    /// Restore original rotation when returning to seat
    /// </summary>
    IEnumerator ReturnToSeatRotation()
    {
        yield return new WaitUntil(() => !isMoving && navAgent != null && navAgent.remainingDistance < 0.2f);
        
        if (originalRotation != null)
        {
            transform.rotation = originalRotation;
        }
    }

    /// <summary>
    /// Mark that student has an answer ready (called from external systems if needed)
    /// </summary>
    public void SetHasAnswer(bool hasAnswerValue)
    {
        hasAnswer = hasAnswerValue;
        if (hasAnswer && currentState == StudentState.Engaged)
        {
            // Immediately raise hand if engaged and has answer
            RaiseHand();
        }
    }

    /// <summary>
    /// Set the visual selection state of this student
    /// </summary>
    public void SetSelected(bool selected)
    {
        // Find the StudentVisualFeedback component (usually on a child object)
        StudentVisualFeedback visualFeedback = GetComponentInChildren<StudentVisualFeedback>();
        if (visualFeedback == null)
        {
            // Try to find it on the same GameObject
            visualFeedback = GetComponent<StudentVisualFeedback>();
        }

        if (visualFeedback != null)
        {
            visualFeedback.SetSelected(selected);
        }
        else
        {
            // Fallback: if no visual feedback component, just log
            Debug.Log($"[StudentAgent] {studentName} selection: {selected} (no StudentVisualFeedback component found)");
        }
    }

    /// <summary>
    /// Respond to a teacher's question with an AI-generated answer
    /// </summary>
    public void RespondToQuestion(string question)
    {
        if (string.IsNullOrEmpty(question))
            return;

        // Find the AI response generator
        StudentAIResponseGenerator responseGenerator = FindObjectOfType<StudentAIResponseGenerator>();
        if (responseGenerator == null)
        {
            Debug.LogWarning($"[StudentAgent] No StudentAIResponseGenerator found for {studentName} to respond to question");
            return;
        }

        // Find or get the response bubble component
        StudentResponseBubble responseBubble = GetComponentInChildren<StudentResponseBubble>();
        if (responseBubble == null)
        {
            // Try to find it on the same GameObject
            responseBubble = GetComponent<StudentResponseBubble>();
        }

        // Start generating response
        StartCoroutine(GenerateAndShowResponse(responseGenerator, responseBubble, question));
    }

    /// <summary>
    /// Coroutine to generate response and display it
    /// </summary>
    private System.Collections.IEnumerator GenerateAndShowResponse(
        StudentAIResponseGenerator generator, 
        StudentResponseBubble bubble, 
        string question)
    {
        string response = "";
        
        // Generate response using the AI generator
        yield return generator.GenerateStudentResponse(this, question, (generatedResponse) =>
        {
            response = generatedResponse;
        });

        // Display the response if we have a bubble
        if (bubble != null && !string.IsNullOrEmpty(response))
        {
            bubble.ShowResponse(response);
            
            // Auto-hide after 5 seconds
            yield return new WaitForSeconds(5f);
            bubble.HideBubble();
        }
        else if (!string.IsNullOrEmpty(response))
        {
            // Log response if no bubble is available
            Debug.Log($"{studentName} says: {response}");
        }

        // Update emotional state based on responding
        if (currentState == StudentState.Engaged || currentState == StudentState.Listening)
        {
            emotions.Happiness += 0.5f;
            emotions.Boredom = Mathf.Max(1f, emotions.Boredom - 1f);
        }
    }

    /// <summary>
    /// Send student on a break for specified duration (in minutes)
    /// </summary>
    public void StartBreak(float durationMinutes)
    {
        if (isOnBreak)
        {
            Debug.LogWarning($"{studentName} is already on break!");
            return;
        }

        isOnBreak = true;
        breakStartTime = Time.time;
        breakDurationSeconds = durationMinutes * 60f;

        // Store current position and state
        positionBeforeBreak = transform.position;
        rotationBeforeBreak = transform.rotation;
        wasActiveBeforeBreak = gameObject.activeSelf;

        // Deactivate the student (hide from classroom)
        gameObject.SetActive(false);

        // Apply emotional effect - break can reduce stress/boredom
        emotions.Boredom = Mathf.Max(0f, emotions.Boredom - 1f);
        emotions.Frustration = Mathf.Max(0f, emotions.Frustration - 0.5f);

        Debug.Log($"{studentName} went on break for {durationMinutes} minutes");
    }

    /// <summary>
    /// Return student from break to the classroom
    /// Called by ClassroomManager after break duration elapses
    /// </summary>
    public void ReturnFromBreak()
    {
        if (!isOnBreak)
            return;

        isOnBreak = false;

        // Reactivate the student
        gameObject.SetActive(wasActiveBeforeBreak);

        // Restore position and rotation
        transform.position = positionBeforeBreak;
        transform.rotation = rotationBeforeBreak;

        // Reset to a calmer state after break
        TransitionToState(StudentState.Listening);
        
        // Slight emotional boost from break
        emotions.Boredom = Mathf.Max(0f, emotions.Boredom - 1f);
        emotions.Happiness = Mathf.Min(10f, emotions.Happiness + 0.5f);

        Debug.Log($"{studentName} returned from break");
    }

    /// <summary>
    /// Check if student is currently on break
    /// </summary>
    public bool IsOnBreak()
    {
        return isOnBreak;
    }

    /// <summary>
    /// Get remaining break time in seconds (returns 0 if not on break)
    /// </summary>
    public float GetRemainingBreakTime()
    {
        if (!isOnBreak)
            return 0f;

        float elapsedTime = Time.time - breakStartTime;
        return Mathf.Max(0f, breakDurationSeconds - elapsedTime);
    }

    /// <summary>
    /// Force return student from break early (if needed)
    /// </summary>
    public void ForceReturnFromBreak()
    {
        if (isOnBreak)
        {
            ReturnFromBreak();
        }
    }
}

/// <summary>
/// Discrete behavioral states for FSM
/// </summary>
public enum StudentState
{
    Listening,      // Attentive, following lesson
    Engaged,        // Actively participating
    Distracted,     // Mind wandering, not focused
    SideTalk,       // Talking to peers
    Arguing,        // Confrontational with teacher/peers
    Withdrawn       // Silent, emotionally shut down
}