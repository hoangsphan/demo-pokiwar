using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using System;
using UnityEngine.SceneManagement;

public class FirebaseAuthManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject loadingPanel;

    [Header("Settings")]
    [SerializeField] private string gameSceneName = "MainMenuScene"; // Tên scene game

    private FirebaseAuth auth;
    private FirebaseUser user;

    void Start()
    {
        // Hide loading panel initially
        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        // Clear status text
        if (statusText != null)
            statusText.text = "";

        // Auto - fill email từ RegisterScene
        if (PlayerPrefs.HasKey("RegisteredEmail"))
        {
            string savedEmail = PlayerPrefs.GetString("RegisteredEmail");
            if (emailInput != null && !string.IsNullOrEmpty(savedEmail))
            {
                emailInput.text = savedEmail;
                ShowStatus($"Chào mừng trở lại! Hãy nhập mật khẩu.", new Color(0.3f, 0.7f, 0.9f));
            }
            // Xóa sau khi dùng
            PlayerPrefs.DeleteKey("RegisteredEmail");
        }

        // Initialize Firebase
        InitializeFirebase();

        // Setup button listeners
        if (loginButton != null)
            loginButton.onClick.AddListener(OnLoginButtonClick);
    }

    void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            DependencyStatus dependencyStatus = task.Result;

            if (dependencyStatus == DependencyStatus.Available)
            {
                // Firebase is ready to use
                auth = FirebaseAuth.DefaultInstance;
                auth.StateChanged += AuthStateChanged;
                AuthStateChanged(this, null);

                Debug.Log("Firebase initialized successfully!");
                ShowStatus("Sẵn sàng đăng nhập", new Color(0.3f, 0.7f, 0.3f));
            }
            else
            {
                Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
                ShowStatus("Lỗi khởi tạo Firebase!", Color.red);
            }
        });
    }

    void AuthStateChanged(object sender, EventArgs eventArgs)
    {
        if (auth.CurrentUser != user)
        {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null;

            if (!signedIn && user != null)
            {
                Debug.Log("Signed out: " + user.UserId);
            }

            user = auth.CurrentUser;

            if (signedIn)
            {
                Debug.Log("Signed in: " + user.UserId);
                ShowStatus($"Chào mừng {user.Email}!", new Color(0.2f, 0.8f, 0.2f));

                // Wait 1 second then load game scene
                Invoke(nameof(LoadGameScene), 1f);
            }
        }
    }

    void OnLoginButtonClick()
    {
        LoginUser();
    }

    void OnRegisterButtonClick()
    {
        RegisterUser();
    }

    public void LoginUser()
    {
        if (!ValidateInputs())
            return;

        SetLoadingState(true);
        ShowStatus("Đang đăng nhập...", Color.yellow);

        auth.SignInWithEmailAndPasswordAsync(emailInput.text.Trim(), passwordInput.text)
            .ContinueWithOnMainThread(task =>
            {
                SetLoadingState(false);

                if (task.IsCanceled)
                {
                    ShowStatus("Đăng nhập bị hủy", Color.yellow);
                    Debug.LogWarning("SignIn was canceled.");
                    return;
                }

                if (task.IsFaulted)
                {
                    HandleAuthError(task.Exception);
                    return;
                }

                // Success - AuthStateChanged will handle the rest
                FirebaseUser newUser = task.Result.User;
                Debug.LogFormat("User signed in successfully: {0} ({1})",
                    newUser.DisplayName, newUser.UserId);
            });
    }

    public void RegisterUser()
    {
        if (!ValidateInputs())
            return;

        SetLoadingState(true);
        ShowStatus("Đang đăng ký...", Color.yellow);

        auth.CreateUserWithEmailAndPasswordAsync(emailInput.text.Trim(), passwordInput.text)
            .ContinueWithOnMainThread(task =>
            {
                SetLoadingState(false);

                if (task.IsCanceled)
                {
                    ShowStatus("Đăng ký bị hủy", Color.yellow);
                    Debug.LogWarning("CreateUser was canceled.");
                    return;
                }

                if (task.IsFaulted)
                {
                    HandleAuthError(task.Exception);
                    return;
                }

                // Success
                FirebaseUser newUser = task.Result.User;
                ShowStatus("Đăng ký thành công! Đang đăng nhập...", new Color(0.2f, 0.8f, 0.2f));
                Debug.LogFormat("Firebase user created successfully: {0} ({1})",
                    newUser.DisplayName, newUser.UserId);
            });
    }

    bool ValidateInputs()
    {
        // Check if email is empty
        if (string.IsNullOrEmpty(emailInput.text.Trim()))
        {
            ShowStatus("Vui lòng nhập email!", Color.red);
            return false;
        }

        // Check if email format is valid
        if (!emailInput.text.Contains("@") || !emailInput.text.Contains("."))
        {
            ShowStatus("Email không đúng định dạng!", Color.red);
            return false;
        }

        // Check if password is empty
        if (string.IsNullOrEmpty(passwordInput.text))
        {
            ShowStatus("Vui lòng nhập mật khẩu!", Color.red);
            return false;
        }

        // Check password length
        if (passwordInput.text.Length < 6)
        {
            ShowStatus("Mật khẩu phải có ít nhất 6 ký tự!", Color.red);
            return false;
        }

        return true;
    }

    void HandleAuthError(AggregateException exception)
    {
        if (exception == null)
        {
            ShowStatus("Lỗi không xác định!", Color.red);
            return;
        }

        FirebaseException firebaseEx = exception.GetBaseException() as FirebaseException;

        if (firebaseEx == null)
        {
            ShowStatus("Lỗi kết nối!", Color.red);
            Debug.LogError(exception);
            return;
        }

        AuthError errorCode = (AuthError)firebaseEx.ErrorCode;
        string message = "Lỗi không xác định";

        switch (errorCode)
        {
            case AuthError.InvalidEmail:
                message = "Email không hợp lệ!";
                break;
            case AuthError.WrongPassword:
                message = "Mật khẩu không đúng!";
                break;
            case AuthError.UserNotFound:
                message = "Tài khoản không tồn tại!";
                break;
            case AuthError.EmailAlreadyInUse:
                message = "Email đã được sử dụng!";
                break;
            case AuthError.WeakPassword:
                message = "Mật khẩu quá yếu! Tối thiểu 6 ký tự.";
                break;
            case AuthError.NetworkRequestFailed:
                message = "Lỗi kết nối mạng!";
                break;
            case AuthError.TooManyRequests:
                message = "Quá nhiều yêu cầu! Vui lòng thử lại sau.";
                break;
            default:
                message = $"Lỗi: {errorCode}";
                break;
        }

        ShowStatus(message, Color.red);
        Debug.LogError($"Auth Error: {errorCode} - {message}");
    }

    void ShowStatus(string message, Color color)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color;
        }
        Debug.Log($"Status: {message}");
    }

    void SetLoadingState(bool isLoading)
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(isLoading);

        if (loginButton != null)
            loginButton.interactable = !isLoading;

    }

    void LoadGameScene()
    {
        // Check if scene exists in Build Settings
        if (SceneUtility.GetBuildIndexByScenePath(gameSceneName) == -1)
        {
            Debug.LogWarning($"Scene '{gameSceneName}' not found in Build Settings! Staying in Login scene.");
            ShowStatus("Scene game chưa được thiết lập!", Color.yellow);
            return;
        }

        Debug.Log($"Loading scene: {gameSceneName}");
        SceneManager.LoadScene(gameSceneName);
    }

    void OnDestroy()
    {
        // Clean up
        if (auth != null)
        {
            auth.StateChanged -= AuthStateChanged;
            auth = null;
        }
    }
}