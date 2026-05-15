// Basic script for triggering tutorial message when an object is picked up. For the last part of tutorial.
using UnityEngine;

public class tutorialPickup : MonoBehaviour
{
    // Just want to clear the text prompt when pickup is picked up
    private void OnDestroy()
    {
        GameEvents.OnZoneEnter?.Invoke("");
    }
}
