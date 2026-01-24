using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Generates AI responses for students based on teacher's questions and each student's emotional state.
/// Each student gets a unique response based on their emotion vector and personality traits.
/// </summary>
public class StudentAIResponseGenerator : MonoBehaviour
{
    [Header("AI Configuration")]
    [Tooltip("Use HuggingFace API for response generation")]
    public bool useHuggingFace = true;
    
    [Tooltip("HuggingFace API Token (optional - can leave empty if using free tier)")]
    public string huggingFaceToken = "";
    
    [Tooltip("Model for text generation (e.g., 'microsoft/DialoGPT-medium', 'gpt2')")]
    public string modelId = "gpt2"; // Can be changed to better models
    
    [Header("Settings")]
    [Tooltip("Enable to log AI responses to console")]
    public bool logResponses = true;
    
    [Tooltip("Maximum response length")]
    public int maxResponseLength = 100;
    
    [Tooltip("Default language for responses (Hebrew by default)")]
    public bool preferHebrew = true;

    // Cache for responses (to avoid regenerating same responses)
    private Dictionary<string, string> responseCache = new Dictionary<string, string>();

    /// <summary>
    /// Generate a unique AI response for a student based on teacher's question and student's emotional state
    /// </summary>
    public IEnumerator GenerateStudentResponse(
        StudentAgent student,
        string teacherQuestion,
        System.Action<string> onResponseGenerated)
    {
        if (string.IsNullOrEmpty(teacherQuestion))
        {
            onResponseGenerated?.Invoke("");
            yield break;
        }

        // Create a unique prompt based on student's emotional state and personality
        string prompt = CreateEmotionBasedPrompt(student, teacherQuestion);
        
        // Check cache first
        string cacheKey = $"{student.studentId}_{teacherQuestion.GetHashCode()}";
        if (responseCache.ContainsKey(cacheKey))
        {
            if (logResponses)
                Debug.Log($"[StudentAI] Cached response for {student.studentName}: {responseCache[cacheKey]}");
            onResponseGenerated?.Invoke(responseCache[cacheKey]);
            yield break;
        }

        if (useHuggingFace && !string.IsNullOrEmpty(huggingFaceToken))
        {
            // Use HuggingFace API for generation
            yield return StartCoroutine(GenerateWithHuggingFace(prompt, (response) =>
            {
                if (!string.IsNullOrEmpty(response))
                {
                    responseCache[cacheKey] = response;
                    onResponseGenerated?.Invoke(response);
                }
                else
                {
                    // Fallback to rule-based response
                    string fallback = GenerateFallbackResponse(student, teacherQuestion);
                    onResponseGenerated?.Invoke(fallback);
                }
            }));
        }
        else
        {
            // Use rule-based response generation (no API needed)
            string response = GenerateFallbackResponse(student, teacherQuestion);
            onResponseGenerated?.Invoke(response);
        }
    }

    /// <summary>
    /// Create a prompt that incorporates student's emotions and personality
    /// </summary>
    private string CreateEmotionBasedPrompt(StudentAgent student, string question)
    {
        StringBuilder prompt = new StringBuilder();
        
        // Base personality description
        prompt.Append($"You are a student named {student.studentName}. ");
        prompt.Append($"Your emotional state: ");
        
        // Add emotional context
        if (student.emotions.Happiness >= 7f)
            prompt.Append("very happy and enthusiastic. ");
        else if (student.emotions.Happiness <= 3f)
            prompt.Append("not very happy. ");
        
        if (student.emotions.Sadness >= 7f)
            prompt.Append("feeling sad or anxious. ");
        
        if (student.emotions.Anger >= 7f)
            prompt.Append("feeling angry or frustrated. ");
        
        if (student.emotions.Boredom >= 7f)
            prompt.Append("very bored and disinterested. ");
        
        if (student.emotions.Frustration >= 7f)
            prompt.Append("frustrated and struggling. ");
        
        // Add personality traits
        if (student.extroversion > 0.7f)
            prompt.Append("You are outgoing and confident. ");
        else if (student.extroversion < 0.3f)
            prompt.Append("You are shy and reserved. ");
        
        if (student.academicMotivation > 0.7f)
            prompt.Append("You are motivated to learn. ");
        else if (student.academicMotivation < 0.3f)
            prompt.Append("You lack motivation. ");
        
        if (student.rebelliousness > 0.7f)
            prompt.Append("You tend to be rebellious. ");
        
        // Add the question
        prompt.Append($"The teacher asks: \"{question}\" ");
        
        // Prefer Hebrew by default, but respond in the language of the question
        bool isHebrewQuestion = ContainsHebrew(question);
        if (isHebrewQuestion || preferHebrew)
        {
            prompt.Append("Respond naturally as this student would, in Hebrew. ");
        }
        else
        {
            prompt.Append("Respond naturally as this student would, in the same language as the question. ");
        }
        prompt.Append("Keep the response short and realistic (1-2 sentences max).");
        
        return prompt.ToString();
    }

    /// <summary>
    /// Generate response using HuggingFace API
    /// </summary>
    private IEnumerator GenerateWithHuggingFace(string prompt, System.Action<string> onComplete)
    {
        string apiUrl = $"https://api-inference.huggingface.co/models/{modelId}";
        
        // Create JSON payload
        string jsonPayload = $"{{\"inputs\": \"{EscapeJson(prompt)}\", \"parameters\": {{\"max_length\": {maxResponseLength}, \"return_full_text\": false}}}}";
        
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
        
        using (var www = UnityEngine.Networking.UnityWebRequest.PostWwwForm(apiUrl, ""))
        {
            www.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            
            if (!string.IsNullOrEmpty(huggingFaceToken))
            {
                www.SetRequestHeader("Authorization", $"Bearer {huggingFaceToken}");
            }
            
            yield return www.SendWebRequest();
            
            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                string response = ParseHuggingFaceResponse(www.downloadHandler.text);
                onComplete?.Invoke(response);
            }
            else
            {
                Debug.LogWarning($"[StudentAI] HuggingFace API error: {www.error}");
                onComplete?.Invoke("");
            }
        }
    }

    /// <summary>
    /// Generate fallback response using rule-based system (no API needed)
    /// </summary>
    private string GenerateFallbackResponse(StudentAgent student, string question)
    {
        string lowerQuestion = question.ToLower();
        
        // Determine response style based on emotions
        bool isHappy = student.emotions.Happiness >= 7f;
        bool isSad = student.emotions.Sadness >= 7f;
        bool isAngry = student.emotions.Anger >= 7f;
        bool isBored = student.emotions.Boredom >= 7f;
        bool isFrustrated = student.emotions.Frustration >= 7f;
        bool isShy = student.extroversion < 0.3f;
        bool isMotivated = student.academicMotivation >= 0.7f;
        bool isNeutral = !isHappy && !isSad && !isAngry && !isBored && !isFrustrated;
        
        // Check if question is in Hebrew
        bool isHebrew = ContainsHebrew(question);
        
        // Default to Hebrew if preferHebrew is true, otherwise use question language
        bool useHebrew = preferHebrew || isHebrew;
        
        // First, check if this is a feeling question (e.g., "How was your day?")
        if (IsFeelingQuestion(question))
        {
            return GenerateFeelingResponse(student, useHebrew, isHappy, isSad, isAngry, isBored, isFrustrated, isNeutral);
        }
        
        // Then check for generic classroom questions
        string genericResponse = CheckGenericQuestions(question, student, useHebrew);
        if (genericResponse != null)
        {
            return genericResponse;
        }
        
        List<string> responses = new List<string>();
        
        if (isHappy && isMotivated)
        {
            if (useHebrew)
                responses.AddRange(new[] { "אני חושב שהתשובה היא...", "כן, אני יודע!", "בואו ננסה יחד!", "אני יכול לענות על זה!", "זה קל!" });
            else
                responses.AddRange(new[] { "I think the answer is...", "Yes, I know this!", "Let's try together!" });
        }
        else if (isBored)
        {
            if (useHebrew)
                responses.AddRange(new[] { "אה... אולי?", "לא בטוח.", "לא יודע בדיוק.", "זה משעמם...", "לא מעניין אותי." });
            else
                responses.AddRange(new[] { "Uh... maybe?", "Not sure.", "Don't really know." });
        }
        else if (isSad || isFrustrated)
        {
            if (useHebrew)
                responses.AddRange(new[] { "אני לא בטוח.", "קשה לי להבין.", "לא חושב שאני יודע.", "זה קשה מדי.", "אני לא מבין." });
            else
                responses.AddRange(new[] { "I'm not sure.", "It's hard for me.", "Don't think I know." });
        }
        else if (isAngry)
        {
            if (useHebrew)
                responses.AddRange(new[] { "אני לא רוצה לענות.", "לא אכפת לי.", "למה אני?", "תשאלו מישהו אחר.", "לא!" });
            else
                responses.AddRange(new[] { "I don't want to answer.", "Don't care.", "Why me?" });
        }
        else if (isShy)
        {
            if (useHebrew)
                responses.AddRange(new[] { "אה... אולי...", "אני לא בטוח...", "יכול להיות...", "אני חושב ש...", "אולי..." });
            else
                responses.AddRange(new[] { "Um... maybe...", "I'm not sure...", "Could be..." });
        }
        else
        {
            // Neutral responses - more variety for Hebrew
            if (useHebrew)
                responses.AddRange(new[] { "אני חושב ש...", "נראה לי ש...", "יכול להיות ש...", "אולי התשובה היא...", "אני מאמין ש...", "זה נראה כמו..." });
            else
                responses.AddRange(new[] { "I think...", "It seems like...", "Could be that..." });
        }
        
        // Select random response
        if (responses.Count > 0)
        {
            string response = responses[Random.Range(0, responses.Count)];
            
            if (logResponses)
                Debug.Log($"[StudentAI] {student.studentName} says: {response}");
            
            return response;
        }
        
        return isHebrew ? "לא יודע" : "I don't know";
    }

    /// <summary>
    /// Parse HuggingFace API response
    /// </summary>
    private string ParseHuggingFaceResponse(string jsonResponse)
    {
        try
        {
            // Simple JSON parsing - extract generated text
            if (jsonResponse.Contains("\"generated_text\""))
            {
                int start = jsonResponse.IndexOf("\"generated_text\"") + 17;
                int end = jsonResponse.IndexOf("\"", start + 1);
                if (end > start)
                {
                    string text = jsonResponse.Substring(start, end - start);
                    // Clean up the response
                    text = text.Replace("\\n", " ").Replace("\\r", " ").Trim();
                    if (text.Length > maxResponseLength)
                        text = text.Substring(0, maxResponseLength);
                    return text;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[StudentAI] Failed to parse response: {e.Message}");
        }
        
        return "";
    }

    private string EscapeJson(string text)
    {
        return text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
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
    /// Clear response cache
    /// </summary>
    public void ClearCache()
    {
        responseCache.Clear();
    }

    /// <summary>
    /// Check if the question is about feelings/mood
    /// </summary>
    private bool IsFeelingQuestion(string question)
    {
        string lowerQuestion = question.ToLower();
        string[] feelingIndicators = {
            // Hebrew
            "מרגישים", "מרגיש", "שלומכם", "שלומך", "שלום", "איך אתם", "איך את", "מה נשמע",
            "מה קורה", "מה המצב", "הכל בסדר", "בסדר", "מה איתכם", "מה איתך",
            "איך היה היום", "איך היה", "איך הייתם", "איך הייתה",
            // English
            "how are you", "how do you feel", "feeling", "how's everyone", "what's up", "how's it going",
            "how was your day", "how was today", "how were you"
        };
 
        foreach (string indicator in feelingIndicators)
        {
            if (lowerQuestion.Contains(indicator.ToLower()))
                return true;
        }
        return false;
    }
 
    /// <summary>
    /// Generate response based on student's emotional state for feeling questions
    /// </summary>
    private string GenerateFeelingResponse(StudentAgent student, bool useHebrew, bool isHappy, bool isSad, bool isAngry, bool isBored, bool isFrustrated, bool isNeutral)
    {
        List<string> responses = new List<string>();
 
        if (useHebrew)
        {
            if (isHappy)
            {
                responses.AddRange(new[] {
                    "מעולה!",
                    "טוב מאוד!",
                    "סבבה!",
                    "הכל טוב!",
                    "בסדר גמור!",
                    "טוב!"
                });
            }
            else if (isNeutral)
            {
                responses.AddRange(new[] {
                    "בסדר",
                    "ככה ככה",
                    "בסדר גמור",
                    "סבבה",
                    "הכל טוב",
                    "אוקיי"
                });
            }
            else if (isSad)
            {
                responses.AddRange(new[] {
                    "לא טוב",
                    "לא כל כך טוב",
                    "ככה",
                    "יכול להיות יותר טוב",
                    "לא משהו",
                    "לא טוב היום"
                });
            }
            else if (isFrustrated || isBored)
            {
                responses.AddRange(new[] {
                    "משעמם",
                    "משעמם לנו",
                    "לא טוב",
                    "עייפים",
                    "ממש משעמם",
                    "לא משהו"
                });
            }
            else if (isAngry)
            {
                responses.AddRange(new[] {
                    "לא טוב",
                    "לא בכיף",
                    "רע",
                    "עצבני היום",
                    "לא במצב רוח",
                    "לא טוב"
                });
            }
        }
        else
        {
            if (isHappy)
            {
                responses.AddRange(new[] { "Good!", "Great!", "Fine, thanks!", "Doing well!" });
            }
            else if (isNeutral)
            {
                responses.AddRange(new[] { "Okay", "Fine", "Alright", "So-so" });
            }
            else if (isSad)
            {
                responses.AddRange(new[] { "Not great...", "Could be better", "Not so good" });
            }
            else if (isFrustrated || isBored)
            {
                responses.AddRange(new[] { "Bored...", "When's break?", "Tired..." });
            }
            else if (isAngry)
            {
                responses.AddRange(new[] { "Not good", "Bad", "Not in the mood" });
            }
        }
 
        if (responses.Count > 0)
        {
            string response = responses[Random.Range(0, responses.Count)];
            if (logResponses)
                Debug.Log($"[StudentAI] {student.studentName} says: {response}");
            return response;
        }
 
        return useHebrew ? "בסדר" : "Okay";
    }
 
    /// <summary>
    /// Check for other generic classroom questions and return appropriate responses
    /// </summary>
    private string CheckGenericQuestions(string question, StudentAgent student, bool useHebrew)
    {
        bool isHappy = student.emotions.Happiness >= 7f;
        bool isBored = student.emotions.Boredom >= 7f;
        bool isFrustrated = student.emotions.Frustration >= 7f;
 
        List<string> responses = new List<string>();
 
        // "Did you understand?" / "הבנתם?"
        if (question.Contains("הבנתם") || question.Contains("הבנת") || question.Contains("מובן") ||
            question.Contains("understand") || question.Contains("got it") || question.Contains("clear"))
        {
            if (useHebrew)
            {
                if (isHappy || student.academicMotivation > 0.7f)
                    responses.AddRange(new[] { "כן!", "הבנתי!", "ברור!", "כן המורה" });
                else if (isBored)
                    responses.AddRange(new[] { "כן כן...", "בסדר...", "אוקיי" });
                else
                    responses.AddRange(new[] { "לא בטוח...", "אפשר שוב?", "לא ממש...", "יכול להיות" });
            }
            else
            {
                if (isHappy || student.academicMotivation > 0.7f)
                    responses.AddRange(new[] { "Yes!", "Got it!", "Clear!" });
                else
                    responses.AddRange(new[] { "Not sure...", "Can you repeat?", "Maybe..." });
            }
        }
        // "Ready?" / "מוכנים?"
        else if (question.Contains("מוכנים") || question.Contains("מוכן") || question.Contains("ready"))
        {
            if (useHebrew)
            {
                if (isHappy)
                    responses.AddRange(new[] { "כן!", "מוכנים!", "יאללה!", "בוא!" });
                else if (isBored || isFrustrated)
                    responses.AddRange(new[] { "לא...", "עוד רגע", "רגע...", "אממ..." });
                else
                    responses.AddRange(new[] { "כן", "מוכן", "בסדר", "אוקיי" });
            }
            else
            {
                if (isHappy)
                    responses.AddRange(new[] { "Yes!", "Ready!", "Let's go!" });
                else
                    responses.AddRange(new[] { "Not yet...", "One moment", "Okay" });
            }
        }
        // "Any questions?" / "יש שאלות?"
        else if (question.Contains("שאלות") || question.Contains("שאלה") || question.Contains("questions"))
        {
            if (useHebrew)
            {
                if (student.academicMotivation > 0.7f)
                    responses.AddRange(new[] { "כן, יש לי שאלה!", "אפשר לשאול?", "יש לי!" });
                else
                    responses.AddRange(new[] { "לא", "אין", "לא כרגע", "הכל ברור" });
            }
            else
            {
                if (student.academicMotivation > 0.7f)
                    responses.AddRange(new[] { "Yes, I have a question!", "Can I ask?" });
                else
                    responses.AddRange(new[] { "No", "None", "All clear" });
            }
        }
        // "Want a break?" / "רוצים הפסקה?"
        else if (question.Contains("הפסקה") || question.Contains("break"))
        {
            if (useHebrew)
            {
                responses.AddRange(new[] { "כן!", "כן בבקשה!", "יאללה הפסקה!", "סוף סוף!", "כן המורה!" });
            }
            else
            {
                responses.AddRange(new[] { "Yes!", "Yes please!", "Finally!", "Yes teacher!" });
            }
        }
        // "Who wants to answer?" / "מי רוצה לענות?"
        else if (question.Contains("מי רוצה") || question.Contains("who wants"))
        {
            if (useHebrew)
            {
                if (isHappy && student.academicMotivation > 0.6f)
                    responses.AddRange(new[] { "אני!", "אני רוצה!", "בחר בי!", "אני אני!" });
                else if (isBored)
                    responses.AddRange(new[] { "...", "לא אני", "מישהו אחר" });
                else
                    responses.AddRange(new[] { "אולי אני...", "יכול להיות...", "אממ..." });
            }
            else
            {
                if (isHappy && student.academicMotivation > 0.6f)
                    responses.AddRange(new[] { "Me!", "I want to!", "Pick me!" });
                else
                    responses.AddRange(new[] { "...", "Not me", "Someone else" });
            }
        }
        // "What did you learn today?" / "מה למדתם היום?"
        else if (question.Contains("למדתם") || question.Contains("למדנו") || question.Contains("learn") || question.Contains("learned"))
        {
            if (useHebrew)
            {
                if (student.academicMotivation > 0.6f)
                    responses.AddRange(new[] { "דברים מעניינים!", "הרבה!", "זה היה מגניב" });
                else if (isBored)
                    responses.AddRange(new[] { "לא זוכר...", "מה?", "לא יודע" });
                else
                    responses.AddRange(new[] { "כמה דברים", "יש לי רעיון", "משהו..." });
            }
            else
            {
                if (student.academicMotivation > 0.6f)
                    responses.AddRange(new[] { "Interesting stuff!", "A lot!", "It was cool" });
                else
                    responses.AddRange(new[] { "Don't remember...", "What?", "Something..." });
            }
        }
        // "Good morning" / "בוקר טוב"
        else if (question.Contains("בוקר טוב") || question.Contains("good morning"))
        {
            if (useHebrew)
            {
                if (isHappy)
                    responses.AddRange(new[] { "בוקר טוב!", "בוקר אור!", "בוקר טוב המורה!" });
                else
                    responses.AddRange(new[] { "בוקר...", "בוקר טוב", "היי..." });
            }
            else
            {
                responses.AddRange(new[] { "Good morning!", "Morning!", "Hi!" });
            }
        }
              // "Did you do homework?" / "עשיתם שיעורי בית?"
        else if (question.Contains("שיעורי בית") || question.Contains("homework"))
        {
            if (useHebrew)
            {
                if (student.academicMotivation > 0.7f)
                    responses.AddRange(new[] { "כן!", "עשיתי הכל!", "ברור!" });
                else if (isBored || student.academicMotivation < 0.4f)
                    responses.AddRange(new[] { "אופס...", "שכחתי...", "כמעט...", "לא הספקתי" });
                else
                    responses.AddRange(new[] { "כן", "עשיתי", "רוב זה", "כמעט הכל" });
            }
            else
            {
                if (student.academicMotivation > 0.7f)
                    responses.AddRange(new[] { "Yes!", "All done!", "Of course!" });
                else
                    responses.AddRange(new[] { "Oops...", "I forgot...", "Almost..." });
            }
        }
        // "Who's talking?" / "מי מדבר?"
        else if (question.Contains("מי מדבר") || question.Contains("who's talking") || question.Contains("who is talking"))
        {
            if (useHebrew)
            {
                responses.AddRange(new[] { "לא אני!", "זה לא אני", "...", "מה?" });
            }
            else
            {
                responses.AddRange(new[] { "Not me!", "Wasn't me", "...", "What?" });
            }
        }
        // "Are you listening?" / "אתם מקשיבים?"
        else if (question.Contains("מקשיבים") || question.Contains("מקשיב") || question.Contains("listening") || question.Contains("paying attention"))
        {
            if (useHebrew)
            {
                if (isBored)
                    responses.AddRange(new[] { "כן כן...", "אה... כן", "מה?", "בטח..." });
                else
                    responses.AddRange(new[] { "כן!", "מקשיבים!", "ברור!", "כן המורה" });
            }
            else
            {
                if (isBored)
                    responses.AddRange(new[] { "Yeah yeah...", "Uh... yes", "What?", "Sure..." });
                else
                    responses.AddRange(new[] { "Yes!", "Listening!", "Of course!" });
            }
        }
        // "Do you like it?" / "אתם אוהבים?"
        else if (question.Contains("אוהבים") || question.Contains("אהבתם") || question.Contains("נהנים") ||
                 question.Contains("do you like") || question.Contains("enjoying"))
        {
            if (useHebrew)
            {
                if (isHappy)
                    responses.AddRange(new[] { "כן!", "מאוד!", "אוהבים!", "זה כיף!" });
                else if (isBored)
                    responses.AddRange(new[] { "ככה ככה...", "לא ממש...", "בסדר..." });
                else
                    responses.AddRange(new[] { "כן", "זה בסדר", "נחמד", "אוקיי" });
            }
            else
            {
                if (isHappy)
                    responses.AddRange(new[] { "Yes!", "A lot!", "Love it!", "It's fun!" });
                else
                    responses.AddRange(new[] { "So-so...", "Not really...", "It's okay..." });
            }
        }
        // "What's the answer?" / "מה התשובה?"
        else if (question.Contains("התשובה") || question.Contains("the answer"))
        {
            if (useHebrew)
            {
                if (student.academicMotivation > 0.6f)
                    responses.AddRange(new[] { "אני יודע!", "יש לי!", "רגע...", "אממ..." });
                else
                    responses.AddRange(new[] { "לא יודע", "מה?", "...", "לא בטוח" });
            }
            else
            {
                if (student.academicMotivation > 0.6f)
                    responses.AddRange(new[] { "I know!", "I got it!", "Wait...", "Umm..." });
                else
                    responses.AddRange(new[] { "Don't know", "What?", "...", "Not sure" });
            }
        }
        // "Finished?" / "סיימתם?"
        else if (question.Contains("סיימתם") || question.Contains("סיימת") || question.Contains("גמרתם") ||
                 question.Contains("finished") || question.Contains("done"))
        {
            if (useHebrew)
            {
                if (student.academicMotivation > 0.6f)
                    responses.AddRange(new[] { "כן!", "סיימתי!", "כבר מזמן!", "עוד רגע" });
                else
                    responses.AddRange(new[] { "כמעט...", "עוד קצת", "לא עדיין", "רגע..." });
            }
            else
            {
                if (student.academicMotivation > 0.6f)
                    responses.AddRange(new[] { "Yes!", "Done!", "Already!", "Almost" });
                else
                    responses.AddRange(new[] { "Almost...", "A bit more", "Not yet", "Wait..." });
            }
        }
        // "Do you need help?" / "צריכים עזרה?"
        else if (question.Contains("עזרה") || question.Contains("help"))
        {
            if (useHebrew)
            {
                if (isFrustrated || student.emotions.Sadness > 6f)
                    responses.AddRange(new[] { "כן...", "קצת", "יכול להיות", "כן בבקשה" });
                else
                    responses.AddRange(new[] { "לא צריך", "הכל טוב", "בסדר", "לא תודה" });
            }
            else
            {
                if (isFrustrated || student.emotions.Sadness > 6f)
                    responses.AddRange(new[] { "Yes...", "A little", "Maybe", "Yes please" });
                else
                    responses.AddRange(new[] { "No need", "All good", "Fine", "No thanks" });
            }
        }
 
        if (responses.Count > 0)
        {
            string response = responses[Random.Range(0, responses.Count)];
            if (logResponses)
                Debug.Log($"[StudentAI] {student.studentName} says: {response}");
            return response;
        }
 
        return null; // No generic question matched
    }
}