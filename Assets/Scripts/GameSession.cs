using UnityEngine;

// Script này dùng để vận chuyển data (như chọn enemy)
// qua các scene mà không cần các Manager phức tạp phải biết nhau.
public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    // Dữ liệu cần vận chuyển
    public GameObject selectedEnemyPrefab;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Gắn vào DontDestroyOnLoad (vì nó sẽ nằm trên DatabaseManager)
        }
        else
        {
            // Không tự hủy, vì nó nằm trên object đã DontDestroyOnLoad
        }
    }
}