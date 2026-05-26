using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton quản lý tiến trình Room 0-5.
/// Room 0-2 trong SampleScene, Room 3-4 trong Hospital, Room 5 là scene riêng (LevelTst).
/// Tự động chuyển scene khi EnterRoom vào room thuộc scene khác.
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
    public Transform room3SpawnPoint;
    [Tooltip("Vị trí xuất hiện khi vào Room 4")]
    public Transform room4SpawnPoint;

    [Header("Scene Names")]
    [Tooltip("Tên scene chứa Room 0, 1, 2")]
    [SerializeField] private string sampleSceneName = "SampleScene";
    [Tooltip("Tên scene chứa Room 3, 4")]
    [SerializeField] private string hospitalSceneName = "Hospital";
    [Tooltip("Tên scene chứa Room 5 (phải thêm vào Build Settings)")]
    [SerializeField] private string room5SceneName = "LevelTst";

    public RoomState CurrentRoom { get; private set; } = RoomState.Room0;
    public float TotalPlayTime { get; private set; }

    private bool isLoadingScene = false;
    private RoomState pendingRoom = RoomState.Room0;
    private bool hasPendingTeleport = false;

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
        isLoadingScene = false;
        DemonController.ResetAll();
        // Tìm lại player sau mỗi lần load scene
        player = null;
        FindPlayer();

        // Xử lý trùng AudioListener (player cũ DontDestroyOnLoad + camera scene mới)
        CleanupDuplicateAudioListeners();

        // Nếu có pending teleport từ cross-scene transition, thực hiện ngay
        if (hasPendingTeleport)
        {
            hasPendingTeleport = false;
            Debug.Log($"[RoomManager] OnSceneLoaded: Executing pending teleport for {pendingRoom} in scene '{scene.name}'");
            // Reset spawn point references vì scene mới không có chúng
            room012SpawnPoint = null;
            room3SpawnPoint = null;
            room4SpawnPoint = null;
            TeleportToRoomSpawn(pendingRoom);
            OnRoomEntered?.Invoke(pendingRoom);
        }
    }

    /// <summary>
    /// Tìm và disable các AudioListener thừa, chỉ giữ lại 1 trên camera của player.
    /// </summary>
    private void CleanupDuplicateAudioListeners()
    {
        AudioListener[] listeners = FindObjectsOfType<AudioListener>();
        if (listeners.Length <= 1) return;

        // Ưu tiên giữ AudioListener trên camera của player
        AudioListener playerListener = null;
        if (player != null)
        {
            Camera playerCam = player.GetComponentInChildren<Camera>();
            if (playerCam != null)
                playerListener = playerCam.GetComponent<AudioListener>();
        }
        if (playerListener == null && Camera.main != null)
            playerListener = Camera.main.GetComponent<AudioListener>();

        foreach (AudioListener listener in listeners)
        {
            if (listener != playerListener)
            {
                Debug.Log($"[RoomManager] Disabling duplicate AudioListener on '{listener.gameObject.name}'");
                listener.enabled = false;
            }
        }
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
        PlayerPrefs.SetInt("CurrentRoom", (int)room);
        PlayerPrefs.Save();
        Debug.Log($"[RoomManager] Entered {room}");

        // Xác định scene mục tiêu cho room này
        string targetScene = GetSceneForRoom(room);
        string currentScene = SceneManager.GetActiveScene().name;

        // Nếu scene hiện tại khác scene mục tiêu → chuyển scene
        if (!string.Equals(currentScene, targetScene, System.StringComparison.OrdinalIgnoreCase))
        {
            if (!isLoadingScene)
            {
                isLoadingScene = true;
                pendingRoom = room;
                hasPendingTeleport = true;
                Debug.Log($"[RoomManager] Cross-scene transition: '{currentScene}' → '{targetScene}' for {room}");
                SceneManager.LoadScene(targetScene);
            }
            return; // Không fire event ở đây — OnSceneLoaded sẽ xử lý
        }

        // Cùng scene → teleport trực tiếp
        TeleportToRoomSpawn(room);
        OnRoomEntered?.Invoke(room);
    }

    /// <summary>
    /// Xác định tên scene cho mỗi room.
    /// </summary>
    private string GetSceneForRoom(RoomState room)
    {
        switch (room)
        {
            case RoomState.Room0:
            case RoomState.Room1:
            case RoomState.Room2:
                return sampleSceneName;
            case RoomState.Room3:
            case RoomState.Room4:
                return hospitalSceneName;
            case RoomState.Room5:
                return room5SceneName;
            default:
                return sampleSceneName;
        }
    }

    /// <summary>
    /// Teleport player đến spawn point phù hợp với room.
    /// Tự động tìm spawn point nếu chưa được gán.
    /// </summary>
    private void TeleportToRoomSpawn(RoomState room)
    {
        if (room == RoomState.Room0) return; // Room 0 không cần teleport

        if (room == RoomState.Room1 || room == RoomState.Room2)
        {
            if (room012SpawnPoint == null)
            {
                GameObject sp = FindGameObjectEvenIfInactive("Room012Spawn");
                if (sp == null) sp = FindGameObjectEvenIfInactive("SpawnPoint");
                if (sp != null) room012SpawnPoint = sp.transform;
            }
            if (room012SpawnPoint != null)
                TeleportPlayer(room012SpawnPoint);
        }
        else if (room == RoomState.Room3)
        {
            if (room3SpawnPoint == null)
            {
                GameObject sp = FindGameObjectEvenIfInactive("Room3Spawn");
                if (sp != null) room3SpawnPoint = sp.transform;
            }
            if (room3SpawnPoint != null)
                TeleportPlayer(room3SpawnPoint);
            else
                Debug.LogWarning("[RoomManager] Room3Spawn not found in scene!");
        }
        else if (room == RoomState.Room4)
        {
            if (room4SpawnPoint == null)
            {
                GameObject sp = FindGameObjectEvenIfInactive("Room4Spawn");
                if (sp != null)
                {
                    room4SpawnPoint = sp.transform;
                    Debug.Log("[RoomManager] Dynamic search found Room4Spawn successfully!");
                }
                else
                {
                    Debug.LogError("[RoomManager] Dynamic search FAILED to find 'Room4Spawn' in active scene!");
                }
            }
            if (room4SpawnPoint != null)
            {
                Debug.Log($"[RoomManager] Teleporting player to Room4Spawn at position: {room4SpawnPoint.position}");
                TeleportPlayer(room4SpawnPoint);
            }
            else
            {
                Debug.LogError("[RoomManager] Cannot teleport to Room 4: room4SpawnPoint is null!");
            }
        }
        // Room 5: scene riêng, Room5Initializer sẽ xử lý spawn
    }

    /// <summary>
    /// Cập nhật CurrentRoom và fire OnRoomEntered mà KHÔNG teleport player.
    /// Dùng bởi scene-local initializer (HospitalSceneManager, Room5Initializer)
    /// khi scene đã load và player đã ở đúng vị trí.
    /// </summary>
    public void NotifyRoomEntered(RoomState room)
    {
        CurrentRoom = room;
        Debug.Log($"[RoomManager] NotifyRoomEntered: {room}");
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
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        GameObject playerObj = null;

        if (players.Length > 1)
        {
            Debug.LogWarning($"[RoomManager] Found multiple ({players.Length}) players in the scene! Cleaning up duplicates...");
            foreach (GameObject p in players)
            {
                if (p.scene.name == "DontDestroyOnLoad")
                {
                    playerObj = p;
                }
                else
                {
                    Debug.Log($"[RoomManager] Destroying duplicate scene-local player '{p.name}' in scene '{p.scene.name}'");
                    p.tag = "Untagged";
                    p.SetActive(false);
                    Destroy(p);
                }
            }
            if (playerObj == null)
            {
                playerObj = players[0];
            }
        }
        else if (players.Length == 1)
        {
            playerObj = players[0];
        }

        if (playerObj == null)
        {
            playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                Debug.Log("[RoomManager] Found player by GameObject name 'Player' instead of tag!");
            }
        }
        if (playerObj == null)
        {
            // Dự phòng: Tìm bất kỳ đối tượng nào có gắn CharacterController
            var fpc = FindObjectOfType<CharacterController>();
            if (fpc != null)
            {
                playerObj = fpc.gameObject;
                Debug.Log($"[RoomManager] Found player by finding CharacterController on GameObject '{playerObj.name}'!");
            }
        }

        if (playerObj != null) 
        {
            player = playerObj.transform;
            Debug.Log($"[RoomManager] FindPlayer assigned successfully: '{player.name}'");

            // Tự động gắn PlayerHandheldManager vào Camera chính của người chơi
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                mainCam = playerObj.GetComponentInChildren<Camera>();
            }
            if (mainCam != null && mainCam.GetComponent<PlayerHandheldManager>() == null)
            {
                mainCam.gameObject.AddComponent<PlayerHandheldManager>();
                Debug.Log("[RoomManager] Đã tự động gắn PlayerHandheldManager vào Camera!");
            }
        }
        else
        {
            Debug.LogError("[RoomManager] FindPlayer FAILED: No player object with tag/name 'Player' or with CharacterController found in scene!");
        }
    }

    private void TeleportPlayer(Transform target)
    {
        if (player == null) FindPlayer();
        if (player == null)
        {
            Debug.LogError($"[RoomManager] Teleport to '{target.name}' aborted: player transform is null!");
            return;
        }

        // Tự động tắt bất kỳ Collider nào trên điểm Spawn để tránh kẹt vật lý (Theo hình ảnh Inspector của user)
        Collider targetCol = target.GetComponent<Collider>();
        if (targetCol != null && targetCol.enabled)
        {
            targetCol.enabled = false;
            Debug.Log($"[RoomManager] Automatically disabled Collider on spawn point '{target.name}' to prevent player getting stuck!");
        }

        Debug.Log($"[RoomManager] Teleporting player '{player.name}' to target '{target.name}' (Position: {target.position})");
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

    private GameObject FindGameObjectEvenIfInactive(string goName)
    {
        string targetName = goName.Trim();

        // 1. Thử tìm thông thường (đối tượng đang hoạt động/active)
        GameObject go = GameObject.Find(targetName);
        if (go != null) return go;

        // Thử tìm thông thường với nguyên gốc
        if (targetName != goName)
        {
            go = GameObject.Find(goName);
            if (go != null) return go;
        }

        // 2. Quét tất cả các đối tượng gốc (Root GameObjects) trong scene đang hoạt động (bao gồm cả bị ẩn/inactive)
        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (activeScene.isLoaded)
        {
            GameObject[] rootObjects = activeScene.GetRootGameObjects();
            foreach (GameObject root in rootObjects)
            {
                GameObject found = FindChildRecursive(root, targetName);
                if (found != null) return found;
            }
        }

        // 3. Dự phòng cuối cùng: Quét tất cả Transform trong bộ nhớ
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform t in allTransforms)
        {
            if (t.gameObject.name.Trim() == targetName)
            {
                // Loại bỏ các đối tượng là asset trong project (chỉ lấy đối tượng trong Hierarchy thực tế)
                if (t.gameObject.hideFlags == HideFlags.None && !string.IsNullOrEmpty(t.gameObject.scene.name))
                {
                    return t.gameObject;
                }
            }
        }
        return null;
    }

    private GameObject FindChildRecursive(GameObject parent, string name)
    {
        if (parent.name.Trim() == name.Trim()) return parent;
        foreach (Transform child in parent.transform)
        {
            GameObject found = FindChildRecursive(child.gameObject, name);
            if (found != null) return found;
        }
        return null;
    }
}
