using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Gem;

public class BattleManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] public Match3Manager board;

    [Header("Actors")]
    // THAY THẾ: Không dùng "new Actor()" nữa
    [Tooltip("Sẽ được gán tự động bởi PokemonDisplayManager")]
    public PetData playerPet;
    [Tooltip("Sẽ được gán tự động bởi PokemonDisplayManager")]
    public PetData enemyPet;

    [Header("Flow")]
    public bool enableAI = true;
    public bool isPlayerTurn = true;

    [Header("Animation")]
    public GameObject[] projectilePrefabs;
    public Transform playerTarget; // Sẽ được gán tự động
    public Transform enemyTarget; // Sẽ được gán tự động
    public float projectileFlyDuration = 0.5f;

    // Events
    public System.Action<string> OnCombatLog;
    public System.Action<Actor, Actor> OnStatsChanged;
    public System.Action<bool> OnTurnChanged;

    // Internal state
    private bool _endingTurn = false;
    private List<List<Gem>> _currentTurnMatches = new List<List<Gem>>();

    void OnEnable()
    {
        if (!board) board = FindFirstObjectByType<Match3Manager>();
        if (board) board.OnMatchesResolved += HandleMatchesResolved;
    }

    void OnDisable()
    {
        if (board) board.OnMatchesResolved -= HandleMatchesResolved;
    }

    void Start()
    {
        if (!board)
        {
            Debug.LogError("[BattleManager] Chưa gán Match3Manager (board)!");
            enabled = false;
            return;
        }

        // CHUYỂN LOGIC START QUA COROUTINE NÀY
        // để chờ PokemonDisplayManager gán pet
        StartCoroutine(WaitForPetsAndStart());
    }

    // Tách ra Coroutine để chờ pet được spawn
    IEnumerator WaitForPetsAndStart()
    {
        // Chờ đến khi PetData được gán (bởi PokemonDisplayManager)
        while (playerPet == null || enemyPet == null)
        {
            Debug.LogWarning("[BattleManager] Đang chờ PokemonDisplayManager spawn pet...");
            yield return new WaitForSeconds(0.1f);
        }

        // Clamp chỉ số ban đầu (lấy từ PetData)
        ClampActor(playerPet.GetActor());
        ClampActor(enemyPet.GetActor());

        // Bật/tắt input theo người đi trước
        board.SetPlayerInputEnabled(isPlayerTurn);

        // Thông báo UI
        OnStatsChanged?.Invoke(playerPet.GetActor(), enemyPet.GetActor());
        OnTurnChanged?.Invoke(isPlayerTurn);

        // Nếu AI đi trước → cho AI chơi
        if (enableAI && !isPlayerTurn)
            StartCoroutine(CallAI());
    }

    // Hàm này giữ nguyên
    void HandleMatchesResolved(List<List<Gem>> matches)
    {
        if (matches == null || matches.Count == 0) return;
        _currentTurnMatches.AddRange(matches);
        StartCoroutine(WaitBoardAndEndTurn());
    }

    // Hàm này SỬA LẠI ĐỂ DÙNG "playerPet.GetActor()"
    IEnumerator WaitBoardAndEndTurn()
    {
        if (_endingTurn) yield break;
        _endingTurn = true;

        while (board != null && !board.IsGridReady())
            yield return null;

        if (_currentTurnMatches.Count > 0)
        {
            board.HideGrid();
            yield return new WaitForSeconds(0.2f);
            yield return StartCoroutine(AnimateCombatEffects(_currentTurnMatches));
            _currentTurnMatches.Clear();
            board.ShowGrid();
            yield return new WaitForSeconds(0.2f);
        }

        yield return new WaitForSeconds(0.05f);

        // SỬA Ở ĐÂY: Dùng "playerPet" và "enemyPet"
        if (IsDead(playerPet.GetActor())) // << SỬA
        {
            Debug.Log("PLAYER DIED");
            yield break;
        }
        if (IsDead(enemyPet.GetActor())) // << SỬA
        {
            Debug.Log("ENEMY DIED");
            yield break;
        }

        isPlayerTurn = !isPlayerTurn;
        OnTurnChanged?.Invoke(isPlayerTurn);
        board.SetPlayerInputEnabled(isPlayerTurn);

        if (enableAI && !isPlayerTurn)
            yield return CallAI();

        _endingTurn = false;
    }

    // Hàm này SỬA LẠI ĐỂ DÙNG "playerPet.GetActor()"
    IEnumerator AnimateCombatEffects(List<List<Gem>> allMatches)
    {
        // SỬA Ở ĐÂY:
        var attacker = isPlayerTurn ? playerPet.GetActor() : enemyPet.GetActor(); // << SỬA
        var defender = isPlayerTurn ? enemyPet.GetActor() : playerPet.GetActor(); // << SỬA

        var attackerTransform = isPlayerTurn ? playerTarget : enemyTarget;
        var defenderTransform = isPlayerTurn ? enemyTarget : playerTarget;

        if (playerTarget == null || enemyTarget == null)
        {
            Debug.LogError("[BattleManager] Chưa gán PlayerTarget hoặc EnemyTarget! (Lỗi này do PokemonDisplayManager)");
            _currentTurnMatches.Clear();
            yield break;
        }

        foreach (var group in allMatches)
        {
            if (group == null || group.Count == 0) continue;

            var fx = CombatSystem.ComputeEffects(new List<List<Gem>> { group });
            if (fx.IsEmpty) continue;

            // ... (Code lấy prefab và xác định mục tiêu giữ nguyên) ...
            GemType type = group[0].gemType;
            int typeIndex = (int)type;
            GameObject prefabToSpawn = null;
            if (typeIndex >= 0 && typeIndex < projectilePrefabs.Length)
                prefabToSpawn = projectilePrefabs[typeIndex];

            if (prefabToSpawn == null)
            {
                Debug.LogWarning($"[BattleManager] Thiếu projectile prefab cho {type}.");
            }
            else
            {
                Transform startPoint = attackerTransform;
                Transform endPoint;
                if (fx.damage > 0) endPoint = defenderTransform;
                else endPoint = attackerTransform;

                yield return StartCoroutine(SpawnAndFlyProjectile(
                    prefabToSpawn,
                    startPoint.position,
                    endPoint.position,
                    projectileFlyDuration
                ));
            }

            // 4. ÁP DỤNG HIỆU ỨNG (Không cần sửa, vì attacker/defender đã đúng)
            OnCombatLog?.Invoke($"🎬 Xử lý match {type} ({group.Count} viên)");
            CombatSystem.ApplyEffects(fx, attacker, defender, OnCombatLog);

            // Cập nhật chỉ số ngay
            // SỬA Ở ĐÂY:
            ClampActor(playerPet.GetActor()); // << SỬA
            ClampActor(enemyPet.GetActor()); // << SỬA
            OnStatsChanged?.Invoke(playerPet.GetActor(), enemyPet.GetActor()); // << SỬA

            yield return new WaitForSeconds(0.15f);
        }
        yield return new WaitForSeconds(0.3f);
    }

    // Các hàm còn lại giữ nguyên
    IEnumerator SpawnAndFlyProjectile(GameObject prefab, Vector3 startPos, Vector3 endPos, float duration)
    {
        // ... (Giữ nguyên code của bạn) ...
        GameObject proj = Instantiate(prefab, startPos, Quaternion.identity);
        if (Vector3.Distance(startPos, endPos) < 0.01f)
        {
            yield return new WaitForSeconds(duration);
        }
        else
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = 1f - Mathf.Pow(1f - t / duration, 3f);
                proj.transform.position = Vector3.Lerp(startPos, endPos, k);
                yield return null;
            }
            proj.transform.position = endPos;
        }
        Destroy(proj, 0.1f);
    }

    IEnumerator CallAI()
    {
        // ... (Giữ nguyên code của bạn) ...
        yield return new WaitForSeconds(0.25f);
        var ai = FindFirstObjectByType<AIController>();
        if (ai == null)
        {
            Debug.LogWarning("[BattleManager] Không tìm thấy AIController trong scene.");
            yield break;
        }
        ai.PlayTurn();
    }

    void ClampActor(Actor a)
    {
        // ... (Giữ nguyên code của bạn) ...
        a.maxHP = Mathf.Max(1, a.maxHP);
        a.maxMana = Mathf.Max(0, a.maxMana);
        a.maxRage = Mathf.Max(0, a.maxRage);
        a.hp = Mathf.Clamp(a.hp, 0, a.maxHP);
        a.mana = Mathf.Clamp(a.mana, 0, a.maxMana);
        a.rage = Mathf.Clamp(a.rage, 0, a.maxRage);
    }

    bool IsDead(Actor a) => a.hp <= 0;
    // ================== HÀM DÙNG THẺ (MỚI) ==================
    // (Các hàm này sẽ được gọi bởi BattleCardManager ở Bước 3)

    /// <summary>
    /// Được gọi bởi Nút UI của Thẻ Mana (từ CardData.cs)
    /// </summary>
    public void UseManaCard(int amount)
    {
        // Chỉ cho dùng khi đến lượt Player
        if (!isPlayerTurn) { OnCombatLog?.Invoke("Chưa tới lượt!"); return; }
        if (playerPet == null) return;

        var player = playerPet.GetActor();
        player.GainMana(amount);
        ClampActor(player);

        // Cập nhật HUD
        OnStatsChanged?.Invoke(player, enemyPet.GetActor());
        OnCombatLog?.Invoke($"Player dùng thẻ, +{amount} Mana");
    }

    /// <summary>
    /// Được gọi bởi Nút UI của Thẻ Rage (từ CardData.cs)
    /// </summary>
    public void UseRageCard(int amount)
    {
        if (!isPlayerTurn) { OnCombatLog?.Invoke("Chưa tới lượt!"); return; }
        if (playerPet == null) return;

        var player = playerPet.GetActor();
        player.GainRage(amount);
        ClampActor(player);

        // Cập nhật HUD
        OnStatsChanged?.Invoke(player, enemyPet.GetActor());
        OnCombatLog?.Invoke($"Player dùng thẻ, +{amount} Rage");
    }

    /// <summary>
    /// Được gọi bởi Nút UI của Thẻ Skill (từ CardData.cs)
    /// </summary>
    public void UseSkillCard(string skillID)
    {
        if (!isPlayerTurn) { OnCombatLog?.Invoke("Chưa tới lượt!"); return; }
        if (playerPet == null || enemyPet == null) return;

        var player = playerPet.GetActor();
        var enemy = enemyPet.GetActor();

        // --- LOGIC SKILL GIẢ LẬP (SAU NÀY SẼ THAY BẰNG DATABASE) ---
        int manaCost = 0;
        int damage = 0;

        if (skillID == "Punch") //
        {
            manaCost = 30; // Ví dụ: Skill "Punch" tốn 30 Mana
            damage = 75;   // Ví dụ: Skill "Punch" gây 75 Sát thương
        }
        else
        {
            OnCombatLog?.Invoke($"Không biết skill ID: {skillID}");
            return;
        }
        // --- KẾT THÚC GIẢ LẬP ---

        // 1. Kiểm tra Mana
        if (player.mana < manaCost)
        {
            OnCombatLog?.Invoke("Không đủ Mana!");
            return;
        }

        // 2. Trừ Mana
        player.DrainMana(manaCost);
        OnCombatLog?.Invoke($"Player dùng {skillID}, -{manaCost} Mana");

        // 3. Gây hiệu ứng (ví dụ: gây sát thương)
        enemy.TakeDamage(damage);
        OnCombatLog?.Invoke($"Gây {damage} sát thương lên {enemy.id}!");

        // 4. Cập nhật HUD
        ClampActor(player);
        ClampActor(enemy);
        OnStatsChanged?.Invoke(player, enemy);
    }
}
