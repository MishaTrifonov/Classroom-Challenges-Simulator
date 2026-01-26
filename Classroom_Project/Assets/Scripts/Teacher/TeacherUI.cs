using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
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
    public Button switchPlacesButton;
    public Button endSessionButton;

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

    [Header("Seat Swap Panel")]
    [Tooltip("Reference to SeatSwapPanelUI component for seat switching")]
    public SeatSwapPanelUI seatSwapPanel;

    [Header("Seat Switching State")]
    private bool isSeatSwitchingMode = false;
    private StudentAgent firstStudentForSwap;
    private StudentAgent secondStudentForSwap;
    private Transform secondSeatForSwap;

    [Header("Feedback Panel")]
    public GameObject feedbackPanel;
    public TextMeshProUGUI feedbackText;
    public Image feedbackIcon;
    public float feedbackDuration = 3f;

    [Header("End Session Feedback Panel")]
    [Tooltip("Panel displaying end session feedback")]
    public GameObject endSessionFeedbackPanel;
    
    [Tooltip("Text displaying end session feedback content (legacy - can be null if using separate fields)")]
    public TextMeshProUGUI endSessionFeedbackText;
    
    [Tooltip("Button to close end session feedback panel and return to home screen")]
    public Button closeEndSessionFeedbackButton;
    
    [Header("Dynamic Value Displays (Numbers Only)")]
    [Tooltip("Score value display (just the number, e.g., '85.5')")]
    public TextMeshProUGUI scoreValueText;
    
    [Tooltip("Duration minutes display (e.g., '32')")]
    public TextMeshProUGUI durationMinutesText;
    
    [Tooltip("Duration seconds display (e.g., '00')")]
    public TextMeshProUGUI durationSecondsText;
    
    [Tooltip("Positive actions count (e.g., '5')")]
    public TextMeshProUGUI positiveActionsText;
    
    [Tooltip("Negative actions count (e.g., '2')")]
    public TextMeshProUGUI negativeActionsText;
    
    [Tooltip("Total actions count (e.g., '7')")]
    public TextMeshProUGUI totalActionsText;
    
    [Tooltip("Average engagement percentage (e.g., '75')")]
    public TextMeshProUGUI engagementPercentageText;
    
    [Tooltip("Disruptions count (e.g., '3')")]
    public TextMeshProUGUI disruptionsText;
    
    [Tooltip("Grade/Performance text (e.g., 'טוב מאוד - עבודה טובה!')")]
    public TextMeshProUGUI gradeText;
    
    [Header("Optional Enhanced UI Elements")]
    [Tooltip("Optional: Score display text (large, prominent)")]
    public TextMeshProUGUI scoreDisplayText;
    
    [Tooltip("Optional: Score progress bar/slider")]
    public Slider scoreProgressBar;
    
    [Tooltip("Optional: Image component for panel background (for color changes)")]
    public Image endSessionPanelBackground;
    
    [Tooltip("Optional: Title text for the feedback panel")]
    public TextMeshProUGUI endSessionTitleText;
    
    [Tooltip("Optional: Slider/progress bar for the title area (shows score progress)")]
    public Slider titleScoreSlider;

    [Header("Action Menu")]
    public GameObject actionMenu;
    public Transform actionMenuContainer;

    [Header("Session Info")]
    private float sessionStartTime;
    
    [Header("Session Summary State")]
    [Tooltip("Stored session report - calculated once when session ends, never updates")]
    private SessionReport storedSessionReport;
    [Tooltip("Whether the session summary has been calculated and is now static")]
    private bool isSessionSummaryStatic = false;

    


    void Start()
    {
        sessionStartTime = Time.time;
        
        // Reset session summary state for new session
        isSessionSummaryStatic = false;
        storedSessionReport = null;

        // Wire up button listeners
        SetupButtons();
        
        // Hide feedback panel initially
        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);

        if (actionMenu != null)
            actionMenu.SetActive(false);

        // Hide end session feedback panel initially
        if (endSessionFeedbackPanel != null)
            endSessionFeedbackPanel.SetActive(false);

        // Setup close button for end session feedback panel
        if (closeEndSessionFeedbackButton != null)
            closeEndSessionFeedbackButton.onClick.AddListener(CloseEndSessionFeedbackPanel);

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

        if (switchPlacesButton != null)
            switchPlacesButton.onClick.AddListener(OnSwitchPlacesButtonClicked);

        if (endSessionButton != null)
            endSessionButton.onClick.AddListener(OnEndSessionButtonClicked);
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
        // Check for right-click to view student info (works even in seat switching mode)
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
                    // Right-click always shows student info, even in seat switching mode
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

            // Always try to raycast for 3D objects (students/seats) first
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f))
            {
                StudentAgent student = hit.collider.GetComponentInParent<StudentAgent>();

                // In seat switching mode, handle selection
                if (isSeatSwitchingMode)
                {
                    if (student != null)
                    {
                        // If no first student selected yet, this becomes the first student
                        if (firstStudentForSwap == null)
                        {
                            EnterSeatSwitchingMode(student);
                            return;
                        }
                        else
                        {
                            // Student clicked - use as second selection
                            HandleSeatSwitchingSecondSelection(student, null);
                            return;
                        }
                    }
                    else
                    {
                        // Check if we hit a seat/spawn point
                        Transform hitTransform = hit.collider.transform;

                        // Check if the hit object has "Seat" or "SpawnPoint" in its name
                        // Also check parent objects in case the collider is a child
                        bool isSeatOrSpawnPoint = hitTransform.name.Contains("Seat") || 
                                                  hitTransform.name.Contains("SpawnPoint");
                        
                        // If not found in current transform, check parent
                        if (!isSeatOrSpawnPoint && hitTransform.parent != null)
                        {
                            isSeatOrSpawnPoint = hitTransform.parent.name.Contains("Seat") || 
                                                hitTransform.parent.name.Contains("SpawnPoint");
                            if (isSeatOrSpawnPoint)
                            {
                                hitTransform = hitTransform.parent;
                            }
                        }

                        if (isSeatOrSpawnPoint)
                        {
                            // Only allow seat selection if we already have a first student
                            if (firstStudentForSwap != null)
                            {
                                HandleSeatSwitchingSecondSelection(null, hitTransform);
                                return;
                            }
                        }
                    }
                }
                else
                {
                    // Normal selection mode
                    if (student != null)
                    {
                        // Always allow clicking on students, regardless of UI
                        SelectStudent(student);
                        return;
                    }
                }
            }

            // If we didn't hit a student or seat, handle deselection
            // Only deselect if we're NOT clicking on UI
            if (!clickingOnUI && !isSeatSwitchingMode)
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
    /// Display temporary feedback message (Hebrew RTL).
    /// </summary>
    public void ShowFeedback(string message, Color color)
    {
        if (feedbackPanel == null || feedbackText == null) return;

        feedbackText.isRightToLeftText = true;
        feedbackText.alignment = TMPro.TextAlignmentOptions.Right;

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
    /// Handle switch places button click - enter seat switching mode
    /// </summary>
    void OnSwitchPlacesButtonClicked()
    {
        Debug.Log("[TeacherUI] Switch places button clicked!");
        
        // Show the seat swap panel immediately
        if (seatSwapPanel != null)
        {
            Debug.Log("[TeacherUI] seatSwapPanel is assigned, checking panel reference...");
            
            if (seatSwapPanel.panel == null)
            {
                Debug.LogError("[TeacherUI] seatSwapPanel.panel is NULL! Please assign the panel GameObject in the Inspector.");
                ShowFeedback("שגיאה: פאנל החלפת מקומות לא מוגדר", Color.red);
                return;
            }
            
            if (selectedStudent != null)
            {
                // If student is already selected, enter seat switching mode with that student
                Debug.Log($"[TeacherUI] Student already selected: {selectedStudent.studentName}");
                EnterSeatSwitchingMode(selectedStudent);
            }
            else
            {
                // If no student selected, show panel and enter mode to select first student
                Debug.Log("[TeacherUI] No student selected, showing panel in waiting mode");
                isSeatSwitchingMode = true;
                firstStudentForSwap = null;
                secondStudentForSwap = null;
                secondSeatForSwap = null;

                // Show the panel in "waiting for first student" mode
                seatSwapPanel.ShowPanelWaitingForFirst(OnSeatSwapConfirmed, ExitSeatSwitchingMode);
                
                Debug.Log($"[TeacherUI] Panel should now be active: {seatSwapPanel.panel.activeSelf}");

                // Hide action menu during swap mode
                if (actionMenu != null)
                    actionMenu.SetActive(false);

                ShowFeedback("בחר תלמיד ראשון להחלפת מקום", Color.cyan);
                Debug.Log("Entered seat switching mode - waiting for first student selection");
            }
        }
        else
        {
            Debug.LogError("[TeacherUI] seatSwapPanel is NULL! Please assign SeatSwapPanelUI component in the Inspector.");
            ShowFeedback("שגיאה: פאנל החלפת מקומות לא מוגדר", Color.red);
        }
    }

    /// <summary>
    /// Enter seat switching mode with the first selected student
    /// </summary>
    void EnterSeatSwitchingMode(StudentAgent firstStudent)
    {
        isSeatSwitchingMode = true;
        firstStudentForSwap = firstStudent;
        secondStudentForSwap = null;
        secondSeatForSwap = null;

        // Apply red outline to first student
        var feedback = firstStudentForSwap.GetComponentInChildren<StudentVisualFeedback>();
        if (feedback != null)
        {
            feedback.SetSelected(false);  // Remove blue selection
            feedback.SetSwapSelected(true);  // Add red swap selection
        }

        // Show the seat swap panel
        if (seatSwapPanel != null)
        {
            seatSwapPanel.ShowPanel(firstStudentForSwap, OnSeatSwapConfirmed, ExitSeatSwitchingMode);
        }

        // Hide action menu during swap mode
        if (actionMenu != null)
            actionMenu.SetActive(false);

        ShowFeedback($"בחר תלמיד שני או מקום ריק להחלפה עם {firstStudentForSwap.studentName}", Color.cyan);

        Debug.Log($"Entered seat switching mode with {firstStudentForSwap.studentName}");
    }

    /// <summary>
    /// Exit seat switching mode and reset selections
    /// </summary>
    void ExitSeatSwitchingMode()
    {
        // Remove red outline from first student
        if (firstStudentForSwap != null)
        {
            var feedback = firstStudentForSwap.GetComponentInChildren<StudentVisualFeedback>();
            if (feedback != null)
            {
                feedback.SetSwapSelected(false);
            }
        }

        // Remove selection from second student if any
        if (secondStudentForSwap != null)
        {
            var feedback = secondStudentForSwap.GetComponentInChildren<StudentVisualFeedback>();
            if (feedback != null)
            {
                feedback.SetSelected(false);
            }
        }

        isSeatSwitchingMode = false;
        firstStudentForSwap = null;
        secondStudentForSwap = null;
        secondSeatForSwap = null;

        Debug.Log("Exited seat switching mode");
    }

    /// <summary>
    /// Handle second selection in seat switching mode
    /// </summary>
    void HandleSeatSwitchingSecondSelection(StudentAgent student, Transform seat)
    {
        // Don't allow selecting the same student
        if (student != null && student == firstStudentForSwap)
        {
            ShowFeedback("לא ניתן להחליף תלמיד עם עצמו!", Color.yellow);
            return;
        }

        // Clear previous second selection
        if (secondStudentForSwap != null)
        {
            var prevFeedback = secondStudentForSwap.GetComponentInChildren<StudentVisualFeedback>();
            if (prevFeedback != null)
                prevFeedback.SetSelected(false);
        }

        secondStudentForSwap = student;
        secondSeatForSwap = seat;

        // Apply blue outline to second student if it's a student
        if (secondStudentForSwap != null)
        {
            var feedback = secondStudentForSwap.GetComponentInChildren<StudentVisualFeedback>();
            if (feedback != null)
                feedback.SetSelected(true);
        }

        // Update the panel
        if (seatSwapPanel != null)
        {
            seatSwapPanel.SetSecondSelection(secondStudentForSwap, secondSeatForSwap);
        }

        string secondName = secondStudentForSwap != null ? secondStudentForSwap.studentName : seat?.name ?? "מקום ריק";
        ShowFeedback($"נבחר: {secondName}. לחץ 'החלף' לאישור", Color.green);
    }

    /// <summary>
    /// Called when seat swap is confirmed from the panel
    /// </summary>
    void OnSeatSwapConfirmed(StudentAgent first, StudentAgent second, Transform seat)
    {
        if (first == null)
        {
            Debug.LogWarning("TeacherUI: First student is null!");
            ExitSeatSwitchingMode();
            return;
        }

        if (second == null && seat == null)
        {
            Debug.LogWarning("TeacherUI: Both second student and seat are null!");
            ExitSeatSwitchingMode();
            return;
        }

        // Execute the seat swap through ClassroomManager
        if (classroomManager != null)
        {
            classroomManager.SwapSeats(first, second, null, seat);

            string swapMessage;
            if (second != null)
            {
                swapMessage = $"הוחלפו מקומות: {first.studentName} ↔ {second.studentName}";
            }
            else
            {
                swapMessage = $"{first.studentName} הועבר למקום חדש";
            }

            ShowFeedback(swapMessage, Color.green);
        }

        // Exit seat switching mode
        ExitSeatSwitchingMode();

        // Deselect any selected student
        DeselectStudent();
    }

    /// <summary>
    /// Handle end session button click
    /// </summary>
    void OnEndSessionButtonClicked()
    {
        EndSession();
    }

    /// <summary>
    /// End session and show summary (calculated once, becomes static)
    /// </summary>
    public void EndSession()
    {
        // Calculate report once and store it - summary becomes static after this
        storedSessionReport = classroomManager.EndSession();
        isSessionSummaryStatic = true;
        ShowSessionSummary(storedSessionReport);
    }

    /// <summary>
    /// Display session performance summary in feedback panel with enhanced UI
    /// Summary is static - calculated once when session ends, never updates
    /// Updates only the dynamic numbers, static text should be pre-written in Unity
    /// </summary>
    void ShowSessionSummary(SessionReport report)
    {
        // Ensure we're using the stored static report if summary has been calculated
        if (isSessionSummaryStatic && storedSessionReport != null)
        {
            report = storedSessionReport;
        }
        
        if (endSessionFeedbackPanel == null)
        {
            // Fallback to old feedback method if panel not assigned
            if (endSessionFeedbackText != null)
            {
                string fallbackSummary = $"השלמת שיעור\n\n" +
                                $"ציון: {report.score:F1}/100\n" +
                                $"משך זמן: {report.sessionData.duration:F1} שניות\n" +
                                $"סך פעולות: {report.totalActions}\n" +
                                $"חיוביות: {report.positiveActions} | שליליות: {report.negativeActions}\n" +
                                $"מעורבות ממוצעת: {report.averageEngagement:P0}\n" +
                                $"הפרעות: {report.totalDisruptions}\n\n" +
                                GetPerformanceGrade(report.score);
                ShowFeedback(fallbackSummary, Color.cyan);
            }
            return;
        }

        // Get color scheme based on performance
        Color scoreColor = GetScoreColor(report.score);
        Color panelTint = GetPanelTintColor(report.score);
        string gradeTextValue = GetPerformanceGrade(report.score);
        string gradeEmoji = GetPerformanceEmoji(report.score);

        // Format duration
        int minutes = Mathf.FloorToInt(report.sessionData.duration / 60f);
        int seconds = Mathf.FloorToInt(report.sessionData.duration % 60f);

        // Update dynamic number fields (only numbers, text is pre-written in Unity)
        UpdateDynamicValues(report, minutes, seconds, scoreColor, gradeTextValue);

        // Update optional enhanced UI elements
        UpdateOptionalUIElements(report, scoreColor, panelTint, gradeEmoji);

        // Legacy: Update old feedback text if still using it
        if (endSessionFeedbackText != null && 
            scoreValueText == null && durationMinutesText == null) // Only if not using new system
        {
            string summary = BuildEnhancedSummary(report, minutes, seconds, scoreColor, gradeTextValue, gradeEmoji);
            endSessionFeedbackText.isRightToLeftText = true;
            endSessionFeedbackText.alignment = TMPro.TextAlignmentOptions.Right;
            endSessionFeedbackText.text = summary;
        }

        // Hide any checkbox/box UI elements in the panel
        HideCheckboxElements();
        
        // Show panel
        endSessionFeedbackPanel.SetActive(true);
    }
    
    /// <summary>
    /// Hide checkbox/box UI elements (Toggle components or Image components used as boxes) in the session summary panel
    /// </summary>
    void HideCheckboxElements()
    {
        if (endSessionFeedbackPanel == null) return;
        
        // Find and disable all Toggle components (checkboxes) that are not interactive
        Toggle[] toggles = endSessionFeedbackPanel.GetComponentsInChildren<Toggle>(true);
        foreach (Toggle toggle in toggles)
        {
            // Only hide if it's not part of an interactive element (like a button)
            if (toggle.GetComponent<Button>() == null)
            {
                toggle.gameObject.SetActive(false);
            }
        }
        
        // Find and disable Image components that are likely box/checkbox indicators
        // Look for small square images that appear to be list markers
        Image[] images = endSessionFeedbackPanel.GetComponentsInChildren<Image>(true);
        foreach (Image img in images)
        {
            // Skip if it's part of an interactive element
            if (img.GetComponent<Button>() != null || img.GetComponent<Toggle>() != null)
                continue;
                
            // Skip if it's the panel background
            if (img == endSessionPanelBackground)
                continue;
            
            RectTransform rect = img.rectTransform;
            if (rect != null)
            {
                // Check if it's a small square (likely a box/checkbox indicator)
                // Look for squares that are roughly 10-30 pixels in size
                float width = Mathf.Abs(rect.sizeDelta.x);
                float height = Mathf.Abs(rect.sizeDelta.y);
                
                // If it's a small square and roughly square-shaped
                if (width > 5f && width < 35f && height > 5f && height < 35f)
                {
                    float aspectRatio = width / height;
                    if (aspectRatio > 0.7f && aspectRatio < 1.3f) // Roughly square
                    {
                        // Check if it has no text child (likely just a decorative box)
                        if (img.GetComponentInChildren<TextMeshProUGUI>() == null && 
                            img.GetComponentInChildren<Text>() == null)
                        {
                            img.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Update only the dynamic number values (text labels are pre-written in Unity)
    /// Numbers are displayed LTR (left-to-right) even in RTL context
    /// </summary>
    void UpdateDynamicValues(SessionReport report, int minutes, int seconds, Color scoreColor, string gradeTextValue)
    {
        // Score value
        if (scoreValueText != null)
        {
            scoreValueText.text = $"{report.score:F1}";
            scoreValueText.color = scoreColor;
            scoreValueText.isRightToLeftText = false; // Numbers are LTR
        }

        // Duration
        if (durationMinutesText != null)
        {
            durationMinutesText.text = $"{minutes:00}";
            durationMinutesText.isRightToLeftText = false; // Numbers are LTR
        }
        if (durationSecondsText != null)
        {
            durationSecondsText.text = $"{seconds:00}";
            durationSecondsText.isRightToLeftText = false; // Numbers are LTR
        }

        // Actions
        if (positiveActionsText != null)
        {
            positiveActionsText.text = $"{report.positiveActions}";
            positiveActionsText.isRightToLeftText = false; // Numbers are LTR
        }
        if (negativeActionsText != null)
        {
            negativeActionsText.text = $"{report.negativeActions}";
            negativeActionsText.isRightToLeftText = false; // Numbers are LTR
        }
        if (totalActionsText != null)
        {
            totalActionsText.text = $"{report.totalActions}";
            totalActionsText.isRightToLeftText = false; // Numbers are LTR
        }

        // Engagement
        if (engagementPercentageText != null)
        {
            engagementPercentageText.text = $"{Mathf.RoundToInt(report.averageEngagement * 100f):00}";
            engagementPercentageText.color = GetColorFromHex(GetEngagementColor(report.averageEngagement));
            engagementPercentageText.isRightToLeftText = false; // Numbers are LTR
        }

        // Disruptions
        if (disruptionsText != null)
        {
            disruptionsText.text = $"{report.totalDisruptions}";
            disruptionsText.color = GetColorFromHex(GetDisruptionColor(report.totalDisruptions));
            disruptionsText.isRightToLeftText = false; // Numbers are LTR
        }

        // Grade text
        if (gradeText != null)
        {
            gradeText.text = gradeTextValue;
            gradeText.color = scoreColor;
        }
    }

    /// <summary>
    /// Update optional enhanced UI elements
    /// </summary>
    void UpdateOptionalUIElements(SessionReport report, Color scoreColor, Color panelTint, string gradeEmoji)
    {
        // Update title if available - include score in title
        if (endSessionTitleText != null)
        {
            endSessionTitleText.text = $"{gradeEmoji} סיכום שיעור - ציון: {report.score:F1}/100 {gradeEmoji}";
            endSessionTitleText.color = scoreColor;
        }
        
        // Update title slider if available
        if (titleScoreSlider != null)
        {
            titleScoreSlider.value = report.score / 100f;
            // Set color based on score
            var colors = titleScoreSlider.colors;
            colors.normalColor = scoreColor;
            titleScoreSlider.colors = colors;
        }

        // Update score display if available
        if (scoreDisplayText != null)
        {
            scoreDisplayText.text = $"<size=48><b><color=#{ColorUtility.ToHtmlStringRGB(scoreColor)}>{report.score:F1}</color></b></size>\n<size=20>/100</size>";
            scoreDisplayText.isRightToLeftText = true;
            scoreDisplayText.alignment = TMPro.TextAlignmentOptions.Center;
        }

        // Update score progress bar if available
        if (scoreProgressBar != null)
        {
            scoreProgressBar.value = report.score / 100f;
            // Set color based on score
            var colors = scoreProgressBar.colors;
            colors.normalColor = scoreColor;
            scoreProgressBar.colors = colors;
        }

        // Update panel background color if available
        if (endSessionPanelBackground != null)
        {
            endSessionPanelBackground.color = panelTint;
        }
    }

    /// <summary>
    /// Convert hex color string to Color
    /// </summary>
    Color GetColorFromHex(string hex)
    {
        if (ColorUtility.TryParseHtmlString($"#{hex}", out Color color))
        {
            return color;
        }
        return Color.white; // Fallback
    }

    /// <summary>
    /// Build enhanced summary text with colors and visual formatting
    /// </summary>
    string BuildEnhancedSummary(SessionReport report, int minutes, int seconds, Color scoreColor, string gradeText, string gradeEmoji)
    {
        // Color codes for different metrics (using proper hex format without # for TextMeshPro)
        string positiveColor = "4CAF50"; // Green
        string negativeColor = "F44336"; // Red
        string neutralColor = "2196F3"; // Blue
        string warningColor = "FF9800"; // Orange
        string scoreColorHex = ColorUtility.ToHtmlStringRGB(scoreColor);

        // Build summary with rich text formatting (TextMeshPro uses color=#RRGGBB format)
        string summary = $"<size=28><b><color=#{scoreColorHex}>ציון: {report.score:F1}/100</color></b></size>\n\n" +
                        $"<size=22><b>{gradeEmoji} {gradeText}</b></size>\n\n" +
                        $"<color=#{neutralColor}>⏱ <b>משך זמן:</b></color> {minutes:00}:{seconds:00}\n\n" +
                        $"<color=#{neutralColor}>📊 <b>סטטיסטיקות פעולות:</b></color>\n" +
                        $"   <color=#{positiveColor}>✓ פעולות חיוביות:</color> <b>{report.positiveActions}</b>\n" +
                        $"   <color=#{negativeColor}>✗ פעולות שליליות:</color> <b>{report.negativeActions}</b>\n" +
                        $"   <color=#{neutralColor}>📝 סך פעולות:</color> <b>{report.totalActions}</b>\n\n" +
                        $"<color=#{neutralColor}>📈 <b>ביצועי כיתה:</b></color>\n" +
                        $"   <color=#{GetEngagementColor(report.averageEngagement)}>📚 מעורבות ממוצעת:</color> <b>{report.averageEngagement:P0}</b>\n" +
                        $"   <color=#{GetDisruptionColor(report.totalDisruptions)}>⚠ הפרעות:</color> <b>{report.totalDisruptions}</b>";

        return summary;
    }

    /// <summary>
    /// Get color based on score
    /// </summary>
    Color GetScoreColor(float score)
    {
        if (score >= 90) return new Color(0.2f, 0.8f, 0.2f); // Green - Excellent
        if (score >= 80) return new Color(0.4f, 0.9f, 0.4f); // Light Green - Very Good
        if (score >= 70) return new Color(1f, 0.8f, 0.2f); // Yellow - Good
        if (score >= 60) return new Color(1f, 0.6f, 0.2f); // Orange - Sufficient
        return new Color(1f, 0.3f, 0.3f); // Red - Failed
    }

    /// <summary>
    /// Get panel background tint color based on score
    /// </summary>
    Color GetPanelTintColor(float score)
    {
        Color baseColor = new Color(0.1f, 0.1f, 0.15f, 0.95f); // Dark blue-gray
        Color scoreColor = GetScoreColor(score);
        
        // Blend base color with score color (20% tint)
        return Color.Lerp(baseColor, scoreColor, 0.2f);
    }

    /// <summary>
    /// Get color for engagement level
    /// </summary>
    string GetEngagementColor(float engagement)
    {
        if (engagement >= 0.8f) return "4CAF50"; // Green
        if (engagement >= 0.6f) return "FFC107"; // Yellow
        if (engagement >= 0.4f) return "FF9800"; // Orange
        return "F44336"; // Red
    }

    /// <summary>
    /// Get color for disruption count
    /// </summary>
    string GetDisruptionColor(int disruptions)
    {
        if (disruptions <= 2) return "4CAF50"; // Green
        if (disruptions <= 5) return "FFC107"; // Yellow
        if (disruptions <= 8) return "FF9800"; // Orange
        return "F44336"; // Red
    }

    /// <summary>
    /// Get emoji based on performance
    /// </summary>
    string GetPerformanceEmoji(float score)
    {
        if (score >= 90) return "🌟"; // Star - Excellent
        if (score >= 80) return "⭐"; // Star - Very Good
        if (score >= 70) return "👍"; // Thumbs up - Good
        if (score >= 60) return "📝"; // Memo - Sufficient
        return "📚"; // Books - Needs improvement
    }

    /// <summary>
    /// Close end session feedback panel and return to teacher home screen
    /// </summary>
    void CloseEndSessionFeedbackPanel()
    {
        if (endSessionFeedbackPanel != null)
            endSessionFeedbackPanel.SetActive(false);

        // Return to teacher home screen
        SceneManager.LoadScene("TeacherHomeScreen");
    }
    
    /// <summary>
    /// Get the stored static session report (calculated once when session ended)
    /// Returns null if session hasn't ended yet
    /// </summary>
    public SessionReport GetStoredSessionReport()
    {
        return isSessionSummaryStatic ? storedSessionReport : null;
    }
    
    /// <summary>
    /// Check if session summary is static (has been calculated and won't update)
    /// </summary>
    public bool IsSessionSummaryStatic()
    {
        return isSessionSummaryStatic;
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