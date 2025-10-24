using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BattleCardManager : MonoBehaviour
{
    [Header("Tham chiếu")]
    [Tooltip("Kéo BattleManager trong Scene vào đây")]
    [SerializeField] private BattleManager battleManager;

    // Lưu ý: Script này được gắn TRỰC TIẾP
    // lên panel chứa thẻ, nên không cần biến 'cardSlotsParent'

    void Start()
    {
        // 1. Tự tìm BattleManager nếu chưa gán
        if (battleManager == null)
        {
            battleManager = FindFirstObjectByType<BattleManager>();
        }
        if (battleManager == null)
        {
            Debug.LogError("[BattleCardManager] Không tìm thấy BattleManager!");
            return;
        }

        // 2. Kiểm tra LobbyManager và lấy danh sách thẻ
        if (LobbyManager.Instance != null && LobbyManager.Instance.selectedCardPrefabs.Count > 0)
        {
            // Lấy danh sách thẻ đã chọn từ Lobby
            var cardsToLoad = LobbyManager.Instance.selectedCardPrefabs;
            SpawnCards(cardsToLoad);
        }
        else
        {
            Debug.LogWarning("[BattleCardManager] Không tìm thấy LobbyManager hoặc không có thẻ nào được chọn.");
        }
    }

    void SpawnCards(List<GameObject> cardPrefabs)
    {
        // 'transform' ở đây chính là cái 'BattleCardPanel'
        // vì script được gắn trực tiếp lên nó.
        Transform cardSlotsParent = this.transform;

        foreach (GameObject cardPrefab in cardPrefabs)
        {
            // 1. Tạo thẻ bài (dùng prefab từ Lobby)
            // Dùng cách Instantiate "an toàn" để tránh lỗi layout
            GameObject cardInstance = Instantiate(cardPrefab);
            cardInstance.transform.SetParent(cardSlotsParent, false);
            cardInstance.transform.localScale = Vector3.one;

            // 2. Lấy data và nút từ prefab
            CardData cardData = cardInstance.GetComponent<CardData>();
            Button cardButton = cardInstance.GetComponent<Button>();

            if (cardData == null || cardButton == null)
            {
                Debug.LogError("Prefab thẻ bài thiếu CardData hoặc Button!");
                continue;
            }

            // 3. Gán sự kiện Click cho nút
            cardButton.onClick.AddListener(() =>
            {
                OnCardClicked(cardData, cardButton);
            });
        }
    }

    /// <summary>
    /// Được gọi khi một thẻ bài được click trong trận
    /// </summary>
    void OnCardClicked(CardData data, Button button)
    {
        // Kiểm tra xem có phải lượt Player không
        if (!battleManager.isPlayerTurn)
        {
            battleManager.OnCombatLog?.Invoke("Chưa tới lượt!");
            return;
        }

        // Gọi hàm tương ứng trên BattleManager
        switch (data.cardType)
        {
            case CardData.CardType.ManaItem:
                battleManager.UseManaCard(data.itemValue);
                button.interactable = false; // Vô hiệu hóa thẻ
                break;
            case CardData.CardType.RageItem:
                battleManager.UseRageCard(data.itemValue);
                button.interactable = false; // Vô hiệu hóa thẻ
                break;
            case CardData.CardType.Skill:
                battleManager.UseSkillCard(data.skillID);
                // (Tùy bạn, có thể vô hiệu hóa hoặc không)
                // button.interactable = false; 
                break;
        }
    }
}