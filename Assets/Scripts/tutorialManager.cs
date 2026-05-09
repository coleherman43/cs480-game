using UnityEngine;
using TMPro;
using System.Collections; // Required for TextMesh Pro

public class TextController : MonoBehaviour
{
    // Drag your TextMeshPro component here in the Inspector
    public TextMeshProUGUI tutorialText;


 // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        StartCoroutine(intro());
        GameEvents.OnZoneEnter += SetText;
    }
    void OnDisable()
    {
        GameEvents.OnZoneEnter -= SetText;
    }
    
    public void SetText(string newText)
    {
        tutorialText.text = newText;
    }
    
    IEnumerator intro()
    {
        yield return new WaitForSeconds(4);
        SetText("Use W A S D and Shift to move around and sprint!");
    }
}
