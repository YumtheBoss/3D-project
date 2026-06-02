using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

[System.Serializable]
public class LeaderboardEntry
{
    public string playerName;
    public float completion_time_seconds;
    public string timestamp;
    public bool isCompleted;
}

public class FirebaseDatabaseManager : MonoBehaviour
{
    private static FirebaseDatabaseManager _instance;
    public static FirebaseDatabaseManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<FirebaseDatabaseManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("FirebaseDatabaseManager_Auto");
                    _instance = go.AddComponent<FirebaseDatabaseManager>();
                    Debug.Log("[FirebaseDatabaseManager] Tự động tạo FirebaseDatabaseManager_Auto tại runtime để tránh lỗi NullReference!");
                }
            }
            return _instance;
        }
    }

    [Header("Firebase Config")]
    [Tooltip("Dán URL Realtime Database của bạn vào đây (phải có https:// và kết thúc bằng /)")]
    public string databaseURL = "https://your-project-default-rtdb.asia-southeast1.firebasedatabase.app/";

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Gửi điểm số hoàn thành game lên bảng xếp hạng Firebase
    /// </summary>
    public void SaveLeaderboardScore(string name, float timeInSeconds, bool isCompleted = false)
    {
        string path = "Leaderboard.json";
        
        // Tạo chuỗi JSON đơn giản chứa tên, thời gian, ngày hiện tại và trạng thái hoàn thành
        string json = $"{{\"playerName\": \"{name}\", \"completion_time_seconds\": {timeInSeconds:F2}, \"timestamp\": \"{System.DateTime.Now.ToString("yyyy-MM-dd")}\", \"isCompleted\": {isCompleted.ToString().ToLower()}}}";
        
        StartCoroutine(PostData(path, json));
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

    /// <summary>
    /// Lấy danh sách điểm từ bảng xếp hạng Firebase
    /// </summary>
    public void GetLeaderboardScores(System.Action<List<LeaderboardEntry>> callback)
    {
        StartCoroutine(FetchLeaderboard(callback));
    }

    private IEnumerator FetchLeaderboard(System.Action<List<LeaderboardEntry>> callback)
    {
        List<LeaderboardEntry> list = new List<LeaderboardEntry>();

        if (string.IsNullOrEmpty(databaseURL) || !databaseURL.StartsWith("http"))
        {
            Debug.LogError("[Firebase] URL Database không hợp lệ! Vui lòng nhập đúng URL.");
            callback?.Invoke(list);
            yield break;
        }

        string url = databaseURL;
        if (!url.EndsWith("/")) url += "/";
        url += "Leaderboard.json";

        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"[Firebase] Lỗi tải dữ liệu bảng xếp hạng: {request.error}");
        }
        else
        {
            string json = request.downloadHandler.text;
            Debug.Log($"[Firebase] Tải bảng xếp hạng thành công! Phản hồi: {json}");

            if (!string.IsNullOrEmpty(json) && json != "null" && json != "{}")
            {
                try
                {
                    // Regex bóc tách các object con nằm giữa dấu ngoặc nhọn { ... }
                    var objMatches = System.Text.RegularExpressions.Regex.Matches(json, @"\{[^{}]+\}");
                    foreach (System.Text.RegularExpressions.Match objMatch in objMatches)
                    {
                        string objStr = objMatch.Value;
                        
                        // Lấy từng thuộc tính một cách độc lập để không phụ thuộc vào thứ tự key của JSON
                        var nameMatch = System.Text.RegularExpressions.Regex.Match(objStr, @"\""playerName\""\s*:\s*\""([^\""]+)\""");
                        var timeMatch = System.Text.RegularExpressions.Regex.Match(objStr, @"\""completion_time_seconds\""\s*:\s*([0-9\.]+)");
                        var dateMatch = System.Text.RegularExpressions.Regex.Match(objStr, @"\""timestamp\""\s*:\s*\""([^\""]+)\""");
                        var completedMatch = System.Text.RegularExpressions.Regex.Match(objStr, @"\""isCompleted\""\s*:\s*(true|false)");

                        if (nameMatch.Success && timeMatch.Success)
                        {
                            bool isCompleted = false;
                            if (completedMatch.Success)
                            {
                                isCompleted = bool.Parse(completedMatch.Groups[1].Value);
                            }

                            if (isCompleted)
                            {
                                LeaderboardEntry entry = new LeaderboardEntry();
                                entry.playerName = nameMatch.Groups[1].Value;
                                entry.completion_time_seconds = float.Parse(timeMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                                entry.timestamp = dateMatch.Success ? dateMatch.Groups[1].Value : "";
                                entry.isCompleted = true;
                                list.Add(entry);
                            }
                        }
                    }

                    // Sắp xếp tăng dần theo thời gian hoàn thành (người chơi hoàn thành nhanh nhất lên top)
                    list.Sort((a, b) => a.completion_time_seconds.CompareTo(b.completion_time_seconds));
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Firebase] Lỗi phân tích cú pháp JSON bảng xếp hạng: {ex.Message}");
                }
            }
        }

        callback?.Invoke(list);
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
