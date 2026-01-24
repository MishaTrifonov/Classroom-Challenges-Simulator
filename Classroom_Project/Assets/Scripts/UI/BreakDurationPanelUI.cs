using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI panel for selecting break duration for a student
/// </summary>
public class BreakDurationPanelUI : MonoBehaviour
{
    [Header("Panel References")]
    public GameObject panel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI durationDisplayText;
    
    [Header("Duration Selection")]
    public Slider durationSlider;
    public TMP_InputField durationInputField;
    public Button confirmButton;
    public Button cancelButton;
    
    [Header("Duration Settings")]
    public float minDurationMinutes = 1f;
    public float maxDurationMinutes = 30f;
    public float defaultDurationMinutes = 5f;
    
    private float currentDurationMinutes;
    private StudentAgent targetStudent;
    private System.Action<float> onConfirmCallback;
    private bool isUpdatingFromSlider = false;
    private bool isUpdatingFromInput = false;
    
    void Start()
    {
        // Hide panel initially
        if (panel != null)
            panel.SetActive(false);
        
        // Setup slider
        if (durationSlider != null)
        {
            durationSlider.minValue = minDurationMinutes;
            durationSlider.maxValue = maxDurationMinutes;
            durationSlider.value = defaultDurationMinutes;
            durationSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }
        
        // Setup input field
        if (durationInputField != null)
        {
            durationInputField.contentType = TMP_InputField.ContentType.DecimalNumber;
            durationInputField.text = defaultDurationMinutes.ToString("F1");
            durationInputField.onValueChanged.AddListener(OnInputFieldValueChanged);
        }
        
        // Setup buttons
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);
        
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClicked);
        
        currentDurationMinutes = defaultDurationMinutes;
        UpdateDurationDisplay();
    }
    
    void Update()
    {
        // Ensure display stays in sync (fallback for real-time updates)
        if (panel != null && panel.activeSelf)
        {
            // Sync with slider if it exists and value differs
            if (durationSlider != null && !isUpdatingFromSlider && !isUpdatingFromInput)
            {
                float sliderValue = durationSlider.value;
                if (Mathf.Abs(sliderValue - currentDurationMinutes) > 0.01f)
                {
                    currentDurationMinutes = sliderValue;
                    UpdateDurationDisplay();
                    
                    // Update input field to match
                    if (durationInputField != null && durationInputField.text != sliderValue.ToString("F1"))
                    {
                        durationInputField.onValueChanged.RemoveListener(OnInputFieldValueChanged);
                        durationInputField.text = sliderValue.ToString("F1");
                        durationInputField.onValueChanged.AddListener(OnInputFieldValueChanged);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Show the break duration panel for a specific student
    /// </summary>
    public void ShowPanel(StudentAgent student, System.Action<float> onConfirm)
    {
        if (student == null)
        {
            Debug.LogWarning("BreakDurationPanelUI: Cannot show panel for null student");
            return;
        }
        
        targetStudent = student;
        onConfirmCallback = onConfirm;
        
        // Update title text
        if (titleText != null)
        {
            titleText.text = $"בחר משך הפסקה ל-{student.studentName}";
        }
        
        // Reset to default duration
        currentDurationMinutes = defaultDurationMinutes;
        
        // Update slider without triggering event
        if (durationSlider != null)
        {
            durationSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
            durationSlider.value = currentDurationMinutes;
            durationSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }
        
        // Update input field without triggering event
        if (durationInputField != null)
        {
            durationInputField.onValueChanged.RemoveListener(OnInputFieldValueChanged);
            durationInputField.text = currentDurationMinutes.ToString("F1");
            durationInputField.onValueChanged.AddListener(OnInputFieldValueChanged);
        }
        
        UpdateDurationDisplay();
        
        // Show panel
        if (panel != null)
            panel.SetActive(true);
    }
    
    /// <summary>
    /// Hide the break duration panel
    /// </summary>
    public void HidePanel()
    {
        if (panel != null)
            panel.SetActive(false);
        
        targetStudent = null;
        onConfirmCallback = null;
    }
    
    /// <summary>
    /// Called when slider value changes
    /// </summary>
    void OnSliderValueChanged(float value)
    {
        // Prevent recursive updates
        if (isUpdatingFromInput)
            return;
        
        isUpdatingFromSlider = true;
        currentDurationMinutes = value;
        UpdateDurationDisplay();
        
        // Update input field (without triggering its event)
        if (durationInputField != null)
        {
            // Temporarily remove listener to prevent recursion
            durationInputField.onValueChanged.RemoveListener(OnInputFieldValueChanged);
            durationInputField.text = currentDurationMinutes.ToString("F1");
            durationInputField.onValueChanged.AddListener(OnInputFieldValueChanged);
        }
        
        isUpdatingFromSlider = false;
    }
    
    /// <summary>
    /// Called when input field value changes
    /// </summary>
    void OnInputFieldValueChanged(string value)
    {
        // Prevent recursive updates
        if (isUpdatingFromSlider)
            return;
        
        if (float.TryParse(value, out float parsedValue))
        {
            isUpdatingFromInput = true;
            
            // Clamp value to valid range
            parsedValue = Mathf.Clamp(parsedValue, minDurationMinutes, maxDurationMinutes);
            currentDurationMinutes = parsedValue;
            
            // Update slider (without triggering its event)
            if (durationSlider != null)
            {
                // Temporarily remove listener to prevent recursion
                durationSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
                durationSlider.value = currentDurationMinutes;
                durationSlider.onValueChanged.AddListener(OnSliderValueChanged);
            }
            
            // Update display
            UpdateDurationDisplay();
            
            isUpdatingFromInput = false;
        }
        else
        {
            // If parsing failed, still update display with current value
            UpdateDurationDisplay();
        }
    }
    
    /// <summary>
    /// Update the duration display text
    /// </summary>
    void UpdateDurationDisplay()
    {
        if (durationDisplayText != null)
        {
            int minutes = Mathf.FloorToInt(currentDurationMinutes);
            int seconds = Mathf.RoundToInt((currentDurationMinutes - minutes) * 60f);
            
            if (seconds >= 60)
            {
                minutes += 1;
                seconds = 0;
            }
            
            if (minutes > 0 && seconds > 0)
            {
                durationDisplayText.text = $"משך זמן: {minutes} דקות {seconds} שניות";
            }
            else if (minutes > 0)
            {
                durationDisplayText.text = $"משך זמן: {minutes} דקות";
            }
            else
            {
                durationDisplayText.text = $"משך זמן: {seconds} שניות";
            }
        }
    }
    
    /// <summary>
    /// Called when confirm button is clicked
    /// </summary>
    void OnConfirmClicked()
    {
        if (targetStudent != null && onConfirmCallback != null)
        {
            onConfirmCallback(currentDurationMinutes);
        }
        
        HidePanel();
    }
    
    /// <summary>
    /// Called when cancel button is clicked
    /// </summary>
    void OnCancelClicked()
    {
        HidePanel();
    }
}
