using UnityEngine;
using System.Collections;

/// <summary>
/// Handles student reactions when the teacher asks a question.
/// Implements progressive interest system based on personality traits and emotional state.
/// Students show varying levels of eagerness to answer questions.
/// </summary>
public class StudentQuestionResponder : MonoBehaviour
{
    [Header("References")]
    public StudentAgent studentAgent;
    public StudentResponseBubble responseBubble;
    public StudentReactionAnimator reactionAnimator;

    [Header("Response Settings")]
    [Tooltip("Base threshold for responding to questions (0-1). Lower = more likely to respond")]
    [Range(0f, 1f)]
    public float baseResponseThreshold = 0.5f;

    [Tooltip("How long to wait before showing preview text after raising hand")]
    public float previewDelay = 1f;

    [Tooltip("How long to keep the eager bubble visible")]
    public float eagerBubbleDuration = 5f;

    [Header("Eagerness Display")]
    [Tooltip("Should this student show eagerness when questions are asked?")]
    public bool showEagerness = true;

    private bool isShowingEagerness = false;
    private bool hasAnswerReady = false;
    private Coroutine eagerCoroutine;
    private string currentQuestion = ""; // Store the question student is eager to answer

    void Awake()
    {
        // Initialize references immediately when component is added
        // This ensures references are set even if component is added dynamically at runtime
        InitializeReferences();
    }

    void Start()
    {
        // Ensure references are initialized (in case Awake wasn't called)
        InitializeReferences();
    }

    /// <summary>
    /// Initialize component references. Safe to call multiple times.
    /// </summary>
    private void InitializeReferences()
    {
        // Auto-find references if not assigned
        if (studentAgent == null)
            studentAgent = GetComponent<StudentAgent>();

        if (responseBubble == null)
        {
            responseBubble = GetComponentInChildren<StudentResponseBubble>();

            // If not found as child, check on same GameObject
            if (responseBubble == null)
                responseBubble = GetComponent<StudentResponseBubble>();
        }

        if (reactionAnimator == null)
            reactionAnimator = GetComponent<StudentReactionAnimator>();
    }

    /// <summary>
    /// Called when teacher asks a question. Determines if this student wants to answer.
    /// </summary>
    public void OnQuestionAsked(string question)
    {
        // Ensure references are initialized (critical for dynamically added components)
        InitializeReferences();

        if (studentAgent == null || !showEagerness)
            return;

        // Store the question for later use
        currentQuestion = question;

        // Calculate willingness to answer based on personality and state
        float willingnessScore = CalculateWillingnessToAnswer();

        // If student is willing to answer, show eagerness
        if (willingnessScore > baseResponseThreshold)
        {
            ShowEagerness(question, willingnessScore);
        }
    }

    /// <summary>
    /// Calculate how willing the student is to answer (0-1 scale)
    /// </summary>
    private float CalculateWillingnessToAnswer()
    {
        if (studentAgent == null)
            return 0f;

        float score = 0f;

        // Academic motivation is the strongest factor (40%)
        score += studentAgent.academicMotivation * 0.4f;

        // Extroversion contributes (30%)
        score += studentAgent.extroversion * 0.3f;

        // Happiness boosts willingness (20%)
        score += (studentAgent.emotions.Happiness / 10f) * 0.2f;

        // Boredom reduces willingness (10% penalty)
        score -= (studentAgent.emotions.Boredom / 10f) * 0.1f;

        // Current state affects willingness
        switch (studentAgent.currentState)
        {
            case StudentState.Engaged:
                score += 0.2f; // Boost for engaged students
                break;
            case StudentState.Listening:
                score += 0.1f; // Small boost for attentive students
                break;
            case StudentState.Distracted:
            case StudentState.SideTalk:
                score -= 0.3f; // Penalty for distracted students
                break;
            case StudentState.Arguing:
            case StudentState.Withdrawn:
                score -= 0.5f; // Large penalty for negative states
                break;
        }

        // Clamp between 0 and 1
        return Mathf.Clamp01(score);
    }

    /// <summary>
    /// Show eagerness based on personality and willingness score
    /// </summary>
    private void ShowEagerness(string question, float willingnessScore)
    {
        if (isShowingEagerness)
            return; // Already showing eagerness

        // Stop any existing eager coroutine
        if (eagerCoroutine != null)
            StopCoroutine(eagerCoroutine);

        // Start new eagerness display
        eagerCoroutine = StartCoroutine(DisplayEagernessCoroutine(question, willingnessScore));
    }

    /// <summary>
    /// Coroutine to display eagerness with animations and bubbles
    /// </summary>
    private IEnumerator DisplayEagernessCoroutine(string question, float willingnessScore)
    {
        isShowingEagerness = true;
        hasAnswerReady = true;

        // Trigger raise hand animation
        if (reactionAnimator != null)
        {
            reactionAnimator.RaiseHand();
        }

        // Wait a moment before showing preview text (so animation plays first)
        yield return new WaitForSeconds(previewDelay);

        // Show preview text based on personality
        string previewText = GetEagerPreviewText(willingnessScore);

        if (responseBubble != null)
        {
            // Show in "eager" mode (small bubble)
            responseBubble.ShowEagerBubble(previewText);
            Debug.Log($"[StudentQuestionResponder] {studentAgent?.studentName ?? "Student"} showing eager bubble: '{previewText}'");
        }
        else
        {
            Debug.LogWarning($"[StudentQuestionResponder] {studentAgent?.studentName ?? "Student"} cannot show eager bubble - StudentResponseBubble component is missing!");
        }

        // Keep eager bubble visible for duration
        yield return new WaitForSeconds(eagerBubbleDuration);

        // Hide bubble if still showing preview
        if (responseBubble != null && responseBubble.IsShowing() && !responseBubble.IsAnswerMode())
        {
            responseBubble.HideBubble();
        }

        isShowingEagerness = false;
        hasAnswerReady = false;
    }

    /// <summary>
    /// Generate preview text based on student personality and willingness
    /// </summary>
    private string GetEagerPreviewText(float willingnessScore)
    {
        if (studentAgent == null)
            return "!";

        // High willingness (very eager)
        if (willingnessScore > 0.8f)
        {
            if (studentAgent.extroversion > 0.7f)
            {
                // Extroverted and very motivated
                string[] phrases = { "אני יודע!", "אני אני!", "יש לי תשובה!", "אני!", "בחר בי!" };
                return phrases[Random.Range(0, phrases.Length)];
            }
            else
            {
                // Introverted but motivated
                string[] phrases = { "אני יודע...", "יש לי תשובה", "אולי אני...", "אני חושב..." };
                return phrases[Random.Range(0, phrases.Length)];
            }
        }
        // Medium willingness (moderately eager)
        else if (willingnessScore > 0.6f)
        {
            if (studentAgent.extroversion > 0.5f)
            {
                string[] phrases = { "אולי יש לי...", "חושב שאני יודע", "יש לי רעיון", "אפשר לנסות" };
                return phrases[Random.Range(0, phrases.Length)];
            }
            else
            {
                string[] phrases = { "אממ...", "אולי...", "לא בטוח...", "נראה לי..." };
                return phrases[Random.Range(0, phrases.Length)];
            }
        }
        // Low willingness (hesitant)
        else
        {
            if (studentAgent.emotions.Boredom > 7f)
            {
                string[] phrases = { "...", "מה?", "לא יודע", "אין לי מושג" };
                return phrases[Random.Range(0, phrases.Length)];
            }
            else
            {
                string[] phrases = { "...", "כן?", "אני?", "מה?" };
                return phrases[Random.Range(0, phrases.Length)];
            }
        }
    }

    /// <summary>
    /// Show full answer (called when teacher clicks on student)
    /// </summary>
    public void ShowFullAnswer(string question = null)
    {
        if (studentAgent == null)
            return;

        // Cancel eager display if active
        if (eagerCoroutine != null)
        {
            StopCoroutine(eagerCoroutine);
            isShowingEagerness = false;
        }

        // Use provided question, or fall back to current question
        string questionToAnswer = question ?? currentQuestion;

        if (string.IsNullOrEmpty(questionToAnswer))
        {
            Debug.LogWarning($"[StudentQuestionResponder] No question to answer for {studentAgent.studentName}");
            return;
        }

        // Trigger student to generate and show full response
        studentAgent.RespondToQuestion(questionToAnswer);

        hasAnswerReady = false;
        currentQuestion = ""; // Clear after answering
    }

    /// <summary>
    /// Check if student currently has an answer ready
    /// </summary>
    public bool HasAnswerReady()
    {
        return hasAnswerReady;
    }

    /// <summary>
    /// Get the current question this student is eager to answer
    /// </summary>
    public string GetCurrentQuestion()
    {
        return currentQuestion;
    }

    /// <summary>
    /// Reset eagerness state (useful for new questions)
    /// </summary>
    public void ResetEagerness()
    {
        if (eagerCoroutine != null)
        {
            StopCoroutine(eagerCoroutine);
            eagerCoroutine = null;
        }

        isShowingEagerness = false;
        hasAnswerReady = false;
        currentQuestion = "";

        if (responseBubble != null && !responseBubble.IsAnswerMode())
        {
            responseBubble.HideBubble();
        }
    }
}
