/*
This script is for a singleton GameObject that utilizes DontDestroyOnLoad to maintain persistent money, unlocked scenes
and upgrades/abilities.

The script will allow players money, unlocked scenes, and abilities to be managed in one place, and accessible from all scenes
in the game.

Note: we are using SceneManager.LoadScene(<int>);
<int> is a reference to the scene number in the Unity Build Profiles -> Scene List

Every scene can access the methods below through: GameManager.Instance
Examples:

To add money when a player collects an item worth $10
GameManager.Instance.AddMoney(10);

To buy a new scene worth $600
if (GameManager.Instance.SpendMoney(600))
{
    GameManager.Instance.UnlockScene(<Scene Number>);
}
else
{
    Debug.Log("Not enough cash!");
}

To play and transition to a new scene
if (GameManager.Instance.IsSceneUnlocked(<Scene Number>))
{
    SceneManager.LoadScene(<Scene Number>);
}
else
{
    Debug.Log("Map locked!");
}

Unlock an Upgrade
GameManager.Instance.UnlockUpgrade("GemMagnet");

Check if the Player has a certain Upgrade
if (GameManager.Instance.IsUpgradeUnlocked("GoldSpeed"))
{
    // Modify player mechanics
}
*/

using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // ==============================
    // Player money
    // ==============================
    public int playerMoney = 0;

    // ==============================
    // Maps/Scenes the player has unlocked
    // ==============================
    public List<int> unlockedScenes = new List<int>();

    // ==============================
    // Upgrades/abilities the player has unlocked
    // ==============================
    public Dictionary<string, bool> unlockedUpgrades = new Dictionary<string, bool>();

    // ==============================
    // Money Multiplier
    // ==============================
    public int moneyMultiplier = 1;

    // ==============================
    // Player penalty
    // ==============================
    public int playerPenalty = 500;

    private void Awake()
    {
        // Set up singleton
        if (Instance == null)
        {
            Instance = this;

            // Keep this object alive across scenes
            DontDestroyOnLoad(gameObject);

            InitializeGameData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializeGameData()
    {
        // Tutorial map is unlocked by default (1 in Scene List)
        // unlockedScenes.Add(1);

        // ==============================
        // Initialize Upgrades
        // ==============================
        unlockedUpgrades.Add("GemMagnet", false);
        unlockedUpgrades.Add("GoldMagnet", false);
        unlockedUpgrades.Add("IronMagnet", false);

        unlockedUpgrades.Add("GemSpeed", false);
        unlockedUpgrades.Add("GoldSpeed", false);
        unlockedUpgrades.Add("IronSpeed", false);

        unlockedUpgrades.Add("Money2x", false);
        unlockedUpgrades.Add("Money5x", false);
        unlockedUpgrades.Add("Money10x", false);
    }

    // ==============================
    // Money
    // ==============================

    public int GetMoney()
    {
        return playerMoney;
    }

    // Add money
    public void AddMoney(int amount)
    {
        playerMoney += amount * moneyMultiplier;
    }

    // Spend money
    public bool SpendMoney(int amount)
    {
        if (playerMoney >= amount)
        {
            playerMoney -= amount;
            return true;
        }
        
        return false;
    }

    // Player penalty taking away money when they die
    public void PenalizePlayer()
    {
        if (playerMoney < playerPenalty)
        {
            playerMoney = 0;
        }
        else
        {
            playerMoney -= playerPenalty;
        }
    }

    // ==============================
    // Scenes
    // ==============================

    // Unlock a scene
    public void UnlockScene(int sceneNum)
    {
        if (!unlockedScenes.Contains(sceneNum))
        {
            unlockedScenes.Add(sceneNum);
        }
    }

    // Check if a scene is unlocked
    public bool IsSceneUnlocked(int sceneNum)
    {
        return unlockedScenes.Contains(sceneNum);
    }

    // ==============================
    // Upgrades
    // ==============================

    public void UnlockUpgrade(string upgradeName)
    {
        if (unlockedUpgrades.ContainsKey(upgradeName))
        {
            unlockedUpgrades[upgradeName] = true;

            // Money multiplier upgrades
            if (upgradeName == "Money2x")
            {
                moneyMultiplier = 2;
            }
            else if (upgradeName == "Money5x")
            {
                moneyMultiplier = 5;
            }
            else if (upgradeName == "Money10x")
            {
                moneyMultiplier = 10;
            }
        }
    }

    public bool IsUpgradeUnlocked(string upgradeName)
    {
        if (unlockedUpgrades.ContainsKey(upgradeName))
        {
            return unlockedUpgrades[upgradeName];
        }

        return false;
    }
}