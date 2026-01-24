using UnityEngine;
using System.Collections.Generic;

public class StudentSpawner : MonoBehaviour
{
    public enum SpawnMode { Seats, RandomSeats, Grid }

    [Header("Prefab")]
    public GameObject studentPrefab;

    [Header("Seat Spawns")]
    [Tooltip("Parent GameObject containing spawn point children (e.g., SpawnPoint_01, SpawnPoint_02, etc.). If null, will use Spawned Students Parent.")]
    public Transform seatParent;

    [Header("Spawn Mode")]
    public SpawnMode spawnMode = SpawnMode.Seats;
    public bool shuffleProfiles = false;

    [Header("Grid Settings")]
    public Vector3 gridOrigin;
    public float gridXSpacing = 2.5f;
    public float gridZSpacing = 2.5f;
    public int gridColumns = 5;

    [Header("Height Adjustment")]
    [Tooltip("Y offset to raise students above spawn point. Increase this if students appear below chairs. For scale (5,6,5) models, try values between 2-4.")]
    public float heightOffset = 3.0f;
    
    [Tooltip("Automatically adjust height based on the model's scale (recommended for scaled models)")]
    public bool useScaleBasedOffset = true;
    
    [Tooltip("Multiplier for scale-based height. Increase if students sink, decrease if they float. Default 0.5 works for most models.")]
    public float scaleMultiplier = 0.5f;

    [Header("Parenting")]
    public Transform spawnedStudentsParent;

    private List<StudentAgent> activeStudents = new List<StudentAgent>();

    public List<StudentAgent> Spawn(List<StudentProfile> profiles)
    {
        Clear();

        if (studentPrefab == null)
        {
            Debug.LogError("[StudentSpawner] Student prefab is not assigned!");
            return activeStudents;
        }

        if (profiles == null || profiles.Count == 0)
        {
            Debug.LogWarning("[StudentSpawner] No student profiles provided.");
            return activeStudents;
        }

        List<StudentProfile> list = new List<StudentProfile>(profiles);

        if (shuffleProfiles)
            Shuffle(list);

        // Auto-use spawnedStudentsParent as seatParent if seatParent is not set
        if (seatParent == null && spawnedStudentsParent != null)
        {
            seatParent = spawnedStudentsParent;
            Debug.Log($"[StudentSpawner] seatParent was null, using spawnedStudentsParent ({spawnedStudentsParent.name}) as seat parent.");
        }

        List<Transform> seats = GetSeats();
        
        if (seats.Count == 0 && spawnMode != SpawnMode.Grid)
        {
            Debug.LogWarning($"[StudentSpawner] No spawn points found in seatParent! Found {seats.Count} spawn points. Using grid mode as fallback.");
        }
        else
        {
            Debug.Log($"[StudentSpawner] Found {seats.Count} spawn points from '{seatParent?.name ?? "null"}'");
        }

        if (spawnMode == SpawnMode.RandomSeats)
            Shuffle(seats);

        for (int i = 0; i < list.Count; i++)
        {
            Vector3 pos;
            Quaternion rot = Quaternion.identity;

            if (spawnMode == SpawnMode.Grid || i >= seats.Count)
            {
                int col = (gridColumns <= 0) ? 0 : i % gridColumns;
                int row = (gridColumns <= 0) ? i : i / gridColumns;
                pos = gridOrigin + new Vector3(col * gridXSpacing, 0, row * gridZSpacing);
            }
            else
            {
                if (seats[i] == null)
                {
                    Debug.LogWarning($"[StudentSpawner] Seat at index {i} is null, using grid position instead.");
                    int col = (gridColumns <= 0) ? 0 : i % gridColumns;
                    int row = (gridColumns <= 0) ? i : i / gridColumns;
                    pos = gridOrigin + new Vector3(col * gridXSpacing, 0, row * gridZSpacing);
                }
                else
                {
                    pos = seats[i].position;
                    rot = seats[i].rotation;
                }
            }

            Transform parentToUse = spawnedStudentsParent != null ? spawnedStudentsParent : transform;
            GameObject go = Instantiate(studentPrefab, pos, rot, parentToUse);
            
            // Ensure the instantiated object is active
            if (!go.activeSelf)
            {
                go.SetActive(true);
            }

            // Ensure all renderers are enabled and visible
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                if (r != null)
                {
                    r.enabled = true;
                    r.gameObject.SetActive(true);
                }
            }

            // Ensure scale is reasonable (not zero or too small)
            if (go.transform.localScale.magnitude < 0.01f)
            {
                Debug.LogWarning($"[StudentSpawner] Student '{go.name}' has invalid scale: {go.transform.localScale}. Setting to (1,1,1)");
                go.transform.localScale = Vector3.one;
            }
            
            // Calculate and apply proper height offset based on scale
            float finalHeightOffset = heightOffset;
            
            if (useScaleBasedOffset)
            {
                // For models with extreme scales (like 5,6,5), we need to account for the scale
                float yScale = go.transform.localScale.y;
                
                // Calculate scale-based offset
                // The idea: bigger models need more offset to sit properly
                float scaleBasedOffset = yScale * scaleMultiplier;
                
                // Combine manual offset with scale-based offset
                finalHeightOffset = heightOffset + scaleBasedOffset;
                
                Debug.Log($"[StudentSpawner] Scale-based offset for '{list[i].name}': " +
                    $"Y Scale={yScale}, Base Offset={heightOffset}, Scale Offset={scaleBasedOffset}, Final={finalHeightOffset}");
            }
            
            // Apply the final height offset
            Vector3 finalPos = go.transform.position;
            finalPos.y += finalHeightOffset;
            go.transform.position = finalPos;

            StudentAgent student = go.GetComponent<StudentAgent>();

            if (student == null)
            {
                Debug.LogError("[StudentSpawner] Student prefab missing StudentAgent component!");
                Destroy(go);
                continue;
            }

            ApplyProfile(student, list[i]);
            
            // Ensure StudentResponseBubble component exists for voice responses
            StudentResponseBubble responseBubble = go.GetComponentInChildren<StudentResponseBubble>();
            if (responseBubble == null)
            {
                responseBubble = go.GetComponent<StudentResponseBubble>();
                if (responseBubble == null)
                {
                    responseBubble = go.AddComponent<StudentResponseBubble>();
                    Debug.Log($"[StudentSpawner] Added StudentResponseBubble to {student.studentName}");
                }
            }

            // Ensure StudentQuestionResponder component exists for question handling
            StudentQuestionResponder questionResponder = go.GetComponent<StudentQuestionResponder>();
            if (questionResponder == null)
            {
                questionResponder = go.AddComponent<StudentQuestionResponder>();
                Debug.Log($"[StudentSpawner] Added StudentQuestionResponder to {student.studentName}");
            }
            
            // Set seat position reference for the student
            if (spawnMode != SpawnMode.Grid && i < seats.Count && seats[i] != null)
            {
                student.seatPosition = seats[i];
            }
            else
            {
                // Create a reference point for grid-spawned students
                if (student.seatPosition == null)
                {
                    GameObject seatObj = new GameObject($"Seat_{student.studentName}");
                    seatObj.transform.position = pos;
                    seatObj.transform.rotation = rot;
                    student.seatPosition = seatObj.transform;
                }
            }
            
            activeStudents.Add(student);
            
            // Diagnostic logging
            Debug.Log($"[StudentSpawner] ✓ Spawned '{student.studentName}' at Y={go.transform.position.y:F2}, " +
                $"Scale={go.transform.localScale}, Applied Offset={finalHeightOffset:F2}");
        }

        return activeStudents;
    }

    public void Clear()
    {
        foreach (var s in activeStudents)
            if (s != null) Destroy(s.gameObject);

        activeStudents.Clear();
    }

    void ApplyProfile(StudentAgent s, StudentProfile p)
    {
        if (s == null || p == null)
        {
            Debug.LogError("[StudentSpawner] Cannot apply profile - StudentAgent or StudentProfile is null!");
            return;
        }

        s.studentId = p.id ?? "";
        s.studentName = p.name ?? "Unnamed Student";

        s.extroversion = Mathf.Clamp01(p.extroversion);
        s.sensitivity = Mathf.Clamp01(p.sensitivity);
        s.rebelliousness = Mathf.Clamp01(p.rebelliousness);
        s.academicMotivation = Mathf.Clamp01(p.academicMotivation);

        // Clamp emotion values to valid range (1-10)
        s.emotions.Happiness = Mathf.Clamp(p.initialHappiness, 1f, 10f);
        s.emotions.Boredom = Mathf.Clamp(p.initialBoredom, 1f, 10f);
    }

    List<Transform> GetSeats()
    {
        List<Transform> seats = new List<Transform>();
        if (seatParent == null) return seats;

        // Get all child transforms (spawn points) from the seat parent
        for (int i = 0; i < seatParent.childCount; i++)
        {
            Transform child = seatParent.GetChild(i);
            if (child != null)
            {
                seats.Add(child);
            }
        }

        return seats;
    }

    void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}