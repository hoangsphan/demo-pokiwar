using UnityEngine;

public class PetData : MonoBehaviour
{
    [Header("Dữ liệu chiến đấu (Stats)")]
    [Tooltip("Đây là chỉ số gốc VÀ cũng là chỉ số 'live' trong trận")]
    public Actor combatStats = new Actor();

    [Header("Kỹ năng (Tương lai)")]
    // public List<Skill> skills;
    public string note = "Sau này sẽ thay bằng List<Skill>";

    /// <summary>
    /// Trả về Actor 'live' để BattleManager/CombatSystem sử dụng
    /// </summary>
    public Actor GetActor()
    {
        // Gán ID cho Actor dựa trên tên GameObject, 
        // vì Actor.id mặc định là "Actor"
        if (combatStats.id == "Actor" || string.IsNullOrEmpty(combatStats.id))
        {
            combatStats.id = this.gameObject.name;
        }
        return combatStats;
    }
}