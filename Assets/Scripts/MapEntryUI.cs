using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MapEntryUI : MonoBehaviour
{
    [Header("Scene Info")]
    public string sceneName;
    public int sceneNum;
    public int cost;

    [Header("UI")]
    public TextMeshProUGUI sceneNameText;
    public Button playButton;
    public Button buyButton;
    public TextMeshProUGUI buyButtonText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sceneNameText.text = sceneName;

        UpdateUI();

        playButton.onClick.AddListener(PlayScene);
        buyButton.onClick.AddListener(BuyScene);
    }

    void UpdateUI()
    {
        bool unlocked = GameManager.Instance.IsSceneUnlocked(sceneNum);

        playButton.interactable = unlocked;

        buyButton.gameObject.SetActive(!unlocked);

        if (!unlocked)
        {
            buyButtonText.text = "Buy ($" + cost + ")"; 
        }
    }

    void PlayScene()
    {
        if (GameManager.Instance.IsSceneUnlocked(sceneNum))
        {
            SceneManager.LoadScene(sceneNum);
        }
    }

    void BuyScene()
    {
        if (GameManager.Instance.SpendMoney(cost))
        {
            GameManager.Instance.UnlockScene(sceneNum);

            UpdateUI();
        }
        else
        {
            Debug.Log("Not enough cash!");
        }
    }
}
