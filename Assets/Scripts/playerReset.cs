using UnityEngine;
using UnityEngine.SceneManagement;

public class playerReset : MonoBehaviour
{
    public Transform respawnPosition;
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
