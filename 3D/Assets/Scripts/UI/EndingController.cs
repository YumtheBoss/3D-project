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
    private AudioSource audioSource;

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
        int minutes = Mathf.FloorToInt(playTime / 60f);
        int seconds = Mathf.FloorToInt(playTime % 60f);

        background.color = Color.white;
        mainText.color = new Color(0.1f, 0.1f, 0.1f);
        mainText.fontSize = 52;
        mainText.text = $"Bạn đã được giải thoát.\n\n<size=36>Thời gian: {minutes} phút {seconds} giây</size>";

        retryButton.gameObject.SetActive(false);
        // Căn giữa nút menu cho good ending
        RectTransform menuRt = menuButton.GetComponent<RectTransform>();
        menuRt.anchorMin = new Vector2(0.35f, 0.08f);
        menuRt.anchorMax = new Vector2(0.65f, 0.22f);

        OpenCanvas();
        PlayOneShot(goodEndingMusic);
    }

    public void ShowBadEnding(string reason)
    {
        background.color = new Color(0.04f, 0f, 0f);
        mainText.color = new Color(0.85f, 0f, 0f);
        mainText.fontSize = 80;
        mainText.text = "GAME OVER";

        retryButton.gameObject.SetActive(true);
        RectTransform menuRt = menuButton.GetComponent<RectTransform>();
        menuRt.anchorMin = new Vector2(0.55f, 0.08f);
        menuRt.anchorMax = new Vector2(0.75f, 0.22f);

        OpenCanvas();
        PlayOneShot(badEndingZombieSound);
        PlayLoop(badEndingAmbient);
    }

    public void ShowBadEndingTrapped()
    {
        background.color = new Color(0.02f, 0f, 0f);
        mainText.color = new Color(0.6f, 0f, 0f);
        mainText.fontSize = 44;
        mainText.text = "Bạn bị mắc kẹt trong thực thể mãi mãi...\n\nKhông có lối thoát.";

        retryButton.gameObject.SetActive(true);
        RectTransform menuRt = menuButton.GetComponent<RectTransform>();
        menuRt.anchorMin = new Vector2(0.55f, 0.08f);
        menuRt.anchorMax = new Vector2(0.75f, 0.22f);

        OpenCanvas();
        PlayOneShot(badEndingZombieSound);
        PlayLoop(badEndingAmbient);
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
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnMenu()
    {
        audioSource.Stop();
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
}
