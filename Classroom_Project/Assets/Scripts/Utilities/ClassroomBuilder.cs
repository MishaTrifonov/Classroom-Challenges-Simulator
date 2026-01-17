using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class ClassroomBuilder : MonoBehaviour
{
    public GameObject floorPrefab;
    public GameObject wallPrefab;

    public float roomWidth = 20f;
    public float roomDepth = 20f;

    [ContextMenu("Build Classroom")]
    public void BuildClassroom()
    {
        ClearRoom();

        GameObject floor = BuildFloor();
        BuildWalls(floor);
    }

    // ---------- FLOOR ----------
    GameObject BuildFloor()
    {
        GameObject floor = Instantiate(floorPrefab, transform);
        floor.name = "Floor";
        floor.transform.localPosition = Vector3.zero;
        floor.transform.localRotation = Quaternion.identity;
        floor.transform.localScale = new Vector3(roomWidth, 1f, roomDepth);
        return floor;
    }

    // ---------- WALLS ----------
    void BuildWalls(GameObject floor)
    {
        Bounds floorBounds = floor.GetComponent<Renderer>().bounds;

        // קיר לדוגמה למדידת גובה + עובי
        GameObject temp = Instantiate(wallPrefab);
        Renderer wr = temp.GetComponent<Renderer>();
        float wallHeight = wr.bounds.size.y;
        float wallThickness = Mathf.Min(wr.bounds.size.x, wr.bounds.size.z);
        DestroyImmediate(temp);

        float y = floorBounds.max.y;

        // ===== קדמי =====
        CreateWall(
            new Vector3(0, y, floorBounds.max.z + wallThickness / 2f),
            new Vector3(roomWidth, 1, 1),
            90
        );

        // ===== אחורי =====
        CreateWall(
            new Vector3(0, y, floorBounds.min.z - wallThickness / 2f),
            new Vector3(roomWidth, 1, 1),
            90
        );

        // ===== שמאל =====
        CreateWall(
            new Vector3(floorBounds.min.x - wallThickness / 2f, y, 0),
            new Vector3(roomDepth, 1, 1),
            0
        );

        // ===== ימין =====
        CreateWall(
            new Vector3(floorBounds.max.x + wallThickness / 2f, y, 0),
            new Vector3(roomDepth, 1, 1),
            0
        );
    }

    void CreateWall(Vector3 pos, Vector3 scaleXZ, float rotY)
    {
        GameObject wall = Instantiate(wallPrefab, transform);
        wall.name = "Wall";

        wall.transform.position = pos;
        wall.transform.rotation = Quaternion.Euler(0, rotY, 0);

        // ⚠️ חשוב: מותחים רק את האורך, לא את הגובה
        wall.transform.localScale = new Vector3(
            scaleXZ.x,
            1f,
            scaleXZ.z
        );
    }

    // ---------- CLEAN ----------
    [ContextMenu("Clear Room")]
    public void ClearRoom()
    {
        List<GameObject> list = new List<GameObject>();
        foreach (Transform t in transform)
            list.Add(t.gameObject);

        list.ForEach(o => DestroyImmediate(o));
    }
}
