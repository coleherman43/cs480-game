using UnityEngine;

public class tutorialZone : MonoBehaviour
{
    public string message;

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            GameEvents.OnZoneEnter?.Invoke(message);
        }
    }
}
