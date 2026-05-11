using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class pickupDetection : MonoBehaviour
{
    private List<GameObject> items = new List<GameObject>();
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            pickupNearby();
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        PickupItem scr = other.GetComponent<PickupItem>();
        scr.toggleShader();

        items.Add(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        PickupItem scr = other.GetComponent<PickupItem>();
        scr.toggleShader();

        items.Remove(other.gameObject);
    }

    private void pickupNearby()
    {
        Debug.Log("pickup attempt");
        foreach(var i in items)
        {
            PickupItem scr = i.GetComponent<PickupItem>();
            scr.OnPickup();
        }
        Debug.Log(items);
    }
}
