using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton quản lý tiến trình Room 0-5.
/// Room 0-4 trong SampleScene. Room 5 là scene riêng.
/// Room 3 và Room 4 có spawn point: EnterRoom sẽ teleport player.
/// </summary>
public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }
    public static event Action<RoomState> OnRoomEntered;

    public enum RoomState { Room0, Room1, Room2, Room3, Room4, Room5 }

    [Header("Player")]
    [Tooltip("Transform của Player. Để trống thì tự tìm tag 'Player'.")]
    [SerializeField] private Transform player;

    [Header("Spawn Points (trong SampleScene)")]
    [Tooltip("Spawn point đầu map — dùng cho Room 0, 1, 2 (vòng lặp cùng map)")]
    [SerializeField] private Transform room012SpawnPoint;
    [Tooltip("Vị trí xuất hiện khi vào Room 3")]
    [SerializeField] private Transform room3SpawnPoint;
    [Tooltip("Vị trí xuất hiện khi vào Room 4")]
    [SerializeField] private Transform room4SpawnPoint;

    [Header("Room 5 — Scene riêng")]
    [Tooltip("Tên scene của Room 5 (phải thêm vào Build Settings)")]
    [SerializeField] private string room5SceneName = "Room5";

    public RoomState CurrentRoom { get; private set; } = RoomState.Room0;
    public float TotalPlayTime { get; private set; }

    private bool isLoadingRoom5 = false;

    // ═══════════════════════════════════════════════════════════
    // LIFECYCLE
    // ═══════════════════════════════════════════════════════════

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
            return;
        }

        DemonController.ResetAll();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isLoadingRoom5 = false;
        DemonController.ResetAll();
        // Tìm lại player sau mỗi lần load scene
        player = null;
        FindPlayer();
    }

    private void Update()
    {
        TotalPlayTime += Time.deltaTime;
    }

    // ═══════════════════════════════════════════════════════════
    // PUBLIC API
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Tiến sang room tiếp theo (dùng cho trigger cửa ra ở map vòng lặp 0-1-2).
    /// </summary>
    public void AdvanceToNextRoom()
    {
        int next = (int)CurrentRoom + 1;
        if (next > (int)RoomState.Room5) return;
        EnterRoom((RoomState)next);
    }

    public void EnterRoom(RoomState room)
    {
        CurrentRoom = room;
        Debug.Log($"[RoomManager] Entered {room}");

        // Room 0/1/2 dùng chung map → teleport về đầu map khi bắt đầu Room 1 hoặc Room 2
        if ((room == RoomState.Room1 || room == RoomState.Room2) && room012SpawnPoint != null)
            TeleportPlayer(room012SpawnPoint);
        else if (room == RoomState.Room3 && room3SpawnPoint != null)
            TeleportPlayer(room3SpawnPoint);
        else if (room == RoomState.Room4 && room4SpawnPoint != null)
            TeleportPlayer(room4SpawnPoint);
        else if (room == RoomState.Room5)
        {
            if (!isLoadingRoom5)
            {
                isLoadingRoom5 = true;
                // Không fire event ở đây — Room5Initializer.Start() sẽ gọi lại sau khi scene load
                SceneManager.LoadScene(room5SceneName);
            }
            return;
        }

        OnRoomEntered?.Invoke(room);
    }

    public void TriggerGoodEnding()
    {
        EndingController.Instance?.ShowGoodEnding(TotalPlayTime);
    }

    public void TriggerBadEnding(string reason)
    {
        EndingController.Instance?.ShowBadEnding(reason);
    }

    public void TriggerBadEndingTrapped()
    {
        EndingController.Instance?.ShowBadEndingTrapped();
    }

    /// <summary>
    /// Dùng bởi Room5ExitDoor khi player chọn sai cửa.
    /// Tìm GameObject có tag "Room5Spawn" trong scene hiện tại.
    /// </summary>
    public void RespawnInRoom5()
    {
        GameObject spawnGO = GameObject.FindGameObjectWithTag("Room5Spawn");
        if (spawnGO != null)
            TeleportPlayer(spawnGO.transform);
    }

    /// <summary>Teleport player đến transform chỉ định (dùng bởi Room5Initializer).</summary>
    public void RespawnToPoint(Transform target)
    {
        TeleportPlayer(target);
    }

    // ═══════════════════════════════════════════════════════════
    // INTERNAL
    // ═══════════════════════════════════════════════════════════

    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    private void TeleportPlayer(Transform target)
    {
        if (player == null) FindPlayer();
        if (player == null) return;
        StartCoroutine(TeleportRoutine(target));
    }

    private IEnumerator TeleportRoutine(Transform target)
    {
        Rigidbody rb = player.GetComponent<Rigidbody>();
        CharacterController cc = player.GetComponent<CharacterController>();

        if (cc != null) cc.enabled = false;

        // Kinematic trong lúc teleport để physics không can thiệp
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.position = target.position;
            rb.rotation = target.rotation;
        }

        player.position = target.position;
        player.rotation = target.rotation;

        // Chờ 2 FixedUpdate để collider settle, tránh bị wall-push ngay sau teleport
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (cc != null) cc.enabled = true;
    }
}
