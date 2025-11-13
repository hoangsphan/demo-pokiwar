using UnityEngine;
using System.Collections.Generic; // <-- THÊM DÒNG NÀY

public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    // Dữ liệu cần vận chuyển
    public GameObject selectedEnemyPrefab;
    public string previousSceneName; // Dùng để lưu tên map (ví dụ: "FireMap")

    // === THÊM CÁC BIẾN NÀY ĐỂ MANG DATA CHO BATTLEMANAGER ===
    public GameObject selectedPetPrefab;
    public List<GameObject> selectedCardPrefabs = new List<GameObject>();
    // =======================================================

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Script này nằm trên DatabaseManager, 
            // mà DatabaseManager đã DontDestroyOnLoad, nên nó sẽ sống.
        }
        else
        {
            // Không tự hủy, vì nó nằm trên object đã DontDestroyOnLoad
        }
    }
}