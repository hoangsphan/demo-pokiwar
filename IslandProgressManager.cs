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

    // Bộ đệm (cache)
    private Dictionary<string, bool> islandUnlockStatus = new Dictionary<string, bool>();

    public bool IsDataLoaded { get; private set; } = false;
    private bool isLoadingData = false;
    private bool isFirebaseInitialized = false; // Cờ kiểm tra đã khởi tạo chưa

    public static event Action OnDataLoaded;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Giữ object này sống qua các scene
            Debug.Log("=== IslandProgressManager Created ===");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- HÀM MỚI: Xóa sạch dữ liệu cũ (Dùng khi Logout hoặc vào lại LoginScene) ---
    public void ResetData()
    {
        Debug.Log("[ProgressManager] 🧹 Resetting Data...");
        islandUnlockStatus.Clear();
        IsDataLoaded = false;
        isLoadingData = false; // Mở khóa để cho phép lần đăng nhập tiếp theo chạy
    }
    // --------------------------------------------------------------------------

    // Hàm này được gọi từ FirebaseAuthManager
    public void Initialize()
    {
        if (isFirebaseInitialized) return;

        Debug.Log("[ProgressManager] 🔧 Initializing manually...");
        try
        {
            auth = FirebaseAuth.DefaultInstance;
            db = FirebaseFirestore.DefaultInstance;

            auth.StateChanged += OnAuthStateChanged;
            isFirebaseInitialized = true;

            Debug.Log("[ProgressManager] ✓ Firebase Dependencies OK via Manager");

            // Nếu user đã login sẵn (trường hợp reload)
            if (auth.CurrentUser != null)
            {
                _ = LoadIslandProgressAsync(auth.CurrentUser.UserId);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[ProgressManager] ❌ Init Error: {e.Message}");
        }
    }

    private void OnAuthStateChanged(object sender, EventArgs eventArgs)
    {
        if (!isFirebaseInitialized) return;

        if (auth.CurrentUser == null)
        {
            // Khi logout thì xóa cache
            ResetData();
        }
    }

    public async Task LoadIslandProgressAsync(string userId)
    {
        if (!isFirebaseInitialized || db == null || string.IsNullOrEmpty(userId)) return;

        // Reset cờ loading nếu nó bị treo quá lâu (phòng hờ)
        if (isLoadingData)
        {
            Debug.LogWarning("[ProgressManager] ⚠️ Đang tải dữ liệu, bỏ qua lệnh gọi trùng.");
            return;
        }

        isLoadingData = true;
        IsDataLoaded = false;

        Debug.Log($"[ProgressManager] 📥 Loading data for: {userId}");

        try
        {
            DocumentReference userDocRef = db.Collection("users").Document(userId);
            var snapshot = await userDocRef.GetSnapshotAsync();

            if (snapshot.Exists && snapshot.TryGetValue("islandProgress", out object islandDataObj))
            {
                islandUnlockStatus.Clear();
                var islandDataMap = islandDataObj as Dictionary<string, object>;

                if (islandDataMap != null)
                {
                    foreach (var pair in islandDataMap)
                    {
                        islandUnlockStatus[pair.Key] = (bool)pair.Value;
                    }
                }
                Debug.Log($"[ProgressManager] ✅ Loaded {islandUnlockStatus.Count} islands");
            }
            else
            {
                Debug.Log("[ProgressManager] ℹ️ New user or no data. Unlocking first island.");
                await UnlockIslandAsync("FireIsland");
            }

            IsDataLoaded = true;
            OnDataLoaded?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[ProgressManager] ❌ Load Error: {e.Message}");
        }
        finally
        {
            isLoadingData = false; // Luôn mở khóa dù thành công hay thất bại
        }
    }

    public bool IsIslandUnlocked(string islandID)
    {
        return islandUnlockStatus.TryGetValue(islandID, out bool unlocked) && unlocked;
    }

    public async Task UnlockIslandAsync(string islandID)
    {
        if (!isFirebaseInitialized || auth.CurrentUser == null) return;

        string userId = auth.CurrentUser.UserId;
        islandUnlockStatus[islandID] = true; // Update cache ngay cho mượt

        try
        {
            DocumentReference userDocRef = db.Collection("users").Document(userId);
            var dataToMerge = new Dictionary<string, object>
            {
                { "islandProgress", new Dictionary<string, object> { { islandID, true } } }
            };
            await userDocRef.SetAsync(dataToMerge, SetOptions.MergeAll);
            Debug.Log($"[ProgressManager] 💾 Saved unlock: {islandID}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ProgressManager] ❌ Save Error: {e.Message}");
        }
    }

    void OnDestroy()
    {
        if (auth != null) auth.StateChanged -= OnAuthStateChanged;
    }
}