using TMPro;
using UnityEngine;

public class SceneMenuManager : MonoBehaviour
{
    public TextMeshProUGUI moneyText;

    // Update is called once per frame
    void Update()
    {
        moneyText.text = "$" + GameManager.Instance.GetMoney().ToString();
    }
}
