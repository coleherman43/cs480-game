using UnityEngine;

public class pickupDetection : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        PickupItem scr = other.GetComponent<PickupItem>();
        scr.toggleShader();
    }

    private void OnTriggerExit(Collider other)
    {
        PickupItem scr = other.GetComponent<PickupItem>();
        scr.toggleShader();
    }
}
