using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Text.RegularExpressions;

/// <summary>
/// Handles user registration interface.
/// Validates input and creates new user accounts in MongoDB.
/// </summary>
public class RegistrationUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Main registration panel")]
    public GameObject registrationPanel;
    
    [Tooltip("Input fields")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TMP_InputField confirmPasswordInput;
    public TMP_InputField emailInput;
    public TMP_InputField fullNameInput;
    
    [Tooltip("Role selection dropdown")]
    public TMP_Dropdown roleDropdown;
    
    [Tooltip("Buttons")]
    public Button registerButton;
    public Button backToLoginButton;
    
    [Tooltip("Message display")]
    public TextMeshProUGUI messageText;
    
    [Tooltip("Loading indicator")]
    public GameObject loadingIndicator;

    [Header("Scene References")]
    [Tooltip("Login panel to return to")]
    public GameObject loginPanel;

    [Header("Visual Settings")]
    public Color errorColor = Color.red;
    public Color successColor = Color.green;
    public Color warningColor = Color.yellow;
    public Color normalColor = Color.white;

    // References
    private AuthenticationManager authManager;
    private bool isProcessingRegistration = false;

    void Start()
    {
        // Find authentication manager
        authManager = AuthenticationManager.Instance;
        
        if (authManager == null)
        {
            Debug.LogError("AuthenticationManager not found!");
            return;
        }

        // Set up buttons
        if (registerButton != null)
            registerButton.onClick.AddListener(OnRegisterButtonClicked);
        
        if (backToLoginButton != null)
            backToLoginButton.onClick.AddListener(OnBackToLoginClicked);

        // Setup role dropdown
        SetupRoleDropdown();

        // Hide loading indicator
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);

        // Don't force panel state in Start - let it be controlled by the editor initial state
        // if (registrationPanel != null)
        //     registrationPanel.SetActive(false);

        // Clear message
        ShowMessage("", normalColor);
    }

    /// <summary>
    /// Setup the role dropdown with available roles
    /// </summary>
    void SetupRoleDropdown()
    {
        if (roleDropdown != null)
        {
            roleDropdown.ClearOptions();
            
            // Add role options
            roleDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "סטודנט - מורה מתמחה",
                "מדריך - מפקח",
                "מנהל מערכת"
            });
            
            // Default to Student
            roleDropdown.value = 0;
        }
    }

    /// <summary>
    /// Show registration panel
    /// </summary>
    public void ShowRegistrationPanel()
    {
        if (registrationPanel != null)
            registrationPanel.SetActive(true);
        
        if (loginPanel != null)
            loginPanel.SetActive(false);

        // Clear all fields
        ClearFields();
        ShowMessage("צור חשבון חדש", normalColor);
    }

    /// <summary>
    /// Hide registration panel and return to login
    /// </summary>
    void OnBackToLoginClicked()
    {
        if (registrationPanel != null)
            registrationPanel.SetActive(false);
        
        if (loginPanel != null)
            loginPanel.SetActive(true);

        ClearFields();
    }

    /// <summary>
    /// Clear all input fields
    /// </summary>
    void ClearFields()
    {
        if (usernameInput != null) usernameInput.text = "";
        if (passwordInput != null) passwordInput.text = "";
        if (confirmPasswordInput != null) confirmPasswordInput.text = "";
        if (emailInput != null) emailInput.text = "";
        if (fullNameInput != null) fullNameInput.text = "";
        if (roleDropdown != null) roleDropdown.value = 0;
    }

    /// <summary>
    /// Called when register button is clicked
    /// </summary>
    void OnRegisterButtonClicked()
    {
        if (isProcessingRegistration)
        {
            Debug.Log("Registration already in progress...");
            return;
        }

        // Get input values
        string username = usernameInput.text.Trim();
        string password = passwordInput.text;
        string confirmPassword = confirmPasswordInput.text;
        string email = emailInput.text.Trim();
        string fullName = fullNameInput.text.Trim();
        UserRole role = GetSelectedRole();

        // Validate input
        string validationError = ValidateInput(username, password, confirmPassword, email, fullName);
        
        if (!string.IsNullOrEmpty(validationError))
        {
            ShowMessage(validationError, errorColor);
            return;
        }

        // Start registration process
        ProcessRegistration(username, password, email, fullName, role);
    }

    /// <summary>
    /// Validate all registration input
    /// </summary>
    string ValidateInput(string username, string password, string confirmPassword, string email, string fullName)
    {
        // Username validation
        if (string.IsNullOrEmpty(username))
            return "שם משתמש נדרש";
        
        if (username.Length < 3)
            return "שם משתמש חייב להכיל לפחות 3 תווים";
        
        if (username.Length > 20)
            return "שם משתמש חייב להכיל פחות מ-20 תווים";
        
        if (!Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$"))
            return "שם משתמש יכול להכיל רק אותיות, מספרים וקו תחתון";

        // Password validation
        if (string.IsNullOrEmpty(password))
            return "סיסמה נדרשת";
        
        if (password.Length < 6)
            return "סיסמה חייבת להכיל לפחות 6 תווים";
        
        if (password.Length > 50)
            return "סיסמה חייבת להכיל פחות מ-50 תווים";

        // Confirm password
        if (password != confirmPassword)
            return "הסיסמאות אינן תואמות";

        // Email validation
        if (string.IsNullOrEmpty(email))
            return "כתובת אימייל נדרשת";
        
        if (!IsValidEmail(email))
            return "אנא הזן כתובת אימייל תקינה";

        // Full name validation
        if (string.IsNullOrEmpty(fullName))
            return "שם מלא נדרש";
        
        if (fullName.Length < 2)
            return "שם מלא חייב להכיל לפחות 2 תווים";

        // All validation passed
        return null;
    }

    /// <summary>
    /// Validate email format
    /// </summary>
    bool IsValidEmail(string email)
    {
        string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        return Regex.IsMatch(email, pattern);
    }

    /// <summary>
    /// Get selected role from dropdown
    /// </summary>
    UserRole GetSelectedRole()
    {
        if (roleDropdown == null)
            return UserRole.Student;

        switch (roleDropdown.value)
        {
            case 0: return UserRole.Student;
            case 1: return UserRole.Instructor;
            case 2: return UserRole.Administrator;
            default: return UserRole.Student;
        }
    }

    /// <summary>
    /// Process registration using coroutine
    /// </summary>
    void ProcessRegistration(string username, string password, string email, string fullName, UserRole role)
    {
        StartCoroutine(ProcessRegistrationCoroutine(username, password, email, fullName, role));
    }

    IEnumerator ProcessRegistrationCoroutine(string username, string password, string email, string fullName, UserRole role)
    {
        isProcessingRegistration = true;

        // Show loading state
        ShowMessage("יוצר חשבון...", normalColor);
        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);
        if (registerButton != null)
            registerButton.interactable = false;

        bool registrationComplete = false;
        bool registrationSuccess = false;
        string errorMessage = "";

        // Call authentication manager to register user
        yield return authManager.RegisterCoroutine(
            username,
            password,
            email,
            fullName,
            role,
            (response) => {
                registrationComplete = true;
                registrationSuccess = true;
            },
            (error) => {
                registrationComplete = true;
                registrationSuccess = false;
                errorMessage = error;
            }
        );

        // Wait for completion
        yield return new WaitUntil(() => registrationComplete);

        if (registrationSuccess)
        {
            OnRegistrationSuccess(username, password);
        }
        else
        {
            OnRegistrationFailure(string.IsNullOrEmpty(errorMessage) ? "ההרשמה נכשלה. אנא נסה שוב." : errorMessage);
        }

        isProcessingRegistration = false;
        
        // Hide loading state
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
        if (registerButton != null)
            registerButton.interactable = true;
    }

    /// <summary>
    /// Handle successful registration
    /// </summary>
    void OnRegistrationSuccess(string username, string password)
    {
        Debug.Log($"Registration successful for: {username}");
        
        ShowMessage($"✓ החשבון נוצר בהצלחה!\nברוך הבא, {fullNameInput.text}!", successColor);

        OnBackToLoginClicked();
    }

    /// <summary>
    /// Handle registration failure
    /// </summary>
    void OnRegistrationFailure(string message)
    {
        Debug.Log($"Registration failed: {message}");
        ShowMessage(message, errorColor);
    }

    /// <summary>
    /// Transition to scenario selection screen
    /// </summary>
    void TransitionToScenarioSelection()
    {
        // Find scenario selection UI
        ScenarioSelectionUI scenarioUI = FindObjectOfType<ScenarioSelectionUI>();
        
        if (scenarioUI != null && scenarioUI.scenarioSelectionPanel != null)
        {
            scenarioUI.scenarioSelectionPanel.SetActive(true);
            scenarioUI.RefreshScenarioList();
        }
    }

    /// <summary>
    /// Display a message to the user
    /// </summary>
    void ShowMessage(string message, Color color)
    {
        if (messageText != null)
        {
            messageText.text = message;
            messageText.color = color;
        }
    }

    /// <summary>
    /// Show password strength indicator (optional enhancement)
    /// </summary>
    public void OnPasswordChanged()
    {
        if (passwordInput == null) return;

        string password = passwordInput.text;
        
        if (string.IsNullOrEmpty(password))
        {
            // No feedback if empty
            return;
        }

        // Calculate password strength
        int strength = CalculatePasswordStrength(password);
        
        // Show strength indicator
        string strengthText = "";
        Color strengthColor = normalColor;

        if (strength < 2)
        {
            strengthText = "סיסמה חלשה";
            strengthColor = errorColor;
        }
        else if (strength < 4)
        {
            strengthText = "סיסמה בינונית";
            strengthColor = warningColor;
        }
        else
        {
            strengthText = "סיסמה חזקה";
            strengthColor = successColor;
        }

        // You could show this in a separate text field if desired
        Debug.Log($"Password strength: {strengthText}");
    }

    /// <summary>
    /// Calculate password strength (0-5 scale)
    /// </summary>
    int CalculatePasswordStrength(string password)
    {
        int strength = 0;

        if (password.Length >= 8) strength++;
        if (password.Length >= 12) strength++;
        if (Regex.IsMatch(password, @"[a-z]")) strength++; // Lowercase
        if (Regex.IsMatch(password, @"[A-Z]")) strength++; // Uppercase
        if (Regex.IsMatch(password, @"\d")) strength++;     // Numbers
        if (Regex.IsMatch(password, @"[^a-zA-Z0-9]")) strength++; // Special chars

        return Mathf.Min(strength, 5);
    }
}