using UnityEngine;
using System.Collections.Generic;
using static Gem; // Gem.GemType

public static class CombatSystem
{
    // Hiệu ứng gộp của 1 lượt
    public struct Effects
    {
        public int rawDamagePower; // Sửa từ "damage"
        public int healSelf;
        public int manaGain;
        public int rageGain;
        public int rageDrain;

        public bool IsEmpty =>
            rawDamagePower == 0 && healSelf == 0 && manaGain == 0 && rageGain == 0 && rageDrain == 0;
    }

    public static int GetAIScoreValue(GemType type)
    {
        switch (type)
        {
            case GemType.Yellow: return 12; // AI vẫn thấy Yellow 12 điểm
            case GemType.Grey: return 8;   // AI thấy Grey 8 điểm
            case GemType.Green: return 7;
            case GemType.Blue: return 7;
            case GemType.Red: return 7;
            case GemType.Purple: return 6;
            default: return 0;
        }
    }

    // Tổng hợp hiệu ứng từ các match của cả lượt (kể cả cascade)
    public static Effects ComputeEffects(List<List<Gem>> matches, Actor attacker)
    {
        Effects fx = default;
        if (matches == null || matches.Count == 0) return fx;

        // Giá trị cơ bản cho các gem không phải sát thương
        // (Bạn có thể đổi các số 10, 7, 8 này nếu muốn)
        const int BASE_MANA_GAIN = 10;
        const int BASE_RAGE_GAIN = 10;
        const int BASE_HEAL_GAIN = 7;
        const int BASE_RAGE_DRAIN = 8;

        foreach (var group in matches)
        {
            if (group == null || group.Count == 0) continue;

            GemType type = group[0].gemType;
            int multiplier = 1 + (group.Count - 3); // 3=x1, 4=x2, 5=x3...

            // THAY ĐỔI LOGIC TÍNH TOÁN
            switch (type)
            {
                case GemType.Yellow:
                    // Sát thương = Attack của Pet * Hệ số match
                    fx.rawDamagePower += attacker.attack * multiplier;
                    break;
                case GemType.Grey:
                    // Sát thương hút máu = 75% Attack * Hệ số match
                    int greyPower = Mathf.RoundToInt(attacker.attack * 0.75f * multiplier);
                    fx.rawDamagePower += greyPower;
                    fx.healSelf += Mathf.RoundToInt(greyPower * 0.5f); // Hút 50%
                    break;
                case GemType.Green:
                    // Hồi máu (vẫn giữ cố định, hoặc bạn có thể đổi thành attacker.attack nếu muốn)
                    fx.healSelf += BASE_HEAL_GAIN * multiplier;
                    break;
                case GemType.Blue:
                    fx.manaGain += BASE_MANA_GAIN * multiplier;
                    break;
                case GemType.Red:
                    fx.rageGain += BASE_RAGE_GAIN * multiplier;
                    break;
                case GemType.Purple:
                    fx.rageDrain += BASE_RAGE_DRAIN * multiplier;
                    break;
            }
        }

        return fx;
    }

    // Áp hiệu ứng 1 lần khi kết thúc lượt 
    // 5. SỬA HÀM "ApplyEffects"
    // Thêm logic tính sát thương dựa trên Defense
    public static void ApplyEffects(Effects fx, Actor attacker, Actor defender, System.Action<string> log = null)
    {
        // THAY ĐỔI LOGIC SÁT THƯƠNG
        if (fx.rawDamagePower > 0)
        {
            // Sát thương = Sức mạnh thô - Phòng thủ của đối phương
            int finalDamage = fx.rawDamagePower - defender.defense;

            // Đảm bảo sát thương luôn ít nhất là 1 (hoặc 0 nếu bạn muốn)
            finalDamage = Mathf.Max(1, finalDamage);

            defender.TakeDamage(finalDamage);
            log?.Invoke($"{attacker.id} gây {finalDamage} sát thương");
        }

        // Phần còn lại giữ nguyên
        if (fx.healSelf > 0)
        {
            attacker.Heal(fx.healSelf);
            log?.Invoke($"{attacker.id} hồi {fx.healSelf} HP");
        }
        // ... (Mana, Rage, RageDrain giữ nguyên) ...
        if (fx.manaGain > 0)
        {
            attacker.GainMana(fx.manaGain);
            log?.Invoke($"{attacker.id} +{fx.manaGain} Mana");
        }

        if (fx.rageGain > 0)
        {
            attacker.GainRage(fx.rageGain);
            log?.Invoke($"{attacker.id} +{fx.rageGain} Rage");
        }

        if (fx.rageDrain > 0)
        {
            int drained = defender.DrainRage(fx.rageDrain);
            if (drained > 0)
            {
                attacker.GainRage(drained);
                log?.Invoke($"{attacker.id} hút {drained} Rage");
            }
        }

        attacker.Clamp();
        defender.Clamp();
    }
}
