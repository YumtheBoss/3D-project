using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Singleton quản lý màn hình kết thúc game.
// Good ending: màn trắng tinh + peaceful audio.
// Bad ending: GAME OVER + zombie audio + nút thử lại.
// Bad ending trapped: bị mắc kẹt mãi mãi (Room 2 sai / Room 5 sai 3 lần).
public class EndingController : MonoBehaviour
{
    public static EndingController Instance { get; private set; }

    [Header("Audio")]
    [Tooltip("Nhạc good ending - thanh thản, nhẹ nhàng")]
    public AudioClip goodEndingMusic;
    [Tooltip("Âm thanh zombie nhai ngấu nghiến (Arya of Terror pack)")]
    public AudioClip badEndingZombieSound;
    [Tooltip("Nhạc nền bad ending - đáng sợ, loop")]
    public AudioClip badEndingAmbient;

    [Header("Visual")]
    [Tooltip("Font chữ tùy chỉnh (để trống dùng font mặc định)")]
    public Font customFont;
    [Tooltip("Tốc độ fade-in màn hình kết thúc (giây)")]
    public float fadeDuration = 1.5f;

    private Canvas endCanvas;
    private Image background;
    private Text mainText;
    private Button retryButton;
    private Button menuButton;
    private InputField nameInputField;
    private Button submitScoreButton;
    private AudioSource audioSource;

    private float currentPlayTime = 0f;
    private bool isGoodEnding = false;

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
        BuildUI();
    }

    private void BuildUI()
    {
        Font font = customFont != null
            ? customFont
            : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // --- Canvas ---
        GameObject canvasObj = new GameObject("_EndingCanvas_Auto");
        canvasObj.transform.SetParent(transform, false);
        endCanvas = canvasObj.AddComponent<Canvas>();
        endCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        endCanvas.sortingOrder = 9999;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // --- Background ---
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        background = bgObj.AddComponent<Image>();
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        // --- Main Text ---
        GameObject textObj = new GameObject("MainText");
        textObj.transform.SetParent(canvasObj.transform, false);
        mainText = textObj.AddComponent<Text>();
        mainText.alignment = TextAnchor.MiddleCenter;
        mainText.font = font;
        mainText.fontSize = 60;
        mainText.resizeTextForBestFit = false;
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0.1f, 0.3f);
        textRt.anchorMax = new Vector2(0.9f, 0.9f);
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        // --- Buttons ---
        retryButton = CreateButton(canvasObj.transform, "RetryBtn", "Thử Lại",
            new Vector2(0.25f, 0.08f), new Vector2(0.45f, 0.22f), font);
        retryButton.onClick.AddListener(OnRetry);

        menuButton = CreateButton(canvasObj.transform, "MenuBtn", "Menu Chính",
            new Vector2(0.55f, 0.08f), new Vector2(0.75f, 0.22f), font);
        menuButton.onClick.AddListener(OnMenu);

        // --- Name InputField & Submit Score Button for GameOver ---
        nameInputField = CreateInputField(canvasObj.transform, "NameInputField", "Nhập tên của bạn...", font);
        RectTransform ipRt = nameInputField.GetComponent<RectTransform>();
        ipRt.anchorMin = new Vector2(0.25f, 0.26f);
        ipRt.anchorMax = new Vector2(0.50f, 0.38f);
        ipRt.offsetMin = Vector2.zero;
        ipRt.offsetMax = Vector2.zero;

        submitScoreButton = CreateButton(canvasObj.transform, "SubmitScoreBtn", "Gửi Bảng Xếp Hạng",
            new Vector2(0.55f, 0.26f), new Vector2(0.75f, 0.38f), font);
        submitScoreButton.onClick.AddListener(OnSubmitScore);

        // --- AudioSource ---
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        canvasObj.SetActive(false);
    }

    private Button CreateButton(Transform parent, string goName, string label,
        Vector2 anchorMin, Vector2 anchorMax, Font font)
    {
        GameObject btnObj = new GameObject(goName);
        btnObj.transform.SetParent(parent, false);
        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);
        btn.colors = colors;

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(btnObj.transform, false);
        Text t = labelObj.AddComponent<Text>();
        t.text = label;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.font = font;
        t.fontSize = 30;
        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        return btn;
    }

    // ──────────────────────────────────────────────────────
    // PUBLIC API
    // ──────────────────────────────────────────────────────

    public void ShowGoodEnding(float playTime)
    {
        currentPlayTime = playTime;
        isGoodEnding = true;

        StopGameplayBGM();
        LoadEndingAudioClips();

        if (GameEndUIController.Instance != null)
        {
            GameEndUIController.Instance.ShowEndScreen(playTime);
            PlayOneShot(goodEndingMusic);
        }
        else
        {
            int minutes = Mathf.FloorToInt(playTime / 60f);
            int seconds = Mathf.FloorToInt(playTime % 60f);

            background.color = Color.white;
            mainText.color = new Color(0.1f, 0.1f, 0.1f);
            mainText.fontSize = 52;
            mainText.text = $"Bạn đã được giải thoát.\n\n<size=36>Thời gian: {minutes} phút {seconds} giây</size>";

            retryButton.gameObject.SetActive(false);
            
            if (nameInputField != null)
            {
                nameInputField.gameObject.SetActive(true);
                nameInputField.interactable = true;
                nameInputField.text = ""; // Xoá tên cũ
            }
            if (submitScoreButton != null)
            {
                submitScoreButton.gameObject.SetActive(true);
                submitScoreButton.interactable = true;
                Text btnText = submitScoreButton.GetComponentInChildren<Text>();
                if (btnText != null) btnText.text = "Gửi Bảng Xếp Hạng";
            }

            // Căn giữa nút menu cho good ending
            RectTransform menuRt = menuButton.GetComponent<RectTransform>();
            menuRt.anchorMin = new Vector2(0.35f, 0.08f);
            menuRt.anchorMax = new Vector2(0.65f, 0.22f);

            OpenCanvas();
            PlayOneShot(goodEndingMusic);
        }
    }

    public void ShowBadEnding(string reason)
    {
        float playTime = RoomManager.Instance != null ? RoomManager.Instance.TotalPlayTime : 0f;
        currentPlayTime = playTime;
        isGoodEnding = false;

        StopGameplayBGM();
        LoadEndingAudioClips();

        int minutes = Mathf.FloorToInt(playTime / 60f);
        int seconds = Mathf.FloorToInt(playTime % 60f);

        // Lưu lại thời gian chơi vào PlayerPrefs (Persistence)
        PlayerPrefs.SetFloat("LastPlayTime", playTime);
        PlayerPrefs.Save();

        background.color = new Color(0.04f, 0f, 0f);
        mainText.color = new Color(0.85f, 0f, 0f);
        mainText.fontSize = 64;
        mainText.text = $"GAME OVER\n\n<size=28><color=#cccccc>Thời gian đã chơi: {minutes} phút {seconds} giây</color></size>";

        retryButton.gameObject.SetActive(true);
        RectTransform menuRt = menuButton.GetComponent<RectTransform>();
        menuRt.anchorMin = new Vector2(0.55f, 0.08f);
        menuRt.anchorMax = new Vector2(0.75f, 0.22f);

        if (nameInputField != null)
        {
            nameInputField.gameObject.SetActive(true);
            nameInputField.interactable = true;
            nameInputField.text = ""; // Xoá tên cũ
        }
        if (submitScoreButton != null)
        {
            submitScoreButton.gameObject.SetActive(true);
            submitScoreButton.interactable = true;
            Text btnText = submitScoreButton.GetComponentInChildren<Text>();
            if (btnText != null) btnText.text = "Gửi Bảng Xếp Hạng";
        }

        OpenCanvas();
        PlayOneShot(badEndingZombieSound);
        PlayLoop(badEndingAmbient);
    }

    public void ShowBadEndingTrapped()
    {
        float playTime = RoomManager.Instance != null ? RoomManager.Instance.TotalPlayTime : 0f;
        currentPlayTime = playTime;
        isGoodEnding = false;

        StopGameplayBGM();
        LoadEndingAudioClips();

        int minutes = Mathf.FloorToInt(playTime / 60f);
        int seconds = Mathf.FloorToInt(playTime % 60f);

        // Lưu lại thời gian chơi vào PlayerPrefs (Persistence)
        PlayerPrefs.SetFloat("LastPlayTime", playTime);
        PlayerPrefs.Save();

        background.color = new Color(0.02f, 0f, 0f);
        mainText.color = new Color(0.6f, 0f, 0f);
        mainText.fontSize = 36;
        mainText.text = $"Bạn bị mắc kẹt trong thực thể mãi mãi...\n\nKhông có lối thoát.\n\n<size=28><color=#999999>Thời gian đã chơi: {minutes} phút {seconds} giây</color></size>";

        retryButton.gameObject.SetActive(true);
        RectTransform menuRt = menuButton.GetComponent<RectTransform>();
        menuRt.anchorMin = new Vector2(0.55f, 0.08f);
        menuRt.anchorMax = new Vector2(0.75f, 0.22f);

        if (nameInputField != null)
        {
            nameInputField.gameObject.SetActive(true);
            nameInputField.interactable = true;
            nameInputField.text = ""; // Xoá tên cũ
        }
        if (submitScoreButton != null)
        {
            submitScoreButton.gameObject.SetActive(true);
            submitScoreButton.interactable = true;
            Text btnText = submitScoreButton.GetComponentInChildren<Text>();
            if (btnText != null) btnText.text = "Gửi Bảng Xếp Hạng";
        }

        OpenCanvas();
        PlayOneShot(badEndingZombieSound);
        PlayLoop(badEndingAmbient);
    }

    private void LoadEndingAudioClips()
    {
        if (goodEndingMusic == null)
        {
            goodEndingMusic = Resources.Load<AudioClip>("EndingAudio/goodEndingMusic");
        }
        if (badEndingZombieSound == null)
        {
            badEndingZombieSound = Resources.Load<AudioClip>("EndingAudio/badEndingZombieSound");
        }
        if (badEndingAmbient == null)
        {
            badEndingAmbient = Resources.Load<AudioClip>("EndingAudio/badEndingAmbient");
        }
    }

    private void StopGameplayBGM()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
        }
        if (Room5AudioDirector.Instance != null)
        {
            Room5AudioDirector.Instance.DisableRoom5Audio();
        }
    }

    // ──────────────────────────────────────────────────────
    // INTERNAL
    // ──────────────────────────────────────────────────────

    private void OpenCanvas()
    {
        endCanvas.gameObject.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null) return;
        audioSource.PlayOneShot(clip);
    }

    private void PlayLoop(AudioClip clip)
    {
        if (clip == null) return;
        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.Play();
    }

    private void OnRetry()
    {
        audioSource.Stop();
        Time.timeScale = 1f;

        // Ẩn Canvas kết thúc để không bị kẹt đè lên màn hình
        if (endCanvas != null) endCanvas.gameObject.SetActive(false);

        // Kích hoạt Reset về Room 0 qua RoomManager
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.ResetToRoom0();
        }
        else
        {
            // Dự phòng nếu không tìm thấy RoomManager
            PlayerPrefs.SetInt("CurrentRoom", 0);
            PlayerPrefs.Save();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void OnMenu()
    {
        audioSource.Stop();
        Time.timeScale = 1f;

        // Ẩn Canvas kết thúc để không bị kẹt đè lên màn hình
        if (endCanvas != null) endCanvas.gameObject.SetActive(false);

        SceneManager.LoadScene(0);
    }

    private InputField CreateInputField(Transform parent, string goName, string placeholderText, Font font)
    {
        // 1. Tạo GameObject chính cho InputField
        GameObject ipObj = new GameObject(goName);
        ipObj.transform.SetParent(parent, false);
        Image bgImage = ipObj.AddComponent<Image>();
        bgImage.color = new Color(0.12f, 0.12f, 0.12f, 0.95f); // Nền xám tối
        
        InputField inputField = ipObj.AddComponent<InputField>();
        
        // 2. Tạo Text hiển thị nội dung nhập (Text Component)
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(ipObj.transform, false);
        Text text = textObj.AddComponent<Text>();
        text.font = font;
        text.fontSize = 24;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.supportRichText = false;
        
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(20, 5); // Padding trái 20px
        textRt.offsetMax = new Vector2(-20, -5);

        // 3. Tạo Placeholder hiển thị gợi ý (Placeholder Text)
        GameObject phObj = new GameObject("Placeholder");
        phObj.transform.SetParent(ipObj.transform, false);
        Text phText = phObj.AddComponent<Text>();
        phText.font = font;
        phText.fontSize = 24;
        phText.color = new Color(0.5f, 0.5f, 0.5f, 0.8f); // Màu chữ xám gợi ý
        phText.text = placeholderText;
        phText.alignment = TextAnchor.MiddleLeft;
        phText.fontStyle = FontStyle.Italic;
        
        RectTransform phRt = phObj.GetComponent<RectTransform>();
        phRt.anchorMin = Vector2.zero;
        phRt.anchorMax = Vector2.one;
        phRt.offsetMin = new Vector2(20, 5);
        phRt.offsetMax = new Vector2(-20, -5);

        // 4. Liên kết các thành phần vào InputField
        inputField.textComponent = text;
        inputField.placeholder = phText;
        
        return inputField;
    }

    private void OnSubmitScore()
    {
        string name = nameInputField != null ? nameInputField.text.Trim() : "";
        if (string.IsNullOrEmpty(name))
        {
            name = "Player"; // Tên mặc định nếu bỏ trống
        }

        float playTime = currentPlayTime;

        Debug.Log($"[EndingController] Gửi điểm số '{name}' với thời gian {playTime:F2}s lên Firebase (Hoàn thành: {isGoodEnding})...");

        if (FirebaseDatabaseManager.Instance != null)
        {
            FirebaseDatabaseManager.Instance.SaveLeaderboardScore(name, playTime, isGoodEnding);
        }
        else
        {
            Debug.LogError("[EndingController] Không tìm thấy FirebaseDatabaseManager.Instance!");
        }

        // Đặt cờ quay về màn hình Leaderboard
        PlayerPrefs.SetInt("ShowLeaderboardOnStart", 1);
        PlayerPrefs.Save();

        // Vô hiệu hoá ô nhập tên và nút gửi điểm
        if (nameInputField != null)
        {
            nameInputField.interactable = false;
        }

        if (submitScoreButton != null)
        {
            submitScoreButton.interactable = false;
            Text btnText = submitScoreButton.GetComponentInChildren<Text>();
            if (btnText != null)
            {
                btnText.text = "Đã Gửi Thành Công!";
            }
        }
    }
}
