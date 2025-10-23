using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý UI hiển thị HP, Mana, Rage cho Player (trái) và Enemy (phải)
/// Đồng bộ với BattleManager qua event
/// </summary>
public class UIController : MonoBehaviour
{
    [Header("=== PLAYER UI (Bên TRÁI) ===")]
    public Slider playerHPSlider;
    public Slider playerManaSlider;
    public Slider playerRageSlider;

    public TMP_Text playerHPText;
    public TMP_Text playerManaText;
    public TMP_Text playerRageText;
    public TMP_Text playerNameText;

    [Header("=== ENEMY UI (Bên PHẢI) ===")]
    public Slider enemyHPSlider;
    public Slider enemyManaSlider;
    public Slider enemyRageSlider;

    public TMP_Text enemyHPText;
    public TMP_Text enemyManaText;
    public TMP_Text enemyRageText;
    public TMP_Text enemyNameText;

    [Header("=== TURN INDICATOR ===")]
    public TMP_Text turnIndicatorText;
    public Color playerTurnColor = new Color(0.3f, 0.8f, 0.3f);
    public Color enemyTurnColor = new Color(0.95f, 0.26f, 0.21f);

    [Header("=== COMBAT LOG ===")]
    public TMP_Text combatLogText;
    public int maxLogLines = 12;

    [Header("=== ANIMATION SETTINGS ===")]
    public float sliderAnimSpeed = 8f;
    public bool enableTurnBlink = true;

    // ===== INTERNAL =====
    private BattleManager battleManager;
    private Queue<string> logQueue = new Queue<string>();

    // Target values cho animation mượt
    private float targetPlayerHP, targetPlayerMana, targetPlayerRage;
    private float targetEnemyHP, targetEnemyMana, targetEnemyRage;

    void Start()
    {
        battleManager = FindFirstObjectByType<BattleManager>();
        if (battleManager == null)
        {
            Debug.LogError("❌ [UIController] Không tìm thấy BattleManager trong scene!");
            enabled = false;
            return;
        }

        battleManager.OnStatsChanged += HandleStatsChanged;
        battleManager.OnTurnChanged += HandleTurnChanged;
        battleManager.OnCombatLog += HandleCombatLog;

        InitializeUI();
        Debug.Log("✅ [UIController] Đã kết nối với BattleManager và sẵn sàng!");
    }

    void OnDestroy()
    {
        if (battleManager != null)
        {
            battleManager.OnStatsChanged -= HandleStatsChanged;
            battleManager.OnTurnChanged -= HandleTurnChanged;
            battleManager.OnCombatLog -= HandleCombatLog;
        }
    }

    /// <summary>Khởi tạo UI lần đầu</summary>
    void InitializeUI()
    {
        if (playerNameText) playerNameText.text = $"🛡️ {battleManager.player.id.ToUpper()}";
        if (enemyNameText) enemyNameText.text = $"⚔️ {battleManager.enemy.id.ToUpper()}";

        // Set maxValue + value để không bị giật khung đầu tiên
        SetupSlidersForActor(battleManager.player, playerHPSlider, playerManaSlider, playerRageSlider);
        SetupSlidersForActor(battleManager.enemy, enemyHPSlider, enemyManaSlider, enemyRageSlider);

        // Cập nhật text số liệu ban đầu
        UpdateActorText(battleManager.player, playerHPText, playerManaText, playerRageText);
        UpdateActorText(battleManager.enemy, enemyHPText, enemyManaText, enemyRageText);

        // Set target cho animation
        targetPlayerHP = battleManager.player.hp;
        targetPlayerMana = battleManager.player.mana;
        targetPlayerRage = battleManager.player.rage;

        targetEnemyHP = battleManager.enemy.hp;
        targetEnemyMana = battleManager.enemy.mana;
        targetEnemyRage = battleManager.enemy.rage;

        HandleTurnChanged(battleManager.isPlayerTurn);
        AddLogMessage("⚔️ Battle Started!");
    }

    /// <summary>Event: stats thay đổi</summary>
    void HandleStatsChanged(Actor player, Actor enemy)
    {
        UpdateActorTargets(player, true);
        UpdateActorTargets(enemy, false);

        // Cập nhật text ngay khi thay đổi (không cần chờ animation slider)
        UpdateActorText(player, playerHPText, playerManaText, playerRageText);
        UpdateActorText(enemy, enemyHPText, enemyManaText, enemyRageText);
    }

    /// <summary>Cập nhật target + maxValue cho sliders</summary>
    void UpdateActorTargets(Actor actor, bool isPlayer)
    {
        if (isPlayer)
        {
            targetPlayerHP = actor.hp;
            targetPlayerMana = actor.mana;
            targetPlayerRage = actor.rage;

            if (playerHPSlider) playerHPSlider.maxValue = actor.maxHP;
            if (playerManaSlider) playerManaSlider.maxValue = actor.maxMana;
            if (playerRageSlider) playerRageSlider.maxValue = actor.maxRage;
        }
        else
        {
            targetEnemyHP = actor.hp;
            targetEnemyMana = actor.mana;
            targetEnemyRage = actor.rage;

            if (enemyHPSlider) enemyHPSlider.maxValue = actor.maxHP;
            if (enemyManaSlider) enemyManaSlider.maxValue = actor.maxMana;
            if (enemyRageSlider) enemyRageSlider.maxValue = actor.maxRage;
        }
    }

    /// <summary>Cập nhật text hiển thị số cho HP/Mana/Rage (TMP)</summary>
    void UpdateActorText(Actor actor, TMP_Text hpText, TMP_Text manaText, TMP_Text rageText)
    {
        if (hpText) hpText.text = $"{actor.hp} / {actor.maxHP}";
        if (manaText) manaText.text = $"{actor.mana} / {actor.maxMana}";
        if (rageText) rageText.text = $"{actor.rage} / {actor.maxRage}";
    }

    /// <summary>Gán maxValue & value cho 3 slider của 1 actor</summary>
    void SetupSlidersForActor(Actor actor, Slider hp, Slider mana, Slider rage)
    {
        if (hp)
        {
            hp.maxValue = actor.maxHP;
            hp.value = actor.hp;
        }
        if (mana)
        {
            mana.maxValue = actor.maxMana;
            mana.value = actor.mana;
        }
        if (rage)
        {
            rage.maxValue = actor.maxRage;
            rage.value = actor.rage;
        }
    }

    /// <summary>Event: đổi lượt</summary>
    void HandleTurnChanged(bool isPlayerTurn)
    {
        if (!turnIndicatorText) return;

        if (isPlayerTurn)
        {
            turnIndicatorText.text = "⚔️ YOUR TURN!";
            turnIndicatorText.color = playerTurnColor;
        }
        else
        {
            turnIndicatorText.text = "🛡️ ENEMY TURN";
            turnIndicatorText.color = enemyTurnColor;
        }

        if (enableTurnBlink) StartCoroutine(BlinkTurnIndicator());
    }

    IEnumerator BlinkTurnIndicator()
    {
        if (!turnIndicatorText) yield break;

        Color originalColor = turnIndicatorText.color;
        for (int i = 0; i < 3; i++)
        {
            turnIndicatorText.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.3f);
            yield return new WaitForSeconds(0.15f);
            turnIndicatorText.color = originalColor;
            yield return new WaitForSeconds(0.15f);
        }
    }

    void HandleCombatLog(string message) => AddLogMessage(message);

    void AddLogMessage(string message)
    {
        if (!combatLogText) return;

        string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
        string line = $"<color=#888888>[{timestamp}]</color> {message}";
        logQueue.Enqueue(line);

        while (logQueue.Count > maxLogLines) logQueue.Dequeue();
        combatLogText.text = string.Join("\n", logQueue);
    }

    void Update()
    {
        if (battleManager == null) return;

        AnimateSlider(playerHPSlider, targetPlayerHP);
        AnimateSlider(playerManaSlider, targetPlayerMana);
        AnimateSlider(playerRageSlider, targetPlayerRage);

        AnimateSlider(enemyHPSlider, targetEnemyHP);
        AnimateSlider(enemyManaSlider, targetEnemyMana);
        AnimateSlider(enemyRageSlider, targetEnemyRage);
    }

    void AnimateSlider(Slider slider, float targetValue)
    {
        if (!slider) return;
        slider.value = Mathf.Lerp(slider.value, targetValue, Time.deltaTime * sliderAnimSpeed);
    }

    // ===== PUBLIC =====
    public void ShowSpecialMessage(string message, Color color, float duration = 2f)
    {
        StartCoroutine(DisplayTemporaryMessage(message, color, duration));
    }

    IEnumerator DisplayTemporaryMessage(string message, Color color, float duration)
    {
        if (!turnIndicatorText) yield break;

        string oldText = turnIndicatorText.text;
        Color oldColor = turnIndicatorText.color;

        turnIndicatorText.text = message;
        turnIndicatorText.color = color;

        yield return new WaitForSeconds(duration);

        turnIndicatorText.text = oldText;
        turnIndicatorText.color = oldColor;
    }

    public void ClearCombatLog()
    {
        logQueue.Clear();
        if (combatLogText) combatLogText.text = "";
    }
}
