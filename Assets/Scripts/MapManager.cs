using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class MapManager : MonoBehaviour
{
    [Header("Thiết lập Scene")]
    public string lobbySceneName = "LobbyScene";

    [Header("Thiết lập Boss")]
    [Tooltip("Nút bấm của Boss (Icarus hoặc Yvetal)")]
    public PetMapButton bossButton;

    [Tooltip("Danh sách TẤT CẢ các nút quái thường (KHÔNG bao gồm Boss)")]
    public List<PetMapButton> regularEnemyButtons;

    void Start()
    {
        StartCoroutine(CheckDatabaseAndSetup());
    }

    IEnumerator CheckDatabaseAndSetup()
    {
        // 1. Chờ DatabaseManager tồn tại (OK)
        while (DatabaseManager.Instance == null)
        {
            Debug.LogWarning("[MapManager] Đang chờ DatabaseManager.Instance...");
            yield return new WaitForSeconds(0.1f);
        }

        // 2. Chờ DatabaseManager tải xong dữ liệu (OK)
        while (!DatabaseManager.Instance.IsInitialized)
        {
            Debug.LogWarning("[MapManager] Đang chờ DatabaseManager khởi tạo (IsInitialized)...");
            yield return new WaitForSeconds(0.1f);
        }

        Debug.Log("[MapManager] Database đã sẵn sàng! Bắt đầu kiểm tra tiến trình...");

        var playerData = DatabaseManager.Instance.playerData;

        if (playerData == null)
        {
            Debug.LogError("[MapManager] Database đã khởi tạo nhưng playerData vẫn null! Khóa các nút.");
            foreach (var btn in regularEnemyButtons) btn.SetLocked(true);
            if (bossButton) bossButton.SetLocked(true);
            yield break;
        }

        // (Code kiểm tra defeatedEnemies và khóa/mở nút giữ nguyên)
        List<string> defeated = playerData.defeatedEnemies;
        int defeatedRegularCount = 0;

        foreach (var button in regularEnemyButtons)
        {
            if (defeated.Contains(button.enemyID))
            {
                button.SetDefeated();
                defeatedRegularCount++;
            }
            else
            {
                button.SetLocked(false);
            }
        }

        if (bossButton != null)
        {
            if (defeatedRegularCount >= regularEnemyButtons.Count)
            {
                bossButton.SetLocked(false);
                if (defeated.Contains(bossButton.enemyID))
                {
                    bossButton.SetDefeated();
                }
            }
            else
            {
                bossButton.SetLocked(true);
            }
        }
    }

    /// <summary>
    /// Được gọi bởi PetMapButton khi click
    /// </summary>
    public void SelectEnemy(GameObject enemyPrefab)
    {
        // === SỬA LỖI Ở ĐÂY ===
        // Thay vì tìm LobbyManager, chúng ta dùng GameSession (luôn tồn tại)
        if (GameSession.Instance == null)
        {
            Debug.LogError("[MapManager] Không tìm thấy GameSession! Bạn đã gắn GameSession.cs vào DatabaseManager chưa?");
            return;
        }

        if (enemyPrefab == null)
        {
            Debug.LogError("[MapManager] Nút này chưa được gán Enemy Pet Prefab!");
            return;
        }

        // Gửi prefab của enemy qua cho GameSession
        GameSession.Instance.selectedEnemyPrefab = enemyPrefab;
        Debug.Log("[MapManager] Đã lưu enemy vào GameSession: " + enemyPrefab.name);

        // Tải Lobby Scene
        SceneManager.LoadScene(lobbySceneName);
    }
}