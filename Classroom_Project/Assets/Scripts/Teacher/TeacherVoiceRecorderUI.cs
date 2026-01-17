using System.Collections;
using TMPro;
using UnityEngine;

public class TeacherVoiceRecorderUI : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text transcriptTopText;     // text shown at top
    public TMP_Text statusText;            // optional

    [Header("Recording")]
    public int sampleRate = 16000;
    public int maxSeconds = 15;

    private string micDevice;
    private AudioClip clip;
    private bool isRecording;
    private Coroutine fakeTranscribeRoutine;

    // WebGL: Unity Microphone API is not supported. We simulate.
#if UNITY_WEBGL && !UNITY_EDITOR
    private const bool MICROPHONE_SUPPORTED = false;
#else
    private const bool MICROPHONE_SUPPORTED = true;
#endif

    void Start()
    {
        if (!MICROPHONE_SUPPORTED)
        {
            micDevice = null;
            SetStatus("WebGL: Microphone not supported (simulated)");
            if (transcriptTopText != null) transcriptTopText.text = "";
            return;
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        // Pick first available microphone
        if (Microphone.devices != null && Microphone.devices.Length > 0)
        {
            micDevice = Microphone.devices[0];
            SetStatus("Ready");
        }
        else
        {
            micDevice = null;
            SetStatus("No microphone detected");
            if (transcriptTopText != null) transcriptTopText.text = "";
        }
#endif
    }

    public void StartRecording()
    {
        if (isRecording) return;

        StopFakeTranscribeIfRunning();

        if (!MICROPHONE_SUPPORTED)
        {
            isRecording = true;
            if (transcriptTopText != null) transcriptTopText.text = "Recording... (simulated)";
            SetStatus("Recording (simulated)");
            return;
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        if (string.IsNullOrEmpty(micDevice))
        {
            SetStatus("No microphone");
            return;
        }

        clip = null;

        if (transcriptTopText != null)
            transcriptTopText.text = "Recording...";

        SetStatus("Recording");

        clip = Microphone.Start(micDevice, false, maxSeconds, sampleRate);
        isRecording = true;
#endif
    }

    public void StopRecording()
    {
        if (!isRecording) return;

        if (!MICROPHONE_SUPPORTED)
        {
            isRecording = false;

            if (transcriptTopText != null)
                transcriptTopText.text = "Processing... (simulated)";

            SetStatus("Processing (simulated)");
            fakeTranscribeRoutine = StartCoroutine(FakeTranscribe());
            return;
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        if (string.IsNullOrEmpty(micDevice)) return;

        int pos = Microphone.GetPosition(micDevice);
        Microphone.End(micDevice);
        isRecording = false;

        if (clip == null || pos <= 0)
        {
            if (transcriptTopText != null)
                transcriptTopText.text = "No audio captured";

            SetStatus("No audio");
            return;
        }

        if (transcriptTopText != null)
            transcriptTopText.text = "Processing...";

        SetStatus("Processing");

        // for now: fake transcription
        fakeTranscribeRoutine = StartCoroutine(FakeTranscribe());
#endif
    }

    public void ResetRecording()
    {
        if (MICROPHONE_SUPPORTED)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            if (isRecording && !string.IsNullOrEmpty(micDevice))
                Microphone.End(micDevice);
#endif
        }

        isRecording = false;
        StopFakeTranscribeIfRunning();
        clip = null;

        if (transcriptTopText != null) transcriptTopText.text = "";
        SetStatus("Reset");
    }

    private IEnumerator FakeTranscribe()
    {
        yield return new WaitForSeconds(1f);

        if (transcriptTopText != null)
            transcriptTopText.text = "Teacher said: please open your notebooks";

        SetStatus("Done");
        fakeTranscribeRoutine = null;
    }

    private void StopFakeTranscribeIfRunning()
    {
        if (fakeTranscribeRoutine != null)
        {
            StopCoroutine(fakeTranscribeRoutine);
            fakeTranscribeRoutine = null;
        }
    }

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
        Debug.Log(msg);
    }
}
