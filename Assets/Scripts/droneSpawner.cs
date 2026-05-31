using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class GlobalPrefabSpawner : MonoBehaviour
{
    public GameObject prefabToSpawn;

    private void Awake()
    {
        if (prefabToSpawn != null)
        {
            StartCoroutine(spawnAfter());
        }
    }

    private IEnumerator spawnAfter()
    {
        yield return new WaitForSeconds(2.5f);
        // Spawn at this object's world position/rotation,
        // but NOT parented to the building.
        Instantiate(
            prefabToSpawn,
            transform.position,
            transform.rotation
        );
    }
}