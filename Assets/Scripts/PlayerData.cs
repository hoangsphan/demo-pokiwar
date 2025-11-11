using System.Collections.Generic;
using Firebase.Firestore;
using UnityEngine; // <-- Thêm dòng này để dùng Debug.Log

[FirestoreData]
public class PlayerData
{
    [FirestoreProperty]
    public string playerName { get; set; }

    [FirestoreProperty]
    public int playerLevel { get; set; }

    [FirestoreProperty]
    public int currency { get; set; }

    [FirestoreProperty]
    public int playerEXP { get; set; }

    [FirestoreProperty]
    public int expToNextLevel { get; set; }

    [FirestoreProperty]
    public List<string> ownedPetPrefabNames { get; set; }

    [FirestoreProperty]
    public List<string> ownedCardPrefabNames { get; set; }

    [FirestoreProperty]
    public string currentPetPrefabName { get; set; }

    [FirestoreProperty]
    public List<string> currentCardPrefabNames { get; set; }

    // === THÊM MỚI: Danh sách kẻ thù đã bị hạ ===
    [FirestoreProperty]
    public List<string> defeatedEnemies { get; set; }
    // ==========================================

    // Constructor rỗng (BẮT BUỘC cho Firestore)
    public PlayerData() { }

    // Constructor khi tạo mới (ĐÃ SỬA)
    public PlayerData(string name)
    {
        playerName = name;
        playerLevel = 1;
        currency = 0;
        playerEXP = 0;
        expToNextLevel = 100;

        ownedPetPrefabNames = new List<string> { "DefaultPetPrefabName" };
        ownedCardPrefabNames = new List<string> { "DefaultCardPrefabName" };
        currentPetPrefabName = "DefaultPetPrefabName";
        currentCardPrefabNames = new List<string> { "DefaultCardPrefabName" };

        // === THÊM MỚI: Khởi tạo list rỗng ===
        defeatedEnemies = new List<string>();
        // ===================================
    }

    // (Hàm AddExp giữ nguyên)
    public void AddExp(int amount)
    {
        if (amount <= 0) return;

        playerEXP += amount;
        Debug.Log($"Nhận được {amount} EXP. Tổng EXP: {playerEXP}/{expToNextLevel}");

        while (playerEXP >= expToNextLevel)
        {
            playerEXP -= expToNextLevel;
            playerLevel++;
            int oldExpToNext = expToNextLevel;
            expToNextLevel = Mathf.RoundToInt(expToNextLevel * 1.5f);

            Debug.Log($"🎉 LÊN CẤP! Đạt level {playerLevel}.");
            Debug.Log($"EXP tiếp theo: {playerEXP}/{expToNextLevel}");
        }
    }

    // === HÀM MỚI: Ghi nhận kẻ thù đã bị hạ ===
    /// <summary>
    /// Ghi nhận đã đánh bại một kẻ thù
    /// </summary>
    public void AddDefeatedEnemy(string enemyID)
    {
        if (defeatedEnemies == null)
        {
            defeatedEnemies = new List<string>();
        }

        // Chỉ thêm nếu chưa có trong danh sách
        if (!defeatedEnemies.Contains(enemyID))
        {
            defeatedEnemies.Add(enemyID);
            Debug.Log($"[PlayerData] Đã ghi nhận đánh bại: {enemyID}");
        }
    }
}