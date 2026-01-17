using UnityEngine;
using System.Collections.Generic;

public class ClassroomFurnitureBuilder : MonoBehaviour
{
    [Header("References")]
    public GameObject floor;

    [Header("Prefabs")]
    public GameObject studentDeskPrefab;
    public GameObject studentChairPrefab;
    public GameObject teacherDeskPrefab;
    public GameObject teacherChairPrefab;

    [Header("Layout Settings")]
    public int rows = 3;
    public int columns = 4;

    public float deskSpacingX = 2.2f;
    public float deskSpacingZ = 2.5f;

    public float frontMargin = 3f;   // רווח מהמורה
    public float sideMargin = 1.5f;  // רווח מהקירות

    [ContextMenu("Build Furniture")]
    public void BuildFurniture()
    {
        ClearFurniture();

        if (!floor)
        {
            Debug.LogError("Assign floor reference");
            return;
        }

        BuildTeacherArea();
        BuildStudentArea();
    }

    // ================= TEACHER =================
    void BuildTeacherArea()
    {
        Bounds floorBounds = floor.GetComponent<Renderer>().bounds;

        Vector3 teacherDeskPos = new Vector3(
            floorBounds.center.x,
            floorBounds.max.y,
            floorBounds.max.z - 1.5f
        );

        Instantiate(teacherDeskPrefab, teacherDeskPos, Quaternion.identity, transform);

        if (teacherChairPrefab)
        {
            Vector3 chairPos = teacherDeskPos - new Vector3(0, 0, 1.2f);
            Instantiate(teacherChairPrefab, chairPos, Quaternion.Euler(0, 180, 0), transform);
        }
    }

    // ================= STUDENTS =================
    void BuildStudentArea()
    {
        Bounds floorBounds = floor.GetComponent<Renderer>().bounds;

        float startX = floorBounds.min.x + sideMargin;
        float startZ = floorBounds.min.z + frontMargin;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                float x = startX + c * deskSpacingX;
                float z = startZ + r * deskSpacingZ;

                Vector3 deskPos = new Vector3(x, floorBounds.max.y, z);
                GameObject desk = Instantiate(
                    studentDeskPrefab,
                    deskPos,
                    Quaternion.identity,
                    transform
                );

                // כיסא תלמיד
                if (studentChairPrefab)
                {
                    Vector3 chairPos = deskPos - new Vector3(0, 0, 0.8f);
                    Instantiate(
                        studentChairPrefab,
                        chairPos,
                        Quaternion.Euler(0, 180, 0),
                        transform
                    );
                }
            }
        }
    }

    // ================= CLEAN =================
    [ContextMenu("Clear Furniture")]
    public void ClearFurniture()
    {
        List<GameObject> children = new List<GameObject>();
        foreach (Transform t in transform)
            children.Add(t.gameObject);

        children.ForEach(o => DestroyImmediate(o));
    }
}
