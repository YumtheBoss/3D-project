using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hệ thống theo dõi người chơi thông minh.
/// Ghi lại lịch sử vị trí để tạo hiệu ứng dịch đuổi trễ (-1s).
/// Học tập và ghi nhận các vùng người chơi hay đứng để cung cấp điểm tuần tra cho Quỷ.
/// </summary>
public class PlayerTracker : MonoBehaviour
{
    public static PlayerTracker Instance { get; private set; }

    [Header("Cấu hình Học tập (Hotspots)")]
    [Tooltip("Khoảng cách tối thiểu giữa các hotspot học tập (mét)")]
    public float hotspotMergeDistance = 5f;
    [Tooltip("Giới hạn số lượng điểm nóng ghi nhớ")]
    public int maxHotspots = 10;
    [Tooltip("Chu kỳ ghi nhận vị trí người chơi (giây)")]
    public float recordInterval = 2.0f;

    private struct PositionStamp
    {
        public Vector3 position;
        public float time;
        public PositionStamp(Vector3 pos, float t) { position = pos; time = t; }
    }

    [System.Serializable]
    public class PlayerHotspot
    {
        public Vector3 position;
        public int visitCount;
        public PlayerHotspot(Vector3 pos) { position = pos; visitCount = 1; }
    }

    private Transform player;
    private Queue<PositionStamp> positionHistory = new Queue<PositionStamp>();
    private List<PlayerHotspot> frequentHotspots = new List<PlayerHotspot>();
    private float recordTimer = 0f;

    private void Awake()
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

    private void Start()
    {
        FindPlayer();
    }

    private void FindPlayer()
    {
        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null) player = pObj.transform;
    }

    private void Update()
    {
        if (player == null)
        {
            FindPlayer();
            return;
        }

        // 1. Ghi nhận lịch sử vị trí từng frame
        positionHistory.Enqueue(new PositionStamp(player.position, Time.time));

        // 2. Học tập các điểm nóng theo chu kỳ
        recordTimer += Time.deltaTime;
        if (recordTimer >= recordInterval)
        {
            recordTimer = 0f;
            RecordHotspot();
        }
    }

    /// <summary>
    /// Lấy vị trí của người chơi từ delaySeconds giây trước.
    /// </summary>
    public Vector3 GetDelayedPlayerPosition(float delaySeconds = 1.0f)
    {
        if (player == null) FindPlayer();
        if (player == null) return Vector3.zero;

        float targetTime = Time.time - delaySeconds;
        Vector3 lastPosition = player.position;

        // Loại bỏ các điểm quá cũ và lấy điểm tiệm cận targetTime gần nhất
        while (positionHistory.Count > 0 && positionHistory.Peek().time < targetTime)
        {
            lastPosition = positionHistory.Dequeue().position;
        }

        return lastPosition;
    }

    private void RecordHotspot()
    {
        Vector3 currentPos = player.position;

        // Tìm hotspot gần nhất
        PlayerHotspot closest = null;
        float minDist = hotspotMergeDistance;

        foreach (var hs in frequentHotspots)
        {
            float dist = Vector3.Distance(hs.position, currentPos);
            if (dist < minDist)
            {
                minDist = dist;
                closest = hs;
            }
        }

        if (closest != null)
        {
            closest.visitCount++;
            // Dịch chuyển nhẹ tâm hotspot về hướng vị trí mới
            closest.position = Vector3.Lerp(closest.position, currentPos, 0.2f);
        }
        else
        {
            if (frequentHotspots.Count < maxHotspots)
            {
                frequentHotspots.Add(new PlayerHotspot(currentPos));
            }
            else
            {
                // Tìm hotspot ít được ghé thăm nhất để thay thế
                int lowestIdx = 0;
                for (int i = 1; i < frequentHotspots.Count; i++)
                {
                    if (frequentHotspots[i].visitCount < frequentHotspots[lowestIdx].visitCount)
                        lowestIdx = i;
                }
                frequentHotspots[lowestIdx] = new PlayerHotspot(currentPos);
            }
        }
    }

    /// <summary>
    /// Lấy một điểm tuần tra ngẫu nhiên dựa trên phân phối trọng số các điểm nóng người chơi hay đứng.
    /// </summary>
    public bool TryGetFrequentPatrolTarget(out Vector3 targetPosition)
    {
        targetPosition = Vector3.zero;
        if (frequentHotspots.Count == 0) return false;

        // Tính tổng trọng số
        int totalWeight = 0;
        foreach (var hs in frequentHotspots) totalWeight += hs.visitCount;

        int randVal = Random.Range(0, totalWeight);
        int currentWeight = 0;

        foreach (var hs in frequentHotspots)
        {
            currentWeight += hs.visitCount;
            if (randVal <= currentWeight)
            {
                targetPosition = hs.position;
                return true;
            }
        }

        targetPosition = frequentHotspots[Random.Range(0, frequentHotspots.Count)].position;
        return true;
    }
}
