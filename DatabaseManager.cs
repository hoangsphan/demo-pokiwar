using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Threading.Tasks;
using System.Collections;
using Firebase.Auth; // <-- ĐÃ THÊM

public class DatabaseManager : MonoBehaviour
{
    public static DatabaseManager Instance { get; private set; }

    public FirebaseFirestore db;
    public PlayerData playerData;
    public string userID;

    // === THÊM BIẾN MỚI ===
    // Biến này sẽ là 'cờ' báo hiệu cho các scene khác
    public bool IsInitialized { get; private set; } = false;
    // ======================

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            db = FirebaseFirestore.DefaultInstance;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Được gọi bởi AuthManager khi Đăng nhập hoặc Đăng ký
    public IEnumerator InitializeAndLoad(string uid)
    {
        userID = uid;
        IsInitialized = false; // Đặt lại cờ khi bắt đầu tải

        DocumentReference docRef = db.Collection("players").Document(userID);
        var loadTask = docRef.GetSnapshotAsync();

        // Chờ cho đến khi Task hoàn thành
        yield return new WaitUntil(() => loadTask.IsCompleted);

        if (loadTask.IsFaulted)
        {
            Debug.LogError("Lỗi khi tải data: " + loadTask.Exception);
            playerData = new PlayerData("ErrorUser");
            // (Vẫn set là true để game không bị kẹt)
            IsInitialized = true; // <-- THÊM DÒNG NÀY
            yield break;
        }

        var snapshot = loadTask.Result;
        if (snapshot.Exists)
        {
            // Nếu có data, chuyển nó thành class PlayerData
            playerData = snapshot.ConvertTo<PlayerData>();
            Debug.Log("Đã tải dữ liệu người chơi!");
        }
        else
        {
            // === PHẦN SỬA QUAN TRỌNG NHẤT ===
            Debug.LogWarning("Không tìm thấy dữ liệu. Tạo dữ liệu mới cho user đã đăng nhập...");

            // Lấy email của user hiện tại từ Auth để làm tên
            string playerName = FirebaseAuth.DefaultInstance.CurrentUser.Email ?? "New Player";

            // Tạo data mới và lưu vào biến local
            playerData = new PlayerData(playerName);

            // Bắt đầu một coroutine mới để LƯU data này lên server
            // mà không cần chờ đợi nó hoàn thành
            StartCoroutine(SaveDataAsync(playerData));
            // ===================================
        }

        // === THÊM DÒNG MỚI ===
        // Báo cho toàn bộ game biết: "Tôi đã tải xong!"
        IsInitialized = true;
        Debug.Log("DatabaseManager ĐÃ SẴN SÀNG (IsInitialized = true)");
        // ======================
    }

    // Được gọi bởi AuthManager khi Đăng ký
    public IEnumerator CreateNewPlayerData(string uid, string playerName)
    {
        userID = uid;
        playerData = new PlayerData(playerName); // Tạo data mặc định

        // === THÊM DÒNG NÀY ===
        // Khi tạo mới, chúng ta cũng phải set là đã "khởi tạo xong"
        IsInitialized = true;
        // ======================

        DocumentReference docRef = db.Collection("players").Document(userID);
        var saveTask = docRef.SetAsync(playerData);

        // Chờ cho đến khi Task hoàn thành
        yield return new WaitUntil(() => saveTask.IsCompleted);

        if (saveTask.IsFaulted)
        {
            Debug.LogError("Lỗi khi tạo data: " + saveTask.Exception);
        }
        else
        {
            Debug.Log("Tạo dữ liệu người chơi mới thành công!");
        }
    }

    // Hàm save mới (private, bất đồng bộ)
    private IEnumerator SaveDataAsync(PlayerData dataToSave)
    {
        DocumentReference docRef = db.Collection("players").Document(userID);
        var saveTask = docRef.SetAsync(dataToSave);

        yield return new WaitUntil(() => saveTask.IsCompleted);

        if (saveTask.IsFaulted)
            Debug.LogError("Lỗi khi save data nền: " + saveTask.Exception);
        else
            Debug.Log("Lưu data nền thành công!");
    }


    // Gọi hàm này khi cần lưu (ví dụ: trước khi vào trận)
    public void SaveData()
    {
        if (playerData == null || string.IsNullOrEmpty(userID))
        {
            Debug.LogError("Chưa đăng nhập, không thể save!");
            return;
        }

        DocumentReference docRef = db.Collection("players").Document(userID);
        docRef.SetAsync(playerData).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
                Debug.LogError("Lỗi khi save: " + task.Exception);
            else
                Debug.Log("Lưu dữ liệu lên Firestore thành công!");
        });
    }
    public IEnumerator SaveDataAndWait()
    {
        if (playerData == null || string.IsNullOrEmpty(userID))
        {
            Debug.LogError("Chưa đăng nhập, không thể save!");
            yield break; // Thoát khỏi Coroutine
        }

        DocumentReference docRef = db.Collection("players").Document(userID);
        Task saveTask = docRef.SetAsync(playerData); // Bắt đầu Task

        // Chờ cho đến khi Task hoàn thành
        yield return new WaitUntil(() => saveTask.IsCompleted);

        // Xử lý kết quả sau khi chờ
        if (saveTask.IsFaulted)
            Debug.LogError("Lỗi khi save (chờ): " + saveTask.Exception);
        else
            Debug.Log("Lưu dữ liệu (chờ) lên Firestore thành công!");
    }
}