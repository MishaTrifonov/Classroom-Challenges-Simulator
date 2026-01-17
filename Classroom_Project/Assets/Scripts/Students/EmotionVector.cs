using UnityEngine;
using System;

/// <summary>
/// Represents a student's emotional state with 5 core parameters.
/// Values range from 1 (minimal) to 10 (maximal).
/// Updates continuously based on classroom events and time decay.
/// </summary>
[Serializable]

public class EmotionVector
{
    [Header("Emotional Parameters (1-10)")]
    [Range(1f, 10f)] public float Happiness = 5f;
    [Range(1f, 10f)] public float Sadness = 1f;
    [Range(1f, 10f)] public float Frustration = 1f;
    [Range(1f, 10f)] public float Boredom = 1f;
    [Range(1f, 10f)] public float Anger = 1f;

    [Header("Decay Rates (per second)")]
    public float happinessDecay = 0.01f;
    public float sadnessDecay = 0.015f;
    public float frustrationDecay = 0.02f;
    public float boredomGrowth = 0.02f;
    public float angerDecay = 0.025f;

    /// <summary>
    /// Apply natural emotional decay over time.
    /// Called every frame or on fixed update.
    /// </summary>
    public void Decay(float deltaTime)
    {
        // Happiness gradually decreases without positive reinforcement
        Happiness = Mathf.Max(1f, Happiness - happinessDecay * deltaTime);

        // Sadness gradually fades
        Sadness = Mathf.Max(1f, Sadness - sadnessDecay * deltaTime);

        // Frustration decreases when not actively triggered
        Frustration = Mathf.Max(1f, Frustration - frustrationDecay * deltaTime);

        // Boredom increases over time without engaging activity
        Boredom = Mathf.Min(10f, Boredom + boredomGrowth * deltaTime);

        // Anger subsides naturally
        Anger = Mathf.Max(1f, Anger - angerDecay * deltaTime);
    }

    /// <summary>
    /// Apply the effects of a teacher action on this emotion vector.
    /// </summary>
    public void ApplyTeacherAction(TeacherAction action, float intensity = 1f)
    {
        switch (action.Type)
        {
            case ActionType.Yell:
                Anger += 2f * intensity;
                Sadness += 1f * intensity;
                Frustration += 0.5f * intensity;
                break;

            case ActionType.Praise:
                Happiness += 2f * intensity;
                Sadness = Mathf.Max(1f, Sadness - 1f * intensity);
                Boredom = Mathf.Max(1f, Boredom - 0.5f * intensity);
                break;

            case ActionType.CallToBoard:
                // Can increase anxiety/nervousness (modeled as sadness)
                Sadness += 1.5f * intensity;
                Boredom = Mathf.Max(1f, Boredom - 2f * intensity);
                break;

            case ActionType.ChangeSeating:
                Frustration += 0.5f * intensity;
                Boredom = Mathf.Max(1f, Boredom - 1f * intensity);
                break;

            case ActionType.RemoveFromClass:
                Anger += 3f * intensity;
                Sadness += 2f * intensity;
                Frustration += 2f * intensity;
                break;

            case ActionType.PositiveReinforcement:
                Happiness += 1.5f * intensity;
                Frustration = Mathf.Max(1f, Frustration - 1f * intensity);
                break;

            case ActionType.Ignore:
                Frustration += 1f * intensity;
                Sadness += 0.5f * intensity;
                break;

            case ActionType.GiveBreak:
                Boredom = Mathf.Max(1f, Boredom - 3f * intensity);
                Frustration = Mathf.Max(1f, Frustration - 2f * intensity);
                break;
        }

        ClampValues();
    }

    /// <summary>
    /// Apply situational triggers (e.g., being ignored, peer conflict)
    /// </summary>
    public void ApplyTrigger(EmotionalTrigger trigger)
    {
        switch (trigger)
        {
            case EmotionalTrigger.IgnoredRaisedHand:
                Frustration += 1.5f;
                Sadness += 0.5f;
                break;

            case EmotionalTrigger.WrongAnswerPublic:
                Sadness += 2f;
                Anger += 0.5f;
                break;

            case EmotionalTrigger.PeerPraise:
                Happiness += 1f;
                break;

            case EmotionalTrigger.LongPassiveActivity:
                Boredom += 2f;
                break;

            case EmotionalTrigger.SuccessfulContribution:
                Happiness += 2f;
                Frustration = Mathf.Max(1f, Frustration - 1f);
                Sadness = Mathf.Max(1f, Sadness - 1f);
                break;

            case EmotionalTrigger.PeerConflict:
                Anger += 2f;
                Frustration += 1f;
                break;
        }

        ClampValues();
    }

    /// <summary>
    /// Ensure all values stay within valid range [1, 10]
    /// </summary>
    private void ClampValues()
    {
        Happiness = Mathf.Clamp(Happiness, 1f, 10f);
        Sadness = Mathf.Clamp(Sadness, 1f, 10f);
        Frustration = Mathf.Clamp(Frustration, 1f, 10f);
        Boredom = Mathf.Clamp(Boredom, 1f, 10f);
        Anger = Mathf.Clamp(Anger, 1f, 10f);
    }

    /// <summary>
    /// Calculate overall emotional state score (higher = more positive)
    /// </summary>
    public float GetOverallMood()
    {
        return Happiness - (Sadness + Frustration + Boredom + Anger) / 4f;
    }

    /// <summary>
    /// Check if student is in a critical emotional state
    /// </summary>
    public bool IsCriticalState()
    {
        return Anger >= 8f || Sadness >= 8f || Frustration >= 8f;
    }

    public override string ToString()
    {
        return $"H:{Happiness:F1} S:{Sadness:F1} F:{Frustration:F1} B:{Boredom:F1} A:{Anger:F1}";
    }
}

/// <summary>
/// Types of teacher interventions available
/// </summary>
public enum ActionType
{
    Yell,
    Praise,
    CallToBoard,
    ChangeSeating,
    RemoveFromClass,
    PositiveReinforcement,
    Ignore,
    GiveBreak
}

/// <summary>
/// Represents a teacher's action with metadata
/// </summary>
[Serializable]
public class TeacherAction
{
    public ActionType Type;
    public string TargetStudentId;
    public float Timestamp;
    public string Context;
}

/// <summary>
/// Environmental/situational triggers that affect emotions
/// </summary>
public enum EmotionalTrigger
{
    IgnoredRaisedHand,
    WrongAnswerPublic,
    PeerPraise,
    LongPassiveActivity,
    SuccessfulContribution,
    PeerConflict
}