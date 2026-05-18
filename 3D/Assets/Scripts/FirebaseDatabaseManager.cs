using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

public class FirebaseDatabaseManager : MonoBehaviour
{
    public static FirebaseDatabaseManager Instance { get; private set; }

    [Header("Firebase Config")]
    [Tooltip("Dán URL Realtime Database của bạn vào đây (phải có https:// và kết thúc bằng /)")]
    public string databaseURL = "https://your-project-default-rtdb.asia-southeast1.firebasedatabase.app/";

    private void Awake()
    {
        // Singleton pattern để dễ dàng gọi từ mọi nơi
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

    /// <summary>
    /// Gửi thời gian hoàn thành game lên Firebase
    /// </summary>
    public void SaveGameCompletionTime(float timeInSeconds)
    {
        string path = "GameCompletions.json";
        
        // Tạo chuỗi JSON đơn giản chứa thời gian và thời điểm hiện tại
        string json = $"{{\"completion_time_seconds\": {timeInSeconds:F2}, \"timestamp\": \"{System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}\"}}";
        
        StartCoroutine(PostData(path, json));
    }

    /// <summary>
    /// Gửi dữ liệu màn chơi bị jumpscare lên Firebase
    /// </summary>
    public void SaveJumpscareLevel(int level)
    {
        string path = "Jumpscares.json";
        
        // Tạo chuỗi JSON
        string json = $"{{\"level\": {level}, \"timestamp\": \"{System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}\"}}";
        
        StartCoroutine(PostData(path, json));
    }

    private IEnumerator PostData(string path, string json)
    {
        // Kiểm tra xem URL có hợp lệ không
        if (string.IsNullOrEmpty(databaseURL) || !databaseURL.StartsWith("http"))
        {
            Debug.LogError("[Firebase] URL Database không hợp lệ! Vui lòng nhập đúng URL từ Firebase Console.");
            yield break;
        }

        string url = databaseURL;
        if (!url.EndsWith("/")) url += "/";
        url += path;
        
        // Cấu hình UnityWebRequest để gửi POST request với dữ liệu JSON
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // Gửi và chờ kết quả
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"[Firebase] Lỗi gửi dữ liệu lên {path}: {request.error}");
        }
        else
        {
            Debug.Log($"[Firebase] Đã gửi dữ liệu thành công lên {path}! Phản hồi: {request.downloadHandler.text}");
        }
    }
}
