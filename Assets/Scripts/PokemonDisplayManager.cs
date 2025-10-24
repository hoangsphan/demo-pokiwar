using UnityEngine;

public class PokemonDisplayManager : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("Prefab của Player Pet (chứa PetData, SpriteRenderer, SimpleBobbing)")]
    public GameObject playerPetPrefab;
    [Tooltip("Prefab của Enemy Pet (chứa PetData, SpriteRenderer, SimpleBobbing)")]
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

    // Biến để lưu trữ pet đã được tạo ra
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

        // Tự động gán target VÀ PetData cho BattleManager
        if (battleManager)
        {
            // Gán Transform cho projectile bay
            battleManager.playerTarget = playerInstance.transform;
            battleManager.enemyTarget = enemyInstance.transform;

            // GÁN PET DATA (Quan trọng)
            battleManager.playerPet = playerInstance.GetComponent<PetData>();
            battleManager.enemyPet = enemyInstance.GetComponent<PetData>();

            // Kiểm tra lỗi
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

    // Hàm này giữ nguyên từ file cũ của bạn
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

    /// <summary>
    /// Hàm này giờ sẽ Instantiate Prefab
    /// </summary>
    GameObject SetupCharacter(bool isPlayer)
    {
        GameObject prefab = isPlayer ? playerPetPrefab : enemyPetPrefab;
        if (!prefab)
        {
            Debug.LogError($"Chưa gán Prefab cho " + (isPlayer ? "Player" : "Enemy"));
            return null;
        }

        Vector3 pos = isPlayer ? playerPos : enemyPos;

        // 1. Instantiate (tạo) pet từ prefab
        GameObject petGO = Instantiate(prefab, pos, Quaternion.identity, transform);
        petGO.name = isPlayer ? "PlayerChar_Instance" : "EnemyChar_Instance";

        // 2. Tùy chỉnh (ví dụ lật hình)
        if (!isPlayer && flipEnemy)
        {
            if (petGO.TryGetComponent<SpriteRenderer>(out var sr))
            {
                sr.flipX = true;
            }
        }

        // 3. Scale
        petGO.transform.localScale = Vector3.one * characterScale;

        // 4. Trả về GameObject đã tạo
        return petGO;
    }


}
