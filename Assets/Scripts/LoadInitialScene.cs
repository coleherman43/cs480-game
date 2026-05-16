using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadInitialScene : MonoBehaviour
{
    void Start()
    {
        SceneManager.LoadScene(0);
    }
}
