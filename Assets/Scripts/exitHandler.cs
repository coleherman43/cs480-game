using UnityEngine;

public class exitHandler : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            GameEvents.gameOver?.Invoke(true);
        }
    }
}
