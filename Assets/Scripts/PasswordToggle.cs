using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PasswordToggle : MonoBehaviour
{
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Image toggleIcon;
    [SerializeField] private Sprite showIcon; // Mắt mở
    [SerializeField] private Sprite hideIcon; // Mắt nhắm

    private bool isPasswordVisible = false;

    void Start()
    {
        // Khởi tạo với password ẩn
        SetPasswordVisibility(false);

        // Gắn sự kiện click
        GetComponent<Button>().onClick.AddListener(TogglePassword);
    }

    public void TogglePassword()
    {
        isPasswordVisible = !isPasswordVisible;
        SetPasswordVisibility(isPasswordVisible);
    }

    void SetPasswordVisibility(bool visible)
    {
        if (passwordInput != null)
        {
            passwordInput.contentType = visible ?
                TMP_InputField.ContentType.Standard :
                TMP_InputField.ContentType.Password;

            passwordInput.ForceLabelUpdate();
        }

        // Đổi icon
        if (toggleIcon != null)
        {
            toggleIcon.sprite = visible ? showIcon : hideIcon;
        }
    }
}