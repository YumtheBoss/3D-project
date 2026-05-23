using UnityEngine;
using AnomalySystem;

/// <summary>
/// Kết nối RoomManager với AnomalyManager.
/// Room 0 → reset về bình thường.
/// Room 1 → luôn bình thường (không anomaly).
/// Room 2 → bắt buộc có bất thường (anomalyProbability = 100).
/// Gắn script này vào bất kỳ GameObject nào trong scene (ví dụ: Game Manager).
/// Sau khi gắn, kéo component AnomalyManager từ Game Manager vào field bên dưới.
/// </summary>
public class Room2AnomalyBridge : MonoBehaviour
{
    [Tooltip("Kéo component AnomalyManager từ Game Manager vào đây")]
    public AnomalyManager anomalyManager;

    private float originalProbability;

    private void OnEnable()
    {
        RoomManager.OnRoomEntered += HandleRoomEntered;
    }

    private void OnDisable()
    {
        RoomManager.OnRoomEntered -= HandleRoomEntered;
    }

    private void Start()
    {
        if (anomalyManager == null)
            anomalyManager = FindObjectOfType<AnomalyManager>();

        if (anomalyManager != null)
            originalProbability = anomalyManager.anomalyProbability;
        else
            Debug.LogError("[Room2AnomalyBridge] Không tìm thấy AnomalyManager!");

        // Nếu game bắt đầu ở Room 0, đảm bảo trạng thái bình thường
        if (RoomManager.Instance == null || RoomManager.Instance.CurrentRoom == RoomManager.RoomState.Room0)
            TriggerLevel(0);
    }

    private void HandleRoomEntered(RoomManager.RoomState room)
    {
        switch (room)
        {
            case RoomManager.RoomState.Room0:
                TriggerLevel(0);
                break;
            case RoomManager.RoomState.Room1:
                TriggerLevel(0); // Room 1 luôn bình thường như Room 0
                break;
            case RoomManager.RoomState.Room2:
                TriggerGuaranteedAnomaly();
                break;
        }
    }

    private void TriggerLevel(int level)
    {
        if (anomalyManager == null) return;
        anomalyManager.anomalyProbability = originalProbability;
        anomalyManager.GenerateNewLevel(level);
    }

    private void TriggerGuaranteedAnomaly()
    {
        if (anomalyManager == null) return;
        // Ép buộc 100% có bất thường ở Room 2 để player thấy đồ đạc xáo trộn
        anomalyManager.anomalyProbability = 100f;
        anomalyManager.GenerateNewLevel(2);
        // Khôi phục xác suất gốc để không ảnh hưởng các room sau
        anomalyManager.anomalyProbability = originalProbability;
    }
}
