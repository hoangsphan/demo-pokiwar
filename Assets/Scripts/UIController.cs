using System.Collections;
using System.Data.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI Battle Manager - CHÍNH XÁC 100% với GemType
/// 
/// GemType enum { Red, Blue, Green, Yellow, Purple, Grey }
///              { 0    1     2      3       4       5     }
/// 
/// Effects (từ CombatSystem.cs):
/// - Red (0): rageGain → Attacker Rage ↑
/// - Blue (1): manaGain → Attacker Mana ↑
/// - Green (2): healSelf → Attacker HP ↑
/// - Yellow (3): damage → Defender HP ↓
/// - Purple (4): rageDrain → Defender Rage ↓, Attacker Rage ↑
/// - Grey (5): damage + healSelf (50%) → Defender HP ↓, Attacker HP ↑
/// </summary>
public class UIController : MonoBehaviour
{
    [Header("=== PLAYER UI (Bên TRÁI) ===")]
    public Slider playerHPSlider;
    public Slider playerManaSlider;
    public Slider playerRageSlider;
    public TextMeshProUGUI playerHPText;
    public TextMeshProUGUI playerManaText;
    public TextMeshProUGUI playerRageText;
    public TextMeshProUGUI playerNameText;

    [Header("=== ENEMY UI (Bên PHẢI) ===")]
    public Slider enemyHPSlider;
    public Slider enemyManaSlider;
    public Slider enemyRageSlider;
    public TextMeshProUGUI enemyHPText;
    public TextMeshProUGUI enemyManaText;
    public TextMeshProUGUI enemyRageText;
    public TextMeshProUGUI enemyNameText;

    [Header("=== TIMER UI (Chỉ Player) ===")]
    public Slider playerTimerSlider;
    public TextMeshProUGUI playerTimerText;

    [Header("=== TURN INDICATOR ===")]
    public GameObject turnIndicatorPanel;
    public TextMeshProUGUI turnIndicatorText;
    public Color playerTurnColor = new Color(0.3f, 0.8f, 0.3f);
    public Color enemyTurnColor = new Color(0.95f, 0.26f, 0.21f);
    public float turnIndicatorDisplayTime = 2f;

    [Header("=== TIMER COLORS ===")]
    public Color timerSafeColor = new Color(0.3f, 0.8f, 0.3f);
    public Color timerWarningColor = new Color(1f, 0.8f, 0f);
    public Color timerDangerColor = new Color(0.95f, 0.26f, 0.21f);

    [Header("=== ANIMATION ===")]
    public float sliderAnimSpeed = 8f;
    public bool enableTurnBlink = true;

    [Header("=== VISUAL EFFECTS ===")]
    public ParticleSystem healParticlePlayer;
    public ParticleSystem healParticleEnemy;
    public ParticleSystem damageParticlePlayer;
    public ParticleSystem damageParticleEnemy;
    public Image manaFlashPlayer;
    public Image manaFlashEnemy;
    public Image rageFlashPlayer;
    public Image rageFlashEnemy;

    // ===== INTERNAL =====
    private BattleManager battleManager;

    // Target values
    private float targetPlayerHP, targetPlayerMana, targetPlayerRage;
    private float targetEnemyHP, targetEnemyMana, targetEnemyRage;

    // Previous values (Dùng để tính diff cho hiệu ứng)
    private int prevPlayerHP, prevPlayerMana, prevPlayerRage;
    private int prevEnemyHP, prevEnemyMana, prevEnemyRage;

    private bool isTimerWarning = false;

    void Start()
    {
        battleManager = FindFirstObjectByType<BattleManager>();

        if (battleManager == null)
        {
            Debug.LogError("❌ [UIController] Không tìm thấy BattleManager!");
            enabled = false;
            return;
        }

        battleManager.OnStatsChanged += HandleStatsChanged;
        battleManager.OnTurnChanged += HandleTurnChanged;
        battleManager.OnTimerTick += HandleTimerTick;
        battleManager.OnTimerWarning += HandleTimerWarning;
        battleManager.OnTimeUp += HandleTimeUp;

        InitializeUI();

        Debug.Log("✅ [UIController] Initialized - GemType matched!");
    }

    void OnDestroy()
    {
        if (battleManager != null)
        {
            battleManager.OnStatsChanged -= HandleStatsChanged;
            battleManager.OnTurnChanged -= HandleTurnChanged;
            battleManager.OnTimerTick -= HandleTimerTick;
            battleManager.OnTimerWarning -= HandleTimerWarning;
            battleManager.OnTimeUp -= HandleTimeUp;
        }
    }

    void InitializeUI()
    {
        if (playerNameText)
            playerNameText.text = $"🛡️ {battleManager.player.id.ToUpper()}";
        if (enemyNameText)
            enemyNameText.text = $"⚔️ {battleManager.enemy.id.ToUpper()}";

        prevPlayerHP = battleManager.player.hp;
        prevPlayerMana = battleManager.player.mana;
        prevPlayerRage = battleManager.player.rage;
        prevEnemyHP = battleManager.enemy.hp;
        prevEnemyMana = battleManager.enemy.mana;
        prevEnemyRage = battleManager.enemy.rage;

        HandleStatsChanged(battleManager.player, battleManager.enemy);
        HandleTurnChanged(battleManager.isPlayerTurn);

        if (playerTimerSlider) playerTimerSlider.value = 1f;

        // Log gem mapping (Vẫn giữ lại để kiểm tra ban đầu)
        Debug.Log("=== GEM MAPPING ===");
        Debug.Log("🔴 Red (0): Rage gain");
        Debug.Log("🔵 Blue (1): Mana gain");
        Debug.Log("🟢 Green (2): Heal");
        Debug.Log("🟡 Yellow (3): Damage");
        Debug.Log("🟣 Purple (4): Rage drain");
        Debug.Log("⚪ Grey (5): Lifesteal");
    }

    // ========== STATS CHANGED ==========
    void HandleStatsChanged(Actor player, Actor enemy)
    {
        // Tính diff (chênh lệch) để kích hoạt hiệu ứng hình ảnh
        int playerHPDiff = player.hp - prevPlayerHP;
        int playerManaDiff = player.mana - prevPlayerMana;
        int playerRageDiff = player.rage - prevPlayerRage;

        int enemyHPDiff = enemy.hp - prevEnemyHP;
        int enemyManaDiff = enemy.mana - prevEnemyMana;
        int enemyRageDiff = enemy.rage - prevEnemyRage;

        // Phát hiện và trigger effects (Hàm này đã đúng)
        DetectEffects(playerHPDiff, playerManaDiff, playerRageDiff, true);
        DetectEffects(enemyHPDiff, enemyManaDiff, enemyRageDiff, false);

        // Cập nhật targets (Hàm này đã đúng)
        UpdateActorTargets(player, true);
        UpdateActorTargets(enemy, false);

        // Lưu lại chỉ số của lượt này để so sánh với lượt sau
        prevPlayerHP = player.hp;
        prevPlayerMana = player.mana;
        prevPlayerRage = player.rage;
        prevEnemyHP = enemy.hp;
        prevEnemyMana = enemy.mana;
        prevEnemyRage = enemy.rage;
    }

    // Hàm này chỉ kích hoạt hiệu ứng hình ảnh (Particle, Flash)
    // Logic này đã đúng và độc lập
    void DetectEffects(int hpDiff, int manaDiff, int rageDiff, bool isPlayer)
    {
        // HP Changes
        if (hpDiff < 0)
        {
            PlayDamageEffect(isPlayer);
            if (isPlayer && CameraShake.Instance != null)
                CameraShake.Instance.Shake(0.15f, 0.08f);
        }
        else if (hpDiff > 0)
        {
            PlayHealEffect(isPlayer);
        }

        // Mana Changes
        if (manaDiff > 0)
        {
            PlayManaGainEffect(isPlayer);
        }

        // Rage Changes
        if (rageDiff > 0)
        {
            PlayRageGainEffect(isPlayer);
        }
        else if (rageDiff < 0)
        {
            PlayRageDrainEffect(isPlayer);
        }
    }

    // Hàm này cập nhật giá trị mục tiêu cho Slider và Text
    // Logic này đã đúng
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

            UpdateActorText(actor, playerHPText, playerManaText, playerRageText);
            UpdateHealthBarColor(playerHPSlider, actor.hp, actor.maxHP);
        }
        else
        {
            targetEnemyHP = actor.hp;
            targetEnemyMana = actor.mana;
            targetEnemyRage = actor.rage;

            if (enemyHPSlider) enemyHPSlider.maxValue = actor.maxHP;
            if (enemyManaSlider) enemyManaSlider.maxValue = actor.maxMana;
            if (enemyRageSlider) enemyRageSlider.maxValue = actor.maxRage;

            UpdateActorText(actor, enemyHPText, enemyManaText, enemyRageText);
            UpdateHealthBarColor(enemyHPSlider, actor.hp, actor.maxHP);
        }
    }

    void UpdateActorText(Actor actor, TextMeshProUGUI hpText, TextMeshProUGUI manaText, TextMeshProUGUI rageText)
    {
        if (hpText) hpText.text = $"{actor.hp} / {actor.maxHP}";
        if (manaText) manaText.text = $"{actor.mana} / {actor.maxMana}";
        if (rageText) rageText.text = $"{actor.rage} / {actor.maxRage}";
    }

    void UpdateHealthBarColor(Slider hpSlider, int currentHP, int maxHP)
    {
        if (hpSlider == null) return;

        float hpPercent = (float)currentHP / maxHP;
        Image fillImage = hpSlider.fillRect.GetComponent<Image>();

        if (fillImage != null)
        {
            if (hpPercent > 0.5f)
                fillImage.color = new Color(0.3f, 0.8f, 0.3f);
            else if (hpPercent > 0.25f)
                fillImage.color = new Color(1f, 0.8f, 0f);
            else
                fillImage.color = new Color(0.95f, 0.26f, 0.21f);
        }
    }

    // ========== VISUAL EFFECTS (Không thay đổi) ==========

    void PlayDamageEffect(bool isPlayer)
    {
        ParticleSystem particle = isPlayer ? damageParticlePlayer : damageParticleEnemy;
        if (particle != null)
            particle.Play();
    }

    void PlayHealEffect(bool isPlayer)
    {
        ParticleSystem particle = isPlayer ? healParticlePlayer : healParticleEnemy;
        if (particle != null)
            particle.Emit(15);

        StartCoroutine(FlashEffect(
            isPlayer ? playerHPSlider : enemyHPSlider,
            new Color(0.3f, 1f, 0.3f),
            0.3f
        ));
    }

    void PlayManaGainEffect(bool isPlayer)
    {
        Image flashImage = isPlayer ? manaFlashPlayer : manaFlashEnemy;
        if (flashImage != null)
            StartCoroutine(FlashImage(flashImage, new Color(0.2f, 0.5f, 1f, 0.5f), 0.4f));

        StartCoroutine(FlashEffect(
            isPlayer ? playerManaSlider : enemyManaSlider,
            new Color(0.3f, 0.6f, 1f),
            0.3f
        ));
    }

    void PlayRageGainEffect(bool isPlayer)
    {
        Image flashImage = isPlayer ? rageFlashPlayer : rageFlashEnemy;
        if (flashImage != null)
            StartCoroutine(FlashImage(flashImage, new Color(1f, 0.3f, 0f, 0.5f), 0.4f));

        StartCoroutine(FlashEffect(
            isPlayer ? playerRageSlider : enemyRageSlider,
            new Color(1f, 0.4f, 0f),
            0.3f
        ));
    }

    void PlayRageDrainEffect(bool isPlayer)
    {
        StartCoroutine(FlashEffect(
            isPlayer ? playerRageSlider : enemyRageSlider,
            new Color(0.6f, 0.2f, 0.8f),
            0.3f
        ));
    }

    IEnumerator FlashEffect(Slider slider, Color flashColor, float duration)
    {
        if (slider == null) yield break;

        Image fillImage = slider.fillRect.GetComponent<Image>();
        if (fillImage == null) yield break;

        Color originalColor = fillImage.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.PingPong(elapsed * 4f, 1f);
            fillImage.color = Color.Lerp(originalColor, flashColor, t);
            yield return null;
        }

        fillImage.color = originalColor;
    }

    IEnumerator FlashImage(Image image, Color flashColor, float duration)
    {
        if (image == null) yield break;

        image.color = flashColor;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Color c = image.color;
            c.a = Mathf.Lerp(flashColor.a, 0f, elapsed / duration);
            image.color = c;
            yield return null;
        }

        Color final = image.color;
        final.a = 0f;
        image.color = final;
    }

    // ========== TURN CHANGED (Không thay đổi) ==========
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

        if (turnIndicatorPanel)
            turnIndicatorPanel.SetActive(true);

        if (enableTurnBlink)
            StartCoroutine(BlinkTurnIndicator());

        StartCoroutine(AutoHideTurnIndicator());
        isTimerWarning = false;
    }

    IEnumerator BlinkTurnIndicator()
    {
        if (!turnIndicatorText) yield break;

        Color originalColor = turnIndicatorText.color;

        for (int i = 0; i < 3; i++)
        {
            turnIndicatorText.color = new Color(
                originalColor.r, originalColor.g, originalColor.b, 0.3f
            );
            yield return new WaitForSeconds(0.15f);
            turnIndicatorText.color = originalColor;
            yield return new WaitForSeconds(0.15f);
        }
    }

    IEnumerator AutoHideTurnIndicator()
    {
        yield return new WaitForSeconds(turnIndicatorDisplayTime);

        if (turnIndicatorPanel)
        {
            CanvasGroup canvasGroup = turnIndicatorPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = turnIndicatorPanel.AddComponent<CanvasGroup>();

            float elapsed = 0f;
            float fadeDuration = 0.5f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = 1f - (elapsed / fadeDuration);
                yield return null;
            }

            turnIndicatorPanel.SetActive(false);
            canvasGroup.alpha = 1f;
        }
    }

    // ========== TIMER (Không thay đổi) ==========
    void HandleTimerTick(float currentTime, float maxTime)
    {
        if (!battleManager.isPlayerTurn)
        {
            if (playerTimerSlider) playerTimerSlider.gameObject.SetActive(false);
            if (playerTimerText) playerTimerText.gameObject.SetActive(false);
            return;
        }

        if (playerTimerSlider) playerTimerSlider.gameObject.SetActive(true);
        if (playerTimerText) playerTimerText.gameObject.SetActive(true);

        float timePercent = currentTime / maxTime;

        if (playerTimerSlider)
        {
            playerTimerSlider.value = timePercent;

            Image fillImage = playerTimerSlider.fillRect.GetComponent<Image>();
            if (fillImage)
            {
                if (timePercent > 0.5f)
                    fillImage.color = timerSafeColor;
                else if (timePercent > 0.3f)
                    fillImage.color = timerWarningColor;
                else
                    fillImage.color = timerDangerColor;
            }
        }

        if (playerTimerText)
        {
            int seconds = Mathf.CeilToInt(currentTime);
            playerTimerText.text = $"{seconds}s";

            if (timePercent <= 0.3f)
                playerTimerText.color = timerDangerColor;
            else
                playerTimerText.color = Color.white;
        }
    }

    void HandleTimerWarning()
    {
        if (isTimerWarning) return;
        isTimerWarning = true;
        StartCoroutine(ShowTimerWarningEffect());
    }

    IEnumerator ShowTimerWarningEffect()
    {
        if (playerTimerSlider)
        {
            Image fillImage = playerTimerSlider.fillRect.GetComponent<Image>();
            if (fillImage)
            {
                for (int i = 0; i < 3; i++)
                {
                    fillImage.color = Color.white;
                    yield return new WaitForSeconds(0.1f);
                    fillImage.color = timerDangerColor;
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }

        if (turnIndicatorPanel && !turnIndicatorPanel.activeSelf)
        {
            ShowSpecialMessage("⚠️ HURRY UP!", Color.yellow, 1.5f);
        }
    }

    void HandleTimeUp()
    {
        ShowSpecialMessage("⏰ TIME'S UP!", timerDangerColor, 2f);
    }

    // ========== UPDATE (Không thay đổi) ==========
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
        if (slider == null) return;
        slider.value = Mathf.Lerp(slider.value, targetValue, Time.deltaTime * sliderAnimSpeed);
    }

    // ========== UTILITIES (Không thay đổi) ==========
    public void ShowSpecialMessage(string message, Color color, float duration = 2f)
    {
        StartCoroutine(DisplayTemporaryMessage(message, color, duration));
    }

    IEnumerator DisplayTemporaryMessage(string message, Color color, float duration)
    {
        if (!turnIndicatorText || !turnIndicatorPanel) yield break;

        bool wasActive = turnIndicatorPanel.activeSelf;
        string oldText = turnIndicatorText.text;
        Color oldColor = turnIndicatorText.color;

        turnIndicatorPanel.SetActive(true);
        CanvasGroup canvasGroup = turnIndicatorPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = turnIndicatorPanel.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;

        turnIndicatorText.text = message;
        turnIndicatorText.color = color;

        Vector3 originalScale = turnIndicatorPanel.transform.localScale;
        float scaleTime = 0.3f;
        float elapsed = 0f;

        while (elapsed < scaleTime)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(1f, 1.2f, elapsed / scaleTime);
            turnIndicatorPanel.transform.localScale = originalScale * scale;
            yield return null;
        }

        yield return new WaitForSeconds(duration - scaleTime * 2);

        elapsed = 0f;
        while (elapsed < scaleTime)
        {
            elapsed += Time.deltaTime;
            float
                scale = Mathf.Lerp(1.2f, 1f, elapsed / scaleTime);
            turnIndicatorPanel.transform.localScale = originalScale * scale;
            yield return null;
        }

        turnIndicatorPanel.transform.localScale = originalScale;

        if (!wasActive)
            turnIndicatorPanel.SetActive(false);
        else
        {
            turnIndicatorText.text = oldText;
            turnIndicatorText.color = oldColor;
        }
    }
}
