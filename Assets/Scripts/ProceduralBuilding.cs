using System.Collections;
using UnityEngine;

public class ProceduralBuilding : MonoBehaviour
{
    private Renderer rend;

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
        // Deterministic seed
        int seed = cell.x * 73856093 ^ cell.y * 19349663;
        Random.InitState(seed);

        // HEIGHT
        float height = Random.Range(minHeight, maxHeight);

        transform.localScale = new Vector3(
            blockSize,
            height,
            blockSize
        );

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

        // ANIMATION
        StartCoroutine(
            RiseBuilding(startPos, finalPos, riseDuration)
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