using UnityEngine;
using System;

/// <summary>
/// Configuration settings for ML-Agent API integration.
/// Manages endpoints, authentication, and model parameters.
/// </summary>
[Serializable]
public class MLAgentConfig : ScriptableObject
{
    [Header("API Configuration")]
    [Tooltip("Base URL for ML-Agent API endpoint")]
    public string apiBaseUrl = "https://api.ml-agents.classroom.ai/v1";

    [Tooltip("API authentication token")]
    public string apiToken = "";

    [Tooltip("Timeout for API requests in seconds")]
    public float requestTimeout = 10f;

    [Tooltip("Enable fallback to rule-based system when API fails")]
    public bool enableFallback = true;

    [Header("Model Configuration")]
    [Tooltip("Model ID for state transition decisions")]
    public string stateTransitionModelId = "classroom-fsm-v1";

    [Tooltip("Model ID for interaction generation")]
    public string interactionModelId = "classroom-interaction-v1";

    [Tooltip("Temperature for generation randomness (0-1)")]
    [Range(0f, 1f)]
    public float generationTemperature = 0.7f;

    [Tooltip("Maximum tokens for generated responses")]
    public int maxGenerationTokens = 50;

    [Header("Behavior Settings")]
    [Tooltip("Minimum interval between spontaneous interactions (seconds)")]
    public float minInteractionInterval = 5f;

    [Tooltip("Maximum interval between spontaneous interactions (seconds)")]
    public float maxInteractionInterval = 30f;

    [Tooltip("Probability multiplier for spontaneous interactions")]
    [Range(0f, 2f)]
    public float interactionProbabilityMultiplier = 1f;

    [Tooltip("Enable debug logging for ML-Agent calls")]
    public bool debugLogging = true;

    [Header("Interaction Types")]
    [Tooltip("Enable spontaneous questions about lesson")]
    public bool enableQuestions = true;

    [Tooltip("Enable misunderstanding/confusion expressions")]
    public bool enableConfusion = true;

    [Tooltip("Enable natural interruptions")]
    public bool enableInterruptions = true;

    [Tooltip("Enable social chatter between students")]
    public bool enableSocialChatter = true;

    [Header("Context Settings")]
    [Tooltip("Include nearby students in context")]
    public bool includeNearbyStudentsContext = true;

    [Tooltip("Maximum nearby students to include in context")]
    public int maxNearbyStudentsInContext = 3;

    [Tooltip("Include lesson topic in context")]
    public bool includeLessonContext = true;

    /// <summary>
    /// Get full API endpoint for state transition model
    /// </summary>
    public string GetStateTransitionEndpoint()
    {
        return $"{apiBaseUrl}/models/{stateTransitionModelId}/predict";
    }

    /// <summary>
    /// Get full API endpoint for interaction generation model
    /// </summary>
    public string GetInteractionEndpoint()
    {
        return $"{apiBaseUrl}/models/{interactionModelId}/generate";
    }

    /// <summary>
    /// Validate configuration settings
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(apiBaseUrl) &&
               !string.IsNullOrEmpty(stateTransitionModelId) &&
               !string.IsNullOrEmpty(interactionModelId);
    }

    /// <summary>
    /// Create default runtime config if no ScriptableObject exists
    /// </summary>
    public static MLAgentConfig CreateDefaultConfig()
    {
        var config = CreateInstance<MLAgentConfig>();
        config.name = "DefaultMLAgentConfig";
        return config;
    }
}

/// <summary>
/// Runtime configuration that can be modified during gameplay
/// </summary>
[Serializable]
public class MLAgentRuntimeConfig
{
    public string apiBaseUrl;
    public string apiToken;
    public float requestTimeout;
    public bool enableFallback;
    public string stateTransitionModelId;
    public string interactionModelId;
    public float generationTemperature;
    public int maxGenerationTokens;
    public float minInteractionInterval;
    public float maxInteractionInterval;
    public float interactionProbabilityMultiplier;
    public bool debugLogging;
    public bool enableQuestions;
    public bool enableConfusion;
    public bool enableInterruptions;
    public bool enableSocialChatter;
    public bool includeNearbyStudentsContext;
    public int maxNearbyStudentsInContext;
    public bool includeLessonContext;

    /// <summary>
    /// Create runtime config from ScriptableObject config
    /// </summary>
    public static MLAgentRuntimeConfig FromConfig(MLAgentConfig config)
    {
        return new MLAgentRuntimeConfig
        {
            apiBaseUrl = config.apiBaseUrl,
            apiToken = config.apiToken,
            requestTimeout = config.requestTimeout,
            enableFallback = config.enableFallback,
            stateTransitionModelId = config.stateTransitionModelId,
            interactionModelId = config.interactionModelId,
            generationTemperature = config.generationTemperature,
            maxGenerationTokens = config.maxGenerationTokens,
            minInteractionInterval = config.minInteractionInterval,
            maxInteractionInterval = config.maxInteractionInterval,
            interactionProbabilityMultiplier = config.interactionProbabilityMultiplier,
            debugLogging = config.debugLogging,
            enableQuestions = config.enableQuestions,
            enableConfusion = config.enableConfusion,
            enableInterruptions = config.enableInterruptions,
            enableSocialChatter = config.enableSocialChatter,
            includeNearbyStudentsContext = config.includeNearbyStudentsContext,
            maxNearbyStudentsInContext = config.maxNearbyStudentsInContext,
            includeLessonContext = config.includeLessonContext
        };
    }
}
