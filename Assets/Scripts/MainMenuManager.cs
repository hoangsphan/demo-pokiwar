using UnityEngine;
using UnityEngine.SceneManagement;
using Firebase.Auth;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    private FirebaseAuth auth;
    private bool isLoggingOut = false;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;

        if (auth.CurrentUser == null)
        {
            SceneManager.LoadScene("LoginScene");
            return;
        }
        Debug.Log($"✓ MainMenu: Logged in as {auth.CurrentUser.Email}");
    }

    public void HandleLogout()
    {
        if (isLoggingOut) return;
        StartCoroutine(LogoutCoroutine());
    }

    private IEnumerator LogoutCoroutine()
    {
        isLoggingOut = true;
        Debug.Log("Processing Logout...");

        if (auth != null)
        {
            auth.SignOut();

            // --- QUAN TRỌNG: Reset dữ liệu của Singleton để lần sau đăng nhập lại được ---
            if (IslandProgressManager.Instance != null)
            {
                IslandProgressManager.Instance.ResetData();
            }
            // ---------------------------------------------------------------------------
        }

        // Xóa lưu trữ tạm
        PlayerPrefs.DeleteKey("RegisteredEmail");
        PlayerPrefs.Save();

        yield return new WaitForSeconds(0.2f);

        Debug.Log("→ Returning to LoginScene");
        SceneManager.LoadScene("LoginScene");
    }

    void OnDestroy()
    {
        isLoggingOut = false;
    }
}