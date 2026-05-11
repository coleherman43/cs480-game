using UnityEngine;

public class InteractableComputer : MonoBehaviour
{
    public KeyCode interactKey;

    public GameObject uiPanel;
    public GameObject promptUI;

    public ComputerRoomPlayerMovement playerMovement;

    private bool playerInRange = false;
    private GameObject player;

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            ToggleUI();
        }
    }

    void OpenUI()
    {
        uiPanel.SetActive(true);

        promptUI.SetActive(false);

        playerMovement.inputLocked = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void CloseUI()
    {
        uiPanel.SetActive(false);

        promptUI.SetActive(true);

        playerMovement.inputLocked = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void ToggleUI()
    {
        if (uiPanel.activeSelf)
        {
            CloseUI();
        }
        else
        {
            OpenUI();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("ComputerRoomPlayer"))
        {
            // Debug.Log("Triggered");
            playerInRange = true;
            promptUI.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("ComputerRoomPlayer"))
        {
            // Debug.Log("Untriggered");
            playerInRange = false;
            promptUI.SetActive(false);
        }
    }
}