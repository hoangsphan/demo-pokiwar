using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Linq;

public class LobbyManager : MonoBehaviour
{
    // ===============================================
    // STATIC INSTANCE
    // ===============================================
    public static LobbyManager Instance { get; private set; }

    [Header("Data Lựa chọn (để qua Scene sau)")]
    public GameObject selectedPetPrefab;
    public List<GameObject> selectedCardPrefabs = new List<GameObject>();
    // (Enemy prefab giờ đã nằm trong GameSession.Instance)

    [Header("Thiết lập Lobby")]
    public int maxCards = 4;
    public string battleSceneName = "SampleScene";

    [Header("Nút Lobby chính")]
    [SerializeField] private Button mainSelectPetButton;
    [SerializeField] private Button mainSelectCardButton;
    [SerializeField] private Button playButton;

    [Header("Hiển thị Lựa chọn (UI)")]
    [SerializeField] private Image selectedPetDisplay;
    [SerializeField] private Transform selectedCardSlotsParent;
    // === THÊM MỚI: UI cho Enemy ===
    [SerializeField] private Image enemyPetDisplay;
    // =============================

    [Header("Popup Chọn Lựa (Chung)")]
    [SerializeField] private GameObject selectionPopup;
    [SerializeField] private Text popupTitle;
    [SerializeField] private Button popupCloseButton;
    [SerializeField] private Transform popupContentParent;
    [SerializeField] private GameObject itemButtonPrefab;

    [Header("Data Nguồn (Kéo Prefab vào đây)")]
    [Tooltip("Danh sách TẤT CẢ các Pet Prefab có trong game")]
    [SerializeField] private List<GameObject> allAvailablePets;
    [Tooltip("Danh sách TẤT CẢ các Card Prefab có trong game")]
    [SerializeField] private List<GameObject> allAvailableCards;

    // (Đã xóa các trường UI Stats vì chúng thuộc về MainMenuStatsUI)

    // ===============================================
    // SETUP
    // ===============================================

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Gán sự kiện cho các nút chính
        if (mainSelectPetButton != null) mainSelectPetButton.onClick.AddListener(() => StartCoroutine(OpenPetSelection()));
        if (mainSelectCardButton != null) mainSelectCardButton.onClick.AddListener(() => StartCoroutine(OpenCardSelection()));
        if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
        if (popupCloseButton != null) popupCloseButton.onClick.AddListener(ClosePopup);
        if (selectionPopup != null) selectionPopup.SetActive(false);

        StartCoroutine(WaitForDatabaseAndSetup());

        // Gán sự kiện "gỡ thẻ" cho các slot
        if (selectedCardSlotsParent != null)
        {
            for (int i = 0; i < selectedCardSlotsParent.childCount; i++)
            {
                Button slotButton = selectedCardSlotsParent.GetChild(i).GetComponent<Button>();
                if (slotButton != null)
                {
                    int index = i;
                    slotButton.onClick.AddListener(() => OnCardSlotClicked(index));
                }
            }
        }
    }

    IEnumerator WaitForDatabaseAndSetup()
    {
        while (DatabaseManager.Instance == null)
        {
            Debug.LogWarning("[LobbyManager] Đang chờ DatabaseManager.Instance...");
            yield return new WaitForSeconds(0.1f);
        }
        while (!DatabaseManager.Instance.IsInitialized)
        {
            Debug.LogWarning("[LobbyManager] Đang chờ DatabaseManager khởi tạo (IsInitialized)...");
            yield return new WaitForSeconds(0.1f);
        }

        Debug.Log("[LobbyManager] Database đã sẵn sàng! Tải data và cập nhật UI.");

        LoadDataFromDatabase();
        // (Hàm UpdatePlayerStatsUI đã bị xóa)

        // === THÊM MỚI: Cập nhật UI Enemy ===
        UpdateSelectedEnemyUI();
        // ==================================
    }

    void LoadDataFromDatabase()
    {
        if (DatabaseManager.Instance == null || DatabaseManager.Instance.playerData == null)
        {
            Debug.LogWarning("[LobbyManager] Không tìm thấy Database. Dùng lựa chọn mặc định.");
            UpdateSelectedPetUI();
            UpdateSelectedCardsUI();
            return;
        }

        var data = DatabaseManager.Instance.playerData;

        // Tải Pet đã trang bị
        if (!string.IsNullOrEmpty(data.currentPetPrefabName))
        {
            selectedPetPrefab = allAvailablePets.Find(p => p.name == data.currentPetPrefabName);
            if (selectedPetPrefab == null)
                Debug.LogWarning($"Không tìm thấy Pet prefab tên là '{data.currentPetPrefabName}' trong 'allAvailablePets'");
        }

        // Tải các Thẻ đã trang bị
        selectedCardPrefabs.Clear();
        if (data.currentCardPrefabNames != null)
        {
            foreach (string cardName in data.currentCardPrefabNames)
            {
                GameObject cardPrefab = allAvailableCards.Find(c => c.name == cardName);
                if (cardPrefab != null)
                    selectedCardPrefabs.Add(cardPrefab);
                else
                    Debug.LogWarning($"Không tìm thấy Card prefab tên là '{cardName}' trong 'allAvailableCards'");
            }
        }

        UpdateSelectedPetUI();
        UpdateSelectedCardsUI();
        Debug.Log("Đã tải lựa chọn (Pet/Card) từ Database.");
    }

    // (Hàm UpdatePlayerStatsUI đã bị xóa)

    // ===============================================
    // (Các hàm Popup, Chọn Item, Cập nhật UI Player/Card giữ nguyên)
    // ===============================================

    public IEnumerator OpenPetSelection()
    {
        if (popupTitle != null) popupTitle.text = "CHỌN PET (SỞ HỮU)";
        ClearPopupContent();
        if (selectionPopup != null) selectionPopup.SetActive(true);

        List<string> ownedPetNames = new List<string>();
        if (DatabaseManager.Instance != null && DatabaseManager.Instance.playerData != null)
            ownedPetNames = DatabaseManager.Instance.playerData.ownedPetPrefabNames;
        else
        {
            Debug.LogWarning("Không tìm thấy Database, hiển thị tạm tất cả Pet");
            ownedPetNames = allAvailablePets.Select(p => p.name).ToList();
        }

        List<GameObject> ownedPets = allAvailablePets
            .Where(p => ownedPetNames.Contains(p.name))
            .ToList();
        yield return null;
        foreach (GameObject petPrefab in ownedPets)
        {
            GameObject itemButton = Instantiate(itemButtonPrefab);
            itemButton.transform.SetParent(popupContentParent, false);
            itemButton.transform.localScale = Vector3.one;
            PetData petData = petPrefab.GetComponent<PetData>();
            Sprite petIcon = petPrefab.GetComponent<SpriteRenderer>().sprite;
            string petName = petData != null ? petData.combatStats.id : petPrefab.name;
            itemButton.transform.Find("Item_Icon").GetComponent<Image>().sprite = petIcon;
            itemButton.transform.Find("Item_Name_Text").GetComponent<Text>().text = petName;
            itemButton.GetComponent<Button>().onClick.AddListener(() => { OnPetSelected(petPrefab); });
        }
    }

    public IEnumerator OpenCardSelection()
    {
        if (popupTitle != null) popupTitle.text = "CHỌN CARD (SỞ HỮU)";
        ClearPopupContent();
        if (selectionPopup != null) selectionPopup.SetActive(true);

        List<string> ownedCardNames = new List<string>();
        if (DatabaseManager.Instance != null && DatabaseManager.Instance.playerData != null)
            ownedCardNames = DatabaseManager.Instance.playerData.ownedCardPrefabNames;
        else
        {
            Debug.LogWarning("Không tìm thấy Database, hiển thị tạm tất cả Card");
            ownedCardNames = allAvailableCards.Select(c => c.name).ToList();
        }

        List<GameObject> ownedCards = allAvailableCards
            .Where(c => ownedCardNames.Contains(c.name))
            .ToList();
        yield return null;

        foreach (GameObject cardPrefab in ownedCards)
        {
            GameObject cardItem = Instantiate(cardPrefab);
            cardItem.transform.SetParent(popupContentParent, false);
            cardItem.transform.localScale = Vector3.one;
            cardItem.GetComponent<Button>().onClick.AddListener(() => { OnCardSelected(cardPrefab); });
        }
    }

    public void ClosePopup()
    {
        if (selectionPopup != null) selectionPopup.SetActive(false);
    }

    private void ClearPopupContent()
    {
        foreach (Transform child in popupContentParent)
        {
            Destroy(child.gameObject);
        }
    }

    void OnPetSelected(GameObject petPrefab)
    {
        if (petPrefab.scene.IsValid())
            Debug.LogError("LỖI NGHIÊM TRỌNG: Pet bạn vừa chọn (" + petPrefab.name + ") là một GameObject TỪ SCENE. Nó KHÔNG PHẢI là Prefab. Kéo prefab từ Project vào 'All Available Pets'.");
        selectedPetPrefab = petPrefab;
        Debug.Log("Đã chọn Pet: " + petPrefab.name);
        ClosePopup();
        UpdateSelectedPetUI();
    }

    void OnCardSelected(GameObject cardPrefab)
    {
        if (selectedCardPrefabs.Count >= maxCards)
        {
            Debug.Log("Đã chọn đủ số thẻ!");
            return;
        }
        CardData newCardData = cardPrefab.GetComponent<CardData>();
        if (newCardData == null)
        {
            Debug.LogError("Prefab thẻ bài: " + cardPrefab.name + " thiếu component CardData!");
            return;
        }
        if (newCardData.cardType == CardData.CardType.Skill)
        {
            foreach (GameObject selectedCard in selectedCardPrefabs)
            {
                CardData existingCardData = selectedCard.GetComponent<CardData>();
                if (existingCardData != null && existingCardData.cardType == CardData.CardType.Skill)
                {
                    Debug.Log("Bạn chỉ được chọn TỐI ĐA 1 thẻ SKILL!");
                    return;
                }
            }
        }
        selectedCardPrefabs.Add(cardPrefab);
        Debug.Log("Đã chọn Card: " + cardPrefab.name);
        UpdateSelectedCardsUI();
    }

    void OnCardSlotClicked(int index)
    {
        if (index < selectedCardPrefabs.Count)
        {
            Debug.Log("Đã gỡ thẻ: " + selectedCardPrefabs[index].name);
            selectedCardPrefabs.RemoveAt(index);
            UpdateSelectedCardsUI();
        }
    }

    void UpdateSelectedPetUI()
    {
        if (selectedPetDisplay == null) return;
        if (selectedPetPrefab != null)
        {
            selectedPetDisplay.sprite = selectedPetPrefab.GetComponent<SpriteRenderer>().sprite;
            selectedPetDisplay.color = Color.white;
            selectedPetDisplay.gameObject.SetActive(true);
        }
        else
        {
            selectedPetDisplay.sprite = null;
            selectedPetDisplay.color = Color.clear;
            selectedPetDisplay.gameObject.SetActive(false);
        }
    }

    // === HÀM MỚI: Cập nhật UI cho Enemy ===
    void OnEnable()
    {
        // Cập nhật UI mỗi khi scene được kích hoạt
        UpdateSelectedEnemyUI();
    }

    // === HÀM MỚI: Cập nhật UI cho Enemy ===
    public void UpdateSelectedEnemyUI() // ← ĐỔI THÀNH PUBLIC
    {
        Debug.Log("=== UpdateSelectedEnemyUI được gọi ===");

        if (enemyPetDisplay == null)
        {
            Debug.LogWarning("[LobbyManager] Chưa gán 'Enemy Pet Display' slot.");
            return;
        }

        // Kiểm tra GameSession (nơi chứa prefab enemy)
        if (GameSession.Instance != null && GameSession.Instance.selectedEnemyPrefab != null)
        {
            GameObject enemyPrefab = GameSession.Instance.selectedEnemyPrefab;
            Debug.Log($"[LobbyManager] Đang cập nhật UI cho enemy: {enemyPrefab.name}");

            // Lấy sprite từ SpriteRenderer của prefab
            SpriteRenderer enemySpriteRenderer = enemyPrefab.GetComponent<SpriteRenderer>();

            if (enemySpriteRenderer != null && enemySpriteRenderer.sprite != null)
            {
                enemyPetDisplay.sprite = enemySpriteRenderer.sprite;
                enemyPetDisplay.color = Color.white;
                enemyPetDisplay.gameObject.SetActive(true);
                Debug.Log($"✓ Đã hiển thị enemy sprite: {enemySpriteRenderer.sprite.name}");
            }
            else
            {
                Debug.LogWarning($"Prefab Enemy {enemyPrefab.name} không có SpriteRenderer hoặc Sprite!");
                enemyPetDisplay.gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.Log("[LobbyManager] Không có enemy nào được chọn, ẩn UI");
            // Nếu không có enemy nào (lỗi hoặc test), ẩn nó đi
            enemyPetDisplay.sprite = null;
            enemyPetDisplay.color = Color.clear;
            enemyPetDisplay.gameObject.SetActive(false);
        }
    }
    // ======================================

    void UpdateSelectedCardsUI()
    {
        if (selectedCardSlotsParent == null) return;
        for (int i = 0; i < selectedCardSlotsParent.childCount; i++)
        {
            Image slotImage = selectedCardSlotsParent.GetChild(i).GetComponent<Image>();
            if (slotImage == null) continue;
            if (i < selectedCardPrefabs.Count)
            {
                slotImage.sprite = selectedCardPrefabs[i].GetComponent<Image>().sprite;
                slotImage.color = Color.white;
            }
            else
            {
                slotImage.sprite = null;
                slotImage.color = Color.clear;
            }
        }
    }

    // (Hàm OnPlayClicked đã được sửa ở lần trước và vẫn chính xác)
    public void OnPlayClicked()
    {
        if (selectedPetPrefab == null)
        {
            Debug.LogWarning("Vui lòng chọn PET trước khi chơi!");
            return;
        }

        if (GameSession.Instance == null || GameSession.Instance.selectedEnemyPrefab == null)
        {
            Debug.LogWarning("Lỗi: Không có kẻ thù nào được chọn (GameSession.Instance.selectedEnemyPrefab bị null). Bạn có đi từ Map không?");
            return;
        }

        if (selectedCardPrefabs.Count == 0)
        {
            Debug.LogWarning("Vui lòng chọn ít nhất 1 CARD!");
            return;
        }

        if (DatabaseManager.Instance != null && DatabaseManager.Instance.playerData != null)
        {
            var data = DatabaseManager.Instance.playerData;
            data.currentPetPrefabName = selectedPetPrefab.name;
            data.currentCardPrefabNames = selectedCardPrefabs.Select(c => c.name).ToList();
            DatabaseManager.Instance.SaveData();
            Debug.Log("Đã lưu lựa chọn (Pet/Card) vào Database.");
        }
        else
            Debug.LogWarning("Không tìm thấy Database. Không thể lưu lựa chọn.");

        Debug.Log($"Bắt đầu tải Scene: {battleSceneName} (Đấu với {GameSession.Instance.selectedEnemyPrefab.name})");
        SceneManager.LoadScene(battleSceneName);
    }
}