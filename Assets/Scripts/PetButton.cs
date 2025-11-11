using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PetButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image petIcon;
    [SerializeField] private TextMeshProUGUI petNameText;
    [SerializeField] private GameObject lockIcon;
    [SerializeField] private Button button;

    [Header("Pet Data")]
    [SerializeField] private PetMapData petmapData;

    [Header("Visual Settings")]
    [SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Màu xám
    [SerializeField] private Color unlockedColor = Color.white; // Màu gốc

    private bool isUnlocked = false;

    void Start()
    {
        SetupButton();
        CheckUnlockStatus();
        UpdateVisual();
    }

    void SetupButton()
    {
        if (petmapData == null)
        {
            Debug.LogError($"Pet Data is missing on {gameObject.name}!");
            return;
        }

        // Hiển thị thông tin pet
        if (petIcon != null && petmapData.petIcon != null)
        {
            petIcon.sprite = petmapData.petIcon;
        }

        if (petNameText != null)
        {
            petNameText.text = petmapData.petName;
        }

        // Gắn sự kiện click
        if (button != null)
        {
            button.onClick.AddListener(OnPetButtonClick);
        }
    }

    void CheckUnlockStatus()
    {
        // Pet đầu tiên luôn unlock
        if (petmapData.isStarterPet)
        {
            isUnlocked = true;
            PetProgressManager.Instance.UnlockPet(petmapData.petID);
            return;
        }

        // Kiểm tra xem pet này đã unlock chưa
        isUnlocked = PetProgressManager.Instance.IsPetUnlocked(petmapData.petID);
    }

    void UpdateVisual()
    {
        if (isUnlocked)
        {
            // Unlock: Màu gốc
            if (petIcon != null)
                petIcon.color = unlockedColor;

            if (lockIcon != null)
                lockIcon.SetActive(false);

            if (button != null)
                button.interactable = true;
        }
        else
        {
            // Lock: Màu xám + khóa
            if (petIcon != null)
                petIcon.color = lockedColor;

            if (lockIcon != null)
                lockIcon.SetActive(true);

            if (button != null)
                button.interactable = false;
        }
    }

    void OnPetButtonClick()
    {
        if (!isUnlocked)
        {
            Debug.Log($"Pet {petmapData.petName} is locked!");
            return;
        }

        Debug.Log($"Starting battle with {petmapData.petName}!");

        // Lưu pet hiện tại để dùng trong Battle Scene
        PetProgressManager.Instance.SetCurrentBattlePet(petmapData);

        // Load Battle Scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(petmapData.battleSceneName);
    }

    // Public method để refresh từ bên ngoài
    public void RefreshUnlockStatus()
    {
        CheckUnlockStatus();
        UpdateVisual();
    }
}