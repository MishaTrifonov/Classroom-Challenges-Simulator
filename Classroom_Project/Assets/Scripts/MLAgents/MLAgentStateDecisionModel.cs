using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// ML-Agent model for FSM state transition decisions.
/// Integrates with external ML API to determine student behavioral state changes
/// based on emotional vectors, personality profiles, and classroom context.
/// </summary>
public class MLAgentStateDecisionModel : MonoBehaviour
{
    [Header("Configuration")]
    public MLAgentConfig config;

    [Header("Runtime Settings")]
    [Tooltip("Whether to use ML-Agent for state decisions (false = use rule-based)")]
    public bool useMLAgent = true;

    [Tooltip("Cache duration for decisions (seconds)")]
    public float decisionCacheDuration = 1f;

    // Internal state
    private Dictionary<string, CachedDecision> decisionCache = new Dictionary<string, CachedDecision>();
    private bool isInitialized = false;

    /// <summary>
    /// Event fired when a state decision is made
    /// </summary>
    public event Action<string, StudentState, float> OnStateDecisionMade;

    private void Start()
    {
        Initialize();
    }

    /// <summary>
    /// Initialize the model with configuration
    /// </summary>
    public void Initialize()
    {
        if (config == null)
        {
            config = MLAgentConfig.CreateDefaultConfig();
            Debug.LogWarning("[MLAgentStateDecision] No config assigned, using defaults");
        }
        isInitialized = true;
    }

    /// <summary>
    /// Request a state transition decision for a student.
    /// Returns the recommended new state based on ML model analysis.
    /// </summary>
    public IEnumerator RequestStateDecision(
        StudentAgent student,
        ClassroomContext context,
        Action<StateDecisionResult> onComplete)
    {
        if (!isInitialized)
            Initialize();

        // Check cache first
        string cacheKey = GetCacheKey(student);
        if (decisionCache.TryGetValue(cacheKey, out CachedDecision cached))
        {
            if (Time.time - cached.timestamp < decisionCacheDuration)
            {
                onComplete?.Invoke(cached.result);
                yield break;
            }
        }

        if (useMLAgent && config.IsValid() && !string.IsNullOrEmpty(config.apiToken))
        {
            // Use ML-Agent API for decision
            yield return StartCoroutine(RequestMLAgentDecision(student, context, (result) =>
            {
                CacheDecision(cacheKey, result);
                onComplete?.Invoke(result);
            }));
        }
        else
        {
            // Use enhanced rule-based fallback
            var result = GenerateFallbackDecision(student, context);
            CacheDecision(cacheKey, result);
            onComplete?.Invoke(result);
        }
    }

    /// <summary>
    /// Request state decision from ML-Agent API
    /// </summary>
    private IEnumerator RequestMLAgentDecision(
        StudentAgent student,
        ClassroomContext context,
        Action<StateDecisionResult> onComplete)
    {
        var request = CreateStateDecisionRequest(student, context);
        string jsonPayload = JsonUtility.ToJson(request);

        using (var www = new UnityWebRequest(config.GetStateTransitionEndpoint(), "POST"))
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
                var response = ParseStateDecisionResponse(www.downloadHandler.text);
                if (response.success)
                {
                    if (config.debugLogging)
                    {
                        Debug.Log($"[MLAgentStateDecision] {student.studentName}: " +
                                  $"ML recommends {response.recommendedState} (conf: {response.confidence:F2})");
                    }

                    OnStateDecisionMade?.Invoke(student.studentId, response.recommendedState, response.confidence);
                    onComplete?.Invoke(response);
                }
                else
                {
                    if (config.debugLogging)
                        Debug.LogWarning($"[MLAgentStateDecision] API error: {response.error}");

                    if (config.enableFallback)
                        onComplete?.Invoke(GenerateFallbackDecision(student, context));
                    else
                        onComplete?.Invoke(StateDecisionResult.NoChange(student.currentState));
                }
            }
            else
            {
                if (config.debugLogging)
                    Debug.LogWarning($"[MLAgentStateDecision] Request failed: {www.error}");

                if (config.enableFallback)
                    onComplete?.Invoke(GenerateFallbackDecision(student, context));
                else
                    onComplete?.Invoke(StateDecisionResult.NoChange(student.currentState));
            }
        }
    }

    /// <summary>
    /// Create API request payload
    /// </summary>
    private StateDecisionRequest CreateStateDecisionRequest(StudentAgent student, ClassroomContext context)
    {
        return new StateDecisionRequest
        {
            studentId = student.studentId,
            studentName = student.studentName,
            currentState = student.currentState.ToString(),
            emotions = new StateDecisionRequest.EmotionData
            {
                happiness = student.emotions.Happiness,
                sadness = student.emotions.Sadness,
                frustration = student.emotions.Frustration,
                boredom = student.emotions.Boredom,
                anger = student.emotions.Anger
            },
            profile = new StateDecisionRequest.ProfileData
            {
                extroversion = student.extroversion,
                sensitivity = student.sensitivity,
                rebelliousness = student.rebelliousness,
                academicMotivation = student.academicMotivation
            },
            context = new StateDecisionRequest.ContextData
            {
                lessonTopic = context?.lessonTopic ?? "",
                timeSinceLastAction = context?.timeSinceLastTeacherAction ?? 0f,
                classEngagement = context?.averageClassEngagement ?? 0.5f,
                nearbyStudentStates = context?.nearbyStudentStates ?? new List<string>()
            },
            temperature = config.generationTemperature
        };
    }

    /// <summary>
    /// Parse API response
    /// </summary>
    private StateDecisionResult ParseStateDecisionResponse(string json)
    {
        try
        {
            var response = JsonUtility.FromJson<StateDecisionAPIResponse>(json);
            if (response != null && response.success)
            {
                StudentState newState = ParseState(response.result.recommendedState);
                return new StateDecisionResult
                {
                    success = true,
                    shouldTransition = response.result.shouldTransition,
                    recommendedState = newState,
                    confidence = response.result.confidence,
                    reasoning = response.result.reasoning
                };
            }
            return StateDecisionResult.Failed(response?.error ?? "Unknown error");
        }
        catch (Exception e)
        {
            return StateDecisionResult.Failed($"Parse error: {e.Message}");
        }
    }

    /// <summary>
    /// Enhanced rule-based fallback decision system.
    /// Mimics ML behavior using weighted probabilities.
    /// </summary>
    private StateDecisionResult GenerateFallbackDecision(StudentAgent student, ClassroomContext context)
    {
        StudentState currentState = student.currentState;
        var emotions = student.emotions;

        // Calculate transition probabilities based on emotions and personality
        var probabilities = CalculateTransitionProbabilities(student, context);

        // Select state based on probabilities
        float roll = UnityEngine.Random.value;
        float cumulative = 0f;
        StudentState selectedState = currentState;

        foreach (var kvp in probabilities)
        {
            cumulative += kvp.Value;
            if (roll <= cumulative)
            {
                selectedState = kvp.Key;
                break;
            }
        }

        bool shouldTransition = selectedState != currentState;
        float confidence = shouldTransition ? probabilities[selectedState] : 0.8f;

        if (config.debugLogging && shouldTransition)
        {
            Debug.Log($"[MLAgentStateDecision] Fallback: {student.studentName} " +
                      $"{currentState} -> {selectedState} (conf: {confidence:F2})");
        }

        return new StateDecisionResult
        {
            success = true,
            shouldTransition = shouldTransition,
            recommendedState = selectedState,
            confidence = confidence,
            reasoning = "Fallback rule-based decision"
        };
    }

    /// <summary>
    /// Calculate transition probabilities for all possible states
    /// </summary>
    private Dictionary<StudentState, float> CalculateTransitionProbabilities(
        StudentAgent student,
        ClassroomContext context)
    {
        var probs = new Dictionary<StudentState, float>();
        var emotions = student.emotions;

        // Initialize all probabilities to 0
        foreach (StudentState state in Enum.GetValues(typeof(StudentState)))
        {
            probs[state] = 0f;
        }

        // Base probability to stay in current state
        probs[student.currentState] = 0.6f;

        // ANGER-DRIVEN: High anger leads to Arguing
        if (emotions.Anger >= 5f)
        {
            float angerProb = (emotions.Anger - 4f) / 6f * student.rebelliousness;
            probs[StudentState.Arguing] += angerProb * 0.4f;
            probs[student.currentState] -= angerProb * 0.2f;
        }

        // BOREDOM-DRIVEN: High boredom leads to Distracted or SideTalk
        if (emotions.Boredom >= 5f)
        {
            float boredomProb = (emotions.Boredom - 4f) / 6f;
            probs[StudentState.Distracted] += boredomProb * 0.3f;
            probs[StudentState.SideTalk] += boredomProb * 0.2f * student.extroversion;
            probs[student.currentState] -= boredomProb * 0.3f;
        }

        // SADNESS-DRIVEN: High sadness leads to Withdrawn
        if (emotions.Sadness >= 5f)
        {
            float sadnessProb = (emotions.Sadness - 4f) / 6f * student.sensitivity;
            probs[StudentState.Withdrawn] += sadnessProb * 0.4f;
            probs[student.currentState] -= sadnessProb * 0.2f;
        }

        // FRUSTRATION-DRIVEN: High frustration leads to disengagement
        if (emotions.Frustration >= 5f)
        {
            float frustProb = (emotions.Frustration - 4f) / 6f;
            probs[StudentState.SideTalk] += frustProb * 0.2f;
            probs[StudentState.Distracted] += frustProb * 0.2f;
            probs[student.currentState] -= frustProb * 0.2f;
        }

        // POSITIVE STATE: High happiness + low boredom leads to Engaged
        if (emotions.Happiness >= 6f && emotions.Boredom < 4f)
        {
            float engageProb = (emotions.Happiness - 5f) / 5f * student.academicMotivation;
            probs[StudentState.Engaged] += engageProb * 0.4f;
            probs[StudentState.Listening] += engageProb * 0.2f;
        }

        // RECOVERY: Moderate overall mood tends toward Listening
        float mood = emotions.GetOverallMood();
        if (mood > 2f && mood < 5f)
        {
            probs[StudentState.Listening] += 0.15f;
        }

        // Context influence: nearby students affect social states
        if (context != null && context.nearbyStudentStates != null)
        {
            int chatteringNearby = 0;
            foreach (var state in context.nearbyStudentStates)
            {
                if (state == "SideTalk" || state == "Distracted")
                    chatteringNearby++;
            }
            if (chatteringNearby > 0)
            {
                probs[StudentState.SideTalk] += 0.05f * chatteringNearby * student.extroversion;
                probs[StudentState.Distracted] += 0.03f * chatteringNearby;
            }
        }

        // Normalize probabilities
        float total = 0f;
        foreach (var prob in probs.Values)
            total += Mathf.Max(0f, prob);

        if (total > 0f)
        {
            var keys = new List<StudentState>(probs.Keys);
            foreach (var key in keys)
            {
                probs[key] = Mathf.Max(0f, probs[key]) / total;
            }
        }

        return probs;
    }

    /// <summary>
    /// Parse state string to enum
    /// </summary>
    private StudentState ParseState(string stateStr)
    {
        if (Enum.TryParse<StudentState>(stateStr, true, out StudentState state))
            return state;
        return StudentState.Listening;
    }

    private string GetCacheKey(StudentAgent student)
    {
        return $"{student.studentId}_{student.currentState}_{student.emotions.GetHashCode()}";
    }

    private void CacheDecision(string key, StateDecisionResult result)
    {
        decisionCache[key] = new CachedDecision
        {
            timestamp = Time.time,
            result = result
        };
    }

    /// <summary>
    /// Clear all cached decisions
    /// </summary>
    public void ClearCache()
    {
        decisionCache.Clear();
    }

    // Internal types
    private class CachedDecision
    {
        public float timestamp;
        public StateDecisionResult result;
    }
}

/// <summary>
/// Result of state transition decision
/// </summary>
[Serializable]
public class StateDecisionResult
{
    public bool success;
    public string error;
    public bool shouldTransition;
    public StudentState recommendedState;
    public float confidence;
    public string reasoning;

    public static StateDecisionResult NoChange(StudentState current)
    {
        return new StateDecisionResult
        {
            success = true,
            shouldTransition = false,
            recommendedState = current,
            confidence = 1f,
            reasoning = "No transition needed"
        };
    }

    public static StateDecisionResult Failed(string error)
    {
        return new StateDecisionResult
        {
            success = false,
            error = error,
            shouldTransition = false,
            confidence = 0f
        };
    }
}

/// <summary>
/// Classroom context for state decisions
/// </summary>
[Serializable]
public class ClassroomContext
{
    public string lessonTopic;
    public float lessonProgress;
    public float timeSinceLastTeacherAction;
    public float averageClassEngagement;
    public List<string> nearbyStudentStates;
    public int totalStudents;
    public int disruptedStudentsCount;
}

/// <summary>
/// API request payload
/// </summary>
[Serializable]
public class StateDecisionRequest
{
    public string studentId;
    public string studentName;
    public string currentState;
    public EmotionData emotions;
    public ProfileData profile;
    public ContextData context;
    public float temperature;

    [Serializable]
    public class EmotionData
    {
        public float happiness;
        public float sadness;
        public float frustration;
        public float boredom;
        public float anger;
    }

    [Serializable]
    public class ProfileData
    {
        public float extroversion;
        public float sensitivity;
        public float rebelliousness;
        public float academicMotivation;
    }

    [Serializable]
    public class ContextData
    {
        public string lessonTopic;
        public float timeSinceLastAction;
        public float classEngagement;
        public List<string> nearbyStudentStates;
    }
}

/// <summary>
/// API response structure
/// </summary>
[Serializable]
public class StateDecisionAPIResponse
{
    public bool success;
    public string error;
    public ResultData result;

    [Serializable]
    public class ResultData
    {
        public bool shouldTransition;
        public string recommendedState;
        public float confidence;
        public string reasoning;
    }
}
