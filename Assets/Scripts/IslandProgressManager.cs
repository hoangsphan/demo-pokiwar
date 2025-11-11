using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;

public class IslandProgressManager : MonoBehaviour
{
    public static IslandProgressManager Instance { get; private set; }

    private FirebaseAuth auth;
    private FirebaseFirestore db;

    // Bộ đệm (cache) để lưu dữ liệu đã tải về
    private Dictionary<string, bool> islandUnlockStatus = new Dictionary<string, bool>();

    public bool IsDataLoaded { get; private set; } = false;
    private bool isLoadingData = false;

    public static event Action OnDataLoaded;

    void Awake()
    {
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

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        auth.StateChanged += OnAuthStateChanged;
        db = FirebaseFirestore.DefaultInstance;

        if (auth.CurrentUser != null)
        {
            _ = LoadIslandProgressAsync(auth.CurrentUser.UserId);
        }
    }

    private void OnAuthStateChanged(object sender, EventArgs eventArgs)
    {
        if (auth.CurrentUser != null)
        {
            _ = LoadIslandProgressAsync(auth.CurrentUser.UserId);
        }
        else
        {
            islandUnlockStatus.Clear();
            IsDataLoaded = false;
        }
    }

    // Hàm tải dữ liệu từ Firestore về cache
    public async Task LoadIslandProgressAsync(string userId)
    {
        if (isLoadingData) return;

        isLoadingData = true;
        IsDataLoaded = false;

        // THÊM TRY-CATCH ĐỂ BẮT LỖI
        try
        {
            DocumentReference userDocRef = db.Collection("users").Document(userId);
            Debug.Log($"[ProgressManager] Bắt đầu tải data cho user: users/{userId}");

            var snapshot = await userDocRef.GetSnapshotAsync();

            isLoadingData = false;

            if (snapshot.Exists && snapshot.TryGetValue("islandProgress", out object islandDataObj))
            {
                islandUnlockStatus.Clear();
                var islandDataMap = islandDataObj as Dictionary<string, object>;

                if (islandDataMap != null)
                {
                    Debug.Log("[ProgressManager] Đã tìm thấy data, đang đọc vào bộ đệm...");
                    foreach (var pair in islandDataMap)
                    {
                        islandUnlockStatus[pair.Key] = (bool)pair.Value;

                        // THÊM LOG QUAN TRỌNG NÀY ĐỂ XEM DATA ĐỌC VỀ
                        Debug.Log($"[ProgressManager] Data trong cache: Key='{pair.Key}', Value='{pair.Value}'");
                    }
                }
                else
                {
                    Debug.LogError("[ProgressManager] Lỗi: Không thể ép kiểu 'islandProgress' về Dictionary!");
                }
            }
            else
            {
                Debug.LogWarning($"[ProgressManager] Không tìm thấy document cho user {userId}! Đang tạo data mặc định...");
                await UnlockIslandAsync("FireIsland"); // Tự động unlock đảo đầu tiên
            }

            IsDataLoaded = true;
            Debug.Log("[ProgressManager] Tải data thành công! Đang bắn sự kiện OnDataLoaded...");
            OnDataLoaded?.Invoke(); // Bắn sự kiện
        }
        catch (Exception e) // NẾU CÓ LỖI (Mạng, Quyền,...) NÓ SẼ HIỆN Ở ĐÂY
        {
            Debug.LogError($"[ProgressManager] LỖI NGHIÊM TRỌNG KHI TẢI DATA: {e.Message}");
            isLoadingData = false;
        }
    }

    // Hàm kiểm tra (đọc từ cache, rất nhanh)
    public bool IsIslandUnlocked(string islandID)
    {
        return islandUnlockStatus.TryGetValue(islandID, out bool unlocked) && unlocked;
    }

    // Hàm mở khóa (ghi đè lên Firebase)
    public async Task UnlockIslandAsync(string islandID)
    {
        if (auth.CurrentUser == null) return;
        string userId = auth.CurrentUser.UserId;

        // 1. Cập nhật cache
        islandUnlockStatus[islandID] = true;

        // 2. Cập nhật Firestore
        DocumentReference userDocRef = db.Collection("users").Document(userId);
        var dataToMerge = new Dictionary<string, object>
        {
            { "islandProgress", new Dictionary<string, object>
                {
                    { islandID, true }
                }
            }
        };

        Debug.Log($"Saving to Firestore: islandProgress.{islandID} = true");
        // Dùng SetAsync + MergeAll để tạo/cập nhật data an toàn
        await userDocRef.SetAsync(dataToMerge, SetOptions.MergeAll);
    }
}