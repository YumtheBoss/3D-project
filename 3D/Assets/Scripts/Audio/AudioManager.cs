using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Bổ sung để lắng nghe sự kiện scene chuyển
using System.Collections.Generic;   // Bổ sung để sử dụng List cấu hình

/// <summary>
/// Quản lý âm thanh toàn bộ game.
/// Gắn script này vào 1 GameObject tên "AudioManager" và đặt nó ở CẢNH MainMenu.
/// Script sẽ tự động tồn tại xuyên suốt các Scene (DontDestroyOnLoad).
/// </summary>
public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Object.FindAnyObjectByType<AudioManager>();
                if (_instance == null)
                {
                    // Tự sinh AudioManager khi debug trực tiếp từ Editor
                    GameObject go = new GameObject("AutoCreated_AudioManager");
                    _instance = go.AddComponent<AudioManager>();
                    Debug.Log("[AudioManager] Đã tự động khởi tạo Instance mới phục vụ quá trình Debug trực tiếp trong Editor!");
                }
            }
            return _instance;
        }
    }

    [Header("Audio Sources")]
    [Tooltip("Nguồn phát nhạc nền (Background Music)")]
    public AudioSource musicSource;
    [Tooltip("Nguồn phát hiệu ứng âm thanh (SFX: bước chân, tiếng cửa...)")]
    public AudioSource sfxSource;

    [System.Serializable]
    public struct SceneAudioConfig
    {
        [Tooltip("Tên chính xác của Scene trong Build Settings (ví dụ: SampleScene, Hospital, LevelTst)")]
        public string sceneName;
        [Tooltip("File nhạc nền BGM tương ứng cho Scene này")]
        public AudioClip bgmClip;
    }

    [Header("Nhạc nền")]
    [Tooltip("File nhạc nền cho Main Menu")]
    public AudioClip menuMusic;
    [Tooltip("File nhạc nền mặc định cho lúc chơi game (nếu không có cấu hình riêng theo Scene bên dưới)")]
    public AudioClip gameMusic;

    [System.Serializable]
    public struct RoomAudioConfig
    {
        [Tooltip("Phòng tương ứng (Room 0, 1, 2, 3, 4, 5)")]
        public RoomManager.RoomState room;
        [Tooltip("Nhạc nền BGM tương ứng cho phòng này (Ví dụ: Room 0, Room 1, Room 2 khác nhau dù cùng 1 map)")]
        public AudioClip bgmClip;
    }

    [Header("Nhạc nền theo từng Scene cụ thể")]
    [Tooltip("Thêm cấu hình nhạc nền riêng cho từng cảnh chơi game tại đây (ví dụ: Hospital, LevelTst)")]
    public List<SceneAudioConfig> sceneCustomBgmList = new List<SceneAudioConfig>();

    [Header("Nhạc nền theo từng Room cụ thể")]
    [Tooltip("Thêm cấu hình nhạc nền riêng cho từng Room tại đây (ƯU TIÊN CAO NHẤT, cực kỳ hữu ích khi cùng scene nhưng các room khác nhạc như Room 0, 1, 2)")]
    public List<RoomAudioConfig> roomCustomBgmList = new List<RoomAudioConfig>();

    [Header("SFX")]
    [Tooltip("Tiếng click khi bấm nút")]
    public AudioClip buttonClickSound;

    // Lưu trữ giá trị âm lượng
    private float masterVolume = 1f;
    private float musicVolume = 1f;
    private float sfxVolume = 1f;

    private void Awake()
    {
        // Singleton: Chỉ cho phép 1 AudioManager duy nhất tồn tại
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Tự tạo AudioSource nếu chưa có
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
        }

        // Đọc âm lượng đã lưu
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

        ApplyVolume();
    }

    private void OnEnable()
    {
        // Đăng ký sự kiện lắng nghe chuyển scene và chuyển room
        SceneManager.sceneLoaded += OnSceneLoaded;
        RoomManager.OnRoomEntered += OnRoomEntered;
    }

    private void OnDisable()
    {
        // Huỷ đăng ký tránh rò rỉ bộ nhớ
        SceneManager.sceneLoaded -= OnSceneLoaded;
        RoomManager.OnRoomEntered -= OnRoomEntered;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Khi scene load, áp dụng nhạc nền baseline theo Scene
        ApplySceneBGM(scene);
    }

    /// <summary>
    /// Sự kiện kích hoạt khi người chơi tiến sang Room mới (được gọi toàn cục bởi RoomManager)
    /// </summary>
    private void OnRoomEntered(RoomManager.RoomState room)
    {
        // 1. Kiểm tra xem có cấu hình nhạc riêng cho Room này trong danh sách không
        AudioClip customBgm = null;
        if (roomCustomBgmList != null)
        {
            foreach (var config in roomCustomBgmList)
            {
                if (config.room == room)
                {
                    customBgm = config.bgmClip;
                    break;
                }
            }
        }

        // 2. Nếu tìm thấy cấu hình nhạc riêng cho Room (như Room 0, Room 1, Room 2...), phát ngay
        if (customBgm != null)
        {
            PlayMusic(customBgm);
            Debug.Log($"[AudioManager] Tự động phát nhạc nền tùy chỉnh cho {room} (Room-specific BGM)");
            return;
        }

        // 3. Nếu không có cấu hình riêng cho Room, giữ nguyên nhạc nền theo Scene hiện tại
        ApplySceneBGM(SceneManager.GetActiveScene());
    }

    /// <summary>
    /// Phát nhạc nền mặc định dựa trên Scene hiện tại
    /// </summary>
    private void ApplySceneBGM(Scene scene)
    {
        // 1. Kiểm tra xem có cấu hình nhạc riêng cho Scene này trong danh sách không
        AudioClip customBgm = null;
        if (sceneCustomBgmList != null)
        {
            foreach (var config in sceneCustomBgmList)
            {
                if (string.Equals(config.sceneName, scene.name, System.StringComparison.OrdinalIgnoreCase))
                {
                    customBgm = config.bgmClip;
                    break;
                }
            }
        }

        // 2. Nếu tìm thấy nhạc riêng cho Scene, phát ngay lập tức
        if (customBgm != null)
        {
            PlayMusic(customBgm);
            Debug.Log($"[AudioManager] Tự động phát nhạc nền tùy chỉnh cho scene: {scene.name}");
            return;
        }

        // 3. Fallback: Nếu không có cấu hình riêng, sử dụng logic mặc định
        string sceneNameLower = scene.name.ToLower();

        if (sceneNameLower.Contains("menu") || sceneNameLower.Contains("main"))
        {
            if (menuMusic != null)
            {
                PlayMusic(menuMusic);
                Debug.Log($"[AudioManager] Tự động phát Nhạc nền Menu chính cho scene: {scene.name}");
            }
        }
        else
        {
            // Bất kỳ màn chơi game nào khác
            if (gameMusic != null)
            {
                PlayMusic(gameMusic);
                Debug.Log($"[AudioManager] Tự động phát Nhạc nền Game Play mặc định cho scene: {scene.name}");
            }
            else
            {
                // Dừng nhạc menu lại để tránh phá bầu không khí u ám của game kinh dị
                StopMusic();
            }
        }
    }

    private void Start()
    {
        // Kích hoạt phát nhạc nền cảnh hiện tại khi game khởi động
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    // ========== ĐIỀU CHỈNH ÂM LƯỢNG ==========

    /// <summary>
    /// Master Volume (âm lượng tổng, ảnh hưởng tất cả)
    /// </summary>
    public void SetMasterVolume(float value)
    {
        masterVolume = value;
        PlayerPrefs.SetFloat("MasterVolume", value);
        PlayerPrefs.Save();
        ApplyVolume();
    }

    /// <summary>
    /// Âm lượng nhạc nền
    /// </summary>
    public void SetMusicVolume(float value)
    {
        musicVolume = value;
        PlayerPrefs.SetFloat("MusicVolume", value);
        PlayerPrefs.Save();
        ApplyVolume();
    }

    /// <summary>
    /// Âm lượng hiệu ứng (SFX: tiếng bước chân, cửa, jumpscare...)
    /// </summary>
    public void SetSFXVolume(float value)
    {
        sfxVolume = value;
        PlayerPrefs.SetFloat("SFXVolume", value);
        PlayerPrefs.Save();
        ApplyVolume();
    }

    private void ApplyVolume()
    {
        // Master Volume ảnh hưởng AudioListener (tất cả âm thanh trong game)
        AudioListener.volume = masterVolume;

        // Music và SFX volume chỉ ảnh hưởng source riêng
        if (musicSource != null) musicSource.volume = musicVolume;
        if (sfxSource != null) sfxSource.volume = sfxVolume;
    }

    // ========== PHÁT ÂM THANH ==========

    /// <summary>
    /// Phát nhạc nền
    /// </summary>
    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null) return;

        // Không phát lại nếu đang phát cùng bài
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.volume = musicVolume; // Áp dụng đúng âm lượng hiện tại
        musicSource.Play();
    }

    /// <summary>
    /// Dừng nhạc nền
    /// </summary>
    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }

    /// <summary>
    /// Phát 1 âm thanh SFX (dùng PlayOneShot để có thể phát chồng nhiều tiếng cùng lúc)
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume);
        }
    }

    /// <summary>
    /// Phát tiếng click nút bấm
    /// </summary>
    public void PlayButtonClick()
    {
        PlaySFX(buttonClickSound);
    }

    // ========== GETTER ==========

    public float GetMasterVolume() { return masterVolume; }
    public float GetMusicVolume() { return musicVolume; }
    public float GetSFXVolume() { return sfxVolume; }
}
