using UnityEngine;
using System.Collections;

/// <summary>
/// Simple camera controller for navigating the classroom view.
/// - WASD or Arrow Keys: Move camera
/// - Mouse Right-Click + Drag: Rotate view
/// - Mouse Scroll: Zoom in/out
/// - Q/E: Move up/down
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("How fast the camera moves")]
    public float moveSpeed = 10f;
    
    [Tooltip("How fast the camera moves when shift is held")]
    public float fastMoveSpeed = 20f;
    
    [Header("Rotation Settings")]
    [Tooltip("How sensitive mouse rotation is")]
    public float rotationSpeed = 3f;
    
    [Header("Zoom Settings")]
    [Tooltip("How fast scrolling zooms")]
    public float zoomSpeed = 5f;
    
    [Tooltip("Closest zoom level")]
    public float minZoom = 5f;
    
    [Tooltip("Farthest zoom level")]
    public float maxZoom = 50f;
    
    [Header("Bounds (Optional)")]
    [Tooltip("Enable to restrict camera movement")]
    public bool useBounds = true;
    
    public Vector3 minBounds = new Vector3(-20, 5, -20);
    public Vector3 maxBounds = new Vector3(20, 30, 20);
    
    [Header("Auto Focus Settings")]
    [Tooltip("Enable automatic camera focus on students who want to answer")]
    public bool autoFocusOnEagerStudents = true;
    
    [Tooltip("How fast the camera moves when focusing on a student")]
    public float focusSpeed = 5f;
    
    [Tooltip("Distance from student when focusing")]
    public float focusDistance = 8f;
    
    [Tooltip("Height offset when focusing on student")]
    public float focusHeight = 2f;
    
    // Internal state
    private Vector3 lastMousePosition;
    private bool isRotating = false;
    private float currentZoom = 15f;
    private bool isFocusing = false;
    private Coroutine focusCoroutine;

    void Start()
    {
        // Set initial zoom, but clamp it to valid range
        currentZoom = Mathf.Clamp(transform.position.y, minZoom, maxZoom);
        
        // If camera is outside bounds and bounds are enabled, adjust position
        if (useBounds)
        {
            Vector3 pos = transform.position;
            bool needsAdjustment = false;
            
            if (pos.y < minBounds.y)
            {
                pos.y = minBounds.y;
                currentZoom = minBounds.y;
                needsAdjustment = true;
            }
            else if (pos.y > maxBounds.y)
            {
                pos.y = maxBounds.y;
                currentZoom = maxBounds.y;
                needsAdjustment = true;
            }
            
            if (needsAdjustment)
            {
                transform.position = pos;
            }
        }
    }

    void Update()
    {
        // Only allow manual control if not currently focusing
        if (!isFocusing)
        {
            HandleMovement();
            HandleRotation();
            HandleZoom();
        }
    }

    /// <summary>
    /// Handle keyboard movement (WASD, Arrow Keys, Q/E)
    /// </summary>
    void HandleMovement()
    {
        // Get input axes
        float horizontal = Input.GetAxis("Horizontal"); // A/D or Left/Right arrows
        float vertical = Input.GetAxis("Vertical");     // W/S or Up/Down arrows
        
        // Q/E for up/down movement
        float upDown = 0f;
        if (Input.GetKey(KeyCode.E)) upDown = 1f;
        if (Input.GetKey(KeyCode.Q)) upDown = -1f;

        // Check if shift is held for faster movement
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) 
            ? fastMoveSpeed 
            : moveSpeed;

        // Calculate movement direction relative to camera's current rotation
        Vector3 forward = transform.forward;
        forward.y = 0; // Keep movement horizontal
        forward.Normalize();
        
        Vector3 right = transform.right;
        right.y = 0;
        right.Normalize();

        // Combine all movement
        Vector3 movement = (forward * vertical + right * horizontal + Vector3.up * upDown) 
                          * currentSpeed * Time.deltaTime;

        // Apply movement
        transform.position += movement;

        // Apply bounds if enabled
        if (useBounds)
        {
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, minBounds.x, maxBounds.x);
            pos.y = Mathf.Clamp(pos.y, minBounds.y, maxBounds.y);
            pos.z = Mathf.Clamp(pos.z, minBounds.z, maxBounds.z);
            transform.position = pos;
        }
    }

    /// <summary>
    /// Handle mouse rotation (Right-click and drag)
    /// </summary>
    void HandleRotation()
    {
        // Start rotation on right mouse button press
        if (Input.GetMouseButtonDown(1)) // Right mouse button
        {
            isRotating = true;
            lastMousePosition = Input.mousePosition;
        }
        
        // Stop rotation on release
        if (Input.GetMouseButtonUp(1))
        {
            isRotating = false;
        }

        // Rotate camera while right mouse button is held
        if (isRotating)
        {
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
            
            // Rotate horizontally (yaw)
            transform.Rotate(Vector3.up, mouseDelta.x * rotationSpeed * Time.deltaTime, Space.World);
            
            // Rotate vertically (pitch)
            transform.Rotate(Vector3.right, -mouseDelta.y * rotationSpeed * Time.deltaTime, Space.Self);
            
            // Prevent camera from flipping upside down
            Vector3 euler = transform.eulerAngles;
            if (euler.x > 80f && euler.x < 180f) euler.x = 80f;
            if (euler.x < 280f && euler.x > 180f) euler.x = 280f;
            transform.eulerAngles = euler;
            
            lastMousePosition = Input.mousePosition;
        }
    }

    /// <summary>
    /// Handle mouse scroll wheel zoom
    /// </summary>
    void HandleZoom()
    {
        // Get scroll input
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            // Adjust zoom based on scroll direction
            currentZoom -= scrollInput * zoomSpeed;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
            
            // Apply zoom by moving camera up/down
            Vector3 pos = transform.position;
            pos.y = currentZoom;
            transform.position = pos;
        }
    }

    /// <summary>
    /// Focus camera on a specific position (useful for centering on students)
    /// </summary>
    public void FocusOn(Vector3 target, float distance = 10f)
    {
        // Position camera at distance from target
        Vector3 offset = transform.forward * -distance;
        transform.position = target + offset;
    }

    /// <summary>
    /// Smoothly focus camera on a student transform
    /// </summary>
    public void FocusOnStudent(Transform studentTransform, System.Action onComplete = null)
    {
        if (studentTransform == null)
            return;

        // Stop any existing focus
        if (focusCoroutine != null)
        {
            StopCoroutine(focusCoroutine);
        }

        focusCoroutine = StartCoroutine(FocusOnStudentCoroutine(studentTransform, onComplete));
    }

    /// <summary>
    /// Coroutine to smoothly move camera to focus on a student
    /// </summary>
    private IEnumerator FocusOnStudentCoroutine(Transform studentTransform, System.Action onComplete)
    {
        isFocusing = true;
        
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        
        // Calculate target position: offset from student at a good viewing angle
        Vector3 studentPosition = studentTransform.position;
        Vector3 targetPosition = studentPosition + Vector3.up * focusHeight + Vector3.back * focusDistance;
        
        // Calculate target rotation: look at the student
        Vector3 directionToStudent = (studentPosition - targetPosition).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(directionToStudent);
        
        float elapsed = 0f;
        float duration = Vector3.Distance(startPosition, targetPosition) / focusSpeed;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            
            // Update current zoom to match Y position
            currentZoom = transform.position.y;
            
            yield return null;
        }
        
        // Ensure we're exactly at target
        transform.position = targetPosition;
        transform.rotation = targetRotation;
        currentZoom = targetPosition.y;
        
        isFocusing = false;
        
        // Call completion callback if provided
        onComplete?.Invoke();
    }

    /// <summary>
    /// Reset camera to default position
    /// </summary>
    public void ResetCamera()
    {
        // Stop any active focus
        if (focusCoroutine != null)
        {
            StopCoroutine(focusCoroutine);
            focusCoroutine = null;
            isFocusing = false;
        }
        
        transform.position = new Vector3(0, 15, -10);
        transform.rotation = Quaternion.Euler(30, 0, 0);
        currentZoom = 15f;
    }
    
    /// <summary>
    /// Stop focusing and return to manual control
    /// </summary>
    public void StopFocusing()
    {
        if (focusCoroutine != null)
        {
            StopCoroutine(focusCoroutine);
            focusCoroutine = null;
        }
        isFocusing = false;
    }

    // Draw bounds in editor for visualization
    void OnDrawGizmosSelected()
    {
        if (useBounds)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(
                (minBounds + maxBounds) / 2f,
                maxBounds - minBounds
            );
        }
    }
}