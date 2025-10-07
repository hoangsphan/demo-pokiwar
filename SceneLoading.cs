using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoading : MonoBehaviour
{
    // Hàm này sẽ được gọi khi bạn nhấn nút
    public void LoadNewScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}