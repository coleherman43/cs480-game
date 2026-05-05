using UnityEngine;

public class playerReset : MonoBehaviour
{
    public Transform respawnPosition;
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            //Debug.Log(other.gameObject.name);
            // This dream is dead until player movement is finalized
        }
    }
}
