using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Core student behavior agent using Finite State Machine.
/// Manages behavioral states and transitions based on emotional vectors.
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

    [Header("Proximity Settings")]
    public float influenceRadius = 3f;
    private List<StudentAgent> nearbyStudents = new List<StudentAgent>();

    [Header("Animation & Visual")]
    public Animator animator;
    public Renderer studentRenderer;
    private Color originalColor;

    [Header("Behavior Timing")]
    private float stateDuration = 0f;
    private float nextStateCheckTime = 0f;
    public float stateCheckInterval = 2f;

    void Start()
    {
        if (studentRenderer != null)
            originalColor = studentRenderer.material.color;

        UpdateNearbyStudents();
        InvokeRepeating(nameof(UpdateNearbyStudents), 0f, 5f);
    }

    void Update()
    {
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

        // Update visual feedback
        UpdateVisualFeedback();
    }

    /// <summary>
    /// Evaluate whether to transition to a new behavioral state
    /// based on current emotions and context
    /// </summary>
    void EvaluateStateTransition()
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
                // Active participation, hand raised
                if (Random.value < 0.01f) // 1% chance per frame to raise hand
                {
                    RaiseHand();
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
    void TransitionToState(StudentState newState)
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
                break;

            case ActionType.RemoveFromClass:
                // Handle removal
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

    void RaiseHand()
    {
        Debug.Log($"{studentName} raised hand");
        // Visual indicator or animation
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