/*
Test file to subscribe a scoring system to the OnPickupCollected GameEvent broadcasts
*/

using UnityEngine;
using TMPro;
using System.Collections;

public class TestScoreManager : MonoBehaviour
{
    // private int score = 0;

    public TextMeshProUGUI scoreText;

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
        // score += amount;
        // SetText(score);
        // Debug.Log("Score: " + score);

        GameManager.Instance.AddMoney(amount);
        SetText(GameManager.Instance.GetMoney());
    }

    private void SetText(int newText)
    {
        scoreText.text = ("$ " + newText.ToString());
    }
}
