using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// ML-Agent model for generating spontaneous verbal interactions.
/// Creates realistic classroom interactions including questions, confusion,
/// interruptions, and social chatter based on student profiles and context.
/// </summary>
public class MLAgentInteractionGenerator : MonoBehaviour
{
    [Header("Configuration")]
    public MLAgentConfig config;

    [Header("Runtime Settings")]
    [Tooltip("Whether to use ML-Agent for generation (false = use templates)")]
    public bool useMLAgent = true;

    [Tooltip("Maximum concurrent interactions per student")]
    public int maxConcurrentInteractions = 1;

    [Header("Probability Weights")]
    [Tooltip("Base probability for question generation")]
    [Range(0f, 1f)] public float questionWeight = 0.3f;

    [Tooltip("Base probability for confusion expression")]
    [Range(0f, 1f)] public float confusionWeight = 0.2f;

    [Tooltip("Base probability for interruption")]
    [Range(0f, 1f)] public float interruptionWeight = 0.15f;

    [Tooltip("Base probability for social chatter")]
    [Range(0f, 1f)] public float chatterWeight = 0.2f;

    [Tooltip("Base probability for comments")]
    [Range(0f, 1f)] public float commentWeight = 0.1f;

    [Tooltip("Base probability for help requests")]
    [Range(0f, 1f)] public float helpRequestWeight = 0.05f;

    // Internal state
    private Dictionary<string, float> lastInteractionTime = new Dictionary<string, float>();
    private Dictionary<string, int> activeInteractionCount = new Dictionary<string, int>();
    private bool isInitialized = false;

    /// <summary>
    /// Event fired when a new interaction is generated
    /// </summary>
    public event Action<SpontaneousInteraction> OnInteractionGenerated;

    private void Start()
    {
        Initialize();
    }

    /// <summary>
    /// Initialize the generator with configuration
    /// </summary>
    public void Initialize()
    {
        if (config == null)
        {
            config = MLAgentConfig.CreateDefaultConfig();
            Debug.LogWarning("[MLAgentInteraction] No config assigned, using defaults");
        }
        isInitialized = true;
    }

    /// <summary>
    /// Attempt to generate a spontaneous interaction for a student.
    /// May return null if conditions aren't met.
    /// </summary>
    public IEnumerator TryGenerateInteraction(
        StudentAgent student,
        ClassroomContext context,
        Action<SpontaneousInteraction> onComplete)
    {
        if (!isInitialized)
            Initialize();

        // Check cooldown
        if (!CanGenerateInteraction(student))
        {
            onComplete?.Invoke(null);
            yield break;
        }

        // Determine if an interaction should occur
        if (!ShouldGenerateInteraction(student, context))
        {
            onComplete?.Invoke(null);
            yield break;
        }

        // Determine interaction type based on student state and probabilities
        InteractionType type = DetermineInteractionType(student, context);

        // Check if this interaction type is enabled
        if (!IsInteractionTypeEnabled(type))
        {
            onComplete?.Invoke(null);
            yield break;
        }

        if (useMLAgent && config.IsValid() && !string.IsNullOrEmpty(config.apiToken))
        {
            yield return StartCoroutine(GenerateMLAgentInteraction(student, context, type, onComplete));
        }
        else
        {
            var interaction = GenerateFallbackInteraction(student, context, type);
            RecordInteraction(student.studentId);
            OnInteractionGenerated?.Invoke(interaction);
            onComplete?.Invoke(interaction);
        }
    }

    /// <summary>
    /// Force generate a specific type of interaction (bypasses probability checks)
    /// </summary>
    public IEnumerator ForceGenerateInteraction(
        StudentAgent student,
        ClassroomContext context,
        InteractionType type,
        Action<SpontaneousInteraction> onComplete)
    {
        if (!isInitialized)
            Initialize();

        if (useMLAgent && config.IsValid() && !string.IsNullOrEmpty(config.apiToken))
        {
            yield return StartCoroutine(GenerateMLAgentInteraction(student, context, type, onComplete));
        }
        else
        {
            var interaction = GenerateFallbackInteraction(student, context, type);
            RecordInteraction(student.studentId);
            OnInteractionGenerated?.Invoke(interaction);
            onComplete?.Invoke(interaction);
        }
    }

    /// <summary>
    /// Check if student can generate a new interaction (cooldown)
    /// </summary>
    private bool CanGenerateInteraction(StudentAgent student)
    {
        string studentId = student.studentId;

        // Check cooldown
        if (lastInteractionTime.TryGetValue(studentId, out float lastTime))
        {
            float elapsed = Time.time - lastTime;
            if (elapsed < config.minInteractionInterval)
                return false;
        }

        // Check concurrent interaction limit
        if (activeInteractionCount.TryGetValue(studentId, out int count))
        {
            if (count >= maxConcurrentInteractions)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Determine if an interaction should occur based on probability
    /// </summary>
    private bool ShouldGenerateInteraction(StudentAgent student, ClassroomContext context)
    {
        // Base probability from config
        float baseProbability = 0.1f * config.interactionProbabilityMultiplier;

        // Modify based on emotional state
        var emotions = student.emotions;

        // High boredom increases interaction probability (acting out)
        if (emotions.Boredom >= 6f)
            baseProbability *= 1.5f;

        // High frustration increases questions/confusion
        if (emotions.Frustration >= 5f)
            baseProbability *= 1.3f;

        // High extroversion increases all interactions
        baseProbability *= (0.5f + student.extroversion);

        // Certain states are more likely to generate interactions
        switch (student.currentState)
        {
            case StudentState.SideTalk:
                baseProbability *= 2f;
                break;
            case StudentState.Distracted:
                baseProbability *= 1.5f;
                break;
            case StudentState.Engaged:
                baseProbability *= 1.2f;
                break;
            case StudentState.Withdrawn:
                baseProbability *= 0.3f;
                break;
        }

        return UnityEngine.Random.value < baseProbability;
    }

    /// <summary>
    /// Determine the most appropriate interaction type
    /// </summary>
    private InteractionType DetermineInteractionType(StudentAgent student, ClassroomContext context)
    {
        var emotions = student.emotions;
        var weights = new Dictionary<InteractionType, float>();

        // Initialize with base weights
        weights[InteractionType.Question] = questionWeight;
        weights[InteractionType.Confusion] = confusionWeight;
        weights[InteractionType.Interruption] = interruptionWeight;
        weights[InteractionType.SocialChatter] = chatterWeight;
        weights[InteractionType.Comment] = commentWeight;
        weights[InteractionType.HelpRequest] = helpRequestWeight;
        weights[InteractionType.CallOut] = 0.05f;

        // Modify weights based on emotional state

        // Frustrated students ask more questions and express confusion
        if (emotions.Frustration >= 5f)
        {
            weights[InteractionType.Question] *= 1.5f;
            weights[InteractionType.Confusion] *= 2f;
            weights[InteractionType.HelpRequest] *= 2f;
        }

        // Bored students chat more and interrupt
        if (emotions.Boredom >= 5f)
        {
            weights[InteractionType.SocialChatter] *= 2f;
            weights[InteractionType.Interruption] *= 1.5f;
            weights[InteractionType.Question] *= 0.5f;
        }

        // Happy, engaged students make comments and call out
        if (emotions.Happiness >= 6f && student.currentState == StudentState.Engaged)
        {
            weights[InteractionType.Comment] *= 2f;
            weights[InteractionType.CallOut] *= 2f;
            weights[InteractionType.Question] *= 1.2f;
        }

        // Sad/withdrawn students rarely interact
        if (emotions.Sadness >= 6f)
        {
            foreach (var key in new List<InteractionType>(weights.Keys))
            {
                weights[key] *= 0.3f;
            }
            weights[InteractionType.HelpRequest] *= 2f; // But may ask for help
        }

        // State-based modifications
        switch (student.currentState)
        {
            case StudentState.SideTalk:
                weights[InteractionType.SocialChatter] *= 3f;
                break;
            case StudentState.Engaged:
                weights[InteractionType.CallOut] *= 2f;
                weights[InteractionType.Question] *= 1.5f;
                break;
            case StudentState.Distracted:
                weights[InteractionType.Interruption] *= 2f;
                break;
        }

        // Personality modifications
        weights[InteractionType.Question] *= student.academicMotivation;
        weights[InteractionType.SocialChatter] *= student.extroversion;
        weights[InteractionType.CallOut] *= student.extroversion * (1f + student.rebelliousness);

        // Normalize and select
        float total = 0f;
        foreach (var w in weights.Values)
            total += w;

        float roll = UnityEngine.Random.value * total;
        float cumulative = 0f;

        foreach (var kvp in weights)
        {
            cumulative += kvp.Value;
            if (roll <= cumulative)
                return kvp.Key;
        }

        return InteractionType.Comment;
    }

    /// <summary>
    /// Check if interaction type is enabled in config
    /// </summary>
    private bool IsInteractionTypeEnabled(InteractionType type)
    {
        switch (type)
        {
            case InteractionType.Question:
                return config.enableQuestions;
            case InteractionType.Confusion:
                return config.enableConfusion;
            case InteractionType.Interruption:
                return config.enableInterruptions;
            case InteractionType.SocialChatter:
                return config.enableSocialChatter;
            default:
                return true;
        }
    }

    /// <summary>
    /// Generate interaction using ML-Agent API
    /// </summary>
    private IEnumerator GenerateMLAgentInteraction(
        StudentAgent student,
        ClassroomContext context,
        InteractionType type,
        Action<SpontaneousInteraction> onComplete)
    {
        var request = CreateInteractionRequest(student, context, type);
        string jsonPayload = JsonUtility.ToJson(request);

        using (var www = new UnityWebRequest(config.GetInteractionEndpoint(), "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            if (!string.IsNullOrEmpty(config.apiToken))
            {
                www.SetRequestHeader("Authorization", $"Bearer {config.apiToken}");
            }

            www.timeout = (int)config.requestTimeout;

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var response = ParseInteractionResponse(www.downloadHandler.text);
                if (response.success)
                {
                    var interaction = CreateInteractionFromResponse(student, response.result);

                    if (config.debugLogging)
                    {
                        Debug.Log($"[MLAgentInteraction] {student.studentName} ({type}): {interaction.content}");
                    }

                    RecordInteraction(student.studentId);
                    OnInteractionGenerated?.Invoke(interaction);
                    onComplete?.Invoke(interaction);
                }
                else
                {
                    if (config.enableFallback)
                    {
                        var fallback = GenerateFallbackInteraction(student, context, type);
                        RecordInteraction(student.studentId);
                        OnInteractionGenerated?.Invoke(fallback);
                        onComplete?.Invoke(fallback);
                    }
                    else
                    {
                        onComplete?.Invoke(null);
                    }
                }
            }
            else
            {
                if (config.debugLogging)
                    Debug.LogWarning($"[MLAgentInteraction] Request failed: {www.error}");

                if (config.enableFallback)
                {
                    var fallback = GenerateFallbackInteraction(student, context, type);
                    RecordInteraction(student.studentId);
                    OnInteractionGenerated?.Invoke(fallback);
                    onComplete?.Invoke(fallback);
                }
                else
                {
                    onComplete?.Invoke(null);
                }
            }
        }
    }

    /// <summary>
    /// Generate interaction using fallback templates
    /// </summary>
    private SpontaneousInteraction GenerateFallbackInteraction(
        StudentAgent student,
        ClassroomContext context,
        InteractionType type)
    {
        string content = GenerateContextualContent(student, context, type);
        InteractionPriority priority = DeterminePriority(student, type);

        var interaction = new SpontaneousInteraction(
            student.studentId,
            student.studentName,
            type,
            content,
            priority);

        interaction.emotionalContext = EmotionalContext.FromStudent(student.emotions, student.currentState);

        // For social chatter, try to find a nearby student as target
        if (type == InteractionType.SocialChatter && context?.nearbyStudentStates != null)
        {
            // This could be enhanced to include actual nearby student IDs
            interaction.targetStudentId = null;
        }

        if (config.debugLogging)
        {
            Debug.Log($"[MLAgentInteraction] Fallback {student.studentName} ({type}): {content}");
        }

        return interaction;
    }

    /// <summary>
    /// Generate contextually appropriate content for the interaction
    /// </summary>
    private string GenerateContextualContent(
        StudentAgent student,
        ClassroomContext context,
        InteractionType type)
    {
        var emotions = student.emotions;

        // Get base templates
        string[] templates = InteractionTemplates.GetTemplatesForType(type);
        if (templates == null || templates.Length == 0)
            return "...";

        // Select template based on emotional state
        string template = templates[UnityEngine.Random.Range(0, templates.Length)];

        // For some types, we can make content more contextual
        switch (type)
        {
            case InteractionType.Confusion:
                if (emotions.Frustration >= 7f)
                {
                    // More intense confusion
                    string[] intenseConfusion = new[]
                    {
                        "אני בכלל לא מבין!",
                        "מה?? לא הבנתי כלום",
                        "זה ממש מבלבל אותי",
                        "אני אבוד לגמרי"
                    };
                    template = intenseConfusion[UnityEngine.Random.Range(0, intenseConfusion.Length)];
                }
                break;

            case InteractionType.SocialChatter:
                if (emotions.Boredom >= 7f)
                {
                    // Boredom-driven chatter
                    string[] boredChatter = new[]
                    {
                        "משעמם פה...",
                        "מתי כבר נגמר?",
                        "אני עומד להירדם",
                        "זה לא נגמר??"
                    };
                    template = boredChatter[UnityEngine.Random.Range(0, boredChatter.Length)];
                }
                break;

            case InteractionType.Question:
                if (student.academicMotivation >= 0.7f)
                {
                    // Motivated student questions
                    string[] motivatedQuestions = new[]
                    {
                        "המורה, אפשר לשאול שאלה מתקדמת?",
                        "זה קשור לנושא שלמדנו אתמול?",
                        "אפשר לראות עוד דוגמה?",
                        "מה עוד אפשר ללמוד על זה?"
                    };
                    template = motivatedQuestions[UnityEngine.Random.Range(0, motivatedQuestions.Length)];
                }
                break;

            case InteractionType.HelpRequest:
                if (emotions.Sadness >= 6f || emotions.Frustration >= 7f)
                {
                    // Distressed help request
                    string[] distressedHelp = new[]
                    {
                        "המורה, אני ממש צריך עזרה",
                        "אני לא מצליח בכלום",
                        "אפשר לעזור לי? אני תקוע",
                        "אני לא יודע מה לעשות..."
                    };
                    template = distressedHelp[UnityEngine.Random.Range(0, distressedHelp.Length)];
                }
                break;
        }

        return template;
    }

    /// <summary>
    /// Determine priority based on student state and interaction type
    /// </summary>
    private InteractionPriority DeterminePriority(StudentAgent student, InteractionType type)
    {
        var emotions = student.emotions;

        // Check for urgent situations
        if (emotions.IsCriticalState())
            return InteractionPriority.Urgent;

        // Type-based priority
        switch (type)
        {
            case InteractionType.HelpRequest:
                return emotions.Frustration >= 7f ? InteractionPriority.High : InteractionPriority.Medium;

            case InteractionType.Question:
            case InteractionType.Confusion:
                return InteractionPriority.Medium;

            case InteractionType.Interruption:
                return student.rebelliousness >= 0.7f ? InteractionPriority.High : InteractionPriority.Medium;

            case InteractionType.SocialChatter:
            case InteractionType.Comment:
                return InteractionPriority.Low;

            case InteractionType.CallOut:
                return InteractionPriority.Low;

            default:
                return InteractionPriority.Low;
        }
    }

    /// <summary>
    /// Create API request payload
    /// </summary>
    private InteractionGenerationRequest CreateInteractionRequest(
        StudentAgent student,
        ClassroomContext context,
        InteractionType preferredType)
    {
        var enabledTypes = new List<string>();
        if (config.enableQuestions) enabledTypes.Add("Question");
        if (config.enableConfusion) enabledTypes.Add("Confusion");
        if (config.enableInterruptions) enabledTypes.Add("Interruption");
        if (config.enableSocialChatter) enabledTypes.Add("SocialChatter");
        enabledTypes.Add("Comment");
        enabledTypes.Add("HelpRequest");
        enabledTypes.Add("CallOut");

        var nearbyStudents = new List<InteractionGenerationRequest.NearbyStudentData>();
        // This would be populated with actual nearby student data

        return new InteractionGenerationRequest
        {
            studentId = student.studentId,
            studentName = student.studentName,
            profile = new InteractionGenerationRequest.StudentProfileData
            {
                extroversion = student.extroversion,
                sensitivity = student.sensitivity,
                rebelliousness = student.rebelliousness,
                academicMotivation = student.academicMotivation
            },
            emotions = EmotionalContext.FromStudent(student.emotions, student.currentState),
            classroomContext = new InteractionGenerationRequest.ClassroomContextData
            {
                currentLessonTopic = context?.lessonTopic ?? "",
                lessonProgress = context?.lessonProgress ?? 0f,
                totalStudents = context?.totalStudents ?? 0,
                classAverageEngagement = context?.averageClassEngagement ?? 0.5f,
                nearbyStudents = nearbyStudents,
                timeSinceLastTeacherAction = context?.timeSinceLastTeacherAction ?? 0f,
                lastTeacherActionType = ""
            },
            enabledInteractionTypes = enabledTypes,
            temperature = config.generationTemperature,
            maxTokens = config.maxGenerationTokens
        };
    }

    /// <summary>
    /// Parse API response
    /// </summary>
    private InteractionGenerationResponse ParseInteractionResponse(string json)
    {
        try
        {
            return JsonUtility.FromJson<InteractionGenerationResponse>(json);
        }
        catch (Exception e)
        {
            return new InteractionGenerationResponse
            {
                success = false,
                error = $"Parse error: {e.Message}"
            };
        }
    }

    /// <summary>
    /// Create interaction from API response
    /// </summary>
    private SpontaneousInteraction CreateInteractionFromResponse(
        StudentAgent student,
        InteractionGenerationResponse.InteractionResult result)
    {
        InteractionType type = InteractionType.Comment;
        if (Enum.TryParse<InteractionType>(result.interactionType, true, out InteractionType parsedType))
            type = parsedType;

        InteractionPriority priority = InteractionPriority.Medium;
        if (Enum.TryParse<InteractionPriority>(result.priority, true, out InteractionPriority parsedPriority))
            priority = parsedPriority;

        var interaction = new SpontaneousInteraction(
            student.studentId,
            student.studentName,
            type,
            result.content,
            priority);

        interaction.confidence = result.confidence;
        interaction.requiresResponse = result.requiresResponse;
        interaction.targetStudentId = result.targetStudentId;
        interaction.emotionalContext = EmotionalContext.FromStudent(student.emotions, student.currentState);

        return interaction;
    }

    /// <summary>
    /// Record that an interaction was generated for cooldown tracking
    /// </summary>
    private void RecordInteraction(string studentId)
    {
        lastInteractionTime[studentId] = Time.time;

        if (!activeInteractionCount.ContainsKey(studentId))
            activeInteractionCount[studentId] = 0;
        activeInteractionCount[studentId]++;
    }

    /// <summary>
    /// Mark an interaction as finished (for concurrent limit tracking)
    /// </summary>
    public void MarkInteractionFinished(string studentId)
    {
        if (activeInteractionCount.ContainsKey(studentId))
        {
            activeInteractionCount[studentId] = Mathf.Max(0, activeInteractionCount[studentId] - 1);
        }
    }

    /// <summary>
    /// Clear all tracking state
    /// </summary>
    public void ClearState()
    {
        lastInteractionTime.Clear();
        activeInteractionCount.Clear();
    }
}
