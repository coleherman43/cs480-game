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
    public float blockSize = 45;
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

    void OnEnable()
    {
        GameEvents.gameOver += onOver;
    }

    void OnDisable()
    {
        GameEvents.gameOver -= onOver;
    }

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

        int x = (int)(player.position.x / cellSize);
        int z = (int)(player.position.z / cellSize);

        return new Vector2Int(x , z);
    }

    private int CellOnAxis(int axisPos)
    {
        return 1;
    }

    void UpdateVisibleBuildings()
    {
        Debug.Log("Updating buildings, player is at:" + currentPlayerCell);
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
        Vector3 spawnPos = new Vector3(
            cell.x * cellSize,
            0,
            cell.y * cellSize
        );

        GameObject building =
            Instantiate(buildingPrefab, spawnPos, Quaternion.identity);

        ProceduralBuilding pb =
            building.GetComponent<ProceduralBuilding>();

        if (pb != null)
        {
            Material[] mats = { material1, material2, material3 };

            pb.Initialize(
                cell,
                blockSize,
                minHeight,
                maxHeight,
                spawnDepth,
                riseDuration,
                mats
            );
        }

        activeBuildings.Add(cell, building);
    }
    private void onOver(bool status)
    {
        
    }
}

