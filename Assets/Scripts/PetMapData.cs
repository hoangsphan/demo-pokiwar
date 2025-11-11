using UnityEngine;

[CreateAssetMenu(fileName = "New Pet", menuName = "Game/Pet Data")]
public class PetMapData : ScriptableObject
{
    [Header("Pet Info")]
    public string petName = "Unknown Pet";
    public Sprite petIcon;
    public int petID; // 0, 1, 2, 3...

    [Header("Stats")]
    public int maxHealth = 100;
    public int attackPower = 20;
    public int defense = 10;

    [Header("Battle")]
    public string battleSceneName = "BattleScene";

    [Header("Unlock")]
    public bool isStarterPet = false; // Pet đầu tiên tự động unlock
}