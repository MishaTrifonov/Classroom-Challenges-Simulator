using UnityEngine;
using System.Collections.Generic;

public class StudentSpawner : MonoBehaviour
{
    public enum SpawnMode { Seats, RandomSeats, Grid }

    [Header("Prefab")]
    public GameObject studentPrefab;

    [Header("Seat Spawns")]
    public Transform seatParent;

    [Header("Spawn Mode")]
    public SpawnMode spawnMode = SpawnMode.Seats;
    public bool shuffleProfiles = false;

    [Header("Grid Settings")]
    public Vector3 gridOrigin;
    public float gridXSpacing = 2.5f;
    public float gridZSpacing = 2.5f;
    public int gridColumns = 5;

    private List<StudentAgent> activeStudents = new List<StudentAgent>();

    public List<StudentAgent> Spawn(List<StudentProfile> profiles)
    {
        Clear();

        if (profiles == null || profiles.Count == 0)
        {
            Debug.LogWarning("No student profiles provided.");
            return activeStudents;
        }

        List<StudentProfile> list = new List<StudentProfile>(profiles);

        if (shuffleProfiles)
            Shuffle(list);

        List<Transform> seats = GetSeats();

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
                pos = seats[i].position;
                rot = seats[i].rotation;
            }

            GameObject go = Instantiate(studentPrefab, pos, rot, transform);
            StudentAgent student = go.GetComponent<StudentAgent>();

            if (student == null)
            {
                Debug.LogError("Student prefab missing StudentAgent!");
                Destroy(go);
                continue;
            }

            ApplyProfile(student, list[i]);
            activeStudents.Add(student);
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
        s.studentId = p.id;
        s.studentName = p.name;

        s.extroversion = p.extroversion;
        s.sensitivity = p.sensitivity;
        s.rebelliousness = p.rebelliousness;
        s.academicMotivation = p.academicMotivation;

        s.emotions.Happiness = p.initialHappiness;
        s.emotions.Boredom = p.initialBoredom;
    }

    List<Transform> GetSeats()
    {
        List<Transform> seats = new List<Transform>();
        if (seatParent == null) return seats;

        for (int i = 0; i < seatParent.childCount; i++)
            seats.Add(seatParent.GetChild(i));

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
