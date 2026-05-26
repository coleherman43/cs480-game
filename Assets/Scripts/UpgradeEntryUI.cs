using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeEntryUI : MonoBehaviour
{
    [Header("Upgrade Info")]
    public string upgradeName;
    public int cost;
    public UpgradeType upgradeType;

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
        // Money Multiplier Rules
        // ==============================
        
        if (upgradeType == UpgradeType.MoneyMultiplier)
        {
            // If the player owns 5x, disable 2x
            if (upgradeName == "Money2x" && GameManager.Instance.IsUpgradeUnlocked("Money5x"))
            {
                DisableUpgrade();
                return;
            }

            // If the player owns 10x, disable both
            if ((upgradeName == "Money2x" || upgradeName == "Money5x") && GameManager.Instance.IsUpgradeUnlocked("Money10x"))
            {
                DisableUpgrade();
                return;
            }
        }

        buttonText.text = "Buy ($" + cost + ")";
    }

    void DisableUpgrade()
    {
        buyButton.interactable = false;
        buttonText.text = "Locked";
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
