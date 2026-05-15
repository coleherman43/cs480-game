/*  Script for controlling the radius pickup system.
    Items within the radius will begin to glow (shader)
    and when E is pressed, items in radius are picked up.
    
    This script is attached to an empty game object which
    is a child of the player. That empty object must have
    a trigger sphere collider.*/

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class pickupDetection : MonoBehaviour
{
    //List to dynamically track items in the pickup radius
    private List<GameObject> items = new List<GameObject>();
    private void Update()
    {
        //Check input
        if (Input.GetKeyDown(KeyCode.E))
        {
            pickupNearby();
        }
    }
    //When an object enters the radius.
    private void OnTriggerEnter(Collider other)
    {   
        //Toggle shader and track object
        PickupItem scr = other.GetComponent<PickupItem>();
        scr.toggleShader();

        items.Add(other.gameObject);
    }
    // When an object exits the radius.
    private void OnTriggerExit(Collider other)
    {
        //Toggle shader and untrack object
        PickupItem scr = other.GetComponent<PickupItem>();
        scr.toggleShader();

        items.Remove(other.gameObject);
    }

    private void pickupNearby()
    {
        //Called when 'E' is pressed
        //Debug.Log("pickup attempt");
        //Iterate through tracked objects and trigger pickup
        foreach(var i in items)
        {
            PickupItem scr = i.GetComponent<PickupItem>();
            scr.OnPickup();
        }
        items.Clear(); // reset list
    }
}
