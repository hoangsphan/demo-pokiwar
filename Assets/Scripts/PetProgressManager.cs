using UnityEngine;
using System.Collections.Generic;

public class PetProgressManager : MonoBehaviour
{
    // Singleton pattern
    public static PetProgressManager Instance { get; private set; }

    private const string UNLOCK_KEY_PREFIX = "Pet_Unlocked_";

    [Header("Current Battle")]
    private PetMapData currentBattlePet;

    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Kiểm tra pet đã unlock chưa
    public bool IsPetUnlocked(int petID)
    {
        return PlayerPrefs.GetInt(UNLOCK_KEY_PREFIX + petID, 0) == 1;
    }

    // Unlock pet
    public void UnlockPet(int petID)
    {
        PlayerPrefs.SetInt(UNLOCK_KEY_PREFIX + petID, 1);
        PlayerPrefs.Save();
        Debug.Log($"Pet ID {petID} has been unlocked!");
    }

    // Set pet hiện tại để chiến đấu
    public void SetCurrentBattlePet(PetMapData petData)
    {
        currentBattlePet = petData;
    }

    public PetMapData GetCurrentBattlePet()
    {
        return currentBattlePet;
    }

    // Gọi khi thắng battle
    public void OnBattleWon()
    {
        if (currentBattlePet == null)
        {
            Debug.LogWarning("No current battle pet set!");
            return;
        }

        int nextPetID = currentBattlePet.petID + 1;

        // Unlock pet tiếp theo
        UnlockPet(nextPetID);

        Debug.Log($"Battle won! Unlocked Pet ID: {nextPetID}");
    }

    // Reset toàn bộ progress (dùng cho testing)
    public void ResetAllProgress()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("All progress reset!");
    }

    // Unlock tất cả pet (cheat code cho testing)
    public void UnlockAllPets(int maxPetID)
    {
        for (int i = 0; i <= maxPetID; i++)
        {
            UnlockPet(i);
        }
        Debug.Log("All pets unlocked!");
    }
}