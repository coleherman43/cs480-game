/*
Pickup Item script to be attached to pickup item prefab game objects
When the player collides with the game object, a global event is broadcasted to all subscribers,
(passing in the items value) and the object is destroyed
*/

using UnityEngine;

public class PickupItem : MonoBehaviour
{
    [SerializeField] private int scoreValue = 10;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Fire the event
            GameEvents.OnPickupCollected?.Invoke(scoreValue);

            // Destroy the object
            Destroy(gameObject);
        }
    }
}
