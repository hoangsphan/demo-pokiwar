using UnityEngine;

[System.Serializable]
public class Actor
{
    public string id = "Actor";

    public int maxHP = 100, hp = 100;
    public int maxMana = 100, mana = 0;
    public int maxRage = 100, rage = 0;

    public void Clamp()
    {
        hp = Mathf.Clamp(hp, 0, maxHP);
        mana = Mathf.Clamp(mana, 0, maxMana);
        rage = Mathf.Clamp(rage, 0, maxRage);
    }

    public void TakeDamage(int dmg)
    {
        hp = Mathf.Max(0, hp - Mathf.Max(0, dmg));
    }

    public void Heal(int amount)
    {
        hp = Mathf.Min(maxHP, hp + Mathf.Max(0, amount));
    }

    public void GainMana(int a)
    {
        mana = Mathf.Min(maxMana, mana + Mathf.Max(0, a));
    }

    public int DrainMana(int a)
    {
        int d = Mathf.Clamp(a, 0, mana);
        mana -= d;
        return d;
    }

    public void GainRage(int a)
    {
        rage = Mathf.Min(maxRage, rage + Mathf.Max(0, a));
    }

    // Hút Rage cho Purple
    public int DrainRage(int a)
    {
        int d = Mathf.Clamp(a, 0, rage);
        rage -= d;
        return d;
    }
}
