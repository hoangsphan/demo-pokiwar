using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Match3Agent : Agent
{
    [Header("Tham chiếu")]
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private Match3Manager board;

    [Header("Anti-Loop Settings")]
    [SerializeField] private bool forceRandomExploration = true;
    [SerializeField][Range(0f, 1f)] private float explorationRate = 0.3f; // 30% random trong training

    // Giảm giới hạn retry xuống, vì giờ ta cấm hiệu quả hơn
    private int _retryCount = 0;
    private const int MAX_RETRIES = 10;

    // Track lịch sử nước đi
    private int _lastMoveChoice = -1;
    private List<int> _recentMoves = new List<int>(); // Lưu 10 nước gần nhất

    // DANH SÁCH CẤM (SẼ ĐƯỢC RESET MỖI LƯỢT)
    private HashSet<int> _bannedMovesThisTurn = new HashSet<int>();

    public override void Initialize()
    {
        if (battleManager == null) battleManager = FindFirstObjectByType<BattleManager>();
        if (board == null) board = FindFirstObjectByType<Match3Manager>();
    }

    public override void OnEpisodeBegin()
    {
        // Reset mọi thứ khi bắt đầu ván mới
        _retryCount = 0;
        _lastMoveChoice = -1;
        _recentMoves.Clear();
        _bannedMovesThisTurn.Clear();
        // Debug.Log("[AI] Episode Begin - All counters reset");
    }

    /// <summary>
    /// ACTION MASK (ĐÃ SỬA)
    /// Giờ sẽ cấm cả nước đi ngoài biên VÀ nước đi trong danh sách ban
    /// </summary>
    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        // --- BẮT ĐẦU SỬA ---

        // 1. Cấm các nước đi ra ngoài biên (Cố định)
        for (int i = 0; i < 56; i++) // Nước đi dọc
        {
            int y = i / 8;
            if (y == 7) // Hàng trên cùng không thể đổi lên
            {
                actionMask.SetActionEnabled(0, i, false);
            }
        }
        // (Nước đi ngang 56-111 luôn hợp lệ)

        // 2. Cấm các nước đi ĐÃ BỊ BAN trong lượt này (Để tránh lặp)
        // Đây là cơ chế chống crash: Chỉ cấm khi danh sách ban còn nhỏ
        if (_bannedMovesThisTurn.Count < (112 - 10)) // Trừ hao 10 nước
        {
            foreach (int bannedMoveIndex in _bannedMovesThisTurn)
            {
                actionMask.SetActionEnabled(0, bannedMoveIndex, false);
            }
        }

        // --- KẾT THÚC SỬA ---
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (board == null || battleManager == null ||
            battleManager.playerPet == null || battleManager.enemyPet == null)
        {
            // SỬA: Kích thước Observation là 70 (64 cờ + 6 stats)
            // Lịch sử 10 nước đi (Move History) không hiệu quả, ta bỏ đi
            sensor.AddObservation(new float[70]);
            return;
        }

        Actor player = battleManager.playerPet.GetActor();
        Actor enemy = battleManager.enemyPet.GetActor();

        // 1. Bàn cờ (64)
        for (int y = 0; y < board.gridHeight; y++)
        {
            for (int x = 0; x < board.gridWidth; x++)
            {
                Gem gem = board.GetGem(x, y);
                sensor.AddObservation(gem == null ? 0f : ((float)gem.gemType + 1f) / 10f);
            }
        }

        // 2. Player + Enemy (6)
        sensor.AddObservation((float)player.hp / player.maxHP);
        sensor.AddObservation((float)player.mana / player.maxMana);
        sensor.AddObservation((float)player.rage / player.maxRage);
        sensor.AddObservation((float)enemy.hp / enemy.maxHP);
        sensor.AddObservation((float)enemy.mana / enemy.maxMana);
        sensor.AddObservation((float)enemy.rage / enemy.maxRage);

        // (Đã xóa 10 observations của Move History)
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (board != null && !board.IsGridReady()) return;
        if (battleManager == null || !battleManager.isPlayerTurn) return;

        int moveChoice = actions.DiscreteActions[0];

        // ===== CƯỠNG CHẾ RANDOM TRONG TRAINING =====
        if (forceRandomExploration && Academy.Instance.IsCommunicatorOn)
        {
            if (Random.value < explorationRate)
            {
                List<int> availableMoves = new List<int>();
                for (int i = 0; i < 112; i++)
                {
                    // Lấy các nước đi CHƯA BỊ CẤM
                    if (!_bannedMovesThisTurn.Contains(i))
                    {
                        // Check valid move (không đi ra ngoài)
                        bool isValid = true;
                        if (i < 56 && i / 8 == 7) isValid = false;
                        if (isValid) availableMoves.Add(i);
                    }
                }

                if (availableMoves.Count > 0)
                {
                    moveChoice = availableMoves[Random.Range(0, availableMoves.Count)];
                }
            }
        }

        // ===== PHẠT NẶNG NƯỚC ĐI LẶP LẠI (GIỮ NGUYÊN) =====
        if (moveChoice == _lastMoveChoice)
        {
            float penalty = -0.2f;
            AddReward(penalty);
            // Debug.LogWarning($"[AI] Lặp nước {moveChoice}. Penalty: {penalty}");
        }
        else
        {
            AddReward(0.01f); // Thưởng nhẹ khi đổi nước
        }

        _lastMoveChoice = moveChoice;

        // ===== XỬ LÝ SWAP (GIỮ NGUYÊN) =====
        int x, y;
        Gem gem1 = null, gem2 = null;

        if (moveChoice < 56) // Dọc
        {
            x = moveChoice % 8;
            y = moveChoice / 8;
            gem1 = board.GetGem(x, y);
            gem2 = board.GetGem(x, y + 1);
        }
        else // Ngang
        {
            int horizMove = moveChoice - 56;
            x = horizMove % 7;
            y = horizMove / 7;
            gem1 = board.GetGem(x, y);
            gem2 = board.GetGem(x + 1, y);
        }

        if (gem1 != null && gem2 != null)
        {
            StartCoroutine(board.SwapGems(gem1, gem2));
        }
        else
        {
            // AI chọn nước đi vào ô trống
            AddReward(-0.1f);
            RecordInvalidSwap(moveChoice);
        }
    }

    public void RecordMatchEffects(CombatSystem.Effects fx)
    {
        // Reset counters khi thành công
        _retryCount = 0;
        _bannedMovesThisTurn.Clear();

        // Base reward
        AddReward(0.2f);

        // Damage reward
        if (fx.rawDamagePower > 0)
        {
            float dmgReward = Mathf.Clamp(fx.rawDamagePower / 80f, 0f, 1.5f);
            AddReward(dmgReward);
        }
        // ... (các reward khác giữ nguyên) ...
    }

    // HÀM MỚI: Gọi khi bắt đầu lượt AI
    public void OnTurnStart()
    {
        _bannedMovesThisTurn.Clear(); // Reset ban list mỗi lượt
        _retryCount = 0;
        _lastMoveChoice = -1;
        // Debug.Log("[AI] Turn Start - Banned list cleared");
    }

    // OVERLOAD 1: Gọi từ BattleManager (không có tham số)
    public void RecordInvalidSwap()
    {
        RecordInvalidSwap(_lastMoveChoice);
    }

    // OVERLOAD 2: Gọi với tham số cụ thể
    public void RecordInvalidSwap(int failedMove)
    {
        AddReward(-0.15f);
        _retryCount++;

        // ===== CẤM NƯỚC ĐI NÀY TRONG LƯỢT NÀY =====
        if (failedMove >= 0 && failedMove < 112)
        {
            _bannedMovesThisTurn.Add(failedMove);
        }

        // Debug.LogWarning($"[AI] Invalid swap {failedMove}. Banned. Retry: {_retryCount}/{MAX_RETRIES}");

        if (_retryCount >= MAX_RETRIES)
        {
            // Debug.LogError($"[AI] Quá {MAX_RETRIES} lần thử. BỎ LƯỢT + PENALTY NẶNG.");
            AddReward(-1.0f); // PENALTY CỰC NẶNG

            // Tự động reset và kết thúc lượt
            _retryCount = 0;
            _bannedMovesThisTurn.Clear();

            if (battleManager != null)
            {
                battleManager.StartCoroutine(battleManager.ForceEndTurn());
            }
            return;
        }

        StartCoroutine(WaitAndRequest());
    }

    System.Collections.IEnumerator WaitAndRequest()
    {
        yield return new WaitForSeconds(0.1f); // Giảm độ trễ xuống 0.1s

        if (board != null && board.IsGridReady())
        {
            RequestDecision();
        }
        else
        {
            StartCoroutine(WaitAndRequest());
        }
    }

    public void RecordGameEnd(bool aiWon)
    {
        if (aiWon)
        {
            SetReward(5.0f); // THƯỞNG CỰC LỚN
        }
        else
        {
            SetReward(-2.0f); // PHẠT NẶNG
        }

        _retryCount = 0;
        _lastMoveChoice = -1;
        _recentMoves.Clear();
        _bannedMovesThisTurn.Clear();

        EndEpisode();
    }
}