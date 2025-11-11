using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections; // <-- THÊM DÒNG NÀY

public class MainMenuStatsUI : MonoBehaviour
{
    [Header("Player Stats UI")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private Slider expSlider;
    [SerializeField] private TextMeshProUGUI expText;

    // SỬA: Đổi Start() thành Coroutine
    void Start()
    {
        StartCoroutine(WaitForDatabaseAndUpdateUI());
    }

    // HÀM MỚI: Coroutine để chờ Database
    IEnumerator WaitForDatabaseAndUpdateUI()
    {
        // 1. Chờ DatabaseManager tồn tại
        while (DatabaseManager.Instance == null)
        {
            Debug.LogWarning("[MainMenuStatsUI] Đang chờ DatabaseManager.Instance...");
            yield return new WaitForSeconds(0.1f);
        }

        // 2. Chờ DatabaseManager tải xong dữ liệu
        while (!DatabaseManager.Instance.IsInitialized)
        {
            Debug.LogWarning("[MainMenuStatsUI] Đang chờ DatabaseManager khởi tạo (IsInitialized)...");
            yield return new WaitForSeconds(0.1f);
        }

        Debug.Log("[MainMenuStatsUI] Database đã sẵn sàng! Cập nhật UI Stats.");

        // 3. Chạy logic cập nhật UI (giờ đã an toàn)
        UpdatePlayerStatsUI();
    }

    // (Hàm UpdatePlayerStatsUI giữ nguyên)
    public void UpdatePlayerStatsUI()
    {
        if (DatabaseManager.Instance == null || DatabaseManager.Instance.playerData == null)
        {
            Debug.LogWarning("[MainMenuStatsUI] Không tìm thấy DatabaseManager để cập nhật UI.");
            if (levelText) levelText.text = "Level: ?";
            if (currencyText) currencyText.text = "Vàng: ?";
            if (expText) expText.text = "EXP: ? / ?";
            if (expSlider) expSlider.value = 0;
            return;
        }

        var data = DatabaseManager.Instance.playerData;

        if (levelText)
            levelText.text = $"Level: {data.playerLevel}";
        if (currencyText)
            currencyText.text = $"Vàng: {data.currency}";
        if (expSlider)
        {
            if (data.expToNextLevel > 0)
                expSlider.value = (float)data.playerEXP / data.expToNextLevel;
            else
                expSlider.value = 1f;
        }
        if (expText)
            expText.text = $"{data.playerEXP} / {data.expToNextLevel}";
    }
}