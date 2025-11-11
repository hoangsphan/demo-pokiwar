using UnityEngine;

public class PokemonDisplayManager : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("Prefab Player MẶC ĐỊNH (Dùng nếu chạy scene này trực tiếp không qua Lobby)")]
    public GameObject playerPetPrefab;

    [Tooltip("Prefab của Enemy Pet MẶC ĐỊNH (Dùng nếu chạy scene này trực tiếp không qua Lobby)")]
    public GameObject enemyPetPrefab;

    [Header("Background")]
    public Sprite backgroundSprite;

    [Header("Vị trí (world)")]
    public Vector3 playerPos = new Vector3(-4.1f, 2.09f, 0f);
    public Vector3 enemyPos = new Vector3(9.66f, 1.89f, 0f);

    [Header("Tùy chỉnh hình ảnh")]
    public bool flipEnemy = true;
    public float characterScale = 1.0f;

    [Header("Tham chiếu")]
    [Tooltip("Kéo BattleManager trong Scene vào đây")]
    [SerializeField] private BattleManager battleManager;

    private GameObject playerInstance;
    private GameObject enemyInstance;

    void Start()
    {
        if (!battleManager)
        {
            Debug.LogError("[PokemonDisplayManager] Chưa gán BattleManager!");
            battleManager = FindFirstObjectByType<BattleManager>();
        }

        SetupBackground();
        playerInstance = SetupCharacter(true);   // player
        enemyInstance = SetupCharacter(false);  // enemy

        if (battleManager)
        {
            battleManager.playerTarget = playerInstance.transform;
            battleManager.enemyTarget = enemyInstance.transform;

            battleManager.playerPet = playerInstance.GetComponent<PetData>();
            battleManager.enemyPet = enemyInstance.GetComponent<PetData>();

            if (battleManager.playerPet == null || battleManager.enemyPet == null)
            {
                Debug.LogError("[PokemonDisplayManager] Prefab pet thiếu component PetData!");
            }
            else
            {
                Debug.Log("Đã gán PetData và Target cho BattleManager.");
            }
        }
    }

    void SetupBackground()
    {
        if (!backgroundSprite) return;
        var go = new GameObject("BG");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = backgroundSprite;
        go.transform.position = new Vector3(2.6284f, 2.5977f, 10f);
        go.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        go.transform.localScale = new Vector3(2.1408f, 1.962382f, 0.711111f);
    }

    // === TOÀN BỘ LOGIC HÀM NÀY ĐÃ ĐƯỢC SỬA ===
    GameObject SetupCharacter(bool isPlayer)
    {
        GameObject prefabToSpawn = null;
        Vector3 pos;

        if (isPlayer)
        {
            pos = playerPos;

            // 1. Thử lấy prefab từ LobbyManager (nếu đang chạy game đúng luồng)
            if (LobbyManager.Instance != null && LobbyManager.Instance.selectedPetPrefab != null)
            {
                prefabToSpawn = LobbyManager.Instance.selectedPetPrefab;
                Debug.Log("[PokemonDisplayManager] Đã tải Player Pet từ Lobby: " + prefabToSpawn.name);
            }
            // 2. Nếu không có Lobby (ví dụ: test scene trực tiếp)
            else
            {
                Debug.LogWarning("[PokemonDisplayManager] Không tìm thấy LobbyManager.Instance hoặc selectedPetPrefab. Sử dụng prefab PLAYER MẶC ĐỊNH.");
                prefabToSpawn = playerPetPrefab; // Dùng prefab dự phòng
            }
        }
        else // Nếu là Enemy
        {
            pos = enemyPos;

            // === BẮT ĐẦU SỬA ===
            
            // 1. Thử lấy prefab Enemy từ GameSession (thay vì LobbyManager)
            if (GameSession.Instance != null && GameSession.Instance.selectedEnemyPrefab != null)
            {
                prefabToSpawn = GameSession.Instance.selectedEnemyPrefab;
                Debug.Log("[PokemonDisplayManager] Đã tải Enemy Pet từ GameSession: " + prefabToSpawn.name);
            }
            else
            {
                Debug.LogWarning("[PokemonDisplayManager] Không tìm thấy GameSession.Instance hoặc selectedEnemyPrefab. Sử dụng prefab ENEMY MẶC ĐỊNH.");
                prefabToSpawn = enemyPetPrefab; // Dùng prefab dự phòng
            }
            // === KẾT THÚC SỬA ===
        }

        // 3. Kiểm tra lần cuối trước khi tạo
        if (prefabToSpawn == null)
        {
            Debug.LogError($"[PokemonDisplayManager] Prefab cho {(isPlayer ? "Player" : "Enemy")} bị null! Vui lòng kiểm tra (cả prefab dự phòng).");
            return null;
        }

        // 4. Instantiate (tạo) pet từ prefab đã chọn
        GameObject petGO = Instantiate(prefabToSpawn, pos, Quaternion.identity, transform);

        // QUAN TRỌNG: Không đổi tên để giữ ID prefab
        // petGO.name = isPlayer ? "PlayerChar_Instance" : "EnemyChar_Instance"; // <-- ĐÃ XÓA DÒNG NÀY

        // 5. Tùy chỉnh (ví dụ lật hình)
        if (!isPlayer && flipEnemy)
        {
            if (petGO.TryGetComponent<SpriteRenderer>(out var sr))
            {
                sr.flipX = true;
            }
        }

        // 6. Scale
        petGO.transform.localScale = Vector3.one * characterScale;

        // 7. Trả về GameObject đã tạo
        return petGO;
    }
}