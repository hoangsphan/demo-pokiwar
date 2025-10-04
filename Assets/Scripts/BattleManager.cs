using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý lượt Player ↔ AI, nhận batch match từ Match3Manager,
/// áp dụng hiệu ứng combat, và ĐỔI LƯỢT khi bàn cờ thật sự rảnh.
/// </summary>
public class BattleManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] public Match3Manager board;

    [Header("Actors")]
    public Actor player = new Actor { id = "Player" };
    public Actor enemy = new Actor { id = "Enemy" };

    [Header("Flow")]
    [Tooltip("Bật để có máy đánh cùng bạn")]
    public bool enableAI = true;

    [Tooltip("True = Player đi trước")]
    public bool isPlayerTurn = true;

    // ==== UI/Event hooks (tùy chọn, UIController sẽ lắng nghe) ====
    public System.Action<Actor, Actor> OnStatsChanged;
    public System.Action<bool> OnTurnChanged;      // true = Player turn
    public System.Action<string> OnCombatLog;

    // Nội bộ: chống gọi kết lượt chồng nhau khi cascade bắn nhiều event
    private bool _endingTurn = false;

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

        // Clamp chỉ số ban đầu (tránh vượt max/min)
        ClampActor(player);
        ClampActor(enemy);

        // Bật/tắt input theo người đi trước
        board.SetPlayerInputEnabled(isPlayerTurn);

        // Thông báo UI
        OnStatsChanged?.Invoke(player, enemy);
        OnTurnChanged?.Invoke(isPlayerTurn);

        // Nếu AI đi trước → cho AI chơi
        if (enableAI && !isPlayerTurn)
            StartCoroutine(CallAI());
    }

    /// <summary>
    /// Nhận một batch match (sau Destroy+Fill) từ board.
    /// Không đổi lượt ngay; thay vào đó đặt lịch đợi board rảnh rồi mới kết sổ lượt.
    /// </summary>
    void HandleMatchesResolved(List<List<Gem>> matches)
    {
        if (matches == null || matches.Count == 0) return;

        // Ai đang tấn công / ai đang phòng thủ
        var attacker = isPlayerTurn ? player : enemy;
        var defender = isPlayerTurn ? enemy : player;

        // Tính & áp hiệu ứng từ batch
        var effects = CombatSystem.ComputeEffects(matches);
        if (!effects.IsEmpty)
        {
            CombatSystem.ApplyEffects(effects, attacker, defender, OnCombatLog);

            // Đảm bảo không vượt ngưỡng
            ClampActor(player);
            ClampActor(enemy);

            OnStatsChanged?.Invoke(player, enemy);
        }

        // Luôn đợi board rảnh hoàn toàn (hết cascade/đổ) rồi mới đổi lượt
        StartCoroutine(WaitBoardAndEndTurn());
    }

    /// <summary>
    /// Chờ đến khi board báo rảnh (không swap, không processing) rồi đổi lượt.
    /// Chống gọi trùng bằng cờ _endingTurn.
    /// </summary>
    IEnumerator WaitBoardAndEndTurn()
    {
        if (_endingTurn) yield break;
        _endingTurn = true;

        // Đợi board IsGridReady = true
        while (board != null && !board.IsGridReady())
            yield return null;

        // Cho 1 nhịp nhỏ để animation settle
        yield return new WaitForSeconds(0.05f);

        // Kiểm tra điều kiện thắng/thua
        if (IsDead(player))
        {
            OnCombatLog?.Invoke("❌ Player defeated!");
            board.SetPlayerInputEnabled(false);
            _endingTurn = false;
            yield break;
        }
        if (IsDead(enemy))
        {
            OnCombatLog?.Invoke("✅ Enemy defeated!");
            board.SetPlayerInputEnabled(false);
            _endingTurn = false;
            yield break;
        }

        // Đổi lượt
        isPlayerTurn = !isPlayerTurn;
        OnTurnChanged?.Invoke(isPlayerTurn);

        // Bật/tắt input người chơi
        board.SetPlayerInputEnabled(isPlayerTurn);

        // Nếu tới lượt AI → cho AI đánh
        if (enableAI && !isPlayerTurn)
            yield return CallAI();

        _endingTurn = false;
    }

    /// <summary>
    /// Gọi AI thực hiện một nước đi: AI sẽ tự chọn nước hợp lệ và gọi board.DoSwap(...)
    /// YÊU CẦU: AIController có hàm public void PlayTurn()
    /// </summary>
    IEnumerator CallAI()
    {
        // Nhịp "nghĩ" cho tự nhiên
        yield return new WaitForSeconds(0.25f);

        var ai = FindFirstObjectByType<AIController>();
        if (ai == null)
        {
            Debug.LogWarning("[BattleManager] Không tìm thấy AIController trong scene.");
            yield break;
        }

        // Chỉ gọi PlayTurn (không cần MakeMove)
        ai.PlayTurn();
    }

    // ================= Helpers =================
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
}
