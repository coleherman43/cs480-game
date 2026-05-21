using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteCityManager : MonoBehaviour
{
    [Header("Materials")]
    public Material material1;
    public Material material2;
    public Material material3;

    [Header("References")]
    public Transform player;
    public GameObject buildingPrefab;

    [Header("Grid Settings")]
    public int cellSize = 50;
    public int renderDistance = 2;

    [Header("Building Settings")]
    public float minHeight = 40f;
    public float maxHeight = 140f;

    [Header("Animation Settings")]
    public float spawnDepth = -120f;
    public float riseDuration = 1.5f;

    private Dictionary<Vector2Int, GameObject> activeBuildings =
        new Dictionary<Vector2Int, GameObject>();

    private Vector2Int currentPlayerCell;

    void Start()
    {
        currentPlayerCell = GetPlayerCell();
        UpdateVisibleBuildings();
    }

    void Update()
    {
        Vector2Int newCell = GetPlayerCell();

        if (newCell != currentPlayerCell)
        {
            currentPlayerCell = newCell;
            UpdateVisibleBuildings();
        }
    }

    Vector2Int GetPlayerCell()
    {
        int x = Mathf.FloorToInt(player.position.x / cellSize);
        int z = Mathf.FloorToInt(player.position.z / cellSize);

        return new Vector2Int(x, z);
    }

    void UpdateVisibleBuildings()
    {
        HashSet<Vector2Int> neededCells = new HashSet<Vector2Int>();

        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int z = -renderDistance; z <= renderDistance; z++)
            {
                Vector2Int cell =
                    new Vector2Int(
                        currentPlayerCell.x + x,
                        currentPlayerCell.y + z
                    );

                neededCells.Add(cell);

                if (!activeBuildings.ContainsKey(cell))
                {
                    SpawnBuilding(cell);
                }
            }
        }

        List<Vector2Int> cellsToRemove = new List<Vector2Int>();

        foreach (var kvp in activeBuildings)
        {
            if (!neededCells.Contains(kvp.Key))
            {
                Destroy(kvp.Value);
                cellsToRemove.Add(kvp.Key);
            }
        }

        foreach (Vector2Int cell in cellsToRemove)
        {
            activeBuildings.Remove(cell);
        }
    }

   void SpawnBuilding(Vector2Int cell)
{
    Vector3 targetPos = new Vector3(
        cell.x * cellSize,
        0,
        cell.y * cellSize
    );

    // Deterministic seed
    int seed = cell.x * 73856093 ^ cell.y * 19349663;
    Random.InitState(seed);

    float height = Random.Range(minHeight, maxHeight);

    Vector3 startPos = new Vector3(
        targetPos.x,
        spawnDepth,
        targetPos.z
    );

    GameObject building =
        Instantiate(buildingPrefab, startPos, Quaternion.identity);

    building.transform.localScale = new Vector3(
        45f,
        height,
        45f
    );

    // FINAL POSITION
    Vector3 finalPos =
        targetPos + Vector3.up * (height / 2f);

    // RANDOM MATERIAL
    Material[] materials = { material1, material2, material3 };

    int materialIndex = Random.Range(0, materials.Length);

    Renderer renderer = building.GetComponent<Renderer>();

    if (renderer != null)
    {
        renderer.material = materials[materialIndex];
    }

    activeBuildings.Add(cell, building);

    StartCoroutine(
        RiseBuilding(building, startPos, finalPos)
    );
}
    IEnumerator RiseBuilding(
        GameObject building,
        Vector3 startPos,
        Vector3 finalPos)
    {
        float elapsed = 0f;

        while (elapsed < riseDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / riseDuration;

            // Smooth easing
            t = Mathf.SmoothStep(0f, 1f, t);

            building.transform.position =
                Vector3.Lerp(startPos, finalPos, t);

            yield return null;
        }

        building.transform.position = finalPos;
    }
}