using UnityEngine;
using UnityEngine.UI;

// Gắn script này lên TẤT CẢ các nút chọn Pet trên Map
public class PetMapButton : MonoBehaviour
{
    [Tooltip("Kéo Prefab của Pet tương ứng vào đây (ví dụ: Redgun.prefab)")]
    public GameObject enemyPetPrefab;

    [Tooltip("ID của Pet (phải khớp với ID trong PlayerData.defeatedEnemies, VÍ DỤ: Redgun)")]
    public string enemyID;

    [Tooltip("Kéo icon ổ khóa (nếu có) vào đây")]
    public GameObject lockIcon;

    private Button button;
    private MapManager mapManager;

    void Start()
    {
        button = GetComponent<Button>();
        // Tìm MapManager trong scene
        mapManager = FindFirstObjectByType<MapManager>();

        if (button == null || mapManager == null)
        {
            Debug.LogError($"PetMapButton '{enemyID}' không tìm thấy Button hoặc MapManager!");
            gameObject.SetActive(false); // Tự hủy nếu thiếu
            return;
        }

        // Gán sự kiện click để gọi MapManager
        button.onClick.AddListener(OnSelect);
    }

    void OnSelect()
    {
        // Khi được click, báo cho MapManager biết prefab nào được chọn
        mapManager.SelectEnemy(enemyPetPrefab);
    }

    // MapManager sẽ gọi hàm này để khóa/mở nút
    public void SetLocked(bool isLocked)
    {
        if (button != null)
        {
            button.interactable = !isLocked;
        }
        if (lockIcon != null)
        {
            lockIcon.SetActive(isLocked);
        }
    }

    // MapManager gọi hàm này nếu đã bị đánh bại
    public void SetDefeated()
    {
        // Làm mờ nút và vô hiệu hóa
        SetLocked(true);
        Image img = GetComponent<Image>();
        if (img != null)
        {
            // Đặt màu xám và giảm alpha để làm mờ
            img.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        }
    }
}