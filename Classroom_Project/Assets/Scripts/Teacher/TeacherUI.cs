using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manages the teacher's user interface for interacting with the simulation.
/// Displays metrics, action buttons, and feedback.
/// </summary>
public class TeacherUI : MonoBehaviour
{
    [Header("References")]
    public ClassroomManager classroomManager;

    [Header("Metrics Display")]
    public TextMeshProUGUI engagementText;
    public TextMeshProUGUI disruptionText;
    public TextMeshProUGUI sessionTimeText;
    public Slider engagementSlider;

    [Header("Action Buttons")]
    public Button praiseButton;
    public Button yellButton;
    public Button callToBoardButton;
    public Button changeSeatingButton;
    public Button giveBreakButton;
    public Button removeStudentButton;

    [Header("Student Selection")]
    public TextMeshProUGUI selectedStudentText;
    private StudentAgent selectedStudent;

    [Header("Feedback Panel")]
    public GameObject feedbackPanel;
    public TextMeshProUGUI feedbackText;
    public Image feedbackIcon;
    public float feedbackDuration = 3f;

    [Header("Action Menu")]
    public GameObject actionMenu;
    public Transform actionMenuContainer;

    [Header("Session Info")]
    private float sessionStartTime;

    


    void Start()
    {
        sessionStartTime = Time.time;

        // Wire up button listeners
        SetupButtons();
        
        // Hide feedback panel initially
        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);

        if (actionMenu != null)
            actionMenu.SetActive(false);

        
    }

    void Update()
    {
        UpdateSessionTime();
        CheckForStudentSelection();
    }

    /// <summary>
    /// Wire up all action buttons to their respective methods
    /// </summary>
    void SetupButtons()
    {
        if (praiseButton != null)
            praiseButton.onClick.AddListener(() => ExecuteAction(ActionType.Praise));

        if (yellButton != null)
            yellButton.onClick.AddListener(() => ExecuteAction(ActionType.Yell));

        if (callToBoardButton != null)
            callToBoardButton.onClick.AddListener(() => ExecuteAction(ActionType.CallToBoard));

        if (changeSeatingButton != null)
            changeSeatingButton.onClick.AddListener(() => ExecuteAction(ActionType.ChangeSeating));

        if (giveBreakButton != null)
            giveBreakButton.onClick.AddListener(() => ExecuteClasswideAction(ActionType.GiveBreak));

        if (removeStudentButton != null)
            removeStudentButton.onClick.AddListener(() => ExecuteAction(ActionType.RemoveFromClass));
    }

    /// <summary>
    /// Update real-time classroom metrics display
    /// </summary>
    public void UpdateMetrics(float engagement, int disruptions)
    {
        if (engagementText != null)
            engagementText.text = $"Engagement: {engagement:P0}";

        if (engagementSlider != null)
            engagementSlider.value = engagement;

        if (disruptionText != null)
        {
            disruptionText.text = $"Disruptions: {disruptions}";
            disruptionText.color = disruptions > 5 ? Color.red : Color.white;
        }
    }

    /// <summary>
    /// Update session timer
    /// </summary>
    void UpdateSessionTime()
    {
        if (sessionTimeText != null)
        {
            float elapsed = Time.time - sessionStartTime;
            int minutes = Mathf.FloorToInt(elapsed / 60f);
            int seconds = Mathf.FloorToInt(elapsed % 60f);
            sessionTimeText.text = $"Time: {minutes:00}:{seconds:00}";
        }
    }

    /// <summary>
    /// Detect student clicks via raycast
    /// </summary>
    void CheckForStudentSelection()
{
    if (Input.GetMouseButtonDown(0))
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            StudentAgent student = hit.collider.GetComponentInParent<StudentAgent>();
            if (student != null)
                SelectStudent(student);
        }
    }
}

    /// <summary>
    /// Select a student for targeted actions
    /// </summary>
    public void SelectStudent(StudentAgent student)
    {
        selectedStudent = student;

        if (selectedStudentText != null)
        {
            selectedStudentText.text = $"Selected: {student.studentName}\n" +
                                      $"State: {student.currentState}\n" +
                                      $"Emotions: {student.emotions}";
        }

        // Show action menu
        if (actionMenu != null)
            actionMenu.SetActive(true);

        Debug.Log($"Selected student: {student.studentName}");
    }

    /// <summary>
    /// Execute a teacher action on selected student
    /// </summary>
    void ExecuteAction(ActionType actionType)
    {
        if (selectedStudent == null)
        {
            ShowFeedback("Please select a student first!", Color.yellow);
            return;
        }

        TeacherAction action = new TeacherAction
        {
            Type = actionType,
            TargetStudentId = selectedStudent.studentId,
            Context = $"Targeted action on {selectedStudent.studentName}"
        };

        classroomManager.ExecuteTeacherAction(action);
    }

    /// <summary>
    /// Execute action on entire class
    /// </summary>
    void ExecuteClasswideAction(ActionType actionType)
    {
        classroomManager.ExecuteClasswideAction(actionType, "Classwide intervention");
        ShowFeedback($"Applied {actionType} to entire class", Color.blue);
    }

    /// <summary>
    /// Display feedback for teacher action with predicted outcome
    /// </summary>
    public void ShowActionFeedback(TeacherAction action, StudentAgent target)
    {
        string feedbackMessage = GenerateFeedbackMessage(action, target);
        Color feedbackColor = GetFeedbackColor(action);

        ShowFeedback(feedbackMessage, feedbackColor);
    }

    /// <summary>
    /// Generate contextual feedback based on action and student state
    /// </summary>
    string GenerateFeedbackMessage(TeacherAction action, StudentAgent target)
    {
        string message = $"{action.Type} → {target.studentName}\n";

        switch (action.Type)
        {
            case ActionType.Praise:
                message += target.emotions.Happiness > 7f
                    ? "✓ Student is motivated!"
                    : "✓ Positive reinforcement applied";
                break;

            case ActionType.Yell:
                message += target.rebelliousness > 0.7f
                    ? "⚠ May trigger confrontation"
                    : "Student quieted, but morale affected";
                break;

            case ActionType.CallToBoard:
                message += target.emotions.Sadness > 6f
                    ? "⚠ Student seems anxious"
                    : "✓ Engaging student actively";
                break;

            case ActionType.GiveBreak:
                message += "✓ Class energy reset";
                break;

            case ActionType.RemoveFromClass:
                message += "⚠ Extreme measure - student removed";
                break;

            default:
                message += "Action executed";
                break;
        }

        return message;
    }

    /// <summary>
    /// Determine feedback color based on action effectiveness
    /// </summary>
    Color GetFeedbackColor(TeacherAction action)
    {
        switch (action.Type)
        {
            case ActionType.Praise:
            case ActionType.PositiveReinforcement:
            case ActionType.GiveBreak:
                return Color.green;

            case ActionType.Yell:
            case ActionType.RemoveFromClass:
                return Color.red;

            case ActionType.CallToBoard:
            case ActionType.ChangeSeating:
                return Color.yellow;

            default:
                return Color.white;
        }
    }

    /// <summary>
    /// Display temporary feedback message
    /// </summary>
    void ShowFeedback(string message, Color color)
    {
        if (feedbackPanel == null || feedbackText == null) return;

        feedbackPanel.SetActive(true);
        feedbackText.text = message;
        feedbackText.color = color;

        // Auto-hide after duration
        CancelInvoke(nameof(HideFeedback));
        Invoke(nameof(HideFeedback), feedbackDuration);
    }

    void HideFeedback()
    {
        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);
    }

    /// <summary>
    /// End session and show summary
    /// </summary>
    public void EndSession()
    {
        SessionReport report = classroomManager.EndSession();
        ShowSessionSummary(report);
    }

    /// <summary>
    /// Display session performance summary
    /// </summary>
    void ShowSessionSummary(SessionReport report)
    {
        string summary = $"SESSION COMPLETE\n\n" +
                        $"Score: {report.score:F1}/100\n" +
                        $"Duration: {report.sessionData.duration:F1}s\n" +
                        $"Total Actions: {report.totalActions}\n" +
                        $"Positive: {report.positiveActions} | Negative: {report.negativeActions}\n" +
                        $"Avg Engagement: {report.averageEngagement:P0}\n" +
                        $"Disruptions: {report.totalDisruptions}\n\n" +
                        GetPerformanceGrade(report.score);

        ShowFeedback(summary, Color.cyan);
    }

    string GetPerformanceGrade(float score)
    {
        if (score >= 90) return "Grade: A - Excellent Management!";
        if (score >= 80) return "Grade: B - Good Job!";
        if (score >= 70) return "Grade: C - Satisfactory";
        if (score >= 60) return "Grade: D - Needs Improvement";
        return "Grade: F - Review Strategies";
    }
}