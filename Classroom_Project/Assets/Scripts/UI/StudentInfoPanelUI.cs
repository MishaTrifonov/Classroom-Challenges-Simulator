using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Enhanced UI component that displays detailed student information.
/// Shows student data with visual emotion indicators and color coding.
/// </summary>
public class StudentInfoPanelUI : MonoBehaviour
{
    [Header("Student Info Display")]
    [Tooltip("Text component to display detailed emotion information")]
    public TextMeshProUGUI emotionDetailsText;

    [Tooltip("Optional: Individual emotion bar displays")]
    public Slider happinessBar;
    public Slider sadnessBar;
    public Slider frustrationBar;
    public Slider boredomBar;
    public Slider angerBar;

    [Header("Emotion Labels")]
    public TextMeshProUGUI happinessLabel;
    public TextMeshProUGUI sadnessLabel;
    public TextMeshProUGUI frustrationLabel;
    public TextMeshProUGUI boredomLabel;
    public TextMeshProUGUI angerLabel;

    [Header("Update Settings")]
    [Tooltip("How often to update the display (in seconds)")]
    public float updateInterval = 0.5f;
    private float lastUpdateTime;

    private StudentAgent currentStudent;
    
    // Track previous emotion values to detect changes
    private EmotionVector previousEmotions;

    void Update()
    {
        if (currentStudent != null)
        {
            // Check if emotions have changed
            bool emotionsChanged = HasEmotionsChanged();
            
            // Update display if emotions changed or if update interval elapsed
            if (emotionsChanged || Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateDisplay();
                lastUpdateTime = Time.time;
                
                // Update previous emotions after refresh
                if (currentStudent.emotions != null)
                {
                    previousEmotions = new EmotionVector
                    {
                        Happiness = currentStudent.emotions.Happiness,
                        Sadness = currentStudent.emotions.Sadness,
                        Frustration = currentStudent.emotions.Frustration,
                        Boredom = currentStudent.emotions.Boredom,
                        Anger = currentStudent.emotions.Anger
                    };
                }
            }
        }
    }
    
    /// <summary>
    /// Check if the current student's emotions have changed since last update
    /// </summary>
    private bool HasEmotionsChanged()
    {
        if (currentStudent == null || currentStudent.emotions == null)
            return false;
            
        // If we don't have previous emotions, consider it changed (first time)
        if (previousEmotions == null)
            return true;
            
        EmotionVector current = currentStudent.emotions;
        
        // Check if any emotion value has changed significantly (more than 0.01 to avoid floating point noise)
        float threshold = 0.01f;
        return Mathf.Abs(current.Happiness - previousEmotions.Happiness) > threshold ||
               Mathf.Abs(current.Sadness - previousEmotions.Sadness) > threshold ||
               Mathf.Abs(current.Frustration - previousEmotions.Frustration) > threshold ||
               Mathf.Abs(current.Boredom - previousEmotions.Boredom) > threshold ||
               Mathf.Abs(current.Anger - previousEmotions.Anger) > threshold;
    }

    /// <summary>
    /// Show student info with enhanced visual display
    /// </summary>
    public void ShowStudentInfo(StudentAgent student)
    {
        if (student == null)
        {
            Debug.LogWarning("StudentInfoPanelUI: Cannot show info for null student");
            return;
        }

        currentStudent = student;
        
        // Initialize previous emotions to current emotions
        if (student.emotions != null)
        {
            previousEmotions = new EmotionVector
            {
                Happiness = student.emotions.Happiness,
                Sadness = student.emotions.Sadness,
                Frustration = student.emotions.Frustration,
                Boredom = student.emotions.Boredom,
                Anger = student.emotions.Anger
            };
        }
        
        UpdateDisplay();
    }

    /// <summary>
    /// Update the visual display of student emotions
    /// </summary>
    private void UpdateDisplay()
    {
        if (currentStudent == null) return;

        EmotionVector emotions = currentStudent.emotions;

        // Update text display with readable format
        if (emotionDetailsText != null)
        {
            emotionDetailsText.text = emotions.ToReadableString();
        }

        // Update emotion bars if available
        UpdateEmotionBar(happinessBar, happinessLabel, emotions.Happiness, "שמחה", emotions);
        UpdateEmotionBar(sadnessBar, sadnessLabel, emotions.Sadness, "עצב", emotions);
        UpdateEmotionBar(frustrationBar, frustrationLabel, emotions.Frustration, "תסכול", emotions);
        UpdateEmotionBar(boredomBar, boredomLabel, emotions.Boredom, "שעמום", emotions);
        UpdateEmotionBar(angerBar, angerLabel, emotions.Anger, "כעס", emotions);
    }

    /// <summary>
    /// Update an individual emotion bar with value and color
    /// </summary>
    private void UpdateEmotionBar(Slider bar, TextMeshProUGUI label, float value, string emotionName, EmotionVector emotions)
    {
        if (bar != null)
        {
            bar.maxValue = 10f;
            bar.minValue = 1f;
            bar.value = value;

            // Apply color coding to the bar fill
            Image fillImage = bar.fillRect?.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = emotions.GetEmotionColor(value);
            }
        }

        if (label != null)
        {
            string levelDescription = emotions.GetEmotionLevelDescription(value);
            // Use Left-to-Right Override (LRO) to force LTR direction for the entire number expression
            // This ensures "value/10" displays correctly even in RTL context
            string numberExpression = "\u202D" + value.ToString("F1") + "/10\u202C";
            label.text = $"{emotionName}: {numberExpression} ({levelDescription})";
            label.color = emotions.GetEmotionColor(value);
        }
    }

    /// <summary>
    /// Close panel and clear display
    /// </summary>
    public void ClosePanel()
    {
        currentStudent = null;
        previousEmotions = null;

        if (emotionDetailsText != null)
            emotionDetailsText.text = "";
    }

    /// <summary>
    /// Get the currently selected student
    /// </summary>
    public StudentAgent GetCurrentStudent()
    {
        return currentStudent;
    }

    /// <summary>
    /// Check if showing a student
    /// </summary>
    public bool IsShowingStudent()
    {
        return currentStudent != null;
    }
}
