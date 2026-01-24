using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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
    public Button giveBreakButton;
    public Button removeStudentButton;

    [Header("Student Selection")]
    public TextMeshProUGUI selectedStudentText;
    private StudentAgent selectedStudent;
    [Tooltip("Default text to show when no student is selected")]
    public string defaultStudentText = "לא נבחר תלמיד";

    [Header("Student Info Panel")]
    [Tooltip("Reference to StudentInfoPanelUI component")]
    public StudentInfoPanelUI studentInfoPanel;

    [Header("Break Duration Panel")]
    [Tooltip("Reference to BreakDurationPanelUI component for selecting break duration")]
    public BreakDurationPanelUI breakDurationPanel;

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

        // Set default text for selected student
        if (selectedStudentText != null && string.IsNullOrEmpty(selectedStudentText.text))
            selectedStudentText.text = defaultStudentText;
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

        if (giveBreakButton != null)
            giveBreakButton.onClick.AddListener(OnGiveBreakButtonClicked);

        if (removeStudentButton != null)
            removeStudentButton.onClick.AddListener(() => ExecuteAction(ActionType.RemoveFromClass));
    }

    /// <summary>
    /// Update real-time classroom metrics display
    /// </summary>
    public void UpdateMetrics(float engagement, int disruptions)
    {
        if (engagementText != null)
            engagementText.text = $"מעורבות: {engagement:P0}";

        if (engagementSlider != null)
            engagementSlider.value = engagement;

        if (disruptionText != null)
        {
            disruptionText.text = $"הפרעות: {disruptions}";
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
            sessionTimeText.text = $"{minutes:00}:{seconds:00}";
        }
    }

    /// <summary>
    /// Detect student clicks via raycast
    /// </summary>
    void CheckForStudentSelection()
    {
        // Check for right-click to view student info (works even in change seating mode)
        if (Input.GetMouseButtonDown(1)) // Right mouse button
        {
            bool clickingOnUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (clickingOnUI) return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, 100f))
            {
                StudentAgent student = hit.collider.GetComponentInParent<StudentAgent>();
                if (student != null)
                {
                    // Right-click always shows student info, even in change seating mode
                    SelectStudent(student, forceShowInfo: true);
                    return;
                }
            }
        }

        // Left click handling
        if (Input.GetMouseButtonDown(0))
        {
            // Check if we're clicking on a UI element
            bool clickingOnUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            
            // Always try to raycast for 3D objects (students) first
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, 100f))
            {
                StudentAgent student = hit.collider.GetComponentInParent<StudentAgent>();
                if (student != null)
                {
                    // Always allow clicking on students, regardless of UI
                    SelectStudent(student);
                    return;
                }
            }
            
            // If we didn't hit a student, handle deselection
            // Only deselect if we're NOT clicking on UI
            if (!clickingOnUI)
            {
                // Clicked on empty space or something that's not a student
                DeselectStudent();
            }
        }
    }

    /// <summary>
    /// Select a student for targeted actions
    /// </summary>
    public void SelectStudent(StudentAgent student, bool forceShowInfo = false)
    {

        // Deselect previous student if any
        if (selectedStudent != null && selectedStudent != student)
        {
            var prevFeedback = selectedStudent.GetComponentInChildren<StudentVisualFeedback>();
            if (prevFeedback != null)
                prevFeedback.SetSelected(false);
        }

        selectedStudent = student;

        // Set visual selection on the student
        if (selectedStudent != null)
        {
            var feedback = selectedStudent.GetComponentInChildren<StudentVisualFeedback>();
            if (feedback != null)
                feedback.SetSelected(true);

            // Check if student has an answer ready (eager to respond)
            // If so, trigger the full answer display
            var questionResponder = selectedStudent.GetComponent<StudentQuestionResponder>();
            if (questionResponder != null && questionResponder.HasAnswerReady())
            {
                Debug.Log($"[TeacherUI] {selectedStudent.studentName} is eager to answer! Showing full response...");

                // Trigger the full answer display
                questionResponder.ShowFullAnswer();

                // Optionally show feedback to teacher
                ShowFeedback($"{selectedStudent.studentName} עונה...", Color.cyan);
            }
        }

        if (selectedStudentText != null)
        {
            selectedStudentText.text = $"נבחר: {student.studentName}\n" +
                                      $"מצב: {GetStateHebrew(student.currentState)}\n\n" +
                                      $"רגשות:\n{student.emotions.ToReadableString()}";
        }

        // Show student info panel with detailed vectors
        if (studentInfoPanel != null)
        {
            studentInfoPanel.ShowStudentInfo(student);
        }
        else
        {
            Debug.LogWarning("TeacherUI: studentInfoPanel is not assigned! Cannot show student info.");
        }

        // Show action menu
        if (actionMenu != null)
            actionMenu.SetActive(true);

        Debug.Log($"Selected student: {student.studentName}");
    }

    /// <summary>
    /// Deselect the currently selected student and reset UI
    /// </summary>
    public void DeselectStudent()
    {
        // Remove visual selection from student
        if (selectedStudent != null)
        {
            var feedback = selectedStudent.GetComponentInChildren<StudentVisualFeedback>();
            if (feedback != null)
                feedback.SetSelected(false);
        }

        selectedStudent = null;

        if (selectedStudentText != null)
        {
            selectedStudentText.text = defaultStudentText;
        }

        // Hide student info panel
        if (studentInfoPanel != null)
        {
            studentInfoPanel.ClosePanel();
        }

        // Hide action menu
        if (actionMenu != null)
            actionMenu.SetActive(false);

        Debug.Log("Student deselected");
    }

    /// <summary>
    /// Get the currently selected student
    /// </summary>
    public StudentAgent GetSelectedStudent()
    {
        return selectedStudent;
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
    /// Handle give break button click - show duration selection panel
    /// </summary>
    void OnGiveBreakButtonClicked()
    {
        if (selectedStudent == null)
        {
            ShowFeedback("אנא בחר תלמיד תחילה!", Color.yellow);
            return;
        }

        // Check if student is already on break
        if (selectedStudent.IsOnBreak())
        {
            ShowFeedback($"{selectedStudent.studentName} כבר בהפסקה", Color.yellow);
            return;
        }

        // Show break duration selection panel
        if (breakDurationPanel != null)
        {
            breakDurationPanel.ShowPanel(selectedStudent, OnBreakDurationConfirmed);
        }
        else
        {
            Debug.LogWarning("TeacherUI: breakDurationPanel is not assigned!");
            // Fallback: give break with default duration (5 minutes)
            GiveStudentBreak(selectedStudent, 5f);
        }
    }

    /// <summary>
    /// Called when break duration is confirmed from the panel
    /// </summary>
    void OnBreakDurationConfirmed(float durationMinutes)
    {
        if (selectedStudent != null)
        {
            GiveStudentBreak(selectedStudent, durationMinutes);
        }
    }

    /// <summary>
    /// Give a student a break for specified duration
    /// </summary>
    void GiveStudentBreak(StudentAgent student, float durationMinutes)
    {
        if (student == null || classroomManager == null)
            return;

        // Use ClassroomManager to handle the break
        classroomManager.GiveStudentBreak(student, durationMinutes);

        // Show feedback
        int minutes = Mathf.FloorToInt(durationMinutes);
        int seconds = Mathf.RoundToInt((durationMinutes - minutes) * 60f);
        string durationText = minutes > 0 ? $"{minutes} דקות" : $"{seconds} שניות";
        ShowFeedback($"{student.studentName} יוצא להפסקה של {durationText}", Color.green);

        // Deselect student after giving break
        DeselectStudent();
    }

    /// <summary>
    /// Execute action on entire class
    /// </summary>
    void ExecuteClasswideAction(ActionType actionType)
    {
        classroomManager.ExecuteClasswideAction(actionType, "התערבות כיתתית");
        ShowFeedback($"הוחל {GetActionTypeHebrew(actionType)} על כל הכיתה", Color.blue);
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
        string message = $"{GetActionTypeHebrew(action.Type)} → {target.studentName}\n";

        switch (action.Type)
        {
            case ActionType.Praise:
                message += target.emotions.Happiness > 7f
                    ? "✓ התלמיד מונע!"
                    : "✓ חיזוק חיובי הוחל";
                break;

            case ActionType.Yell:
                message += target.rebelliousness > 0.7f
                    ? "⚠ עלול לעורר עימות"
                    : "התלמיד הושתק, אך המורל נפגע";
                break;

            case ActionType.CallToBoard:
                message += target.emotions.Sadness > 6f
                    ? "⚠ התלמיד נראה חרד"
                    : "✓ מערב את התלמיד באופן פעיל";
                break;

            case ActionType.GiveBreak:
                message += "✓ אנרגיית הכיתה אופסה";
                break;

            case ActionType.RemoveFromClass:
                message += "⚠ צעד קיצוני - התלמיד הוסר";
                break;

            default:
                message += "פעולה בוצעה";
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
    public void ShowFeedback(string message, Color color)
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
        string summary = $"השלמת שיעור\n\n" +
                        $"ציון: {report.score:F1}/100\n" +
                        $"משך זמן: {report.sessionData.duration:F1} שניות\n" +
                        $"סך פעולות: {report.totalActions}\n" +
                        $"חיוביות: {report.positiveActions} | שליליות: {report.negativeActions}\n" +
                        $"מעורבות ממוצעת: {report.averageEngagement:P0}\n" +
                        $"הפרעות: {report.totalDisruptions}\n\n" +
                        GetPerformanceGrade(report.score);

        ShowFeedback(summary, Color.cyan);
    }

    string GetPerformanceGrade(float score)
    {
        if (score >= 90) return "ציון: מצוין - ניהול מעולה!";
        if (score >= 80) return "ציון: טוב מאוד - עבודה טובה!";
        if (score >= 70) return "ציון: טוב - מספק";
        if (score >= 60) return "ציון: מספיק - צריך שיפור";
        return "ציון: נכשל - בדוק אסטרטגיות";
    }

    /// <summary>
    /// Get Hebrew name for student state
    /// </summary>
    string GetStateHebrew(StudentState state)
    {
        switch (state)
        {
            case StudentState.Listening: return "מאזין";
            case StudentState.Engaged: return "מעורב";
            case StudentState.Distracted: return "מוסח";
            case StudentState.SideTalk: return "שיחה צדדית";
            case StudentState.Arguing: return "מתווכח";
            case StudentState.Withdrawn: return "מסוגר";
            default: return state.ToString();
        }
    }

    /// <summary>
    /// Get Hebrew name for action type
    /// </summary>
    string GetActionTypeHebrew(ActionType actionType)
    {
        switch (actionType)
        {
            case ActionType.Praise: return "שבח";
            case ActionType.Yell: return "צעקה";
            case ActionType.CallToBoard: return "קריאה ללוח";
            case ActionType.ChangeSeating: return "שינוי ישיבה";
            case ActionType.GiveBreak: return "הפסקה";
            case ActionType.RemoveFromClass: return "הסרה מהכיתה";
            case ActionType.PositiveReinforcement: return "חיזוק חיובי";
            case ActionType.Ignore: return "התעלמות";
            default: return actionType.ToString();
        }
    }
}