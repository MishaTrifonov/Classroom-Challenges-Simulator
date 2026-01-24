using UnityEngine;
using TMPro;

/// <summary>
/// Handles visual feedback for student emotional states.
/// Shows name tags, emotional color indicators, and state icons.
/// Attach this to each student avatar prefab.
/// </summary>
public class StudentVisualFeedback : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The StudentAgent this visual feedback is for")]
    public StudentAgent studentAgent;
    
    [Header("Visual Elements")]
    [Tooltip("The main body renderer (capsule/character model)")]
    public Renderer bodyRenderer;
    
    [Tooltip("Small sphere above head showing emotion color")]
    public GameObject emotionalIndicator;
    public Renderer emotionalIndicatorRenderer;
    
    [Tooltip("Name tag canvas (world space)")]
    public Canvas nameTagCanvas;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI stateText;
    
    [Header("Color Coding")]
    public Color happyColor = new Color(0.3f, 1f, 0.3f);      // Green
    public Color sadColor = new Color(0.3f, 0.5f, 1f);         // Blue
    public Color angryColor = new Color(1f, 0.3f, 0.3f);       // Red
    public Color boredColor = new Color(0.6f, 0.6f, 0.6f);     // Gray
    public Color frustratedColor = new Color(1f, 0.7f, 0.3f);  // Orange
    public Color neutralColor = Color.white;
    
    [Header("Animation")]
    public float colorTransitionSpeed = 2f;
    public float bobSpeed = 1f;
    public float bobHeight = 0.1f;
    
    [Header("Selection")]
    public bool isSelected = false;
    public Color selectionColor = Color.blue;
    public GameObject selectionRing;

    [Header("Seat Swap Selection")]
    public bool isSwapSelected = false;
    public Color swapSelectionColor = Color.red;
    
    private Color currentBodyColor;
    private Color targetBodyColor;
    private Color originalBodyColor;
    private float bobTimer = 0f;

    void Start()
    {
        // Auto-find references if not assigned
        if (studentAgent == null)
            studentAgent = GetComponentInParent<StudentAgent>();

        
        if (bodyRenderer == null)
            bodyRenderer = GetComponentInChildren<Renderer>();
        
        if (bodyRenderer != null)
            originalBodyColor = bodyRenderer.material.color;
        
        // Initialize name tag
        UpdateNameTag();
        
        // Set random bob offset so students don't all bob in sync
        bobTimer = Random.Range(0f, Mathf.PI * 2f);
        
        // Hide selection ring initially
        if (selectionRing != null)
            selectionRing.SetActive(false);
    }

    void Update()
    {
        if (studentAgent == null) return;

        // Update visual indicators based on emotional state
        UpdateEmotionalColor();
        UpdateEmotionalIndicator();
        UpdateStateDisplay();
        UpdateBobbing();
        
        // Make name tag face camera
        if (nameTagCanvas != null)
        {
            nameTagCanvas.transform.LookAt(Camera.main.transform);
            nameTagCanvas.transform.Rotate(0, 180, 0); // Flip to face camera correctly
        }
    }

    /// <summary>
    /// Determine what color the student should be based on dominant emotion
    /// </summary>
    void UpdateEmotionalColor()
    {
        EmotionVector emotions = studentAgent.emotions;
        
        // Find dominant emotion
        float maxValue = Mathf.Max(
            emotions.Happiness,
            emotions.Sadness,
            emotions.Anger,
            emotions.Boredom,
            emotions.Frustration
        );

        // Set target color based on dominant emotion
        if (maxValue < 5f)
        {
            targetBodyColor = neutralColor;
        }
        else if (emotions.Happiness == maxValue)
        {
            targetBodyColor = happyColor;
        }
        else if (emotions.Sadness == maxValue)
        {
            targetBodyColor = sadColor;
        }
        else if (emotions.Anger == maxValue)
        {
            targetBodyColor = angryColor;
        }
        else if (emotions.Boredom == maxValue)
        {
            targetBodyColor = boredColor;
        }
        else if (emotions.Frustration == maxValue)
        {
            targetBodyColor = frustratedColor;
        }

        // Apply selection tint if selected
        // Swap selection (red) takes priority over normal selection (blue)
        if (isSwapSelected)
        {
            targetBodyColor = Color.Lerp(targetBodyColor, swapSelectionColor, 0.5f);
        }
        else if (isSelected)
        {
            targetBodyColor = Color.Lerp(targetBodyColor, selectionColor, 0.5f);
        }

        // Smoothly transition to target color
        currentBodyColor = Color.Lerp(currentBodyColor, targetBodyColor, Time.deltaTime * colorTransitionSpeed);
        
        if (bodyRenderer != null)
        {
            bodyRenderer.material.color = currentBodyColor;
        }
    }

    /// <summary>
    /// Update the floating emotional indicator sphere
    /// </summary>
    void UpdateEmotionalIndicator()
    {
        if (emotionalIndicatorRenderer == null) return;

        EmotionVector emotions = studentAgent.emotions;
        
        // Blend colors based on emotional intensity
        Color indicatorColor = Color.black;
        float totalIntensity = 0f;

        // Add each emotion's contribution
        if (emotions.Happiness > 5f)
        {
            float weight = (emotions.Happiness - 5f) / 5f;
            indicatorColor += happyColor * weight;
            totalIntensity += weight;
        }
        if (emotions.Sadness > 5f)
        {
            float weight = (emotions.Sadness - 5f) / 5f;
            indicatorColor += sadColor * weight;
            totalIntensity += weight;
        }
        if (emotions.Anger > 5f)
        {
            float weight = (emotions.Anger - 5f) / 5f;
            indicatorColor += angryColor * weight;
            totalIntensity += weight;
        }
        if (emotions.Boredom > 5f)
        {
            float weight = (emotions.Boredom - 5f) / 5f;
            indicatorColor += boredColor * weight;
            totalIntensity += weight;
        }
        if (emotions.Frustration > 5f)
        {
            float weight = (emotions.Frustration - 5f) / 5f;
            indicatorColor += frustratedColor * weight;
            totalIntensity += weight;
        }

        // Normalize if multiple emotions are active
        if (totalIntensity > 0)
            indicatorColor /= totalIntensity;
        else
            indicatorColor = neutralColor;

        emotionalIndicatorRenderer.material.color = indicatorColor;
        
        // Make indicator glow if emotions are intense
        if (totalIntensity > 1.5f)
        {
            emotionalIndicatorRenderer.material.EnableKeyword("_EMISSION");
            emotionalIndicatorRenderer.material.SetColor("_EmissionColor", indicatorColor * 0.5f);
        }
    }

    /// <summary>
    /// Update name tag text with current state
    /// </summary>
    void UpdateNameTag()
    {
        if (nameText != null && studentAgent != null)
        {
            nameText.text = studentAgent.studentName;
        }
    }

    /// <summary>
    /// Update state text display
    /// </summary>
    void UpdateStateDisplay()
    {
        if (stateText != null && studentAgent != null)
        {
            // Show current behavioral state with an emoji/icon
            string stateIcon = GetStateIcon(studentAgent.currentState);
            string stateHebrew = GetStateHebrew(studentAgent.currentState);
            stateText.text = $"{stateIcon} {stateHebrew}";
            
            // Color code state text
            stateText.color = GetStateColor(studentAgent.currentState);
        }
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
    /// Get emoji/icon for each state
    /// </summary>
    string GetStateIcon(StudentState state)
    {
        switch (state)
        {
            case StudentState.Listening: return "👂";
            case StudentState.Engaged: return "✋";
            case StudentState.Distracted: return "💭";
            case StudentState.SideTalk: return "💬";
            case StudentState.Arguing: return "😠";
            case StudentState.Withdrawn: return "😶";
            default: return "•";
        }
    }

    /// <summary>
    /// Get color for each state
    /// </summary>
    Color GetStateColor(StudentState state)
    {
        switch (state)
        {
            case StudentState.Listening: return Color.green;
            case StudentState.Engaged: return Color.cyan;
            case StudentState.Distracted: return Color.yellow;
            case StudentState.SideTalk: return new Color(1f, 0.7f, 0.3f); // Orange
            case StudentState.Arguing: return Color.red;
            case StudentState.Withdrawn: return Color.gray;
            default: return Color.white;
        }
    }

    /// <summary>
    /// Gentle bobbing animation
    /// </summary>
    void UpdateBobbing()
    {
        if (emotionalIndicator == null) return;

        bobTimer += Time.deltaTime * bobSpeed;
        float yOffset = Mathf.Sin(bobTimer) * bobHeight;
        
        // Apply bobbing to emotional indicator
        Vector3 pos = emotionalIndicator.transform.localPosition;
        pos.y = 2.5f + yOffset; // 2.5 is base height above student
        emotionalIndicator.transform.localPosition = pos;
    }

    /// <summary>
    /// Show selection visual
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (selectionRing != null)
            selectionRing.SetActive(selected || isSwapSelected);
    }

    /// <summary>
    /// Show swap selection visual (red outline)
    /// </summary>
    public void SetSwapSelected(bool selected)
    {
        isSwapSelected = selected;

        if (selectionRing != null)
            selectionRing.SetActive(selected || isSelected);
    }

    /// <summary>
    /// Flash briefly (useful for feedback)
    /// </summary>
    public void Flash(Color flashColor, float duration = 0.5f)
    {
        StartCoroutine(FlashCoroutine(flashColor, duration));
    }

    System.Collections.IEnumerator FlashCoroutine(Color flashColor, float duration)
    {
        Color original = bodyRenderer.material.color;
        
        // Flash to color
        bodyRenderer.material.color = flashColor;
        yield return new WaitForSeconds(duration);
        
        // Return to original
        bodyRenderer.material.color = original;
    }
}