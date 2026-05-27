/* Attached to global tutorial manager object, handles zone broadcasts and changes message.*/
using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement; 

public class TextController : MonoBehaviour
{
    // Drag your TextMeshPro component here in the Inspector
    public TextMeshProUGUI tutorialText;


    void OnEnable()
    {
        StartCoroutine(intro());
        GameEvents.OnZoneEnter += SetText; //register listener
        GameEvents.gameOver += onOver;
    }
    void OnDisable()
    {
        GameEvents.OnZoneEnter -= SetText; //unregister listener
        GameEvents.gameOver -= onOver;
    }
    
    public void SetText(string newText)
    {
        tutorialText.text = newText;
    }
    
    public void onOver(bool status)
    {
        if (status)
        {
            SetText("Game Over -- You Won!");
            Invoke(nameof(returnToMenu), 1.5f);
        } else
        {
            SetText("Game Over -- You Failed :(");
            GameManager.Instance.PenalizePlayer();
            Invoke(nameof(returnToMenu), 1.5f);
        }
    }

    private void returnToMenu()
    {
        SceneManager.LoadScene(0); // Must be the same index as computer room
    }
    IEnumerator intro()
    {
        // Change welcome message after a moment.
        yield return new WaitForSeconds(4);
        SetText("Use W A S D and the mouse to move around and look!");
    }
}
