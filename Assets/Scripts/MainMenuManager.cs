using UnityEngine;
using UnityEngine.SceneManagement; // <-- Thêm dòng này để quản lý Scene
using Firebase.Auth;            // <-- Thêm dòng này để dùng Firebase Auth

public class MainMenuManager : MonoBehaviour
{
    private FirebaseAuth auth;

    // Hàm Start được gọi khi đối tượng được bật
    void Start()
    {
        // Khởi tạo và lấy phiên bản FirebaseAuth
        auth = FirebaseAuth.DefaultInstance;
    }

    // Đây là hàm public mà chúng ta sẽ gọi từ nút
    public void HandleLogout()
    {
        // Kiểm tra xem có người dùng nào đang đăng nhập không
        if (auth.CurrentUser != null)
        {
            Debug.Log($"Đang đăng xuất: {auth.CurrentUser.Email}");

            // Lệnh đăng xuất
            auth.SignOut();

            // Quay trở lại cảnh LoginScene
            // (Hãy chắc chắn rằng "LoginScene" là tên chính xác của file scene đăng nhập của bạn)
            SceneManager.LoadScene("LoginScene");
        }
        else
        {
            Debug.LogWarning("Không có ai để đăng xuất, đã ở LoginScene.");
            // Đề phòng trường hợp lỗi, vẫn quay về LoginScene
            SceneManager.LoadScene("LoginScene");
        }
    }
}