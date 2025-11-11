using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using System;
using UnityEngine.SceneManagement;

public class FirebaseRegisterManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_InputField confirmPasswordInput;
    [SerializeField] private Button registerButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject loadingPanel;

    [Header("Settings")]
    [SerializeField] private string loginSceneName = "LoginScene";

    private FirebaseAuth auth;

    void Start()
    {
        // Hide loading panel
        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        // Clear status
        if (statusText != null)
            statusText.text = "";

        // Initialize Firebase
        InitializeFirebase();

        // Setup button listeners
        if (registerButton != null)
            registerButton.onClick.AddListener(OnRegisterButtonClick);

    }

    void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            DependencyStatus dependencyStatus = task.Result;

            if (dependencyStatus == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                Debug.Log("Firebase initialized successfully!");
                ShowStatus("Sẵn sàng đăng ký", new Color(0.3f, 0.7f, 0.3f));
            }
            else
            {
                Debug.LogError($"Could not resolve Firebase dependencies: {dependencyStatus}");
                ShowStatus("Lỗi khởi tạo Firebase!", Color.red);
            }
        });
    }

    void OnRegisterButtonClick()
    {
        RegisterUser();
    }

    public void RegisterUser()
    {
        if (!ValidateInputs())
            return;

        SetLoadingState(true);
        ShowStatus("Đang tạo tài khoản...", Color.yellow);

        string email = emailInput.text.Trim();
        string password = passwordInput.text;

        auth.CreateUserWithEmailAndPasswordAsync(email, password)
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

                // Success!
                FirebaseUser newUser = task.Result.User;
                ShowStatus("Đăng ký thành công! Đang chuyển...", new Color(0.2f, 0.8f, 0.2f));
                Debug.LogFormat("User registered: {0} ({1})", newUser.Email, newUser.UserId);

                // Lưu email để tự động điền ở LoginScene
                PlayerPrefs.SetString("RegisteredEmail", email);
                PlayerPrefs.Save();

                // Chờ 1.5 giây rồi chuyển về LoginScene
                Invoke(nameof(GoBackToLogin), 1.5f);
            });
    }

    bool ValidateInputs()
    {
        // Check email
        if (string.IsNullOrEmpty(emailInput.text.Trim()))
        {
            ShowStatus("Vui lòng nhập email!", Color.red);
            emailInput.Select();
            return false;
        }

        if (!IsValidEmail(emailInput.text.Trim()))
        {
            ShowStatus("Email không đúng định dạng!", Color.red);
            emailInput.Select();
            return false;
        }

        // Check password
        if (string.IsNullOrEmpty(passwordInput.text))
        {
            ShowStatus("Vui lòng nhập mật khẩu!", Color.red);
            passwordInput.Select();
            return false;
        }

        if (passwordInput.text.Length < 6)
        {
            ShowStatus("Mật khẩu phải có ít nhất 6 ký tự!", Color.red);
            passwordInput.Select();
            return false;
        }

        // Check password strength (optional)
        if (!HasNumberAndLetter(passwordInput.text))
        {
            ShowStatus("Mật khẩu nên có cả chữ và số!", new Color(1f, 0.6f, 0f)); // Orange warning
        }

        // Check confirm password
        if (string.IsNullOrEmpty(confirmPasswordInput.text))
        {
            ShowStatus("Vui lòng xác nhận mật khẩu!", Color.red);
            confirmPasswordInput.Select();
            return false;
        }

        if (passwordInput.text != confirmPasswordInput.text)
        {
            ShowStatus("Mật khẩu xác nhận không khớp!", Color.red);
            confirmPasswordInput.Select();
            return false;
        }

        return true;
    }

    bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    bool HasNumberAndLetter(string password)
    {
        bool hasNumber = false;
        bool hasLetter = false;

        foreach (char c in password)
        {
            if (char.IsDigit(c)) hasNumber = true;
            if (char.IsLetter(c)) hasLetter = true;
        }

        return hasNumber && hasLetter;
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
            case AuthError.EmailAlreadyInUse:
                message = "Email này đã được đăng ký!";
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
        Debug.LogError($"Register Error: {errorCode} - {message}");
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

        if (registerButton != null)
            registerButton.interactable = !isLoading;

    }

    public void GoBackToLogin()
    {
        Debug.Log("Returning to LoginScene");
        SceneManager.LoadScene(loginSceneName);
    }

    void OnDestroy()
    {
        if (auth != null)
        {
            auth = null;
        }
    }
}