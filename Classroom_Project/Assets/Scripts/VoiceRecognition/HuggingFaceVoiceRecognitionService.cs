using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microphone = UnityEngine.Microphone;

/// <summary>
/// Voice recognition service using HuggingFace Inference API.
/// Supports multiple languages including Hebrew.
/// </summary>
public class HuggingFaceVoiceRecognitionService : MonoBehaviour
{
    [Header("HuggingFace Configuration")]
    [Tooltip("HuggingFace provider type")]
    public VoiceRecognitionProvider provider = VoiceRecognitionProvider.HuggingFace;
    
    [Tooltip("HuggingFace API Token")]
    public string huggingFaceToken;
    
    [Tooltip("Language code (e.g., 'en', 'he' for Hebrew)")]
    public string language = "he"; // Hebrew!
    
    [Tooltip("HuggingFace model ID for speech recognition")]
    public string modelId = "openai/whisper-large-v3"; // Can be changed to Hebrew-specific model
    
    [Header("Settings")]
    [Tooltip("Maximum recording time in seconds")]
    public int maxRecordingTime = 15;
    
    [Header("Status Display (Optional)")]
    public TMPro.TMP_Text statusText;
    public TMPro.TMP_Text transcriptText;

    // Events
    public event Action<string> OnTranscriptionReceived;
    public event Action<string> OnError;
    public event Action OnRecordingStarted;
    public event Action OnRecordingEnded;

    // Internal state
    private bool isRecording = false;
    private AudioClip currentRecording;
    private string microphoneDevice = null;

    // Singleton
    public static HuggingFaceVoiceRecognitionService Instance { get; private set; }

    public enum VoiceRecognitionProvider
    {
        HuggingFace
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Check microphone availability
        if (UnityEngine.Microphone.devices.Length == 0)
        {
            SetStatus("No microphone found!");
            Debug.LogError("HuggingFaceVoiceRecognitionService: No microphone devices found!");
        }
        else
        {
            microphoneDevice = UnityEngine.Microphone.devices[0];
            SetStatus("Ready - Click Record to start");
        }
    }

    /// <summary>
    /// Start recording voice
    /// </summary>
    public void StartRecording()
    {
        if (isRecording)
        {
            Debug.LogWarning("Already recording");
            return;
        }

        if (string.IsNullOrEmpty(microphoneDevice))
        {
            SetStatus("Error: No microphone available");
            OnError?.Invoke("No microphone available");
            return;
        }

        try
        {
            // Start recording
            currentRecording = UnityEngine.Microphone.Start(microphoneDevice, false, maxRecordingTime, 16000);
            isRecording = true;
            SetStatus("Recording... Speak now");
            OnRecordingStarted?.Invoke();
            Debug.Log("HuggingFace Voice Recognition started recording");
        }
        catch (Exception e)
        {
            SetStatus($"Error: {e.Message}");
            Debug.LogError($"Failed to start recording: {e.Message}");
            OnError?.Invoke(e.Message);
            isRecording = false;
        }
    }

    /// <summary>
    /// Stop recording and process with HuggingFace
    /// </summary>
    public void StopRecording()
    {
        if (!isRecording)
        {
            Debug.LogWarning("Not currently recording");
            return;
        }

        try
        {
            // Stop microphone
            UnityEngine.Microphone.End(microphoneDevice);
            isRecording = false;
            OnRecordingEnded?.Invoke();
            SetStatus("Processing with HuggingFace...");

            // Process audio with HuggingFace
            StartCoroutine(ProcessAudioWithHuggingFace());
        }
        catch (Exception e)
        {
            SetStatus($"Error: {e.Message}");
            Debug.LogError($"Failed to stop recording: {e.Message}");
            OnError?.Invoke(e.Message);
            isRecording = false;
        }
    }

    /// <summary>
    /// Process audio clip with HuggingFace Inference API
    /// </summary>
    private IEnumerator ProcessAudioWithHuggingFace()
    {
        if (currentRecording == null)
        {
            OnError?.Invoke("No audio recorded");
            yield break;
        }

        // Convert AudioClip to WAV format
        byte[] audioData = AudioClipToWAV(currentRecording);
        
        // Create form data for multipart/form-data request
        WWWForm form = new WWWForm();
        form.AddBinaryData("inputs", audioData, "audio.wav", "audio/wav");
        
        // Add language parameter if specified
        if (!string.IsNullOrEmpty(language))
        {
            form.AddField("language", language);
        }

        // Create request headers
        Dictionary<string, string> headers = new Dictionary<string, string>();
        headers["Authorization"] = $"Bearer {huggingFaceToken}";
        headers["Content-Type"] = "multipart/form-data";

        // HuggingFace Inference API endpoint
        string apiUrl = $"https://api-inference.huggingface.co/models/{modelId}";

        // Send request
        using (var www = UnityEngine.Networking.UnityWebRequest.Post(apiUrl, form))
        {
            www.SetRequestHeader("Authorization", $"Bearer {huggingFaceToken}");
            
            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                string responseText = www.downloadHandler.text;
                Debug.Log($"[HuggingFace] Response: {responseText}");
                
                // Parse JSON response
                string transcript = ParseHuggingFaceResponse(responseText);
                
                if (!string.IsNullOrEmpty(transcript))
                {
                    SetStatus("Transcription complete");
                    if (transcriptText != null)
                        transcriptText.text = transcript;
                    OnTranscriptionReceived?.Invoke(transcript);
                }
                else
                {
                    string error = "Failed to parse transcription";
                    SetStatus(error);
                    OnError?.Invoke(error);
                }
            }
            else
            {
                string error = $"HuggingFace API error: {www.error}";
                SetStatus(error);
                Debug.LogError($"[HuggingFace] {error}");
                OnError?.Invoke(error);
            }
        }

        // Clean up
        if (currentRecording != null)
        {
            Destroy(currentRecording);
            currentRecording = null;
        }
    }

    /// <summary>
    /// Convert AudioClip to WAV format bytes
    /// </summary>
    private byte[] AudioClipToWAV(AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        // Convert to 16-bit PCM
        byte[] bytes = new byte[samples.Length * 2];
        int sampleIndex = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            short sample = (short)(samples[i] * 32767f);
            bytes[sampleIndex++] = (byte)(sample & 0xFF);
            bytes[sampleIndex++] = (byte)((sample >> 8) & 0xFF);
        }

        // Create WAV header
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);

        // WAV header
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + bytes.Length);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16); // fmt chunk size
        writer.Write((ushort)1); // audio format (PCM)
        writer.Write((ushort)clip.channels);
        writer.Write(clip.frequency);
        writer.Write(clip.frequency * clip.channels * 2); // byte rate
        writer.Write((ushort)(clip.channels * 2)); // block align
        writer.Write((ushort)16); // bits per sample
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(bytes.Length);
        writer.Write(bytes);

        return stream.ToArray();
    }

    /// <summary>
    /// Parse HuggingFace API response to extract transcript
    /// </summary>
    private string ParseHuggingFaceResponse(string jsonResponse)
    {
        try
        {
            // HuggingFace typically returns: {"text": "transcribed text"}
            // Simple JSON parsing (you might want to use a proper JSON library)
            if (jsonResponse.Contains("\"text\""))
            {
                int startIndex = jsonResponse.IndexOf("\"text\"") + 7;
                int endIndex = jsonResponse.IndexOf("\"", startIndex);
                if (endIndex == -1) endIndex = jsonResponse.IndexOf("}", startIndex);
                
                if (startIndex > 6 && endIndex > startIndex)
                {
                    string text = jsonResponse.Substring(startIndex, endIndex - startIndex);
                    return text.Trim('"', ' ', '\n', '\r');
                }
            }
            
            // Fallback: try to extract any text between quotes
            if (jsonResponse.Contains("\""))
            {
                int firstQuote = jsonResponse.IndexOf('"') + 1;
                int secondQuote = jsonResponse.IndexOf('"', firstQuote);
                if (secondQuote > firstQuote)
                {
                    return jsonResponse.Substring(firstQuote, secondQuote - firstQuote);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to parse HuggingFace response: {e.Message}");
        }

        return jsonResponse; // Return raw response if parsing fails
    }

    private void SetStatus(string msg)
    {
        if (statusText != null)
            statusText.text = msg;
        Debug.Log($"[HuggingFaceVoiceRecognition] {msg}");
    }

    /// <summary>
    /// Check if currently recording
    /// </summary>
    public bool IsRecording()
    {
        return isRecording;
    }

    void OnDestroy()
    {
        if (isRecording)
        {
            UnityEngine.Microphone.End(microphoneDevice);
        }
    }
}
