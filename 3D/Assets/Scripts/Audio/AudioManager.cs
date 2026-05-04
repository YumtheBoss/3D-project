using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

/// <summary>
/// Quản lý âm thanh toàn bộ game.
/// Gắn script này vào 1 GameObject tên "AudioManager" và đặt nó ở CẢNH MainMenu.
/// Script sẽ tự động tồn tại xuyên suốt các Scene (DontDestroyOnLoad).
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [Tooltip("Nguồn phát nhạc nền (Background Music)")]
    public AudioSource musicSource;
    [Tooltip("Nguồn phát hiệu ứng âm thanh (SFX: bước chân, tiếng cửa...)")]
    public AudioSource sfxSource;

    [Header("Nhạc nền Menu")]
    [Tooltip("File nhạc nền cho Main Menu")]
    public AudioClip menuMusic;
    [Tooltip("File nhạc nền cho lúc chơi game (để trống nếu không cần)")]
    public AudioClip gameMusic;

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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
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

    private void Start()
    {
        // Phát nhạc nền menu khi game bắt đầu
        if (menuMusic != null && musicSource != null)
        {
            PlayMusic(menuMusic);
        }
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
