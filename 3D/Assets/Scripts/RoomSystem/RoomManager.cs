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
    [HideInInspector]
    public bool isTimerPaused = false;

    private bool isLoadingScene = false;
    private RoomState pendingRoom = RoomState.Room0;
    private string pendingCustomSpawnPointName = "";
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

        // Tự động kiểm tra và tạo EndingController nếu thiếu trong Scene (Self-Healing)
        EnsureEndingControllerExists();
    }

    private void EnsureEndingControllerExists()
    {
        if (EndingController.Instance == null && FindAnyObjectByType<EndingController>() == null)
        {
            GameObject endingCtrlObj = new GameObject("EndingController_Auto");
            endingCtrlObj.AddComponent<EndingController>();
            Debug.Log("[RoomManager Self-Heal] Phát hiện thiếu EndingController trong Scene! Đã tự động tạo 'EndingController_Auto'.");
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // Khi bắt đầu game, đọc phòng đã lưu
        int savedRoom = PlayerPrefs.GetInt("CurrentRoom", 0);

        #if UNITY_EDITOR
        // Nếu chạy trực tiếp trong Unity Editor, tự động reset về phòng bắt đầu của scene tương ứng để tránh lỗi lệch trạng thái khi test
        if (SceneManager.GetActiveScene().name == sampleSceneName)
        {
            savedRoom = 0;
            PlayerPrefs.SetInt("CurrentRoom", 0);
            PlayerPrefs.Save();
            Debug.Log("[RoomManager] [UNITY_EDITOR] Đã tự động reset về Room0 để tránh lỗi desync khi test trực tiếp trong Editor!");
        }
        else if (SceneManager.GetActiveScene().name == hospitalSceneName)
        {
            savedRoom = (int)RoomState.Room3;
            PlayerPrefs.SetInt("CurrentRoom", savedRoom);
            PlayerPrefs.Save();
            Debug.Log("[RoomManager] [UNITY_EDITOR] Đã tự động reset về Room3 để tránh lỗi desync khi test trực tiếp Hospital trong Editor!");
        }
        else if (SceneManager.GetActiveScene().name == room5SceneName)
        {
            savedRoom = (int)RoomState.Room5;
            PlayerPrefs.SetInt("CurrentRoom", savedRoom);
            PlayerPrefs.Save();
            Debug.Log("[RoomManager] [UNITY_EDITOR] Đã tự động reset về Room5 để tránh lỗi desync khi test trực tiếp LevelTst trong Editor!");
        }
        #endif

        CurrentRoom = (RoomState)savedRoom;
        Debug.Log($"[RoomManager] Start: Khởi tạo phòng. CurrentRoom={CurrentRoom}");
        OnRoomEntered?.Invoke(CurrentRoom);
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

            if (!string.IsNullOrEmpty(pendingCustomSpawnPointName))
            {
                TeleportToCustomSpawn(pendingCustomSpawnPointName);
                pendingCustomSpawnPointName = ""; // reset
            }
            else
            {
                TeleportToRoomSpawn(pendingRoom);
            }

            OnRoomEntered?.Invoke(pendingRoom);
        }
        else
        {
            // Trường hợp tải scene trực tiếp (từ MainMenu Continue hoặc Editor)
            int savedRoom = PlayerPrefs.GetInt("CurrentRoom", 0);

            #if UNITY_EDITOR
            // Nếu chạy trực tiếp trong Unity Editor, tự động reset về phòng tương ứng
            if (scene.name == sampleSceneName)
            {
                savedRoom = 0;
                PlayerPrefs.SetInt("CurrentRoom", 0);
                PlayerPrefs.Save();
                Debug.Log("[RoomManager] [UNITY_EDITOR] Đã tự động reset về Room0 ở OnSceneLoaded!");
            }
            else if (scene.name == hospitalSceneName)
            {
                savedRoom = (int)RoomState.Room3;
                PlayerPrefs.SetInt("CurrentRoom", savedRoom);
                PlayerPrefs.Save();
                Debug.Log("[RoomManager] [UNITY_EDITOR] Đã tự động reset về Room3 ở OnSceneLoaded!");
            }
            else if (scene.name == room5SceneName)
            {
                savedRoom = (int)RoomState.Room5;
                PlayerPrefs.SetInt("CurrentRoom", savedRoom);
                PlayerPrefs.Save();
                Debug.Log("[RoomManager] [UNITY_EDITOR] Đã tự động reset về Room5 ở OnSceneLoaded!");
            }
            #endif

            CurrentRoom = (RoomState)savedRoom;
            Debug.Log($"[RoomManager] OnSceneLoaded: Tải scene trực tiếp. CurrentRoom={CurrentRoom}");
            OnRoomEntered?.Invoke(CurrentRoom);
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
        if (!isTimerPaused && Time.timeScale > 0f)
        {
            TotalPlayTime += Time.deltaTime;
        }
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
                pendingCustomSpawnPointName = ""; // Reset custom spawn
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
        if (room == RoomState.Room0 || room == RoomState.Room1 || room == RoomState.Room2)
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
    /// Tiến vào room mới và dịch chuyển người chơi tới một spawn point tùy chỉnh cụ thể.
    /// </summary>
    public void EnterRoomWithCustomSpawn(RoomState room, string spawnPointName)
    {
        CurrentRoom = room;
        PlayerPrefs.SetInt("CurrentRoom", (int)room);
        PlayerPrefs.Save();
        Debug.Log($"[RoomManager] Entered {room} with custom spawn point: {spawnPointName}");

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
                pendingCustomSpawnPointName = spawnPointName;
                hasPendingTeleport = true;
                Debug.Log($"[RoomManager] Cross-scene transition: '{currentScene}' → '{targetScene}' for {room} with custom spawn '{spawnPointName}'");
                SceneManager.LoadScene(targetScene);
            }
            return;
        }

        // Cùng scene → teleport trực tiếp tới custom spawn point
        TeleportToCustomSpawn(spawnPointName);
        OnRoomEntered?.Invoke(room);
    }

    /// <summary>
    /// Dịch chuyển người chơi kết hợp hiệu ứng Fade màn hình tối đen mượt mà.
    /// </summary>
    public void EnterRoomWithFadeTransition(RoomState room, string spawnPointName, float fadeTime = 0.6f)
    {
        StartCoroutine(EnterRoomWithFadeRoutine(room, spawnPointName, fadeTime));
    }

    private IEnumerator EnterRoomWithFadeRoutine(RoomState room, string spawnPointName, float fadeTime)
    {
        // 1. Tạo Canvas và Image màu đen động đè lên toàn màn hình
        GameObject fadeGO = new GameObject("Dynamic_FadeOverlay");
        DontDestroyOnLoad(fadeGO);

        Canvas canvas = fadeGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; 

        UnityEngine.UI.Image fadeImage = fadeGO.AddComponent<UnityEngine.UI.Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);

        RectTransform rect = fadeImage.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // 2. Fade to Black (Tối màn hình dần)
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            fadeImage.color = new Color(0f, 0f, 0f, Mathf.Clamp01(elapsed / fadeTime));
            yield return null;
        }
        fadeImage.color = new Color(0f, 0f, 0f, 1f);

        // 3. Thực hiện thay đổi Room và dịch chuyển
        CurrentRoom = room;
        PlayerPrefs.SetInt("CurrentRoom", (int)room);
        PlayerPrefs.Save();

        if (!string.IsNullOrEmpty(spawnPointName))
        {
            TeleportToCustomSpawn(spawnPointName);
        }
        else
        {
            TeleportToRoomSpawn(room);
        }

        // Chờ 0.2 giây để hệ thống Camera/Physics định vị tại vị trí mới dưới màn đen
        yield return new WaitForSeconds(0.2f);

        // Phát sự kiện để kích hoạt các logic môi trường (như Demon, v.v.)
        OnRoomEntered?.Invoke(room);

        // 4. Fade in (Sáng màn hình dần)
        elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            fadeImage.color = new Color(0f, 0f, 0f, Mathf.Clamp01(1f - (elapsed / fadeTime)));
            yield return null;
        }

        // 5. Giải phóng tài nguyên
        Destroy(fadeGO);
    }

    private void TeleportToCustomSpawn(string spawnPointName)
    {
        GameObject sp = FindGameObjectEvenIfInactive(spawnPointName);
        if (sp != null)
        {
            Debug.Log($"[RoomManager] Teleporting player to custom spawn '{spawnPointName}' at position: {sp.transform.position}");
            TeleportPlayer(sp.transform);

            // Kiểm tra kích hoạt chủ động chuỗi đuổi bắt của Room 4
            string cleanName = spawnPointName.Trim();
            if (cleanName.StartsWith("Room4Spawn  Demon") || cleanName.StartsWith("Room4Spawn Demon"))
            {
                StartCoroutine(TriggerChaseDelayed());
            }
        }
        else
        {
            Debug.LogError($"[RoomManager] Cannot teleport: custom spawn point '{spawnPointName}' not found!");
        }
    }

    private IEnumerator TriggerChaseDelayed()
    {
        // Chờ 0.5 giây để nhân vật hoàn thành việc định vị vị trí spawn vật lý
        yield return new WaitForSeconds(0.5f);
        Room4ChaseSequence chaseSeq = FindAnyObjectByType<Room4ChaseSequence>();
        if (chaseSeq != null && player != null)
        {
            chaseSeq.ForceStartChase(player);
            Debug.Log("[RoomManager] Force started Room 4 Chase Sequence via code successfully!");
        }
    }

    public void ResetProgress()
    {
        CurrentRoom = RoomState.Room0;
        TotalPlayTime = 0f;
        isLoadingScene = false;
        hasPendingTeleport = false;
        pendingCustomSpawnPointName = "";
        pendingRoom = RoomState.Room0;
        Debug.Log("[RoomManager] Đã reset toàn bộ tiến trình game trong bộ nhớ.");
    }

    public void ResetToRoom0()
    {
        ResetProgress();
        
        // Giải phóng trạng thái đóng băng di chuyển của người chơi khi chơi lại (Self-Healing)
        FirstPersonController.Instance?.UnfreezePlayer();
        
        // Reset tiến trình đã lưu trong PlayerPrefs
        PlayerPrefs.SetInt("CurrentRoom", 0);
        PlayerPrefs.Save();

        string targetScene = sampleSceneName;
        string currentScene = SceneManager.GetActiveScene().name;

        if (!string.Equals(currentScene, targetScene, System.StringComparison.OrdinalIgnoreCase))
        {
            // Chuyển scene về SampleScene (hasPendingTeleport = true để OnSceneLoaded xử lý teleport)
            isLoadingScene = true;
            pendingRoom = RoomState.Room0;
            pendingCustomSpawnPointName = "";
            hasPendingTeleport = true;
            Debug.Log($"[RoomManager ResetToRoom0] Đang chuyển scene từ '{currentScene}' về '{targetScene}'...");
            SceneManager.LoadScene(targetScene);
        }
        else
        {
            // Cùng scene -> Teleport trực tiếp về điểm spawn Room012Spawn
            if (room012SpawnPoint == null)
            {
                GameObject sp = FindGameObjectEvenIfInactive("Room012Spawn");
                if (sp == null) sp = FindGameObjectEvenIfInactive("SpawnPoint");
                if (sp != null) room012SpawnPoint = sp.transform;
            }

            if (room012SpawnPoint != null)
            {
                Debug.Log($"[RoomManager ResetToRoom0] Cùng scene, dịch chuyển player về Room012Spawn: {room012SpawnPoint.position}");
                TeleportPlayer(room012SpawnPoint);
            }
            else
            {
                Debug.LogWarning("[RoomManager ResetToRoom0] Không tìm thấy spawn point Room012Spawn!");
            }

            OnRoomEntered?.Invoke(RoomState.Room0);
        }
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
        EnsureEndingControllerExists();

        if (EndingController.Instance != null)
        {
            EndingController.Instance.ShowGoodEnding(TotalPlayTime);
        }
        else
        {
            Debug.LogWarning("[RoomManager] Good Ending triggered, but EndingController is null!");
        }
    }

    public void TriggerBadEnding(string reason)
    {
        EnsureEndingControllerExists();

        if (EndingController.Instance != null)
        {
            EndingController.Instance.ShowBadEnding(reason);
        }
        else
        {
            Debug.LogWarning($"[RoomManager] EndingController.Instance is null! Reloading scene as fallback. Reason: {reason}");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void TriggerBadEndingTrapped()
    {
        EnsureEndingControllerExists();

        if (EndingController.Instance != null)
        {
            EndingController.Instance.ShowBadEndingTrapped();
        }
        else
        {
            Debug.LogWarning("[RoomManager] Bad Ending Trapped triggered, but EndingController is null! Reloading scene.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
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
        GameObject playerObj = null;

        // 1. Ưu tiên sử dụng FirstPersonController.Instance để xác định chính xác người chơi persistent (DontDestroyOnLoad)
        if (FirstPersonController.Instance != null)
        {
            playerObj = FirstPersonController.Instance.gameObject;
        }

        // Dọn dẹp trùng lặp PlayerHandheldManager trên Player chính chủ
        if (playerObj != null)
        {
            PlayerHandheldManager[] managers = playerObj.GetComponentsInChildren<PlayerHandheldManager>(true);
            if (managers.Length > 1)
            {
                Debug.Log($"[RoomManager] Phát hiện {managers.Length} PlayerHandheldManager trên Player chính chủ. Tiến hành dọn dẹp...");
                PlayerHandheldManager keepManager = null;
                // Ưu tiên giữ manager có gán prefab hoặc ở root
                foreach (var mgr in managers)
                {
                    if (keepManager == null)
                    {
                        keepManager = mgr;
                    }
                    else
                    {
                        if (mgr.teddyBearPrefab != null && keepManager.teddyBearPrefab == null)
                        {
                            keepManager = mgr;
                        }
                    }
                }
                
                // Hủy các manager thừa
                foreach (var mgr in managers)
                {
                    if (mgr != keepManager)
                    {
                        Debug.Log($"[RoomManager] Đang hủy PlayerHandheldManager dư thừa trên GameObject '{mgr.gameObject.name}'");
                        Destroy(mgr);
                    }
                }
            }
        }

        // 2. Tìm các đối tượng có tag "Player" trong scene để quét dọn bản sao cục bộ dư thừa
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length > 0)
        {
            foreach (GameObject p in players)
            {
                // Nếu tìm thấy một đối tượng có tag "Player" nhưng không khớp với Instance chính chủ, ta hủy nó đi
                if (playerObj != null && p != playerObj)
                {
                    // TRƯỚC KHI HỦY: Hãy sao chép cấu hình PlayerHandheldManager từ player bị hủy sang player chính chủ!
                    var localHandheld = p.GetComponentInChildren<PlayerHandheldManager>();
                    if (localHandheld != null)
                    {
                        var mainHandheld = playerObj.GetComponentInChildren<PlayerHandheldManager>();
                        if (mainHandheld == null)
                        {
                            Camera mainPlayerCam = playerObj.GetComponentInChildren<Camera>();
                            if (mainPlayerCam != null)
                            {
                                mainHandheld = mainPlayerCam.gameObject.AddComponent<PlayerHandheldManager>();
                            }
                            else
                            {
                                mainHandheld = playerObj.AddComponent<PlayerHandheldManager>();
                            }
                        }
                        
                        if (mainHandheld != null)
                        {
                            // Sao chép các trường cấu hình
                            mainHandheld.teddyBearPrefab = localHandheld.teddyBearPrefab;
                            mainHandheld.bearPositionOffset = localHandheld.bearPositionOffset;
                            mainHandheld.bearRotationOffset = localHandheld.bearRotationOffset;
                            mainHandheld.bearScaleMultiplier = localHandheld.bearScaleMultiplier;
                            mainHandheld.turnOnSound = localHandheld.turnOnSound;
                            mainHandheld.turnOffSound = localHandheld.turnOffSound;
                            Debug.Log($"[RoomManager] Đã sao chép cấu hình PlayerHandheldManager từ Player cục bộ bị hủy ({p.name}) sang Player chính chủ!");
                        }
                    }

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
            var fpc = FindAnyObjectByType<CharacterController>();
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
        // Chờ 1 frame đầu tiên sau khi load scene để Unity đăng ký đầy đủ hệ thống Physics và Collider của scene mới
        yield return null;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        CharacterController cc = player.GetComponent<CharacterController>();

        if (cc != null) cc.enabled = false;

        RigidbodyInterpolation originalInterpolation = RigidbodyInterpolation.None;

        // Tính toán vị trí spawn thông minh bằng Raycast từ trên xuống dưới
        // Quét mặt sàn thực tế để đặt chân người chơi đứng chính xác trên sàn (+0.02m để an toàn),
        // tránh lún sàn (gây kẹt di chuyển) và cũng không bị nhô cao quá chạm trần.
        Vector3 spawnPosition = target.position;
        float playerHalfHeight = 1.643f; // Chiều cao Capsule (2.0) * Scale Y (1.6429813) / 2
        Vector3 rayOrigin = target.position + Vector3.up * 0.1f; // Bắt đầu quét từ 10cm trên điểm spawn để đảm bảo nằm dưới trần nhà

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 5.0f))
        {
            spawnPosition = new Vector3(target.position.x, hit.point.y + playerHalfHeight + 0.02f, target.position.z);
            Debug.Log($"[RoomManager] Raycast tìm thấy sàn tại Y={hit.point.y}. Đặt vị trí người chơi tại Y={spawnPosition.y}");
        }
        else
        {
            // Dự phòng nếu không tìm thấy sàn
            spawnPosition = target.position + Vector3.up * 0.02f;
            Debug.LogWarning($"[RoomManager] Không tìm thấy sàn bằng Raycast dưới {target.name}. Dùng vị trí mặc định + 0.02m.");
        }

        // Kinematic trong lúc teleport để physics không can thiệp
        if (rb != null)
        {
            originalInterpolation = rb.interpolation;
            rb.interpolation = RigidbodyInterpolation.None; // Tắt tạm thời interpolation để tránh lỗi nội suy vị trí giữa 2 scene cực xa
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.position = spawnPosition;
            rb.rotation = target.rotation;
        }

        player.position = spawnPosition;
        player.rotation = target.rotation;

        // Đồng bộ Transform ngay lập tức với hệ thống Physics
        Physics.SyncTransforms();

        // Chờ 2 FixedUpdate để collider settle, tránh bị wall-push ngay sau teleport
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.interpolation = originalInterpolation; // Khôi phục lại interpolation ban đầu
        }

        Physics.SyncTransforms();

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
        // Để tăng tính chống chịu lỗi (mismatch khoảng trắng giữa 1 hay 2 space ở giữa),
        // ta chuẩn hóa khoảng trắng bằng cách gom các space liên tiếp thành 1 space duy nhất.
        string normalizedTarget = System.Text.RegularExpressions.Regex.Replace(targetName, @"\s+", " ");
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform t in allTransforms)
        {
            string cleanName = t.gameObject.name.Trim();
            string normalizedClean = System.Text.RegularExpressions.Regex.Replace(cleanName, @"\s+", " ");

            if (cleanName == targetName || normalizedClean == normalizedTarget)
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

    public bool CheckAndPlaySafetyLockMonologue()
    {
        string hintText = "";
        bool canExit = true;

        if (CurrentRoom == RoomState.Room0)
        {
            if (AnomalySystem.InventoryManager.Instance == null || !AnomalySystem.InventoryManager.Instance.HasItem("Book"))
            {
                hintText = GameTextConfig.GetSafetyHint(RoomState.Room0);
                canExit = false;
            }
        }
        else if (CurrentRoom == RoomState.Room1)
        {
            if (AnomalySystem.InventoryManager.Instance == null || !AnomalySystem.InventoryManager.Instance.HasItem("TornPage_Room1"))
            {
                hintText = GameTextConfig.GetSafetyHint(RoomState.Room1);
                canExit = false;
            }
        }
        else if (CurrentRoom == RoomState.Room3)
        {
            if (AnomalySystem.InventoryManager.Instance == null || !AnomalySystem.InventoryManager.Instance.HasItem("BibleNote_Room3"))
            {
                hintText = GameTextConfig.GetSafetyHint(RoomState.Room3);
                canExit = false;
            }
        }
        else if (CurrentRoom == RoomState.Room4)
        {
            if (AnomalySystem.InventoryManager.Instance == null || !AnomalySystem.InventoryManager.Instance.HasItem("TeddyBear"))
            {
                hintText = GameTextConfig.GetSafetyHint(RoomState.Room4);
                canExit = false;
            }
        }

        if (!canExit && !string.IsNullOrEmpty(hintText))
        {
            PlaySafetyMonologue(hintText);
            return false;
        }

        return true;
    }

    public void PlaySafetyMonologue(string text)
    {
        // Kiểm tra xem đã có monologue nào đang chạy chưa để tránh trùng lặp
        InnerMonologue existing = FindAnyObjectByType<InnerMonologue>();
        if (existing != null && existing.IsRunning)
        {
            return;
        }

        GameObject hintObj = new GameObject("SafetyLockHint_Auto");
        InnerMonologue mono = hintObj.AddComponent<InnerMonologue>();
        mono.triggerOnce = false;
        mono.lines = new System.Collections.Generic.List<InnerMonologue.MonologueLine>
        {
            new InnerMonologue.MonologueLine { text = text, autoAdvanceDelay = 5f }
        };
        mono.PlayManually();
        
        // Hủy object sau khi chạy xong để tránh rác Hierarchy
        Destroy(hintObj, 10f);
    }
}
