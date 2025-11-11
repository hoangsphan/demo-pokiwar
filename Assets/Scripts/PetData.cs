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
        // SỬA LẠI LOGIC:
        // Nếu ID trong Inspector là "Actor" hoặc rỗng
        // -> tự động lấy tên của GameObject (tên prefab) làm ID
        if (combatStats.id == "Actor" || string.IsNullOrEmpty(combatStats.id))
        {
            // Xóa "(Clone)" nếu đây là một instance được tạo ra
            combatStats.id = this.gameObject.name.Replace("(Clone)", "");
        }
        return combatStats;
    }
}