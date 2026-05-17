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
*/

using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // Player money
    public int playerMoney = 0;

    // Maps/Scenes the player has unlocked
    public List<int> unlockedScenes = new List<int>();

    private void Awake()
    {
        // Set up singleton
        if (Instance == null)
        {
            Instance = this;

            // Keep this object alive across scenes
            DontDestroyOnLoad(gameObject);

            // Tutorial map is unlocked by default (1 in Scene List)
            // unlockedScenes.Add(1);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public int GetMoney()
    {
        return playerMoney;
    }

    // Add money
    public void AddMoney(int amount)
    {
        playerMoney += amount;
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
}