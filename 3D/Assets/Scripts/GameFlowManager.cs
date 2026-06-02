using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using AnomalySystem;

/// <summary>
/// Điều phối luồng game tuyến tính Room0→5 qua nhiều scene.
/// Mỗi gameplay scene đặt 1 instance và khai báo nó phụ trách phòng nào.
/// PlayerPrefs["CurrentRoom"] là cầu nối giữa các scene.
/// </summary>
public class GameFlowManager : MonoBehaviour
{
    public enum RoomStage { Room0 = 0, Room1 = 1, Room2 = 2, Room3 = 3, Room4 = 4, Room5 = 5 }

    public static GameFlowManager Instance { get; private set; }

    [Header("Phạm vi phòng của scene này")]
    [Tooltip("Phòng ĐẦU TIÊN trong scene này (vd: Room0 hoặc Room3)")]
    public RoomStage firstRoomInScene = RoomStage.Room0;
    [Tooltip("Phòng CUỐI CÙNG trong scene này (vd: Room2 hoặc Room5)")]
    public RoomStage lastRoomInScene  = RoomStage.Room2;
    [Tooltip("Tên scene gameplay tiếp theo. Để trống nếu đây là scene cuối.")]
    public string nextSceneName = "";

    [Header("Spawn Points — khớp theo thứ tự firstRoom → lastRoom")]
    [Tooltip("Số phần tử = (lastRoom - firstRoom + 1). Index 0 = firstRoom.")]
    public Transform[] roomSpawnPoints;

    [Header("Player")]
    public Transform player;

    [Header("Room 2 — Anomaly (chỉ cần gán nếu scene này chứa Room 2)")]
    public AnomalyManager anomalyManager;

    [Header("Game State (chỉ đọc)")]
    public RoomStage currentRoom;

    [Header("Events")]
    public UnityEvent<RoomStage> OnRoomEntered;
    public UnityEvent OnGameOver;
    public UnityEvent OnGoodEnding;
    public UnityEvent OnBadEnding;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Vô hiệu hóa GameFlowManager nếu sử dụng hệ thống RoomManager mới để tránh xung đột
        if (FindAnyObjectByType<RoomManager>() != null)
        {
            Debug.Log("[GameFlowManager] Phát hiện RoomManager hoạt động. Tắt GameFlowManager!");
            enabled = false;
        }
    }

    private void Start()
    {
        if (RoomManager.Instance != null || FindAnyObjectByType<RoomManager>() != null)
        {
            Debug.Log("[GameFlowManager] Bỏ qua Start: RoomManager mới đang hoạt động.");
            enabled = false;
            return;
        }

        int saved = PlayerPrefs.GetInt("CurrentRoom", (int)firstRoomInScene);
        currentRoom = (RoomStage)Mathf.Clamp(saved, (int)firstRoomInScene, (int)lastRoomInScene);
        EnterRoom(currentRoom, teleport: true);
    }

    // ─────────────────────────────────────────────────────────
    //  API công khai
    // ─────────────────────────────────────────────────────────

    /// <summary>Gọi từ RoomExitTrigger ở lối ra phòng bình thường.</summary>
    public void AdvanceToNextRoom()
    {
        int next = (int)currentRoom + 1;

        // Hết phòng trong scene → chuyển scene tiếp
        if (currentRoom == lastRoomInScene)
        {
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                SaveProgress((RoomStage)next);
                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                TriggerGoodEnding();
            }
            return;
        }

        currentRoom = (RoomStage)next;
        SaveProgress(currentRoom);
        EnterRoom(currentRoom, teleport: true);
    }

    /// <summary>Gọi từ DoorChoice khi player chọn cửa ở Room 2.</summary>
    public void OnRoom2DoorChoice(bool choseAnomalyDoor)
    {
        if (anomalyManager == null)
        {
            Debug.LogError("[GameFlowManager] Chưa gán AnomalyManager!");
            return;
        }

        bool correct = (choseAnomalyDoor == anomalyManager.isCurrentLevelAnomaly);
        if (correct)
        {
            Debug.Log("[GameFlowManager] Room2: ĐÚNG → tiến sang phòng tiếp.");
            AdvanceToNextRoom();
        }
        else
        {
            Debug.Log("[GameFlowManager] Room2: SAI → Game Over.");
            TriggerGameOver();
        }
    }

    public void TriggerGameOver()
    {
        ClearProgress();
        OnGameOver?.Invoke();
    }

    public void TriggerGoodEnding()
    {
        ClearProgress();
        OnGoodEnding?.Invoke();
    }

    public void TriggerBadEnding()
    {
        ClearProgress();
        OnBadEnding?.Invoke();
    }

    // ─────────────────────────────────────────────────────────
    //  Nội bộ
    // ─────────────────────────────────────────────────────────

    private void EnterRoom(RoomStage room, bool teleport)
    {
        Debug.Log($"[GameFlowManager] Vào {room}.");

        if (teleport) TeleportPlayer(room);

        if (room == RoomStage.Room2 && anomalyManager != null)
            anomalyManager.GenerateNewLevel((int)room);

        OnRoomEntered?.Invoke(room);
    }

    private void TeleportPlayer(RoomStage room)
    {
        int index = (int)room - (int)firstRoomInScene; // index tương đối
        if (roomSpawnPoints == null || index < 0 || index >= roomSpawnPoints.Length) return;
        Transform spawn = roomSpawnPoints[index];
        if (spawn == null || player == null) return;

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = spawn.position;
        }
        player.position = spawn.position;
        player.rotation = spawn.rotation;

        if (cc != null) cc.enabled = true;
    }

    private void SaveProgress(RoomStage room)
    {
        PlayerPrefs.SetInt("CurrentRoom", (int)room);
        PlayerPrefs.Save();
    }

    private void ClearProgress()
    {
        PlayerPrefs.SetInt("CurrentRoom", 0);
        PlayerPrefs.Save();
    }
}
