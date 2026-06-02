using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Bộ điều phối âm thanh động chuyên dụng cho Room 5 (Scene LevelTst).
/// - Tự động tạo 3 AudioSources tại runtime để phục vụ cross-fade (trộn âm).
/// - Trạng thái AN TOÀN (quỷ ở xa): Phát nhạc nền Safe Ambient (loop).
/// - Trạng thái NGUY HIỂM (quỷ ở gần < 10m hoặc đang đuổi): Fade OUT nhạc an toàn, fade IN tiếng quỷ rượt đuổi dồn dập và tiếng thở dốc của nhân vật.
/// - Tiếng thở của nhân vật tự động to dần khi khoảng cách với quỷ càng lúc càng ngắn.
/// </summary>
public class Room5AudioDirector : MonoBehaviour
{
    public static Room5AudioDirector Instance { get; private set; }

    [Header("Cấu hình Âm thanh Room 5")]
    [Tooltip("Nhạc nền khi quỷ ở xa hoặc chưa xuất hiện (Safe Ambient - YouTube xv2RiLGbXo0)")]
    public AudioClip safeAmbientClip;
    [Tooltip("Tiếng quỷ hú hét dồn dập khi đuổi hoặc ở gần (Danger Chase - Backrooms Entity Pack)")]
    public AudioClip dangerChaseClip;
    [Tooltip("Tiếng thở dốc sợ hãi của nhân vật chính khi bị quỷ áp sát")]
    public AudioClip playerBreathingClip;

    [Header("Thông số kích hoạt")]
    [Tooltip("Khoảng cách bắt đầu kích hoạt trạng thái nguy hiểm (mét)")]
    public float dangerDistanceThreshold = 10f;
    [Tooltip("Tốc độ trộn âm / chuyển kênh (fading speed)")]
    public float crossfadeSpeed = 1.5f;

    // Các AudioSource tự tạo tại runtime để quản lý riêng biệt
    private AudioSource musicSourceSafe;
    private AudioSource musicSourceDanger;
    private AudioSource musicSourceBreathing;

    private Transform player;
    private bool isRoom5Active = false;
    private bool wasDanger = false; // Theo dõi trạng thái nguy hiểm của khung hình trước

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

        // Tạo các AudioSource phục vụ cross-fade (Danger_SFX_Source đặt loop = false)
        musicSourceSafe = CreateAudioSource("Safe_BGM_Source", true);
        musicSourceDanger = CreateAudioSource("Danger_SFX_Source", false);
        musicSourceBreathing = CreateAudioSource("Player_Breathing_Source", true);
    }

    private AudioSource CreateAudioSource(string goName, bool loop)
    {
        GameObject child = new GameObject(goName);
        child.transform.SetParent(transform, false);
        AudioSource source = child.AddComponent<AudioSource>();
        source.loop = loop;
        source.playOnAwake = false;
        source.spatialBlend = 0f; // 2D Stereo
        source.volume = 0f;
        return source;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        RoomManager.OnRoomEntered += HandleRoomEntered;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        RoomManager.OnRoomEntered -= HandleRoomEntered;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Chỉ kích hoạt khi nằm trong scene LevelTst (Room 5)
        isRoom5Active = scene.name.Equals("LevelTst", System.StringComparison.OrdinalIgnoreCase);
        
        if (isRoom5Active)
        {
            FindPlayer();
            InitializeAudioPlayback();
        }
        else
        {
            StopAllRoom5Audio();
        }
    }

    private void HandleRoomEntered(RoomManager.RoomState room)
    {
        isRoom5Active = (room == RoomManager.RoomState.Room5);
        if (isRoom5Active)
        {
            FindPlayer();
            InitializeAudioPlayback();
        }
        else
        {
            StopAllRoom5Audio();
        }
    }

    private void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
        }
    }

    private void InitializeAudioPlayback()
    {
        // Dừng nhạc mặc định của AudioManager để nhường chỗ cho AudioDirector chuyên dụng của Room 5
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
        }

        wasDanger = false; // Reset trạng thái nguy hiểm khi khởi chạy lại màn chơi

        // Gán clip và phát dưới volume = 0 (phục vụ fade in)
        SetupAndPlay(musicSourceSafe, safeAmbientClip);
        SetupAndPlay(musicSourceBreathing, playerBreathingClip);

        // Chỉ gán clip chứ không tự động phát nhạc nguy hiểm ngay từ đầu
        if (musicSourceDanger != null)
        {
            musicSourceDanger.clip = dangerChaseClip;
            musicSourceDanger.volume = 0f;
        }

        Debug.Log("[Room5AudioDirector] Khởi tạo thành công hệ thống âm thanh động cho Room 5!");
    }

    private void SetupAndPlay(AudioSource source, AudioClip clip)
    {
        if (source == null || clip == null) return;
        source.clip = clip;
        source.volume = 0f;
        source.Play();
    }

    private void StopAllRoom5Audio()
    {
        if (musicSourceSafe != null) musicSourceSafe.Stop();
        if (musicSourceDanger != null) musicSourceDanger.Stop();
        if (musicSourceBreathing != null) musicSourceBreathing.Stop();
    }

    private void Update()
    {
        if (!isRoom5Active) return;

        if (player == null)
        {
            FindPlayer();
            if (player == null) return;
        }

        // 1. Quét tất cả quỷ FloorDemonAI trong Room 5 để tìm con gần player nhất
        FloorDemonAI[] demons = Object.FindObjectsByType<FloorDemonAI>(FindObjectsSortMode.None);
        FloorDemonAI closestDemon = null;
        float closestDist = float.MaxValue;

        foreach (var demon in demons)
        {
            // Bỏ qua quỷ chưa kích hoạt (Inactive) hoặc đang bị tiêu diệt (Vanishing)
            if (demon == null || !demon.gameObject.activeInHierarchy || 
                demon.CurrentState == FloorDemonAI.State.Inactive || 
                demon.CurrentState == FloorDemonAI.State.Vanishing) continue;

            float dist = Vector3.Distance(player.position, demon.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestDemon = demon;
            }
        }

        // 2. Xác định xem người chơi có đang gặp nguy hiểm không (Thuần túy dựa trên khoảng cách vật lý)
        bool isDanger = false;
        float breathingVolumeIntensity = 0f;

        if (closestDemon != null)
        {
            // Trạng thái Nguy hiểm kích hoạt trực tiếp và chính xác khi quỷ ở trong tầm dangerDistanceThreshold
            isDanger = closestDist <= dangerDistanceThreshold;

            if (isDanger)
            {
                // Tiếng thở nhân vật to dần từ 0f (tại dangerDistanceThreshold) đến 1.0f (khi quỷ áp sát < 2m)
                float t = Mathf.InverseLerp(dangerDistanceThreshold, 2.0f, closestDist);
                breathingVolumeIntensity = Mathf.Clamp01(t);
            }
        }

        // 3. Tính toán âm lượng đích (Target Volumes)
        // Lấy cấu hình âm lượng chung của người dùng từ AudioManager/PlayerPrefs để làm chuẩn (không bị đè to quá)
        float maxMusicVol = AudioManager.Instance != null ? AudioManager.Instance.GetMusicVolume() : PlayerPrefs.GetFloat("MusicVolume", 1f);
        float maxSfxVol = AudioManager.Instance != null ? AudioManager.Instance.GetSFXVolume() : PlayerPrefs.GetFloat("SFXVolume", 1f);

        // Kích hoạt âm thanh Danger 1 LẦN duy nhất khi chuyển từ AN TOÀN sang NGUY HIỂM
        if (isDanger && !wasDanger)
        {
            if (musicSourceDanger != null && dangerChaseClip != null)
            {
                musicSourceDanger.volume = maxMusicVol;
                musicSourceDanger.Play();
            }
        }
        else if (!isDanger && wasDanger)
        {
            // Khi thoát khỏi nguy hiểm, dừng âm thanh Danger để sẵn sàng phát lại cho lần kế tiếp
            if (musicSourceDanger != null)
            {
                musicSourceDanger.Stop();
            }
        }

        wasDanger = isDanger;

        bool isPurging = Room5SealPurge.IsAnySealCurrentlyPurging();
        float targetSafeVol = maxMusicVol;
        if (isPurging)
        {
            targetSafeVol = maxMusicVol * 0.05f; // Giảm sâu nhạc nền xuống 5% khi đang làm nghi thức tế lễ để âm thanh tế lễ nổi bật hoàn toàn
        }
        else if (isDanger)
        {
            targetSafeVol = maxMusicVol * 0.15f; // Giảm xuống 15% khi gặp quỷ thông thường
        }

        float targetBreathingVol = isDanger ? (breathingVolumeIntensity * maxSfxVol) : 0f;

        // 4. Thực hiện Cross-fade mượt mà bằng Mathf.MoveTowards
        musicSourceSafe.volume = Mathf.MoveTowards(musicSourceSafe.volume, targetSafeVol, Time.deltaTime * crossfadeSpeed);
        musicSourceBreathing.volume = Mathf.MoveTowards(musicSourceBreathing.volume, targetBreathingVol, Time.deltaTime * crossfadeSpeed);
        
        // Nếu không nằm trong vùng nguy hiểm, nhanh chóng giảm âm lượng nguồn Danger về 0
        if (!isDanger && musicSourceDanger != null)
        {
            musicSourceDanger.volume = Mathf.MoveTowards(musicSourceDanger.volume, 0f, Time.deltaTime * crossfadeSpeed * 2f);
        }
        
        // Điều chỉnh nhịp độ/tốc độ tiếng thở nhẹ nhàng dựa trên khoảng cách (áp sát -> thở gấp hơn)
        if (isDanger && musicSourceBreathing.isPlaying)
        {
            musicSourceBreathing.pitch = Mathf.Lerp(1.0f, 1.25f, breathingVolumeIntensity);
        }
    }
}
