using UnityEngine;
using UnityEngine.UI;

public class SensitivityOption : MonoBehaviour
{
    [Header("Sensitivity Option Info")]
    public GameManager.SensitivityPreset sensPreset;
    public Button selectionButton;

    void Start()
    {
        UpdateUI();

        selectionButton.onClick.AddListener(SelectSensitivity);
    }

    void UpdateUI()
    {
        if (sensPreset == GameManager.Instance.GetSensPresetAsEnum())
        {
            selectionButton.interactable = false;
        }
        else
        {
            selectionButton.interactable = true;
        }
    }

    void SelectSensitivity()
    {
        GameManager.Instance.SetSensitivityPreset(sensPreset);

        // Refresh the UI of all sensitivity option objects
        SensitivityOption[] allOptions = FindObjectsOfType<SensitivityOption>();
        
        foreach (SensitivityOption entry in allOptions)
        {
            entry.UpdateUI();
        }
    }
}
