/*
Test file to subscribe a scoring system to the OnPickupCollected GameEvent broadcasts
*/

using UnityEngine;

public class TestScoreManager : MonoBehaviour
{
    private int score = 0;

    private void OnEnable()
    {
        GameEvents.OnPickupCollected += AddScore;
    }

    private void OnDisable()
    {
        GameEvents.OnPickupCollected -= AddScore;
    }

    private void AddScore(int amount)
    {
        score += amount;
        Debug.Log("Score: " + score);
    }
}
