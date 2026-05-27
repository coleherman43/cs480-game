using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeEntryUI : MonoBehaviour
{
    [Header("Upgrade Info")]
    public string upgradeName;
    public int cost;
    // Leave blank in inspector for the tier 1 upgrade
    public string requiredUpgrade;

    [Header("UI")]
    public Image iconImage;
    public Button buyButton;
    public TextMeshProUGUI buttonText;

    void Start()
    {
        buyButton.onClick.AddListener(BuyUpgrade);

        UpdateUI();
    }

    public void UpdateUI()
    {
        bool unlocked = GameManager.Instance.IsUpgradeUnlocked(upgradeName);

        // Already owned
        if (unlocked)
        {
            buyButton.interactable = false;
            buttonText.text = "Owned";
            return;
        }

        // ==============================
        // Check prerequisite
        // ==============================
        
        if (!string.IsNullOrEmpty(requiredUpgrade))
        {
            bool prerequisiteUnlocked = GameManager.Instance.IsUpgradeUnlocked(requiredUpgrade);

            if (!prerequisiteUnlocked)
            {
                buyButton.interactable = false;
                buttonText.text = "Locked";
                return;
            }
        }

        // Available to be purchased
        buyButton.interactable = true;
        buttonText.text = "Buy ($" + cost + ")";
    }

    void BuyUpgrade()
    {
        if (GameManager.Instance.SpendMoney(cost))
        {
            GameManager.Instance.UnlockUpgrade(upgradeName);

            // Refresh all upgrade objects
            UpgradeEntryUI[] allEntries = FindObjectsOfType<UpgradeEntryUI>();

            foreach (UpgradeEntryUI entry in allEntries)
            {
                entry.UpdateUI();
            }
        }
    }
}
