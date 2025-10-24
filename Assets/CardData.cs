using UnityEngine;
using UnityEngine.UI;
// using TMPro; 

public class CardData : MonoBehaviour
{
    public enum CardType
    {
        Skill,
        ManaItem,
        RageItem
    }

    [Header("Loại thẻ")]
    public CardType cardType = CardType.Skill;

    [Header("Thông tin thẻ")]
    public int itemValue = 0;
    public string skillID = "Punch";

    [Header("Tham chiếu (Tùy chọn)")]
    public Image iconImage;
    public Text labelText;
    // public TextMeshProUGUI labelText;
}