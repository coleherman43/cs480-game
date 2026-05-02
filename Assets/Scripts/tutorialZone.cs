using UnityEngine;

public class tutorialZone : MonoBehaviour
{
    public string message;
    private TextController tutorialManager;

    void Start()
    {
        tutorialManager = GetComponentInParent<TextController>();
        if (tutorialManager == null)
        {
            Debug.LogError(gameObject.name + "TextController not found in parent!");
        }
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            tutorialManager.SetText(message);
        }
    }
}
