using UnityEngine;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Handles loading classroom scenarios from JSON configuration files.
/// Scenarios define student profiles, classroom settings, and initial conditions.
/// </summary>
public class ScenarioLoader : MonoBehaviour
{
    [Header("Scenario Settings")]
    public string defaultScenarioFileName = "scenario_basic_classroom.json";
    
    [Header("Available Scenarios")]
    public List<string> availableScenarios = new List<string>();
    
    private string scenarioFolderPath;

    void Awake()
    {
        // Set the path where scenarios are stored
        // StreamingAssets is a special Unity folder for external files
        scenarioFolderPath = Path.Combine(Application.streamingAssetsPath, "Scenarios");
        
        // Find all available scenarios
        DiscoverScenarios();
    }

    /// <summary>
    /// Find all JSON scenario files in the Scenarios folder
    /// </summary>
    void DiscoverScenarios()
    {
        availableScenarios.Clear();
        
        // Check if the folder exists
        if (!Directory.Exists(scenarioFolderPath))
        {
            Debug.LogWarning($"Scenarios folder not found at: {scenarioFolderPath}");
            Debug.Log("Creating Scenarios folder...");
            Directory.CreateDirectory(scenarioFolderPath);
            return;
        }

        // Find all .json files
        string[] files = Directory.GetFiles(scenarioFolderPath, "*.json");
        
        foreach (string filePath in files)
        {
            string fileName = Path.GetFileName(filePath);
            availableScenarios.Add(fileName);
            Debug.Log($"Found scenario: {fileName}");
        }
        
        if (availableScenarios.Count == 0)
        {
            Debug.LogWarning("No scenario files found in Scenarios folder!");
        }
    }

    /// <summary>
    /// Load a specific scenario by filename
    /// </summary>
    public ScenarioConfig LoadScenario(string fileName)
    {
        string fullPath = Path.Combine(scenarioFolderPath, fileName);
        
        // Check if file exists
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"Scenario file not found: {fullPath}");
            return CreateDefaultScenario();
        }

        try
        {
            // Read the JSON file
            string jsonContent = File.ReadAllText(fullPath);
            
            // Convert JSON to ScenarioConfig object
            ScenarioConfig scenario = JsonUtility.FromJson<ScenarioConfig>(jsonContent);
            
            Debug.Log($"Successfully loaded scenario: {scenario.scenarioName}");
            Debug.Log($"Students: {scenario.studentProfiles.Count}");
            
            return scenario;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading scenario: {e.Message}");
            return CreateDefaultScenario();
        }
    }

    /// <summary>
    /// Load the default scenario
    /// </summary>
    public ScenarioConfig LoadDefaultScenario()
    {
        return LoadScenario(defaultScenarioFileName);
    }

    /// <summary>
    /// Create a minimal fallback scenario if loading fails
    /// </summary>
    ScenarioConfig CreateDefaultScenario()
    {
        Debug.Log("Creating default fallback scenario...");
        
        ScenarioConfig scenario = new ScenarioConfig
        {
            scenarioName = "Default Classroom",
            description = "Basic classroom with 5 students",
            difficulty = "Easy",
            studentProfiles = new List<StudentProfile>()
        };

        // Create 5 basic students
        for (int i = 1; i <= 5; i++)
        {
            scenario.studentProfiles.Add(new StudentProfile
            {
                id = $"student_{i:000}",
                name = $"Student {i}",
                extroversion = Random.Range(0.3f, 0.8f),
                sensitivity = Random.Range(0.3f, 0.7f),
                rebelliousness = Random.Range(0.2f, 0.6f),
                academicMotivation = Random.Range(0.4f, 0.9f),
                initialHappiness = 5f,
                initialBoredom = 3f
            });
        }

        return scenario;
    }

    /// <summary>
    /// Save a scenario to JSON file (useful for creating new scenarios)
    /// </summary>
    public void SaveScenario(ScenarioConfig scenario, string fileName)
    {
        string fullPath = Path.Combine(scenarioFolderPath, fileName);
        
        try
        {
            // Convert scenario to JSON
            string jsonContent = JsonUtility.ToJson(scenario, true); // true = pretty print
            
            // Write to file
            File.WriteAllText(fullPath, jsonContent);
            
            Debug.Log($"Scenario saved: {fullPath}");
            
            // Refresh available scenarios list
            DiscoverScenarios();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error saving scenario: {e.Message}");
        }
    }

    /// <summary>
    /// Get list of all available scenario names
    /// </summary>
    public List<string> GetAvailableScenarios()
    {
        return new List<string>(availableScenarios);
    }
}