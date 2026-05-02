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
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
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