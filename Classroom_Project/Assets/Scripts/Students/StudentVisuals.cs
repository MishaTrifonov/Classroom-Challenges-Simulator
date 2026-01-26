using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centralized model for managing all student reaction animations.
/// Acts as a bridge between emotional states, behavioral states, and the Animator component.
/// Provides clean methods for triggering all types of student reactions.
/// </summary>
[RequireComponent(typeof(Animator))]
public class StudentReactionAnimator : MonoBehaviour
{
    #region References
    [Header("Required References")]
    [Tooltip("Reference to the student agent this animator controls")]
    public StudentAgent studentAgent;

    private Animator animator;
    #endregion

    #region Animation State Tracking
    [Header("Current Animation State")]
    [SerializeField] private StudentState currentAnimationState;
    [SerializeField] private ReactionType currentReaction = ReactionType.None;
    [SerializeField] private bool isCrying = false;
    [SerializeField] private bool isRaisingHand = false;
    [SerializeField] private bool isWalking = false;
    #endregion

    #region Animator Parameter Names
    // Behavioral State Parameters
    private static readonly string PARAM_IS_LISTENING = "IsListening";
    private static readonly string PARAM_IS_ENGAGED = "IsEngaged";
    private static readonly string PARAM_IS_DISTRACTED = "IsDistracted";
    private static readonly string PARAM_IS_TALKING = "IsTalking";
    private static readonly string PARAM_IS_ARGUING = "IsArguing";
    private static readonly string PARAM_IS_WITHDRAWN = "IsWithdrawn";

    // Reaction Parameters
    private static readonly string PARAM_IS_CRYING = "IsCrying";
    /// <summary>Trigger parameter for hand-raise. In Unity: Animator Controller must have a Trigger named exactly "RaiseHand".</summary>
    private static readonly string PARAM_RAISE_HAND = "RaiseHand";

    // Movement Parameters
    private static readonly string PARAM_IS_WALKING = "IsWalking";
    private static readonly string PARAM_WALK_SPEED = "WalkSpeed";

    // Emotional Reaction Triggers (can be added to animator)
    private static readonly string TRIGGER_HAPPY = "TriggerHappy";
    private static readonly string TRIGGER_SAD = "TriggerSad";
    private static readonly string TRIGGER_ANGRY = "TriggerAngry";
    private static readonly string TRIGGER_FRUSTRATED = "TriggerFrustrated";
    private static readonly string TRIGGER_BORED = "TriggerBored";
    private static readonly string TRIGGER_CELEBRATE = "TriggerCelebrate";
    private static readonly string TRIGGER_SHOCKED = "TriggerShocked";
    private static readonly string TRIGGER_CONFUSED = "TriggerConfused";
    #endregion

    #region Configuration
    [Header("Reaction Configuration")]
    [Tooltip("Duration of quick reaction animations in seconds")]
    public float quickReactionDuration = 2f;

    [Tooltip("Cooldown between automatic emotional reactions")]
    public float emotionalReactionCooldown = 5f;
    private float lastEmotionalReactionTime = 0f;

    [Tooltip("Enable automatic emotion-based reactions")]
    public bool enableAutomaticEmotionalReactions = true;

    [Header("Thresholds")]
    [Tooltip("Emotion level threshold to trigger automatic reactions (1-10)")]
    [Range(1f, 10f)] public float emotionThreshold = 7f;
    #endregion

    #region Initialization
    void Awake()
    {
        animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogError($"[StudentReactionAnimator] No Animator component found on {gameObject.name}!");
        }

        if (studentAgent == null)
        {
            studentAgent = GetComponent<StudentAgent>();
            if (studentAgent == null)
            {
                studentAgent = GetComponentInParent<StudentAgent>();
            }
        }
    }

    void Start()
    {
        // Initialize all animator parameters to default state
        ResetAllAnimationStates();
    }
    #endregion

    #region Update Loop
    void Update()
    {
        if (studentAgent == null || animator == null) return;

        // Update behavioral state animations
        UpdateBehavioralStateAnimation();

        // Monitor emotional states for automatic reactions
        if (enableAutomaticEmotionalReactions && Time.time - lastEmotionalReactionTime >= emotionalReactionCooldown)
        {
            CheckAndTriggerEmotionalReactions();
        }

        // Update crying state based on sadness
        UpdateCryingState();
    }
    #endregion

    #region Behavioral State Animations
    /// <summary>
    /// Update animator based on current behavioral state
    /// </summary>
    private void UpdateBehavioralStateAnimation()
    {
        if (currentAnimationState != studentAgent.currentState)
        {
            TransitionToBehavioralState(studentAgent.currentState);
        }
    }

    /// <summary>
    /// Transition to a new behavioral state animation
    /// </summary>
    public void TransitionToBehavioralState(StudentState newState)
    {
        if (animator == null) return;

        // Reset all behavioral state bools
        animator.SetBool(PARAM_IS_LISTENING, false);
        animator.SetBool(PARAM_IS_ENGAGED, false);
        animator.SetBool(PARAM_IS_DISTRACTED, false);
        animator.SetBool(PARAM_IS_TALKING, false);
        animator.SetBool(PARAM_IS_ARGUING, false);
        animator.SetBool(PARAM_IS_WITHDRAWN, false);

        // Set new state
        switch (newState)
        {
            case StudentState.Listening:
                animator.SetBool(PARAM_IS_LISTENING, true);
                break;
            case StudentState.Engaged:
                animator.SetBool(PARAM_IS_ENGAGED, true);
                break;
            case StudentState.Distracted:
                animator.SetBool(PARAM_IS_DISTRACTED, true);
                break;
            case StudentState.SideTalk:
                animator.SetBool(PARAM_IS_TALKING, true);
                break;
            case StudentState.Arguing:
                animator.SetBool(PARAM_IS_ARGUING, true);
                break;
            case StudentState.Withdrawn:
                animator.SetBool(PARAM_IS_WITHDRAWN, true);
                break;
        }

        currentAnimationState = newState;
        Debug.Log($"[StudentReactionAnimator] {studentAgent.studentName} transitioned to {newState} animation");
    }
    #endregion

    #region Emotional Reactions
    /// <summary>
    /// Check emotional state and trigger appropriate reactions
    /// </summary>
    private void CheckAndTriggerEmotionalReactions()
    {
        EmotionVector emotions = studentAgent.emotions;

        // Check each emotion and trigger reaction if above threshold
        if (emotions.Happiness >= emotionThreshold && emotions.Happiness > GetMaxOtherEmotion(emotions, "Happiness"))
        {
            TriggerHappyReaction();
        }
        else if (emotions.Sadness >= emotionThreshold && emotions.Sadness > GetMaxOtherEmotion(emotions, "Sadness"))
        {
            TriggerSadReaction();
        }
        else if (emotions.Anger >= emotionThreshold && emotions.Anger > GetMaxOtherEmotion(emotions, "Anger"))
        {
            TriggerAngryReaction();
        }
        else if (emotions.Frustration >= emotionThreshold && emotions.Frustration > GetMaxOtherEmotion(emotions, "Frustration"))
        {
            TriggerFrustratedReaction();
        }
        else if (emotions.Boredom >= emotionThreshold && emotions.Boredom > GetMaxOtherEmotion(emotions, "Boredom"))
        {
            TriggerBoredReaction();
        }
    }

    /// <summary>
    /// Helper method to get max of other emotions (excluding specified one)
    /// </summary>
    private float GetMaxOtherEmotion(EmotionVector emotions, string excludeEmotion)
    {
        float max = 0f;
        if (excludeEmotion != "Happiness") max = Mathf.Max(max, emotions.Happiness);
        if (excludeEmotion != "Sadness") max = Mathf.Max(max, emotions.Sadness);
        if (excludeEmotion != "Anger") max = Mathf.Max(max, emotions.Anger);
        if (excludeEmotion != "Frustration") max = Mathf.Max(max, emotions.Frustration);
        if (excludeEmotion != "Boredom") max = Mathf.Max(max, emotions.Boredom);
        return max;
    }

    /// <summary>
    /// Trigger happy/joy reaction
    /// </summary>
    public void TriggerHappyReaction()
    {
        if (!HasAnimatorParameter(TRIGGER_HAPPY)) return;

        animator.SetTrigger(TRIGGER_HAPPY);
        currentReaction = ReactionType.Happy;
        lastEmotionalReactionTime = Time.time;
        Debug.Log($"[StudentReactionAnimator] {studentAgent.studentName} triggered happy reaction");
    }

    /// <summary>
    /// Trigger sad reaction
    /// </summary>
    public void TriggerSadReaction()
    {
        if (!HasAnimatorParameter(TRIGGER_SAD)) return;

        animator.SetTrigger(TRIGGER_SAD);
        currentReaction = ReactionType.Sad;
        lastEmotionalReactionTime = Time.time;
        Debug.Log($"[StudentReactionAnimator] {studentAgent.studentName} triggered sad reaction");
    }

    /// <summary>
    /// Trigger angry reaction
    /// </summary>
    public void TriggerAngryReaction()
    {
        if (!HasAnimatorParameter(TRIGGER_ANGRY)) return;

        animator.SetTrigger(TRIGGER_ANGRY);
        currentReaction = ReactionType.Angry;
        lastEmotionalReactionTime = Time.time;
        Debug.Log($"[StudentReactionAnimator] {studentAgent.studentName} triggered angry reaction");
    }

    /// <summary>
    /// Trigger frustrated reaction
    /// </summary>
    public void TriggerFrustratedReaction()
    {
        if (!HasAnimatorParameter(TRIGGER_FRUSTRATED)) return;

        animator.SetTrigger(TRIGGER_FRUSTRATED);
        currentReaction = ReactionType.Frustrated;
        lastEmotionalReactionTime = Time.time;
        Debug.Log($"[StudentReactionAnimator] {studentAgent.studentName} triggered frustrated reaction");
    }

    /// <summary>
    /// Trigger bored reaction
    /// </summary>
    public void TriggerBoredReaction()
    {
        if (!HasAnimatorParameter(TRIGGER_BORED)) return;

        animator.SetTrigger(TRIGGER_BORED);
        currentReaction = ReactionType.Bored;
        lastEmotionalReactionTime = Time.time;
        Debug.Log($"[StudentReactionAnimator] {studentAgent.studentName} triggered bored reaction");
    }

    /// <summary>
    /// Trigger celebration reaction (e.g., after correct answer or praise)
    /// </summary>
    public void TriggerCelebrationReaction()
    {
        if (!HasAnimatorParameter(TRIGGER_CELEBRATE)) return;

        animator.SetTrigger(TRIGGER_CELEBRATE);
        currentReaction = ReactionType.Celebrating;
        Debug.Log($"[StudentReactionAnimator] {studentAgent.studentName} triggered celebration reaction");
    }

    /// <summary>
    /// Trigger shocked/surprised reaction
    /// </summary>
    public void TriggerShockedReaction()
    {
        if (!HasAnimatorParameter(TRIGGER_SHOCKED)) return;

        animator.SetTrigger(TRIGGER_SHOCKED);
        currentReaction = ReactionType.Shocked;
        Debug.Log($"[StudentReactionAnimator] {studentAgent.studentName} triggered shocked reaction");
    }

    /// <summary>
    /// Trigger confused reaction
    /// </summary>
    public void TriggerConfusedReaction()
    {
        if (!HasAnimatorParameter(TRIGGER_CONFUSED)) return;

        animator.SetTrigger(TRIGGER_CONFUSED);
        currentReaction = ReactionType.Confused;
        Debug.Log($"[StudentReactionAnimator] {studentAgent.studentName} triggered confused reaction");
    }
    #endregion

    #region Specific Action Animations
    /// <summary>
    /// Update crying state based on sadness level
    /// </summary>
    private void UpdateCryingState()
    {
        bool shouldCry = studentAgent.emotions.Sadness >= 10f;

        if (shouldCry != isCrying)
        {
            isCrying = shouldCry;
            animator.SetBool(PARAM_IS_CRYING, isCrying);

            if (isCrying)
            {
                Debug.Log($"[StudentReactionAnimator] {studentAgent.studentName} started crying");
            }
            else
            {
                Debug.Log($"[StudentReactionAnimator] {studentAgent.studentName} stopped crying");
            }
        }
    }

    /// <summary>
    /// Trigger raise hand animation
    /// </summary>
    public void RaiseHand()
    {
        if (animator == null)
        {
            Debug.LogWarning($"[StudentReactionAnimator] Animator is null for {studentAgent?.studentName ?? "Unknown"}. Cannot trigger raise hand.");
            return;
        }

        // Check if the trigger parameter exists
        bool hasParameter = HasAnimatorParameter(PARAM_RAISE_HAND, AnimatorControllerParameterType.Trigger);
        
        if (hasParameter)
        {
            animator.SetTrigger(PARAM_RAISE_HAND);
            isRaisingHand = true;
            Debug.Log($"[StudentReactionAnimator] {studentAgent?.studentName ?? "Student"} raised hand - trigger set successfully");

            // Reset flag after animation duration
            StartCoroutine(ResetRaiseHandAfterDelay(quickReactionDuration));
        }
        else
        {
            Debug.LogWarning($"[StudentReactionAnimator] WARNING: 'RaiseHand' trigger parameter not found in Animator Controller for {studentAgent?.studentName ?? "Student"}. " +
                           $"Please add a Trigger parameter named 'RaiseHand' to the Animator Controller. " +
                           $"See RAISE_HAND_ANIMATION_SETUP.md for setup instructions.");
        }
    }

    private IEnumerator ResetRaiseHandAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isRaisingHand = false;
    }
    #endregion

    #region Movement Animations
    /// <summary>
    /// Update walking animation state
    /// </summary>
    public void UpdateWalkingAnimation(bool walking, float speed = 1f)
    {
        if (animator == null) return;

        if (isWalking != walking)
        {
            isWalking = walking;
            animator.SetBool(PARAM_IS_WALKING, isWalking);
        }

        if (HasAnimatorParameter(PARAM_WALK_SPEED, AnimatorControllerParameterType.Float))
        {
            animator.SetFloat(PARAM_WALK_SPEED, speed);
        }
    }
    #endregion

    #region Reaction Response to Teacher Actions
    /// <summary>
    /// Trigger appropriate animation reaction based on teacher action
    /// </summary>
    public void ReactToTeacherAction(ActionType actionType)
    {
        switch (actionType)
        {
            case ActionType.Praise:
                TriggerCelebrationReaction();
                TriggerHappyReaction();
                break;

            case ActionType.Yell:
                if (studentAgent.rebelliousness > 0.7f)
                {
                    TriggerAngryReaction();
                }
                else
                {
                    TriggerSadReaction();
                }
                break;

            case ActionType.CallToBoard:
                // Mix of nervousness (sad) and engagement
                if (studentAgent.sensitivity > 0.6f)
                {
                    TriggerShockedReaction();
                }
                break;

            case ActionType.RemoveFromClass:
                TriggerAngryReaction();
                break;

            case ActionType.PositiveReinforcement:
                TriggerHappyReaction();
                break;

            case ActionType.Ignore:
                TriggerFrustratedReaction();
                break;

            case ActionType.GiveBreak:
                TriggerHappyReaction();
                break;
        }
    }

    /// <summary>
    /// React to emotional triggers
    /// </summary>
    public void ReactToEmotionalTrigger(EmotionalTrigger trigger)
    {
        switch (trigger)
        {
            case EmotionalTrigger.IgnoredRaisedHand:
                TriggerFrustratedReaction();
                break;

            case EmotionalTrigger.WrongAnswerPublic:
                TriggerSadReaction();
                break;

            case EmotionalTrigger.PeerPraise:
                TriggerHappyReaction();
                break;

            case EmotionalTrigger.SuccessfulContribution:
                TriggerCelebrationReaction();
                break;

            case EmotionalTrigger.PeerConflict:
                TriggerAngryReaction();
                break;

            case EmotionalTrigger.LongPassiveActivity:
                TriggerBoredReaction();
                break;
        }
    }
    #endregion

    #region Utility Methods
    /// <summary>
    /// Reset all animation states to default
    /// </summary>
    public void ResetAllAnimationStates()
    {
        if (animator == null) return;

        // Reset behavioral states
        animator.SetBool(PARAM_IS_LISTENING, false);
        animator.SetBool(PARAM_IS_ENGAGED, false);
        animator.SetBool(PARAM_IS_DISTRACTED, false);
        animator.SetBool(PARAM_IS_TALKING, false);
        animator.SetBool(PARAM_IS_ARGUING, false);
        animator.SetBool(PARAM_IS_WITHDRAWN, false);

        // Reset reactions
        animator.SetBool(PARAM_IS_CRYING, false);
        animator.SetBool(PARAM_IS_WALKING, false);

        currentAnimationState = StudentState.Listening;
        currentReaction = ReactionType.None;
        isCrying = false;
        isRaisingHand = false;
        isWalking = false;
    }

    /// <summary>
    /// Check if animator has a specific parameter
    /// </summary>
    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType? parameterType = null)
    {
        if (animator == null) return false;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == parameterName)
            {
                if (parameterType.HasValue)
                {
                    return param.type == parameterType.Value;
                }
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Get current animation state info for debugging
    /// </summary>
    public string GetAnimationStateInfo()
    {
        return $"State: {currentAnimationState}, Reaction: {currentReaction}, " +
               $"Crying: {isCrying}, RaisingHand: {isRaisingHand}, Walking: {isWalking}";
    }
    #endregion

    #region Debug Visualization
    void OnDrawGizmosSelected()
    {
        // Draw emotion state above student in editor
        if (studentAgent != null && studentAgent.emotions != null)
        {
            UnityEngine.Vector3 offset = UnityEngine.Vector3.up * 2f;
            UnityEngine.Vector3 textPos = transform.position + offset;

            #if UNITY_EDITOR
            UnityEditor.Handles.Label(textPos, $"{studentAgent.studentName}\n{GetAnimationStateInfo()}");
            #endif
        }
    }
    #endregion
}

#region Supporting Enums
/// <summary>
/// Types of reactions that can be triggered
/// </summary>
public enum ReactionType
{
    None,
    Happy,
    Sad,
    Angry,
    Frustrated,
    Bored,
    Celebrating,
    Shocked,
    Confused
}
#endregion