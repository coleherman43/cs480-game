/* Attached to global tutorial manager object, handles zone broadcasts and changes message.*/
using UnityEngine;
using TMPro;
using System.Collections; // Required for TextMesh Pro

public class TextController : MonoBehaviour
{
    // Drag your TextMeshPro component here in the Inspector
    public TextMeshProUGUI tutorialText;


    void OnEnable()
    {
        StartCoroutine(intro());
        GameEvents.OnZoneEnter += SetText; //register listener
    }
    void OnDisable()
    {
        GameEvents.OnZoneEnter -= SetText; //unregister listener
    }
    
    public void SetText(string newText)
    {
        tutorialText.text = newText;
    }
    
    IEnumerator intro()
    {
        // Change welcome message after a moment.
        yield return new WaitForSeconds(4);
        SetText("Use W A S D and the mouse to move around and look!");
    }
}
