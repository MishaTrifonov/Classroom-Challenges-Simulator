using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Component for managing individual student profile entry in the scenario creator
/// Updated to work with Container structure (Container > Slider + Label)
/// </summary>
public class StudentProfileEntry : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField nameInput;

    [Header("Extroversion")]
    public Slider extroversionSlider;
    public TextMeshProUGUI extroversionLabel;

    [Header("Sensitivity")]
    public Slider sensitivitySlider;
    public TextMeshProUGUI sensitivityLabel;

    [Header("Rebelliousness")]
    public Slider rebelliousnessSlider;
    public TextMeshProUGUI rebelliousnessLabel;

    [Header("Academic Motivation")]
    public Slider academicMotivationSlider;
    public TextMeshProUGUI academicMotivationLabel;

    [Header("Initial Happiness")]
    public Slider initialHappinessSlider;
    public TextMeshProUGUI initialHappinessLabel;

    [Header("Initial Boredom")]
    public Slider initialBoredomSlider;
    public TextMeshProUGUI initialBoredomLabel;

    private string studentId;

    void Start()
    {
        Debug.Log("StudentProfileEntry Start() called");

        // Setup slider listeners to update labels
        if (extroversionSlider != null)
            extroversionSlider.onValueChanged.AddListener(v => UpdateSliderLabel(extroversionLabel, v));

        if (sensitivitySlider != null)
            sensitivitySlider.onValueChanged.AddListener(v => UpdateSliderLabel(sensitivityLabel, v));

        if (rebelliousnessSlider != null)
            rebelliousnessSlider.onValueChanged.AddListener(v => UpdateSliderLabel(rebelliousnessLabel, v));

        if (academicMotivationSlider != null)
            academicMotivationSlider.onValueChanged.AddListener(v => UpdateSliderLabel(academicMotivationLabel, v));

        if (initialHappinessSlider != null)
            initialHappinessSlider.onValueChanged.AddListener(v => UpdateSliderLabel(initialHappinessLabel, v));

        if (initialBoredomSlider != null)
            initialBoredomSlider.onValueChanged.AddListener(v => UpdateSliderLabel(initialBoredomLabel, v));

        // Initialize slider values and labels
        InitializeSliders();

        Debug.Log("StudentProfileEntry initialized successfully");
    }

    void InitializeSliders()
    {
        SetSliderDefaults(extroversionSlider, extroversionLabel, 0.5f);
        SetSliderDefaults(sensitivitySlider, sensitivityLabel, 0.5f);
        SetSliderDefaults(rebelliousnessSlider, rebelliousnessLabel, 0.5f);
        SetSliderDefaults(academicMotivationSlider, academicMotivationLabel, 0.5f);
        SetSliderDefaults(initialHappinessSlider, initialHappinessLabel, 5.0f);
        SetSliderDefaults(initialBoredomSlider, initialBoredomLabel, 5.0f);
    }

    void SetSliderDefaults(Slider slider, TextMeshProUGUI label, float defaultValue)
    {
        if (slider != null)
        {
            slider.value = defaultValue;
            UpdateSliderLabel(label, defaultValue);
        }
    }

    void UpdateSliderLabel(TextMeshProUGUI label, float value)
    {
        if (label != null)
            label.text = value.ToString("F2");
    }

    public void SetStudentId(string id)
    {
        studentId = id;
    }

    /// <summary>
    /// Reset all fields to default values
    /// </summary>
    public void ResetToDefaults()
    {
        if (nameInput != null)
            nameInput.text = "";

        SetSliderDefaults(extroversionSlider, extroversionLabel, 0.5f);
        SetSliderDefaults(sensitivitySlider, sensitivityLabel, 0.5f);
        SetSliderDefaults(rebelliousnessSlider, rebelliousnessLabel, 0.5f);
        SetSliderDefaults(academicMotivationSlider, academicMotivationLabel, 0.5f);
        SetSliderDefaults(initialHappinessSlider, initialHappinessLabel, 5.0f);
        SetSliderDefaults(initialBoredomSlider, initialBoredomLabel, 5.0f);
    }

    /// <summary>
    /// Load existing profile data into the editor
    /// </summary>
    public void LoadProfile(StudentProfile profile)
    {
        if (nameInput != null)
            nameInput.text = profile.name;

        if (extroversionSlider != null)
        {
            extroversionSlider.value = profile.extroversion;
            UpdateSliderLabel(extroversionLabel, profile.extroversion);
        }

        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = profile.sensitivity;
            UpdateSliderLabel(sensitivityLabel, profile.sensitivity);
        }

        if (rebelliousnessSlider != null)
        {
            rebelliousnessSlider.value = profile.rebelliousness;
            UpdateSliderLabel(rebelliousnessLabel, profile.rebelliousness);
        }

        if (academicMotivationSlider != null)
        {
            academicMotivationSlider.value = profile.academicMotivation;
            UpdateSliderLabel(academicMotivationLabel, profile.academicMotivation);
        }

        if (initialHappinessSlider != null)
        {
            initialHappinessSlider.value = profile.initialHappiness;
            UpdateSliderLabel(initialHappinessLabel, profile.initialHappiness);
        }

        if (initialBoredomSlider != null)
        {
            initialBoredomSlider.value = profile.initialBoredom;
            UpdateSliderLabel(initialBoredomLabel, profile.initialBoredom);
        }

        studentId = profile.id;
    }

    /// <summary>
    /// Get the student profile from the current input values
    /// </summary>
    public StudentProfile GetStudentProfile()
    {
        return new StudentProfile
        {
            id = string.IsNullOrEmpty(studentId) ? System.Guid.NewGuid().ToString() : studentId,
            name = nameInput != null ? nameInput.text : "Unnamed Student",
            extroversion = extroversionSlider != null ? extroversionSlider.value : 0.5f,
            sensitivity = sensitivitySlider != null ? sensitivitySlider.value : 0.5f,
            rebelliousness = rebelliousnessSlider != null ? rebelliousnessSlider.value : 0.5f,
            academicMotivation = academicMotivationSlider != null ? academicMotivationSlider.value : 0.5f,
            initialHappiness = initialHappinessSlider != null ? initialHappinessSlider.value : 5.0f,
            initialBoredom = initialBoredomSlider != null ? initialBoredomSlider.value : 5.0f
        };
    }
}