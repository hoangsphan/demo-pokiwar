using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class IslandButton : MonoBehaviour
{
    [Header("Định danh Đảo")]
    [Tooltip("PHẢI TRÙNG với key trong Firestore, ví dụ: DragonIsland")]
    public string islandID;

    [Tooltip("Scene sẽ tải khi nhấn vào đảo này")]
    public string sceneToLoad;

    [Header("Visuals")]
    [SerializeField] private Image islandImage;
    [SerializeField] private Button button;

    [Header("Màu sắc")]
    [SerializeField] private Color unlockedColor = Color.white;
    [SerializeField] private Color lockedColor = Color.gray;

    private bool isUnlocked = false;

    void Start()
    {
        if (islandImage == null) islandImage = GetComponent<Image>();
        if (button == null) button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.AddListener(OnIslandClick);
        }

        // --- LOGIC MỚI ĐỂ CHỜ DATA ---
        if (IslandProgressManager.Instance == null)
        {
            Debug.LogError("Không tìm thấy IslandProgressManager!");
            UpdateVisual(); // Hiển thị trạng thái khóa
            return;
        }

        // 1. Kiểm tra xem data đã tải xong SẴN chưa
        if (IslandProgressManager.Instance.IsDataLoaded)
        {
            CheckStatusAndUpdate();
        }
        else
        {
            // 2. Nếu CHƯA, đăng ký lắng nghe sự kiện
            Debug.Log($"Data not ready for {islandID}. Subscribing to OnDataLoaded.");

            // Tạm thời hiển thị là bị khóa
            isUnlocked = false;
            UpdateVisual();

            // Lắng nghe sự kiện
            IslandProgressManager.OnDataLoaded += HandleDataLoaded;
        }
    }

    // Hàm này sẽ được gọi bởi sự kiện OnDataLoaded
    private void HandleDataLoaded()
    {
        // Đảm bảo hàm này chạy trên đúng đảo
        if (this == null)
        {
            IslandProgressManager.OnDataLoaded -= HandleDataLoaded;
            return;
        }

        Debug.Log($"OnDataLoaded event received for {islandID}. Re-checking status.");

        // Ngừng lắng nghe để tránh gọi lại
        IslandProgressManager.OnDataLoaded -= HandleDataLoaded;

        // Bây giờ mới kiểm tra
        CheckStatusAndUpdate();
    }

    void CheckStatusAndUpdate()
    {
        if (IslandProgressManager.Instance == null) return;

        isUnlocked = IslandProgressManager.Instance.IsIslandUnlocked(islandID);
        UpdateVisual();
    }

    void UpdateVisual()
    {
        if (isUnlocked)
        {
            islandImage.color = unlockedColor;
            button.interactable = true;
        }
        else
        {
            islandImage.color = lockedColor;
            button.interactable = false;
        }
    }

    void OnIslandClick()
    {
        if (!isUnlocked) return;

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    // Rất quan trọng: Dọn dẹp sự kiện khi bị hủy
    void OnDestroy()
    {
        if (IslandProgressManager.Instance != null)
        {
            IslandProgressManager.OnDataLoaded -= HandleDataLoaded;
        }
    }
}