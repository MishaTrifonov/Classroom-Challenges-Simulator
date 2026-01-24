using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI panel for managing seat swapping between students.
/// Shows current selections and provides switch confirmation.
/// </summary>
public class SeatSwapPanelUI : MonoBehaviour
{
    [Header("Panel References")]
    [Tooltip("The main panel GameObject")]
    public GameObject panel;

    [Header("UI Elements")]
    [Tooltip("Text showing the first selected student")]
    public TextMeshProUGUI firstStudentText;

    [Tooltip("Text showing the second selected student/seat")]
    public TextMeshProUGUI secondStudentText;

    [Tooltip("Button to confirm the swap")]
    public Button switchButton;

    [Tooltip("Button to cancel and close the panel")]
    public Button cancelButton;

    [Tooltip("Button to reset selections")]
    public Button resetButton;

    [Header("Display Settings")]
    [Tooltip("Text shown when first student is selected")]
    public string firstStudentPrefix = "החלפת מקום: ";

    [Tooltip("Text shown when waiting for second selection")]
    public string waitingForSecondText = "בחר תלמיד שני או מקום ישיבה ריק";

    [Tooltip("Arrow or separator between names")]
    public string separator = " ↔ ";

    // Current selections
    private StudentAgent firstStudent;
    private StudentAgent secondStudent;
    private Transform secondSeat;

    // Callback for when swap is confirmed
    private System.Action<StudentAgent, StudentAgent, Transform> onSwapConfirmed;

    // Callback for when swap is cancelled
    private System.Action onSwapCancelled;

    void Start()
    {
        // Hide panel initially
        if (panel != null)
            panel.SetActive(false);

        // Wire up button listeners
        if (switchButton != null)
        {
            switchButton.onClick.AddListener(OnSwitchButtonClicked);
            switchButton.gameObject.SetActive(false); // Hide until both selections made
        }

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelButtonClicked);

        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetButtonClicked);
    }

    /// <summary>
    /// Show the panel and start seat swap mode
    /// </summary>
    public void ShowPanel(StudentAgent firstSelectedStudent, System.Action<StudentAgent, StudentAgent, Transform> swapCallback, System.Action cancelCallback = null)
    {
        if (panel != null)
            panel.SetActive(true);

        firstStudent = firstSelectedStudent;
        secondStudent = null;
        secondSeat = null;
        onSwapConfirmed = swapCallback;
        onSwapCancelled = cancelCallback;

        UpdateDisplay();
    }

    /// <summary>
    /// Show the panel without a first student (waiting for first selection)
    /// </summary>
    public void ShowPanelWaitingForFirst(System.Action<StudentAgent, StudentAgent, Transform> swapCallback, System.Action cancelCallback = null)
    {
        Debug.Log($"[SeatSwapPanelUI] ShowPanelWaitingForFirst called. Panel reference: {(panel != null ? "assigned" : "NULL")}");
        
        if (panel != null)
        {
            panel.SetActive(true);
            Debug.Log($"[SeatSwapPanelUI] Panel activated. Active state: {panel.activeSelf}");
        }
        else
        {
            Debug.LogError("[SeatSwapPanelUI] Panel GameObject is NULL! Please assign the panel GameObject in the Inspector.");
        }

        firstStudent = null;
        secondStudent = null;
        secondSeat = null;
        onSwapConfirmed = swapCallback;
        onSwapCancelled = cancelCallback;

        UpdateDisplay();
    }

    /// <summary>
    /// Update the second selection (student or seat)
    /// </summary>
    public void SetSecondSelection(StudentAgent student, Transform seat)
    {
        secondStudent = student;
        secondSeat = seat;

        UpdateDisplay();
    }

    /// <summary>
    /// Check if a valid second selection has been made
    /// </summary>
    public bool HasValidSecondSelection()
    {
        return secondStudent != null || secondSeat != null;
    }

    /// <summary>
    /// Update the display text and button states
    /// </summary>
    void UpdateDisplay()
    {
        // Update first student text
        if (firstStudentText != null)
        {
            if (firstStudent != null)
            {
                firstStudentText.text = firstStudentPrefix + firstStudent.studentName;
            }
            else
            {
                // No first student selected yet
                firstStudentText.text = "בחר תלמיד ראשון להחלפה";
            }
        }

        // Update second student/seat text
        if (secondStudentText != null)
        {
            if (firstStudent == null)
            {
                // Waiting for first student
                secondStudentText.text = "";
            }
            else if (secondStudent != null)
            {
                // Second student selected
                secondStudentText.text = separator + secondStudent.studentName;
            }
            else if (secondSeat != null)
            {
                // Empty seat selected
                secondStudentText.text = separator + "מקום ריק (" + secondSeat.name + ")";
            }
            else
            {
                // Waiting for second selection
                secondStudentText.text = waitingForSecondText;
            }
        }

        // Show/hide switch button based on whether we have both selections
        if (switchButton != null)
        {
            switchButton.gameObject.SetActive(firstStudent != null && HasValidSecondSelection());
        }
    }

    /// <summary>
    /// Called when switch button is clicked
    /// </summary>
    void OnSwitchButtonClicked()
    {
        if (firstStudent == null)
        {
            Debug.LogWarning("SeatSwapPanelUI: No first student selected!");
            return;
        }

        if (!HasValidSecondSelection())
        {
            Debug.LogWarning("SeatSwapPanelUI: No valid second selection!");
            return;
        }

        // Invoke the callback with the selections
        onSwapConfirmed?.Invoke(firstStudent, secondStudent, secondSeat);

        // Close the panel
        ClosePanel();
    }

    /// <summary>
    /// Called when cancel button is clicked
    /// </summary>
    void OnCancelButtonClicked()
    {
        // Invoke cancel callback before closing
        onSwapCancelled?.Invoke();

        ClosePanel();
    }

    /// <summary>
    /// Called when reset button is clicked - allows re-selection
    /// </summary>
    void OnResetButtonClicked()
    {
        secondStudent = null;
        secondSeat = null;
        UpdateDisplay();
    }

    /// <summary>
    /// Close the panel and reset state
    /// </summary>
    public void ClosePanel()
    {
        if (panel != null)
            panel.SetActive(false);

        firstStudent = null;
        secondStudent = null;
        secondSeat = null;
        onSwapConfirmed = null;
    }

    /// <summary>
    /// Get the first selected student
    /// </summary>
    public StudentAgent GetFirstStudent()
    {
        return firstStudent;
    }

    /// <summary>
    /// Get the second selected student
    /// </summary>
    public StudentAgent GetSecondStudent()
    {
        return secondStudent;
    }

    /// <summary>
    /// Get the second selected seat
    /// </summary>
    public Transform GetSecondSeat()
    {
        return secondSeat;
    }
}
