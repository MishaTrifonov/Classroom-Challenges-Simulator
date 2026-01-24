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

    void Update()
    {
        // Update display periodically if we have a current student
        if (currentStudent != null && Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateDisplay();
            lastUpdateTime = Time.time;
        }
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
            label.text = $"{emotionName}: {value:F1}/10 ({levelDescription})";
            label.color = emotions.GetEmotionColor(value);
        }
    }

    /// <summary>
    /// Close panel and clear display
    /// </summary>
    public void ClosePanel()
    {
        currentStudent = null;

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
