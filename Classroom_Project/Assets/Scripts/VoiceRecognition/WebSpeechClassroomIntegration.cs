using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Processes voice commands from Web Speech API and triggers classroom actions.
/// This connects the voice recognition to your existing classroom management system.
/// </summary>
public class WebSpeechClassroomIntegration : MonoBehaviour
{
    [Header("References")]
    public WebSpeechRecognition webSpeech;
    public ClassroomManager classroomManager;
    public TeacherUI teacherUI;
    public StudentAIResponseGenerator aiResponseGenerator;

    [Header("Settings")]
    public bool logCommands = true;
    public bool enableQuestionDetection = true; // Enable students to respond to questions
    [Tooltip("Auto-start voice recognition when scene loads (requires user permission)")]
    public bool autoStartVoiceRecognition = false;

    // Command mappings
    private Dictionary<string[], System.Action> commandActions;

    void Start()
    {
        // Find references if not assigned
        if (webSpeech == null)
            webSpeech = FindObjectOfType<WebSpeechRecognition>();

        if (classroomManager == null)
            classroomManager = FindObjectOfType<ClassroomManager>();

        if (teacherUI == null)
            teacherUI = FindObjectOfType<TeacherUI>();

        if (aiResponseGenerator == null)
            aiResponseGenerator = FindObjectOfType<StudentAIResponseGenerator>();

        // Subscribe to transcription events
        if (webSpeech != null)
        {
            webSpeech.OnTranscriptionReceived += ProcessVoiceCommand;
        }
        else
        {
            Debug.LogError("WebSpeechRecognition not found!");
        }

        // Initialize command mappings
        InitializeCommands();
        
        // Auto-start voice recognition if enabled
        if (autoStartVoiceRecognition && webSpeech != null)
        {
            // Delay slightly to ensure everything is initialized
            Invoke(nameof(StartVoiceRecognition), 1f);
        }
    }
    
    /// <summary>
    /// Start voice recognition (can be called manually or auto-started)
    /// </summary>
    public void StartVoiceRecognition()
    {
        if (webSpeech != null && !webSpeech.IsRecording())
        {
            webSpeech.StartRecording();
            if (logCommands)
                Debug.Log("[VoiceCommand] Voice recognition started automatically");
        }
    }

    void OnDestroy()
    {
        if (webSpeech != null)
        {
            webSpeech.OnTranscriptionReceived -= ProcessVoiceCommand;
        }
    }

    void InitializeCommands()
    {
        commandActions = new Dictionary<string[], System.Action>
        {
            // English Praise commands
            { new[] { "good job", "well done", "excellent", "great work", "nice" }, 
              () => ExecuteAction(ActionType.Praise) },

            // Hebrew praise commands
            { new[] { "כל הכבוד", "יפה מאוד", "מצוין" }, 
              () => ExecuteAction(ActionType.Praise) },

            // English Discipline commands
            { new[] { "quiet", "silence", "stop talking", "be quiet", "shh", "settle down" }, 
              () => ExecuteAction(ActionType.Yell) },

            // Hebrew discipline commands
            { new[] { "שקט", "תשקטו", "די" }, 
              () => ExecuteAction(ActionType.Yell) },

            // English Instruction commands
            { new[] { "board", "come to board", "go to board", "write on board" }, 
              () => ExecuteAction(ActionType.CallToBoard) },

            // Hebrew instruction commands
            { new[] { "בוא ללוח" }, 
              () => ExecuteAction(ActionType.CallToBoard) },

            // Hebrew break command
            { new[] { "הפסקה" }, 
              () => ExecuteAction(ActionType.GiveBreak) },

            // Hebrew book command
            { new[] { "פתחו ספרים", "ספרים" }, 
              () => ExecuteBagItem(BagItemType.Book) },

            { new[] { "change seat", "move", "switch seats", "new seat" }, 
              () => ExecuteAction(ActionType.ChangeSeating) },

            { new[] { "break", "take a break", "rest", "relax", "five minutes" }, 
              () => ExecuteAction(ActionType.GiveBreak) },

            { new[] { "leave", "go outside", "time out", "step out" }, 
              () => ExecuteAction(ActionType.RemoveFromClass) },

            // Bag items
            { new[] { "ruler" }, 
              () => ExecuteBagItem(BagItemType.Ruler) },

            { new[] { "game", "play", "activity" }, 
              () => ExecuteBagItem(BagItemType.Game) },

            { new[] { "book", "read", "open book", "books" }, 
              () => ExecuteBagItem(BagItemType.Book) },

            { new[] { "music", "song", "listen" }, 
              () => ExecuteBagItem(BagItemType.Music) }
        };
    }

    void ProcessVoiceCommand(string transcript)
    {
        if (string.IsNullOrEmpty(transcript))
            return;

        string lower = transcript.ToLower().Trim();
        
        if (logCommands)
            Debug.Log($"[VoiceCommand] Processing: '{transcript}'");

        // Check for student name mentions
        StudentAgent targetStudent = DetectStudentMention(lower);

        // Try to match command
        bool commandFound = false;
        foreach (var kvp in commandActions)
        {
            foreach (string keyword in kvp.Key)
            {
                if (lower.Contains(keyword))
                {
                    if (logCommands)
                        Debug.Log($"[VoiceCommand] Matched keyword: '{keyword}'");
                    
                    kvp.Value?.Invoke();
                    commandFound = true;
                    break;
                }
            }
            if (commandFound) break;
        }

        // Check for complex commands
        if (!commandFound)
        {
            ProcessComplexCommand(lower, targetStudent);
        }

        // Check if teacher asked a question - make students respond
        if (enableQuestionDetection && IsQuestion(transcript))
        {
            ProcessTeacherQuestion(transcript, targetStudent);
        }

        // Show feedback
        if (teacherUI != null)
        {
            teacherUI.ShowFeedback($"Voice: {transcript}", Color.cyan);
        }
    }

    /// <summary>
    /// Detect if the teacher's speech is a question
    /// </summary>
    private bool IsQuestion(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;
            
        string lower = text.ToLower().Trim();
        
        // Check for question words (English and Hebrew)
        string[] questionIndicators = {
            "what", "who", "when", "where", "why", "how", "can", "could", "would", "should", "do", "does", "did",
            "מה", "מי", "מתי", "איפה", "למה", "איך", "האם", "יכול", "רוצה", "איזה", "איך", "למה", "מי", "מה"
        };
        
        foreach (string indicator in questionIndicators)
        {
            if (lower.Contains(indicator))
                return true;
        }
        
        // Check for question marks (if transcribed)
        if (text.Contains("?") || text.EndsWith("?"))
            return true;
        
        // Check for question patterns in Hebrew (more comprehensive)
        if (ContainsHebrew(text))
        {
            // Hebrew question words
            if (lower.Contains("איזה") || lower.Contains("מי") || lower.Contains("מה") || 
                lower.Contains("למה") || lower.Contains("איך") || lower.Contains("מתי") ||
                lower.Contains("איפה") || lower.Contains("האם") || lower.Contains("כמה"))
                return true;
            
            // Hebrew question patterns
            if (lower.Contains("תוכל") || lower.Contains("תוכלי") || lower.Contains("תוכלו") ||
                lower.Contains("תגיד") || lower.Contains("תגידי") || lower.Contains("תגידו") ||
                lower.Contains("תסביר") || lower.Contains("תסבירי") || lower.Contains("תסבירו"))
                return true;
        }
        
        // Check for rising intonation patterns (common in questions)
        // Questions often end with certain words
        string[] questionEndings = { "?", "נכון", "כן", "לא", "right", "yes", "no" };
        foreach (string ending in questionEndings)
        {
            if (lower.EndsWith(ending) || lower.Contains(" " + ending))
                return true;
        }
        
        return false;
    }

    private bool ContainsHebrew(string text)
    {
        foreach (char c in text)
        {
            if (c >= 0x0590 && c <= 0x05FF) // Hebrew Unicode range
                return true;
        }
        return false;
    }

    /// <summary>
    /// Process teacher's question and make students respond with AI-generated answers
    /// Uses the new StudentQuestionResponder system for progressive interest
    /// </summary>
    private void ProcessTeacherQuestion(string question, StudentAgent targetStudent)
    {
        if (classroomManager == null || classroomManager.activeStudents == null)
            return;

        if (logCommands)
            Debug.Log($"[VoiceCommand] Teacher asked a question: '{question}'");

        // If specific student is mentioned, only that student responds
        if (targetStudent != null)
        {
            TriggerStudentResponse(targetStudent, question);
            if (logCommands)
                Debug.Log($"[VoiceCommand] {targetStudent.studentName} is responding to question");
        }
        else
        {
            // Classwide question - trigger all students' question responders
            // Each student will independently decide whether to show eagerness
            int eagerStudentCount = 0;

            foreach (var student in classroomManager.activeStudents)
            {
                if (student == null)
                    continue;

                // Get or add StudentQuestionResponder component
                StudentQuestionResponder responder = student.GetComponent<StudentQuestionResponder>();
                if (responder == null)
                {
                    responder = student.gameObject.AddComponent<StudentQuestionResponder>();

                    // CRITICAL: Immediately initialize references since Start() won't be called until next frame
                    // This ensures responseBubble and other references are set before OnQuestionAsked is called
                    if (logCommands)
                        Debug.Log($"[VoiceCommand] Added StudentQuestionResponder to {student.studentName}");
                }

                // Trigger the question for this student
                // The responder will decide if and how to show eagerness
                // Note: InitializeReferences() is now called in OnQuestionAsked() to handle dynamic component addition
                responder.OnQuestionAsked(question);

                // Count how many students are showing eagerness
                if (responder.HasAnswerReady())
                    eagerStudentCount++;
            }

            if (logCommands)
                Debug.Log($"[VoiceCommand] {eagerStudentCount} student(s) are eager to answer");
        }
    }

    private System.Collections.IEnumerator DelayedStudentResponse(StudentAgent student, string question, float delay)
    {
        yield return new WaitForSeconds(delay);
        TriggerStudentResponse(student, question);
    }

    private void TriggerStudentResponse(StudentAgent student, string question)
    {
        if (student == null || string.IsNullOrWhiteSpace(question))
            return;

        if (aiResponseGenerator == null)
            aiResponseGenerator = FindObjectOfType<StudentAIResponseGenerator>();

        if (aiResponseGenerator == null)
        {
            if (logCommands)
                Debug.LogWarning("[VoiceCommand] StudentAIResponseGenerator not found; cannot generate student response.");
            return;
        }

        StartCoroutine(aiResponseGenerator.GenerateStudentResponse(student, question, (response) =>
        {
            if (string.IsNullOrWhiteSpace(response))
                return;

            var bubble = student.GetComponentInChildren<StudentResponseBubble>();
            if (bubble != null)
            {
                bubble.ShowResponse(response);
            }
            else if (logCommands)
            {
                Debug.LogWarning($"[VoiceCommand] StudentResponseBubble not found for {student.studentName}. Response was: {response}");
            }
        }));
    }

    StudentAgent DetectStudentMention(string text)
    {
        if (classroomManager == null || classroomManager.activeStudents == null)
            return null;

        // Check each student name
        foreach (var student in classroomManager.activeStudents)
        {
            string studentName = student.studentName.ToLower();
            string firstName = studentName.Split(' ')[0];
            
            if (text.Contains(firstName))
            {
                if (logCommands)
                    Debug.Log($"[VoiceCommand] Detected student: {student.studentName}");
                return student;
            }
        }

        // Use currently selected student
        if (teacherUI != null)
        {
            return teacherUI.GetSelectedStudent();
        }

        return null;
    }

    void ExecuteAction(ActionType actionType)
    {
        if (classroomManager == null)
            return;

        StudentAgent targetStudent = teacherUI?.GetSelectedStudent();

        if (targetStudent != null)
        {
            // Execute on specific student
            TeacherAction action = new TeacherAction
            {
                Type = actionType,
                TargetStudentId = targetStudent.studentId,
                Context = $"Voice: {actionType} on {targetStudent.studentName}"
            };
            classroomManager.ExecuteTeacherAction(action);

            if (logCommands)
                Debug.Log($"[VoiceCommand] {actionType} on {targetStudent.studentName}");
        }
        else
        {
            // Execute classwide
            classroomManager.ExecuteClasswideAction(actionType, $"Voice: {actionType}");

            if (logCommands)
                Debug.Log($"[VoiceCommand] {actionType} classwide");
        }
    }

    void ExecuteBagItem(BagItemType itemType)
    {
        if (classroomManager == null)
            return;

        StudentAgent targetStudent = teacherUI?.GetSelectedStudent();

        if (targetStudent != null)
        {
            classroomManager.ExecuteBagItemOnStudent(itemType, targetStudent);
            if (logCommands)
                Debug.Log($"[VoiceCommand] Used {itemType} on {targetStudent.studentName}");
        }
        else
        {
            classroomManager.ExecuteBagItem(itemType);
            if (logCommands)
                Debug.Log($"[VoiceCommand] Used {itemType} classwide");
        }
    }

    void ProcessComplexCommand(string text, StudentAgent targetStudent)
    {
        // Handle "everyone" commands
        if (text.Contains("everyone") || text.Contains("class"))
        {
            if (text.Contains("quiet") || text.Contains("attention"))
            {
                ExecuteAction(ActionType.Yell);
                return;
            }

            if (text.Contains("break"))
            {
                ExecuteAction(ActionType.GiveBreak);
                return;
            }

            if (text.Contains("book"))
            {
                ExecuteBagItem(BagItemType.Book);
                return;
            }
        }

        // No match
        if (logCommands)
            Debug.Log($"[VoiceCommand] No action matched for: '{text}'");
    }
}
