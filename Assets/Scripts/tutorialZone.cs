/* Attached to empty game object with trigger collider, use public message parameter to 
   set what message will be displayed by tutorial manager.*/
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
