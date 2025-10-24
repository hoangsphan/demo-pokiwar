using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
// using TMPro; 

public class LobbyManager : MonoBehaviour
{
    // ===============================================
    // STATIC INSTANCE (để giữ lựa chọn khi chuyển scene)
    // ===============================================
    public static LobbyManager Instance { get; private set; }

    [Header("Data Lựa chọn (để qua Scene sau)")]
    public GameObject selectedPetPrefab;
    public List<GameObject> selectedCardPrefabs = new List<GameObject>();

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

    [Header("Popup Chọn Lựa (Chung)")]
    [SerializeField] private GameObject selectionPopup;
    [SerializeField] private Text popupTitle;
    [SerializeField] private Button popupCloseButton;
    [SerializeField] private Transform popupContentParent;
    [SerializeField] private GameObject itemButtonPrefab;

    [Header("Data Nguồn (Kéo Prefab vào đây)")]
    [SerializeField] private List<GameObject> allAvailablePets;
    [SerializeField] private List<GameObject> allAvailableCards;


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
        // Gán sự kiện cho các nút chính (Gọi Coroutine)
        if (mainSelectPetButton != null) mainSelectPetButton.onClick.AddListener(() => StartCoroutine(OpenPetSelection()));
        if (mainSelectCardButton != null) mainSelectCardButton.onClick.AddListener(() => StartCoroutine(OpenCardSelection()));
        if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);

        // Gán sự kiện cho Popup Chọn Lựa
        if (selectionPopup != null) selectionPopup.SetActive(false);
        if (popupCloseButton != null) popupCloseButton.onClick.AddListener(ClosePopup);

        // Cập nhật UI ban đầu
        UpdateSelectedPetUI();
        UpdateSelectedCardsUI();

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
                else
                {
                    Debug.LogWarning("Slot " + i + " (" + selectedCardSlotsParent.GetChild(i).name + ") KHÔNG CÓ component Button! Sẽ không thể gỡ thẻ.");
                }
            }
        }
    }


    // ===============================================
    // POPUP CHỌN LỰA (PET/CARD) - BẢN COROUTINE
    // ===============================================

    public IEnumerator OpenPetSelection()
    {
        if (popupTitle != null) popupTitle.text = "CHỌN PET";
        ClearPopupContent();

        if (selectionPopup != null)
            selectionPopup.SetActive(true);

        yield return null;

        foreach (GameObject petPrefab in allAvailablePets)
        {
            GameObject itemButton = Instantiate(itemButtonPrefab);
            itemButton.transform.SetParent(popupContentParent, false);
            itemButton.transform.localScale = Vector3.one;

            PetData petData = petPrefab.GetComponent<PetData>();
            Sprite petIcon = petPrefab.GetComponent<SpriteRenderer>().sprite;
            string petName = petData != null ? petData.combatStats.id : petPrefab.name;

            itemButton.transform.Find("Item_Icon").GetComponent<Image>().sprite = petIcon;
            itemButton.transform.Find("Item_Name_Text").GetComponent<Text>().text = petName;

            itemButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                OnPetSelected(petPrefab);
            });
        }
    }

    public IEnumerator OpenCardSelection()
    {
        if (popupTitle != null) popupTitle.text = "CHỌN CARD";
        ClearPopupContent();

        if (selectionPopup != null)
            selectionPopup.SetActive(true);

        yield return null;

        foreach (GameObject cardPrefab in allAvailableCards)
        {
            GameObject cardItem = Instantiate(cardPrefab);
            cardItem.transform.SetParent(popupContentParent, false);
            cardItem.transform.localScale = Vector3.one;

            cardItem.GetComponent<Button>().onClick.AddListener(() =>
            {
                OnCardSelected(cardPrefab);
            });
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

    // ===============================================
    // XỬ LÝ KHI CHỌN ITEM
    // ===============================================

    void OnPetSelected(GameObject petPrefab)

    {
        if (petPrefab.scene.IsValid())
        {
            Debug.LogError("LỖI NGHIÊM TRỌNG: Pet bạn vừa chọn (" + petPrefab.name + ") là một GameObject TỪ SCENE (Hierarchy). Nó KHÔNG PHẢI là Prefab (từ Project). Game sẽ crash khi chuyển scene.");
            Debug.LogError("HÃY MỞ LOBBYSCENE, CHỌN LOBBYMANAGER, VÀ KÉO PREFAB TỪ PROJECT VÀO DANH SÁCH 'All Available Pets'.");
        }
        selectedPetPrefab = petPrefab;
        Debug.Log("Đã chọn Pet: " + petPrefab.name);
        ClosePopup();
        UpdateSelectedPetUI();
    }

    // --- HÀM NÀY ĐÃ ĐƯỢC SỬA ---
    void OnCardSelected(GameObject cardPrefab)
    {
        // 1. Kiểm tra giới hạn tổng số thẻ
        if (selectedCardPrefabs.Count >= maxCards)
        {
            Debug.Log("Đã chọn đủ số thẻ!");
            return;
        }

        // 2. Lấy data của thẻ vừa click
       
        CardData newCardData = cardPrefab.GetComponent<CardData>();
        if (newCardData == null)
        {
            Debug.LogError("Prefab thẻ bài: " + cardPrefab.name + " thiếu component LobbyCardData!");
            return;
        }

        // 3. Kiểm tra logic thẻ SKILL
        if (newCardData.cardType == CardData.CardType.Skill)
        {
            // 4. Duyệt qua các thẻ đã chọn
            foreach (GameObject selectedCard in selectedCardPrefabs)
            {
                CardData existingCardData = selectedCard.GetComponent<CardData>();
                if (existingCardData != null && existingCardData.cardType == CardData.CardType.Skill)
                {
                    // 5. Nếu đã có thẻ Skill, báo lỗi và dừng
                    Debug.Log("Bạn chỉ được chọn TỐI ĐA 1 thẻ SKILL!");
                    return; // Dừng lại, không thêm thẻ
                }
            }
        }

        // 6. Thêm thẻ vào danh sách (nếu là thẻ Item hoặc là thẻ Skill đầu tiên)
        selectedCardPrefabs.Add(cardPrefab);
        Debug.Log("Đã chọn Card: " + cardPrefab.name);

        UpdateSelectedCardsUI();
    }
    // --- KẾT THÚC SỬA ---

    void OnCardSlotClicked(int index)
    {
        if (index < selectedCardPrefabs.Count)
        {
            Debug.Log("Đã gỡ thẻ: " + selectedCardPrefabs[index].name);
            selectedCardPrefabs.RemoveAt(index);
            UpdateSelectedCardsUI();
        }
        else
        {
            Debug.Log("Click vào ô trống.");
        }
    }

    // ===============================================
    // CẬP NHẬT UI LOBBY
    // ===============================================

    void UpdateSelectedPetUI()
    {
        if (selectedPetDisplay == null) return;

        if (selectedPetPrefab != null)
        {
            selectedPetDisplay.sprite = selectedPetPrefab.GetComponent<SpriteRenderer>().sprite;
            selectedPetDisplay.color = Color.white;
        }
        else
        {
            selectedPetDisplay.sprite = null;
            selectedPetDisplay.color = Color.clear;
        }
    }

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

    // ===============================================
    // CHƠI GAME
    // ===============================================
    public void OnPlayClicked()
    {
        if (selectedPetPrefab == null)
        {
            Debug.LogWarning("Vui lòng chọn PET trước khi chơi!");
            return;
        }

        if (selectedCardPrefabs.Count == 0)
        {
            Debug.LogWarning("Vui lòng chọn ít nhất 1 CARD!");
            return;
        }

        Debug.Log("Bắt đầu tải Scene: " + battleSceneName);
        SceneManager.LoadScene(battleSceneName);
    }
}