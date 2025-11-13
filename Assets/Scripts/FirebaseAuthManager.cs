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
    [SerializeField] private string gameSceneName = "MainMenuScene";

    private FirebaseAuth auth;
    private bool isLoggingIn = false;

    void Start()
    {
        // Ẩn loading ban đầu
        if (loadingPanel != null) loadingPanel.SetActive(false);
        if (statusText != null) statusText.text = "";

        // Tự động điền email nếu có từ màn hình Đăng ký (giữ nguyên logic của bạn)
        if (PlayerPrefs.HasKey("RegisteredEmail"))
        {
            if (emailInput != null) emailInput.text = PlayerPrefs.GetString("RegisteredEmail");
            PlayerPrefs.DeleteKey("RegisteredEmail");
        }

        InitializeFirebase();

        if (loginButton != null)
            loginButton.onClick.AddListener(OnLoginButtonClick);
    }

    void InitializeFirebase()
    {
        Debug.Log("Initializing Firebase...");
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            DependencyStatus dependencyStatus = task.Result;

            if (dependencyStatus == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;

                // --- PHẦN QUAN TRỌNG ĐỂ SỬA LỖI LOGIN LẠI ---
                // Link với IslandProgressManager và Reset dữ liệu cũ
                if (IslandProgressManager.Instance != null)
                {
                    // Reset dữ liệu cũ để tránh lỗi khi login lại
                    IslandProgressManager.Instance.ResetData();
                    // Khởi tạo thủ công để đảm bảo đúng thứ tự
                    IslandProgressManager.Instance.Initialize();
                }
                // ---------------------------------------------

                // Kiểm tra nếu user đã đăng nhập sẵn (Auto Login)
                if (auth.CurrentUser != null)
                {
                    OnLoginSuccess(auth.CurrentUser);
                }
                else
                {
                    ShowStatus("Sẵn sàng đăng nhập", new Color(0.3f, 0.7f, 0.3f));
                }
            }
            else
            {
                Debug.LogError($"Firebase Error: {dependencyStatus}");
                ShowStatus("Lỗi hệ thống!", Color.red);
            }
        });
    }

    void OnLoginButtonClick()
    {
        LoginUser();
    }

    public void LoginUser()
    {
        // Gọi hàm Validate chi tiết của BẠN
        if (!ValidateInputs()) return;

        if (isLoggingIn) return;

        isLoggingIn = true;
        SetLoadingState(true);
        ShowStatus("Đang đăng nhập...", Color.yellow);

        string email = emailInput.text.Trim();
        string password = passwordInput.text;

        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            // Luôn reset trạng thái loading dù thành công hay thất bại
            isLoggingIn = false;
            SetLoadingState(false);

            if (task.IsCanceled)
            {
                ShowStatus("Đăng nhập bị hủy", Color.yellow);
                return;
            }

            if (task.IsFaulted)
            {
                // Gọi hàm xử lý lỗi chi tiết của BẠN
                HandleAuthError(task.Exception);
                return;
            }

            // Đăng nhập thành công
            OnLoginSuccess(task.Result.User);
        });
    }

    void OnLoginSuccess(FirebaseUser user)
    {
        Debug.Log($"Login Success: {user.Email}");
        ShowStatus($"Xin chào {user.Email}!", new Color(0.2f, 0.8f, 0.2f));

        // Kích hoạt load data
        if (IslandProgressManager.Instance != null)
        {
            IslandProgressManager.Instance.Initialize(); // Đảm bảo Manager đã sẵn sàng
            _ = IslandProgressManager.Instance.LoadIslandProgressAsync(user.UserId);
        }

        // Chuyển cảnh sau 1 giây
        Invoke(nameof(LoadGameScene), 1f);
    }

    // --- GIỮ NGUYÊN HÀM KIỂM TRA CỦA BẠN ---
    bool ValidateInputs()
    {
        if (string.IsNullOrEmpty(emailInput.text.Trim()))
        {
            ShowStatus("Vui lòng nhập email!", Color.red);
            return false;
        }
        if (!emailInput.text.Contains("@") || !emailInput.text.Contains("."))
        {
            ShowStatus("Email không đúng định dạng!", Color.red);
            return false;
        }
        if (string.IsNullOrEmpty(passwordInput.text))
        {
            ShowStatus("Vui lòng nhập mật khẩu!", Color.red);
            return false;
        }
        if (passwordInput.text.Length < 6)
        {
            ShowStatus("Mật khẩu phải có ít nhất 6 ký tự!", Color.red);
            return false;
        }
        return true;
    }

    // --- GIỮ NGUYÊN HÀM XỬ LÝ LỖI CỦA BẠN ---
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
        Debug.LogError($"✗ Auth Error: {errorCode} - {message}");
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
        if (loadingPanel != null) loadingPanel.SetActive(isLoading);
        if (loginButton != null) loginButton.interactable = !isLoading;
    }

    void LoadGameScene()
    {
        SceneManager.LoadScene(gameSceneName);
    }
}