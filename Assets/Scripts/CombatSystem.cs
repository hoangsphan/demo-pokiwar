using UnityEngine;
using System.Collections.Generic;
using static Gem; // Gem.GemType

public static class CombatSystem
{
    // Hiệu ứng gộp của 1 lượt
    public struct Effects
    {
        public int damage;    // gây lên đối thủ (Yellow, Grey)
        public int healSelf;  // hồi cho bản thân (Green + lifesteal từ Grey)
        public int manaGain;  // Blue 
        public int rageGain;  // Red
        public int rageDrain; // Purple: hút Rage đối thủ

        public bool IsEmpty =>
            damage == 0 && healSelf == 0 && manaGain == 0 && rageGain == 0 && rageDrain == 0;
    }

    // Cho AI chấm điểm
    public static int GetBaseEffectFor(GemType type)
    {
        switch (type)
        {
            case GemType.Red: return 7; // Rage gain
            case GemType.Blue: return 7;  // Mana gain
            case GemType.Green: return 7; // Heal
            case GemType.Grey: return 6;  // Lifesteal (damage + heal 50%)
            case GemType.Yellow: return 12; // Damage
            case GemType.Purple: return 6;  // Rage drain
            default: return 0;
        }
    }

    // Tổng hợp hiệu ứng từ các match của cả lượt (kể cả cascade)
    public static Effects ComputeEffects(List<List<Gem>> matches)
    {
        Effects fx = default;
        if (matches == null || matches.Count == 0) return fx;

        foreach (var group in matches)
        {
            if (group == null || group.Count == 0) continue;

            GemType type = group[0].gemType;
            int multiplier = 1 + (group.Count - 3); // 3=x1, 4=x2, 5=x3...
            int val = Mathf.Max(0, GetBaseEffectFor(type) * Mathf.Max(1, multiplier));

            switch (type)
            {
                case GemType.Yellow:
                    fx.damage += val;
                    break;
                case GemType.Grey:
                    fx.damage += val;
                    fx.healSelf += Mathf.RoundToInt(val * 0.5f);
                    break;
                case GemType.Green:
                    fx.healSelf += val;
                    break;
                case GemType.Blue:
                    fx.manaGain += val;
                    break;
                case GemType.Red:
                    fx.rageGain += val;
                    break;
                case GemType.Purple:
                    fx.rageDrain += val;
                    break;
            }
        }

        return fx;
    }

    // Áp hiệu ứng 1 lần khi kết thúc lượt 
    public static void ApplyEffects(Effects fx, Actor attacker, Actor defender, System.Action<string> log = null)
    {
        if (fx.damage > 0)
        {
            defender.TakeDamage(fx.damage);
            log?.Invoke($"{attacker.id} gây {fx.damage} sát thương");
        }

        if (fx.healSelf > 0)
        {
            attacker.Heal(fx.healSelf);
            log?.Invoke($"{attacker.id} hồi {fx.healSelf} HP");
        }

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
