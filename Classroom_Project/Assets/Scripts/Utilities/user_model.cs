using System;

[Serializable]
public class UserModel
{
    // In WebGL/Unity we just store the string id we receive from the backend (Mongo ObjectId as string)
    public string id;

    // Login credentials (still hashed on the backend!)
    public string username;
    public string passwordHash;

    // User information
    public string email;
    public string fullName;

    // Role determines what user can do
    public UserRole role = UserRole.Student;

    // Tracking (store as ISO string if you want to be super safe across JSON libs)
    // If your backend sends real ISO strings, keep these as string.
    // If you control both sides and parse manually, you can change to DateTime.
    public string createdAt;
    public string lastLogin;

    public int sessionCount = 0;

    // Permissions
    // Empty or null => allow all scenarios
    public string[] allowedScenarios;

    public bool isActive = true;

    public UserModel()
    {
        // For new local instances (not coming from backend)
        createdAt = DateTime.UtcNow.ToString("o"); // ISO-8601
        lastLogin = null;
        isActive = true;
        sessionCount = 0;
        role = UserRole.Student;

        // Empty means all scenarios allowed (as you intended)
        allowedScenarios = Array.Empty<string>();
    }

    public bool CanAccessScenario(string scenarioName)
    {
        if (string.IsNullOrWhiteSpace(scenarioName))
            return false;

        // If allowedScenarios is empty or null, user can access all scenarios
        if (allowedScenarios == null || allowedScenarios.Length == 0)
            return true;

        for (int i = 0; i < allowedScenarios.Length; i++)
        {
            var allowed = allowedScenarios[i];
            if (!string.IsNullOrEmpty(allowed) &&
                allowed.Equals(scenarioName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}

public enum UserRole
{
    Student,        // Regular trainee teacher
    Instructor,     // Can view all student sessions
    Administrator   // Can create users and manage system
}

[Serializable]
public class LoginRequest
{
    public string username;
    public string password;
}

[Serializable]
public class LoginResponse
{
    public bool success;
    public string message;
    public UserModel user;
    public string sessionToken; // For maintaining login state
}
