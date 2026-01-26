using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Simple UI bubble that displays text above a model.
/// Attach this to a student model with an existing Canvas child (e.g., "studentbubbleCanvas").
/// The script will find and use the existing canvas and UI elements.
/// Bubble position and size are in world space and not affected by student's scale.
/// </summary>
public class StudentResponseBubble : MonoBehaviour
{
    [Header("UI Elements (Auto-created if not assigned)")]
    [Tooltip("Canvas for the response bubble")]
    public Canvas responseCanvas;

    [Tooltip("Text component displaying the response")]
    public TextMeshProUGUI responseText;

    [Tooltip("Image background for the bubble")]
    public Image bubbleBackground;

    [Header("Positioning")]
    [Tooltip("Offset above the model in world space. Not affected by student scale.")]
    public Vector3 headOffset = new Vector3(0, 2.0f, 0);

    [Tooltip("Camera to use for world-to-screen conversion (auto-finds Main Camera if not set)")]
    public Camera mainCamera;

    [Header("Sizing")]
    [Tooltip("Minimum width of the bubble")]
    public float minWidth = 12f;

    [Tooltip("Maximum width of the bubble")]
    public float maxWidth = 10f;

    [Tooltip("Padding around text")]
    public Vector2 textPadding = new Vector2(4f, 3f);

    [Tooltip("Width for eager/preview bubbles")]
    public float eagerBubbleWidth = 10f;

    [Tooltip("Max width for full answer bubbles")]
    public float answerBubbleMaxWidth = 10f;

    [Tooltip("Max height for full answer bubbles")]
    public float answerBubbleMaxHeight = 10f;

    [Header("Styling")]
    [Tooltip("Background color of the bubble")]
    public Color backgroundColor = new Color(1f, 1f, 1f, 0.95f);

    [Tooltip("Text color")]
    public Color textColor = new Color(0.2f, 0.2f, 0.2f, 1f);

    [Tooltip("Font size")]
    public int fontSize = 2;

    [Tooltip("Font size for eager/preview text")]
    public int eagerFontSize = 2;

    [Header("Auto-hide")]
    [Tooltip("Seconds after which the full response bubble is hidden")]
    public float responseHideDelay = 15f;

    [Header("Scale")]
    [Tooltip("Scale of the bubble in world space (0.2–0.5 = smaller, 1 = same as layout). Tune this to match your student size.")]
    [Range(0.1f, 2f)]
    public float bubbleScaleMultiplier = 0.35f;

    private string currentResponse = "";
    private RectTransform bubbleRect;
    private bool isAnswerMode = false; // Track if showing full answer or eager preview
    private Coroutine hideAfterDelayCoroutine;

    void Start()
    {
        // Find camera if not assigned
        if (mainCamera == null)
            mainCamera = Camera.main;

        // Setup canvas and UI elements (delayed to avoid interfering with spawner)
        SetupBubbleUI();

        // Hide initially
        HideBubble();
    }

    void Update()
    {
        if (responseCanvas == null || !responseCanvas.gameObject.activeSelf)
            return;

        // Update position to follow model's head
        UpdatePosition();
    }

    void LateUpdate()
    {
        if (responseCanvas == null || !responseCanvas.gameObject.activeSelf)
            return;

        UpdatePosition();
    }

    /// <summary>
    /// Setup the bubble UI elements - finds existing canvas and UI elements (no auto-creation)
    /// </summary>
    void SetupBubbleUI()
    {
        // Find existing canvas (no auto-creation)
        if (responseCanvas == null)
        {
            responseCanvas = GetComponentInChildren<Canvas>();
        }

        if (responseCanvas == null)
        {
            Debug.LogWarning("StudentResponseBubble: No Canvas found in children. Please assign responseCanvas manually or add a Canvas as a child.");
            return;
        }

        // Ensure canvas is set to World Space rendering mode
        if (responseCanvas.renderMode != RenderMode.WorldSpace)
        {
            responseCanvas.renderMode = RenderMode.WorldSpace;
            Debug.Log($"[StudentResponseBubble] Changed canvas render mode to WorldSpace for {gameObject.name}");
        }

        // Keep canvas unparented or parented to a non-scaled parent to avoid world scale effects
        // Canvas should not be parented to the student to avoid scale inheritance
        if (responseCanvas.transform.parent == transform)
        {
            responseCanvas.transform.SetParent(null, true); // Unparent but keep world position
            Debug.Log($"[StudentResponseBubble] Unparented canvas from student transform for {gameObject.name} to avoid scale inheritance");
        }

        // Find existing background
        if (bubbleBackground == null)
        {
            bubbleBackground = responseCanvas.GetComponentInChildren<Image>();
        }

        // Find existing text component
        if (responseText == null)
        {
            responseText = responseCanvas.GetComponentInChildren<TextMeshProUGUI>();
        }

        // Find existing RectTransform (could be on background or a panel)
        if (bubbleRect == null && bubbleBackground != null)
        {
            bubbleRect = bubbleBackground.GetComponent<RectTransform>();
        }
        else if (bubbleRect == null && responseText != null)
        {
            bubbleRect = responseText.GetComponent<RectTransform>();
        }
        else if (bubbleRect == null)
        {
            // Try to find any RectTransform in the canvas
            RectTransform[] rects = responseCanvas.GetComponentsInChildren<RectTransform>();
            if (rects.Length > 1) // More than just the canvas itself
            {
                bubbleRect = rects[1]; // Use first child RectTransform
            }
        }

        if (bubbleRect == null)
        {
            Debug.LogWarning("StudentResponseBubble: No RectTransform found for bubble. Please ensure the canvas has UI elements.");
        }
    }

    /// <summary>
    /// Update bubble position to follow model's head.
    /// Uses world space positioning and scale, not affected by student's scale.
    /// </summary>
    void UpdatePosition()
    {
        if (mainCamera == null || responseCanvas == null)
            return;

        // Ensure canvas is NOT parented to the student to avoid scale inheritance
        if (responseCanvas.transform.parent == transform)
        {
            responseCanvas.transform.SetParent(null, true); // Unparent but keep world position
        }

        // Calculate world position: use world space offset (not affected by student scale)
        Vector3 worldPosition = transform.position + headOffset;

        // Convert to screen space to check visibility
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPosition);

        // Check if model is visible
        if (screenPos.z < 0)
        {
            responseCanvas.gameObject.SetActive(false);
            return;
        }

        // Apply scale multiplier in world space (not affected by parent scale)
        float s = Mathf.Clamp(bubbleScaleMultiplier, 0.1f, 2f);
        responseCanvas.transform.localScale = new Vector3(s, s, s);

        // Position canvas above model using world position (not affected by student scale)
        responseCanvas.transform.position = worldPosition;
        
        // Make bubble face the camera
        if (mainCamera != null)
        {
            Vector3 bubbleWorldPos = responseCanvas.transform.position;
            Vector3 directionToCamera = mainCamera.transform.position - bubbleWorldPos;
            directionToCamera.y = 0; // Keep bubble upright, only rotate on Y axis
            if (directionToCamera != Vector3.zero)
            {
                responseCanvas.transform.rotation = Quaternion.LookRotation(-directionToCamera);
            }
        }
    }


    /// <summary>
    /// Show response text in the bubble (full answer mode).
    /// Auto-hides after responseHideDelay seconds.
    /// </summary>
    public void ShowResponse(string response)
    {
        if (string.IsNullOrEmpty(response))
        {
            HideBubble();
            return;
        }

        if (hideAfterDelayCoroutine != null)
        {
            StopCoroutine(hideAfterDelayCoroutine);
            hideAfterDelayCoroutine = null;
        }

        currentResponse = response;
        isAnswerMode = true;

        if (responseText != null)
        {
            responseText.text = response;
            responseText.fontSize = fontSize;

            // Auto-detect Hebrew and set RTL alignment
            bool isHebrew = ContainsHebrew(response);
            if (isHebrew)
            {
                responseText.isRightToLeftText = true;
                responseText.alignment = TextAlignmentOptions.Right;
            }
            else
            {
                responseText.isRightToLeftText = false;
                responseText.alignment = TextAlignmentOptions.Center;
            }
        }

        // Calculate size based on text
        if (responseText != null && bubbleRect != null)
        {
            // Force text to recalculate preferred size
            responseText.ForceMeshUpdate();

            float textWidth = responseText.preferredWidth;
            float textHeight = responseText.preferredHeight;

            // Set width (clamped to min/max for full answers)
            float width = Mathf.Clamp(textWidth + textPadding.x * 2, minWidth, answerBubbleMaxWidth);
            float height = Mathf.Clamp(textHeight + textPadding.y * 2, 16f, answerBubbleMaxHeight);

            bubbleRect.sizeDelta = new Vector2(width, height);

            // Log bubble size and response text
            string studentName = transform.name;
            Debug.Log($"[StudentResponseBubble] {studentName} - Bubble Size: {width:F2} x {height:F2} | Response Text: \"{response}\"");
        }

        // Show the bubble
        if (responseCanvas != null)
        {
            responseCanvas.gameObject.SetActive(true);
        }

        hideAfterDelayCoroutine = StartCoroutine(HideAfterDelayCoroutine());
    }

    private IEnumerator HideAfterDelayCoroutine()
    {
        yield return new WaitForSeconds(responseHideDelay);
        hideAfterDelayCoroutine = null;
        HideBubble();
    }

    /// <summary>
    /// Show eager/preview bubble (small, for "I know!" type messages).
    /// </summary>
    public void ShowEagerBubble(string previewText)
    {
        if (string.IsNullOrEmpty(previewText))
        {
            HideBubble();
            return;
        }

        currentResponse = previewText;
        isAnswerMode = false;

        if (responseText != null)
        {
            responseText.text = previewText;
            responseText.fontSize = eagerFontSize;

            // Auto-detect Hebrew and set RTL alignment
            bool isHebrew = ContainsHebrew(previewText);
            if (isHebrew)
            {
                responseText.isRightToLeftText = true;
                responseText.alignment = TextAlignmentOptions.Center;
            }
            else
            {
                responseText.isRightToLeftText = false;
                responseText.alignment = TextAlignmentOptions.Center;
            }
        }

        // Use smaller, fixed size for eager bubbles
        if (bubbleRect != null)
        {
            // Force text to recalculate preferred size
            responseText.ForceMeshUpdate();

            float textWidth = responseText.preferredWidth;
            float textHeight = responseText.preferredHeight;

            // Smaller, more compact bubble for eager text
            float width = Mathf.Max(eagerBubbleWidth, textWidth + textPadding.x * 2);
            float height = Mathf.Max(12f, textHeight + textPadding.y);

            bubbleRect.sizeDelta = new Vector2(width, height);
        }

        // Show the bubble
        if (responseCanvas != null)
        {
            responseCanvas.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Hide the response bubble.
    /// </summary>
    public void HideBubble()
    {
        if (hideAfterDelayCoroutine != null)
        {
            StopCoroutine(hideAfterDelayCoroutine);
            hideAfterDelayCoroutine = null;
        }

        if (responseCanvas != null)
        {
            responseCanvas.gameObject.SetActive(false);
        }

        currentResponse = "";
        isAnswerMode = false;
    }

    /// <summary>
    /// Check if bubble is currently showing
    /// </summary>
    public bool IsShowing()
    {
        return responseCanvas != null && responseCanvas.gameObject.activeSelf && !string.IsNullOrEmpty(currentResponse);
    }

    /// <summary>
    /// Check if bubble is in answer mode (showing full answer) or eager mode (preview text)
    /// </summary>
    public bool IsAnswerMode()
    {
        return isAnswerMode;
    }

    /// <summary>
    /// Check if text contains Hebrew characters
    /// </summary>
    private bool ContainsHebrew(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;
            
        foreach (char c in text)
        {
            if (c >= 0x0590 && c <= 0x05FF) // Hebrew Unicode range
                return true;
        }
        return false;
    }

    /// <summary>
    /// Recursively set layer for all children
    /// </summary>
    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}