//Basic script for bottom of map object to reset scene if player falls.
using UnityEngine;
using UnityEngine.SceneManagement;

public class playerReset : MonoBehaviour
{
    public Transform respawnPosition;
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
           GameEvents.gameOver?.Invoke(false);
        }
    }
}
