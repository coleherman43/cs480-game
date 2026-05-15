/* Basic Script for continuously spawning coin pickups in tutorial map. */
using UnityEngine;

public class CoinSpawner : MonoBehaviour
{
    public GameObject coinPrefab;

    public float spawnInterval = 2f;
    public float spawnVariance = 0.5f;

    public float launchForce = 3f;
    public float upwardBias = 2f;

    private float timer;

    void Start()
    {
        ResetTimer();
    }

    void Update()
    {
        //Basic timer check, checks if time elapsed is greater than a set interval.
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            //Spawn coins and reset
            SpawnCoin();
            ResetTimer();
        }
    }

    void ResetTimer()
    {
        timer = spawnInterval + Random.Range(-spawnVariance, spawnVariance);
    }

    void SpawnCoin()
    {
        //Instantiates new pickup objects, and gives them some random velocity to increase randomness.
        GameObject coin = Instantiate(coinPrefab, transform.position, Quaternion.identity);

        Rigidbody rb = coin.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 randomDir = new Vector3(
                Random.Range(-1f, 1f),
                upwardBias,
                Random.Range(-1f, 1f)
            );

            rb.AddForce(randomDir.normalized * launchForce, ForceMode.Impulse);
        }
    }
}