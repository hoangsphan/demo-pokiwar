using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Gem;

public class BattleManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] public Match3Manager board;
    [SerializeField] private UIController uiController;

    [Header("Actors")]
    [Tooltip("Sẽ được gán tự động bởi PokemonDisplayManager")]
    public PetData playerPet;
    [Tooltip("Sẽ được gán tự động bởi PokemonDisplayManager")]
    public PetData enemyPet;
    [Header("Rewards")]
    public int expReward = 50;
    public int currencyReward = 20;
    [Header("Flow")]
    public bool enableAI = true;
    public bool isPlayerTurn = true;

    [Header("Animation")]
    public GameObject[] projectilePrefabs;
    public Transform playerTarget; // Sẽ được gán tự động
    public Transform enemyTarget; // Sẽ được gán tự động
    public float projectileFlyDuration = 0.5f;

    [Header("Timer Settings")]
    [Tooltip("Thời gian cho mỗi lượt (giây). 0 = không giới hạn")]
    public float turnTimeLimit = 30f;
    [Tooltip("Bật/tắt timer")]
    public bool enableTimer = true;
    [Tooltip("Thời gian cảnh báo còn ít (giây)")]
    public float warningTime = 10f;

    // ==== Timer Runtime ====
    private float currentTurnTime;
    private bool isTimerRunning = false;
    // Events
    public System.Action<string> OnCombatLog;
    public System.Action<Actor, Actor> OnStatsChanged;
    public System.Action<bool> OnTurnChanged;
    public System.Action<float, float> OnTimerTick; // (currentTime, maxTime)
    public System.Action OnTimerWarning;
    public System.Action OnTimeUp;

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
        if (uiController == null) uiController = FindFirstObjectByType<UIController>();
        StartCoroutine(WaitForPetsAndStart());
        StartTurnTimer();
    }

    void Update()
    {
        if (isTimerRunning && enableTimer && turnTimeLimit > 0)
        {
            currentTurnTime -= Time.deltaTime;
            OnTimerTick?.Invoke(currentTurnTime, turnTimeLimit);

            if (currentTurnTime <= warningTime && currentTurnTime > warningTime - 0.1f)
            {
                OnTimerWarning?.Invoke();
                OnCombatLog?.Invoke(" Warning: Time running out!");
            }

            if (currentTurnTime <= 0f)
            {
                OnTimeUp?.Invoke();
                OnCombatLog?.Invoke($" {(isPlayerTurn ? "Player" : "Enemy")} ran out of time!");
                StopTurnTimer();
                StartCoroutine(ForceEndTurn());
            }
        }
    }

    void StartTurnTimer()
    {
        if (!enableTimer || turnTimeLimit <= 0) return;
        currentTurnTime = turnTimeLimit;
        isTimerRunning = true;
    }

    void StopTurnTimer()
    {
        isTimerRunning = false;
    }

    IEnumerator WaitForPetsAndStart()
    {
        while (playerPet == null || enemyPet == null)
        {
            Debug.LogWarning("[BattleManager] Đang chờ PokemonDisplayManager spawn pet...");
            yield return new WaitForSeconds(0.1f);
        }

        InitializeActor(playerPet.GetActor());
        InitializeActor(enemyPet.GetActor());

        board.SetPlayerInputEnabled(isPlayerTurn);
        OnStatsChanged?.Invoke(playerPet.GetActor(), enemyPet.GetActor());
        OnTurnChanged?.Invoke(isPlayerTurn);

        if (enableAI && !isPlayerTurn)
            StartCoroutine(CallAI());
    }

    void HandleMatchesResolved(List<List<Gem>> matches)
    {
        StopTurnTimer();
        if (matches == null || matches.Count == 0) return;
        _currentTurnMatches.AddRange(matches);
        StartCoroutine(WaitBoardAndEndTurn());
    }

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

        if (IsDead(playerPet.GetActor()))
        {
            Debug.Log("PLAYER DIED");
            //OnCombatLog?.Invoke("❌ Bạn đã thua!");
            if (uiController) uiController.ShowEndgamePopup(false, "DEFEATED!");
            StopTurnTimer();
            // (NÊN HIỆN NÚT "VỀ LOBBY" TẠI ĐÂY)
            // if (backToLobbyButton != null) backToLobbyButton.SetActive(true);
            yield break;
        }

        // === SỬA KHỐI LỆNH KHI THẮNG ===
        if (IsDead(enemyPet.GetActor()))
        {
            Debug.Log("ENEMY DIED");
            OnCombatLog?.Invoke($"✅ Bạn thắng! +{expReward} EXP, +{currencyReward} Vàng");

            // 1. Kiểm tra DatabaseManager
            if (DatabaseManager.Instance != null && DatabaseManager.Instance.playerData != null)
            {
                var playerData = DatabaseManager.Instance.playerData;

                // 2. Cộng thưởng (vào biến local)
                playerData.AddExp(expReward);
                playerData.currency += currencyReward;

                // 3. Ghi nhận đã hạ gục
                string defeatedEnemyID = enemyPet.GetActor().id;
                playerData.AddDefeatedEnemy(defeatedEnemyID);

                // 4. SỬA: Lưu data và CHỜ cho đến khi lưu xong
                Debug.Log("Bắt đầu lưu tiến trình (chờ)...");
                yield return StartCoroutine(DatabaseManager.Instance.SaveDataAndWait());
                Debug.Log("...Lưu tiến trình hoàn tất.");
            }
            else
            {
                Debug.LogError("[BattleManager] Không tìm thấy DatabaseManager để lưu thưởng!");
            }

            // 5. CHỈ HIỂN THỊ POPUP SAU KHI ĐÃ LƯU XONG
            if (uiController) uiController.ShowEndgamePopup(true, "VICTORY!");

            StopTurnTimer();
            yield break;
        }
        // === KẾT THÚC SỬA ===


        isPlayerTurn = !isPlayerTurn;
        OnTurnChanged?.Invoke(isPlayerTurn);
        board.SetPlayerInputEnabled(isPlayerTurn);
        StartTurnTimer();

        if (enableAI && !isPlayerTurn)
            yield return CallAI();

        _endingTurn = false;
    }

    IEnumerator AnimateCombatEffects(List<List<Gem>> allMatches)
    {
        var attacker = isPlayerTurn ? playerPet.GetActor() : enemyPet.GetActor();
        var defender = isPlayerTurn ? enemyPet.GetActor() : playerPet.GetActor();

        var attackerTransform = isPlayerTurn ? playerTarget : enemyTarget;
        var defenderTransform = isPlayerTurn ? enemyTarget : playerTarget;

        if (playerTarget == null || enemyTarget == null)
        {
            Debug.LogError("[BattleManager] Chưa gán PlayerTarget hoặc EnemyTarget!");
            _currentTurnMatches.Clear();
            yield break;
        }

        foreach (var group in allMatches)
        {
            if (group == null || group.Count == 0) continue;

            var fx = CombatSystem.ComputeEffects(new List<List<Gem>> { group }, attacker);
            if (fx.IsEmpty) continue;

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
                if (fx.rawDamagePower > 0) endPoint = defenderTransform;
                else endPoint = attackerTransform;

                yield return StartCoroutine(SpawnAndFlyProjectile(
                    prefabToSpawn,
                    startPoint.position,
                    endPoint.position,
                    projectileFlyDuration
                ));
            }

            OnCombatLog?.Invoke($"🎬 Xử lý match {type} ({group.Count} viên)");
            CombatSystem.ApplyEffects(fx, attacker, defender, OnCombatLog);

            ClampActor(playerPet.GetActor());
            ClampActor(enemyPet.GetActor());
            OnStatsChanged?.Invoke(playerPet.GetActor(), enemyPet.GetActor());

            yield return new WaitForSeconds(0.15f);
        }
        yield return new WaitForSeconds(0.3f);
    }

    void InitializeActor(Actor a)
    {
        if (a == null) return;
        a.hp = a.maxHP;
        a.mana = 0;
        a.rage = 0;
        ClampActor(a);
    }
    IEnumerator SpawnAndFlyProjectile(GameObject prefab, Vector3 startPos, Vector3 endPos, float duration)
    {
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
        yield return new WaitForSeconds(0.25f);
        var ai = FindFirstObjectByType<AIController>();
        if (ai == null)
        {
            Debug.LogWarning("[BattleManager] Không tìm thấy AIController trong scene.");
            yield break;
        }
        ai.PlayTurn();
    }

    IEnumerator ForceEndTurn()
    {
        if (_endingTurn) yield break;
        _endingTurn = true;

        while (board != null && !board.IsGridReady())
            yield return null;

        yield return new WaitForSeconds(0.05f);

        if (IsDead(playerPet.GetActor()))
        {
            OnCombatLog?.Invoke("❌ Player defeated!");
            board.SetPlayerInputEnabled(false);
            _endingTurn = false;
            // (NÊN HIỆN NÚT "VỀ LOBBY" TẠI ĐÂY)
            // if (backToLobbyButton != null) backToLobbyButton.SetActive(true);
            yield break;
        }
        if (IsDead(enemyPet.GetActor()))
        {
            OnCombatLog?.Invoke("✅ Enemy defeated!");
            board.SetPlayerInputEnabled(false);
            _endingTurn = false;
            // (Không lưu thưởng khi hết giờ, chỉ khi thắng)
            // (NÊN HIỆN NÚT "VỀ LOBBY" TẠI ĐÂY)
            // if (backToLobbyButton != null) backToLobbyButton.SetActive(true);
            yield break;
        }

        isPlayerTurn = !isPlayerTurn;
        OnTurnChanged?.Invoke(isPlayerTurn);
        board.SetPlayerInputEnabled(isPlayerTurn);
        StartTurnTimer();

        if (enableAI && !isPlayerTurn)
            yield return CallAI();

        _endingTurn = false;
    }

    void ClampActor(Actor a)
    {
        a.maxHP = Mathf.Max(1, a.maxHP);
        a.maxMana = Mathf.Max(0, a.maxMana);
        a.maxRage = Mathf.Max(0, a.maxRage);
        a.hp = Mathf.Clamp(a.hp, 0, a.maxHP);
        a.mana = Mathf.Clamp(a.mana, 0, a.maxMana);
        a.rage = Mathf.Clamp(a.rage, 0, a.maxRage);
    }

    bool IsDead(Actor a) => a.hp <= 0;

    // ================== HÀM DÙNG THẺ (GIỮ NGUYÊN) ==================
    public void UseManaCard(int amount)
    {
        if (!isPlayerTurn) { OnCombatLog?.Invoke("Chưa tới lượt!"); return; }
        if (playerPet == null) return;

        var player = playerPet.GetActor();
        player.GainMana(amount);
        ClampActor(player);
        OnStatsChanged?.Invoke(player, enemyPet.GetActor());
        OnCombatLog?.Invoke($"Player dùng thẻ, +{amount} Mana");
    }

    public void UseRageCard(int amount)
    {
        if (!isPlayerTurn) { OnCombatLog?.Invoke("Chưa tới lượt!"); return; }
        if (playerPet == null) return;

        var player = playerPet.GetActor();
        player.GainRage(amount);
        ClampActor(player);
        OnStatsChanged?.Invoke(player, enemyPet.GetActor());
        OnCombatLog?.Invoke($"Player dùng thẻ, +{amount} Rage");
    }

    public void UseSkillCard(string skillID)
    {
        if (!isPlayerTurn) { OnCombatLog?.Invoke("Chưa tới lượt!"); return; }
        if (playerPet == null || enemyPet == null) return;

        var player = playerPet.GetActor();
        var enemy = enemyPet.GetActor();

        int manaCost = 0;
        int damage = 0;

        if (skillID == "Punch")
        {
            manaCost = 40;
            damage = 30;
        }
        else
        {
            OnCombatLog?.Invoke($"Không biết skill ID: {skillID}");
            return;
        }

        if (player.mana < manaCost)
        {
            OnCombatLog?.Invoke("Không đủ Mana!");
            return;
        }

        player.DrainMana(manaCost);
        OnCombatLog?.Invoke($"Player dùng {skillID}, -{manaCost} Mana");
        enemy.TakeDamage(damage);
        OnCombatLog?.Invoke($"Gây {damage} sát thương lên {enemy.id}!");

        ClampActor(player);
        ClampActor(enemy);
        OnStatsChanged?.Invoke(player, enemy);
    }

    public float GetRemainingTime() => currentTurnTime;
    public float GetTimePercent() => turnTimeLimit > 0 ? currentTurnTime / turnTimeLimit : 1f;
}