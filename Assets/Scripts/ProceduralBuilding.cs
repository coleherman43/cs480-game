using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralBuilding : MonoBehaviour
{
    private Renderer rend;

    [Header("Rooftop Generation")]
    public List<GameObject> rooftopPrefabs = new List<GameObject>();

    [Tooltip("Empty child object used as rooftop spawn anchor")]
    public GameObject rooftopAnchor;

    void Awake()
    {
        rend = GetComponent<Renderer>();
    }

    public void Initialize(
        Vector2Int cell,
        float blockSize,
        float minHeight,
        float maxHeight,
        float spawnDepth,
        float riseDuration,
        Material[] materials)
    {
        /*
        // Deterministic seed
        int seed = cell.x * 73856093 ^ cell.y * 19349663;
        Random.InitState(seed);
        */
        // HEIGHT
        float height = Random.Range(minHeight, maxHeight);

        transform.localScale = new Vector3(
            blockSize,
            height,
            blockSize
        );

        // Move rooftop anchor to top of cube
        if (rooftopAnchor != null)
        {
            rooftopAnchor.transform.localPosition =
                new Vector3(0f, 0.5f, 0f);
        }

        // MATERIAL
        int materialIndex = Random.Range(0, materials.Length);

        if (rend != null)
        {
            rend.material = materials[materialIndex];
        }

        // POSITIONING
        Vector3 finalPos = new Vector3(
            transform.position.x,
            height / 2f,
            transform.position.z
        );

        Vector3 startPos = new Vector3(
            finalPos.x,
            spawnDepth,
            finalPos.z
        );

        transform.position = startPos;

        // ROOFTOP GENERATION
        GenerateRooftop();

        // ANIMATION
        StartCoroutine(
            RiseBuilding(startPos, finalPos, riseDuration)
        );
    }

    void GenerateRooftop()
    {
        // Safety checks
        if (rooftopAnchor == null)
        {
            Debug.LogWarning(
                $"No rooftop anchor assigned on {gameObject.name}"
            );

            return;
        }

        if (rooftopPrefabs == null ||
            rooftopPrefabs.Count == 0)
        {
            return;
        }

        // Pick random rooftop prefab
        int prefabIndex =
            Random.Range(0, rooftopPrefabs.Count);

        GameObject rooftopPrefab =
            rooftopPrefabs[prefabIndex];

        // Random rotation:
        // 0, 90, or 180
        int[] rotations = { 0, 90, 180 };

        int rotationY =
            rotations[
                Random.Range(0, rotations.Length)
            ];

        Quaternion rotation =
            Quaternion.Euler(0f, rotationY, 0f);

        GameObject rooftop = Instantiate(
            rooftopPrefab,
            rooftopAnchor.transform.position,
            rotation,
            rooftopAnchor.transform
        );

        rooftop.transform.localPosition =
            Vector3.zero;

        // Preserve authored prefab scale
        Vector3 prefabScale =
            rooftopPrefab.transform.localScale;
        
        rooftop.transform.localScale = new Vector3(
            prefabScale.x,
            prefabScale.y * 50f / transform.localScale.y,
            prefabScale.z
        );
        
    }

    IEnumerator RiseBuilding(
        Vector3 startPos,
        Vector3 finalPos,
        float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / duration;

            t = Mathf.SmoothStep(0f, 1f, t);

            transform.position =
                Vector3.Lerp(startPos, finalPos, t);

            yield return null;
        }

        transform.position = finalPos;
    }
}