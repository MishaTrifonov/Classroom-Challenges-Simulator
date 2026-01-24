using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Component for managing individual student profile entry in the scenario creator
/// </summary>
public class StudentProfileEntry : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField nameInput;
    public Slider extroversionSlider;
    public TextMeshProUGUI extroversionValue;
    public Slider sensitivitySlider;
    public TextMeshProUGUI sensitivityValue;
    public Slider rebelliousnessSlider;
    public TextMeshProUGUI rebelliousnessValue;
    public Slider academicMotivationSlider;
    public TextMeshProUGUI academicMotivationValue;
    public Slider initialHappinessSlider;
    public TextMeshProUGUI initialHappinessValue;
    public Slider initialBoredomSlider;
    public TextMeshProUGUI initialBoredomValue;
    public Button removeButton;

    private string studentId;

    [HideInInspector]
    public GameObject editingItemReference; // Reference to the display item being edited

    void Start()
    {
        Debug.Log("StudentProfileEntry Start() called");

        // Setup slider listeners to update value displays
        if (extroversionSlider != null)
            extroversionSlider.onValueChanged.AddListener(v => UpdateSliderValue(extroversionValue, v));

        if (sensitivitySlider != null)
            sensitivitySlider.onValueChanged.AddListener(v => UpdateSliderValue(sensitivityValue, v));

        if (rebelliousnessSlider != null)
            rebelliousnessSlider.onValueChanged.AddListener(v => UpdateSliderValue(rebelliousnessValue, v));

        if (academicMotivationSlider != null)
            academicMotivationSlider.onValueChanged.AddListener(v => UpdateSliderValue(academicMotivationValue, v));

        if (initialHappinessSlider != null)
            initialHappinessSlider.onValueChanged.AddListener(v => UpdateSliderValue(initialHappinessValue, v));

        if (initialBoredomSlider != null)
            initialBoredomSlider.onValueChanged.AddListener(v => UpdateSliderValue(initialBoredomValue, v));

        if (removeButton != null)
            removeButton.onClick.AddListener(RemoveProfile);

        // Initialize slider values
        InitializeSliders();

        Debug.Log("StudentProfileEntry initialized successfully");
    }

    void InitializeSliders()
    {
        SetSliderDefaults(extroversionSlider, extroversionValue, 0.5f);
        SetSliderDefaults(sensitivitySlider, sensitivityValue, 0.5f);
        SetSliderDefaults(rebelliousnessSlider, rebelliousnessValue, 0.5f);
        SetSliderDefaults(academicMotivationSlider, academicMotivationValue, 0.5f);
        SetSliderDefaults(initialHappinessSlider, initialHappinessValue, 5.0f);
        SetSliderDefaults(initialBoredomSlider, initialBoredomValue, 5.0f);
    }

    void SetSliderDefaults(Slider slider, TextMeshProUGUI valueText, float defaultValue)
    {
        if (slider != null)
        {
            slider.value = defaultValue;
            UpdateSliderValue(valueText, defaultValue);
        }
    }

    void UpdateSliderValue(TextMeshProUGUI valueText, float value)
    {
        if (valueText != null)
            valueText.text = value.ToString("F2");
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

        SetSliderDefaults(extroversionSlider, extroversionValue, 0.5f);
        SetSliderDefaults(sensitivitySlider, sensitivityValue, 0.5f);
        SetSliderDefaults(rebelliousnessSlider, rebelliousnessValue, 0.5f);
        SetSliderDefaults(academicMotivationSlider, academicMotivationValue, 0.5f);
        SetSliderDefaults(initialHappinessSlider, initialHappinessValue, 5.0f);
        SetSliderDefaults(initialBoredomSlider, initialBoredomValue, 5.0f);

        editingItemReference = null;
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
            UpdateSliderValue(extroversionValue, profile.extroversion);
        }

        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = profile.sensitivity;
            UpdateSliderValue(sensitivityValue, profile.sensitivity);
        }

        if (rebelliousnessSlider != null)
        {
            rebelliousnessSlider.value = profile.rebelliousness;
            UpdateSliderValue(rebelliousnessValue, profile.rebelliousness);
        }

        if (academicMotivationSlider != null)
        {
            academicMotivationSlider.value = profile.academicMotivation;
            UpdateSliderValue(academicMotivationValue, profile.academicMotivation);
        }

        if (initialHappinessSlider != null)
        {
            initialHappinessSlider.value = profile.initialHappiness;
            UpdateSliderValue(initialHappinessValue, profile.initialHappiness);
        }

        if (initialBoredomSlider != null)
        {
            initialBoredomSlider.value = profile.initialBoredom;
            UpdateSliderValue(initialBoredomValue, profile.initialBoredom);
        }

        studentId = profile.id;
    }

    public StudentProfile GetStudentProfile()
    {
        return new StudentProfile
        {
            id = studentId,
            name = nameInput != null ? nameInput.text : "Unnamed Student",
            extroversion = extroversionSlider != null ? extroversionSlider.value : 0.5f,
            sensitivity = sensitivitySlider != null ? sensitivitySlider.value : 0.5f,
            rebelliousness = rebelliousnessSlider != null ? rebelliousnessSlider.value : 0.5f,
            academicMotivation = academicMotivationSlider != null ? academicMotivationSlider.value : 0.5f,
            initialHappiness = initialHappinessSlider != null ? initialHappinessSlider.value : 5.0f,
            initialBoredom = initialBoredomSlider != null ? initialBoredomSlider.value : 5.0f
        };
    }

    void RemoveProfile()
    {
        Destroy(gameObject);
    }

    /// <summary>
    /// Create default UI for student profile if no prefab is provided
    /// </summary>
    public void CreateDefaultUI(Transform parent)
    {
        // Create vertical layout
        var verticalLayout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
        verticalLayout.spacing = 5;
        verticalLayout.padding = new RectOffset(10, 10, 10, 10);
        verticalLayout.childControlHeight = false;
        verticalLayout.childControlWidth = true;
        verticalLayout.childForceExpandHeight = false;
        verticalLayout.childForceExpandWidth = true;

        // Name input
        nameInput = CreateInputField(parent, "Student Name", "Enter student name...");

        // Create sliders
        extroversionSlider = CreateSliderWithLabel(parent, "Extroversion", 0f, 1f, out extroversionValue);
        sensitivitySlider = CreateSliderWithLabel(parent, "Sensitivity", 0f, 1f, out sensitivityValue);
        rebelliousnessSlider = CreateSliderWithLabel(parent, "Rebelliousness", 0f, 1f, out rebelliousnessValue);
        academicMotivationSlider = CreateSliderWithLabel(parent, "Academic Motivation", 0f, 1f, out academicMotivationValue);
        initialHappinessSlider = CreateSliderWithLabel(parent, "Initial Happiness", 0f, 10f, out initialHappinessValue);
        initialBoredomSlider = CreateSliderWithLabel(parent, "Initial Boredom", 0f, 10f, out initialBoredomValue);

        // Remove button
        removeButton = CreateButton(parent, "Remove Student", Color.red);
    }

    TMP_InputField CreateInputField(Transform parent, string labelText, string placeholder)
    {
        GameObject fieldObj = new GameObject(labelText);
        fieldObj.transform.SetParent(parent, false);

        var rectTransform = fieldObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(0, 30);

        var layoutElement = fieldObj.AddComponent<LayoutElement>();
        layoutElement.minHeight = 30;
        layoutElement.preferredHeight = 30;

        // Background
        var image = fieldObj.AddComponent<Image>();
        image.color = new Color(0.1f, 0.1f, 0.1f, 1f);

        // Input field
        var inputField = fieldObj.AddComponent<TMP_InputField>();

        // Text area
        GameObject textArea = new GameObject("Text Area");
        textArea.transform.SetParent(fieldObj.transform, false);
        var textAreaRect = textArea.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.sizeDelta = Vector2.zero;
        textAreaRect.offsetMin = new Vector2(5, 0);
        textAreaRect.offsetMax = new Vector2(-5, 0);

        // Placeholder
        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(textArea.transform, false);
        var placeholderRect = placeholderObj.AddComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.sizeDelta = Vector2.zero;
        var placeholderText = placeholderObj.AddComponent<TextMeshProUGUI>();
        placeholderText.text = placeholder;
        placeholderText.fontSize = 14;
        placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1f);

        // Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(textArea.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.fontSize = 14;
        text.color = Color.white;

        inputField.textViewport = textAreaRect;
        inputField.textComponent = text;
        inputField.placeholder = placeholderText;

        return inputField;
    }

    Slider CreateSliderWithLabel(Transform parent, string labelText, float minValue, float maxValue, out TextMeshProUGUI valueText)
    {
        GameObject sliderContainer = new GameObject(labelText + " Container");
        sliderContainer.transform.SetParent(parent, false);

        var containerRect = sliderContainer.AddComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(0, 30);

        var layoutElement = sliderContainer.AddComponent<LayoutElement>();
        layoutElement.minHeight = 30;
        layoutElement.preferredHeight = 30;

        var horizontalLayout = sliderContainer.AddComponent<HorizontalLayoutGroup>();
        horizontalLayout.spacing = 10;
        horizontalLayout.childControlWidth = true;
        horizontalLayout.childControlHeight = true;
        horizontalLayout.childForceExpandWidth = false;
        horizontalLayout.childForceExpandHeight = true;

        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(sliderContainer.transform, false);
        var labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(150, 0);
        var labelLayoutElement = labelObj.AddComponent<LayoutElement>();
        labelLayoutElement.minWidth = 150;
        labelLayoutElement.preferredWidth = 150;
        var label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = labelText;
        label.fontSize = 12;
        label.color = Color.white;

        // Slider
        GameObject sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(sliderContainer.transform, false);
        var sliderRect = sliderObj.AddComponent<RectTransform>();
        var sliderLayoutElement = sliderObj.AddComponent<LayoutElement>();
        sliderLayoutElement.flexibleWidth = 1;
        var slider = sliderObj.AddComponent<Slider>();
        slider.minValue = minValue;
        slider.maxValue = maxValue;

        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(sliderObj.transform, false);
        var bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        var bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        var fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fillRect = fill.AddComponent<RectTransform>();
        fillRect.sizeDelta = Vector2.zero;
        var fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.3f, 0.6f, 0.9f, 1f);

        // Handle Area
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObj.transform, false);
        var handleAreaRect = handleArea.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.sizeDelta = Vector2.zero;

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        var handleRect = handle.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20, 0);
        var handleImage = handle.AddComponent<Image>();
        handleImage.color = Color.white;

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;

        // Value text
        GameObject valueObj = new GameObject("Value");
        valueObj.transform.SetParent(sliderContainer.transform, false);
        var valueRect = valueObj.AddComponent<RectTransform>();
        valueRect.sizeDelta = new Vector2(50, 0);
        var valueLayoutElement = valueObj.AddComponent<LayoutElement>();
        valueLayoutElement.minWidth = 50;
        valueLayoutElement.preferredWidth = 50;
        valueText = valueObj.AddComponent<TextMeshProUGUI>();
        valueText.fontSize = 12;
        valueText.color = Color.white;
        valueText.alignment = TextAlignmentOptions.Center;

        return slider;
    }

    Button CreateButton(Transform parent, string buttonText, Color color)
    {
        GameObject buttonObj = new GameObject(buttonText);
        buttonObj.transform.SetParent(parent, false);

        var rectTransform = buttonObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(0, 30);

        var layoutElement = buttonObj.AddComponent<LayoutElement>();
        layoutElement.minHeight = 30;
        layoutElement.preferredHeight = 30;

        var image = buttonObj.AddComponent<Image>();
        image.color = color;

        var button = buttonObj.AddComponent<Button>();

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = buttonText;
        text.fontSize = 14;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;

        return button;
    }
}

