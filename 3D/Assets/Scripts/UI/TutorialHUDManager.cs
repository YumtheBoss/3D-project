using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý hiển thị các HUD hướng dẫn chơi (Tutorial HUD) đa giai đoạn.
/// Tự động dựng giao diện UI (Canvas, Text, Background) tại runtime (Self-Healing UI).
/// </summary>
public class TutorialHUDManager : MonoBehaviour
{
    public enum TutorialType
    {
        MovementAndZoom, // WASD di chuyển + Chuột phải zoom (Màn 0)
        EquipTeddy,      // Phím Q trang bị gấu + G bật/tắt hào quang (Màn 4 khi nhặt gấu)
        Sprint           // Giữ Shift chạy nhanh (Khi quỷ rượt đuổi)
    }

    private static TutorialHUDManager _instance;
    public static TutorialHUDManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<TutorialHUDManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("_TutorialHUDManager_Auto");
                    _instance = go.AddComponent<TutorialHUDManager>();
                }
            }
            return _instance;
        }
    }

    private Canvas tutorialCanvas;
    private CanvasGroup canvasGroup;
    private Image backgroundBox;
    private Text tutorialText;

    private TutorialType currentType;
    private bool isShowing = false;
    private float showTime = 0f; // Thời điểm bắt đầu hiển thị hướng dẫn
    private Coroutine activeFadeRoutine;
    private Coroutine autoHideRoutine;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            BuildUI();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

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
        // Nhận diện Room 0 bằng cách kiểm tra tên Scene hoặc trạng thái RoomManager
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool isRoom0 = sceneName == "SampleScene" || sceneName == "Room0" || 
                       (RoomManager.Instance != null && RoomManager.Instance.CurrentRoom == RoomManager.RoomState.Room0);

        if (isRoom0)
        {
            ShowTutorial(TutorialType.MovementAndZoom);
        }
    }

    private void HandleRoomEntered(RoomManager.RoomState room)
    {
        if (room == RoomManager.RoomState.Room0)
        {
            ShowTutorial(TutorialType.MovementAndZoom);
        }
    }

    private void Update()
    {
        if (!isShowing) return;

        // Tránh tắt nhầm hướng dẫn quá nhanh khi đang thao tác phím (cho người chơi đọc ít nhất 1.5 giây)
        if (Time.time - showTime < 1.5f) return;

        // Tự động tắt hướng dẫn nhanh khi phát hiện người chơi đã thao tác thành công
        switch (currentType)
        {
            case TutorialType.MovementAndZoom:
                // Nếu bấm nút di chuyển hoặc zoom
                if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) || 
                    Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D) || 
                    Input.GetKeyDown(KeyCode.Mouse1))
                {
                    HideTutorial();
                }
                break;

            case TutorialType.EquipTeddy:
                // Nếu bấm Q trang bị gấu bông
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    HideTutorial();
                }
                break;

            case TutorialType.Sprint:
                // Nếu giữ Shift để chạy nhanh
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    HideTutorial();
                }
                break;
        }
    }

    /// <summary>
    /// Hiển thị HUD hướng dẫn cụ thể trên màn hình.
    /// </summary>
    public void ShowTutorial(TutorialType type)
    {
        // Nếu là hướng dẫn di chuyển ở Room 0, ta trì hoãn hiển thị để nhường chỗ cho Độc thoại nội tâm
        if (type == TutorialType.MovementAndZoom)
        {
            StartCoroutine(ShowMovementTutorialWithDelay());
        }
        else
        {
            ShowTutorialDirect(type);
        }
    }

    private void ShowTutorialDirect(TutorialType type)
    {
        if (tutorialText == null) BuildUI();

        currentType = type;
        isShowing = true;
        showTime = Time.time; // Ghi nhận thời gian hiển thị

        string title = "";
        string desc = "";

        switch (type)
        {
            case TutorialType.MovementAndZoom:
                title = "<color=#ffcc00><b>HƯỚNG DẪN DI CHUYỂN & QUAN SÁT</b></color>";
                desc = "• Dùng phím <b>W, A, S, D</b> để di chuyển nhân vật\n• Giữ <b>Chuột Phải [Mouse 1]</b> để phóng to tầm nhìn (Zoom)";
                break;

            case TutorialType.EquipTeddy:
                title = "<color=#ff9900><b>TRANG BỊ LÁ CHẮN TÂM LINH</b></color>";
                desc = "• Nhấn phím <b>Q</b> để Rút / Cất Gấu Bông phát sáng\n• Nhấn phím <b>G</b> để Bật / Tắt Hào Quang thanh tẩy phong ấn";
                break;

            case TutorialType.Sprint:
                title = "<color=#ff3333><b>CHẠY NHANH SINH TỒN (RƯỢT ĐUỔI)</b></color>";
                desc = "• Giữ phím <b>Shift Trái</b> khi di chuyển để chạy nhanh thoát thân\n• <i>Lưu ý: Chạy nhanh sẽ tiêu hao Thể Lực (Stamina Bar phía dưới)</i>";
                break;
        }

        tutorialText.text = $"{title}\n{desc}";

        // Chạy hiệu ứng Fade In
        if (activeFadeRoutine != null) StopCoroutine(activeFadeRoutine);
        activeFadeRoutine = StartCoroutine(FadeCanvasGroup(1f, 0.4f));

        // Tự động ẩn sau 10 giây nếu người chơi không bấm phím
        if (autoHideRoutine != null) StopCoroutine(autoHideRoutine);
        autoHideRoutine = StartCoroutine(AutoHideDelay(10f));
    }

    /// <summary>
    /// Trì hoãn hiển thị hướng dẫn di chuyển cho đến khi Độc thoại nội tâm (Inner Monologue) hoàn tất.
    /// Tránh trường hợp hai thông báo hiển thị đè nhau hoặc người chơi bấm nút qua hội thoại tắt nhầm hướng dẫn.
    /// </summary>
    private IEnumerator ShowMovementTutorialWithDelay()
    {
        // Chờ 1.5 giây đầu để độc thoại nội tâm khởi chạy xong (nếu có)
        yield return new WaitForSeconds(1.5f);

        // Chờ độc thoại nội tâm kết thúc hoàn toàn (giới hạn tối đa 10 giây)
        float timeout = 10f;
        float elapsed = 0f;
        while (elapsed < timeout)
        {
            InnerMonologue activeMono = FindAnyObjectByType<InnerMonologue>();
            if (activeMono == null || !activeMono.IsRunning)
            {
                break;
            }
            elapsed += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }

        // Sau khi độc thoại kết thúc hoặc hết thời gian chờ, mới hiển thị hướng dẫn di chuyển
        ShowTutorialDirect(TutorialType.MovementAndZoom);
    }

    /// <summary>
    /// Ẩn HUD hướng dẫn đi.
    /// </summary>
    public void HideTutorial()
    {
        if (!isShowing) return;
        isShowing = false;

        if (activeFadeRoutine != null) StopCoroutine(activeFadeRoutine);
        activeFadeRoutine = StartCoroutine(FadeCanvasGroup(0f, 0.3f));

        if (autoHideRoutine != null) StopCoroutine(autoHideRoutine);
    }

    private IEnumerator AutoHideDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideTutorial();
    }

    private IEnumerator FadeCanvasGroup(float targetAlpha, float duration)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }

    private void BuildUI()
    {
        // 1. Tạo Canvas
        GameObject canvasObj = new GameObject("_TutorialHUDCanvas_Auto");
        canvasObj.transform.SetParent(transform, false);
        
        tutorialCanvas = canvasObj.AddComponent<Canvas>();
        tutorialCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        tutorialCanvas.sortingOrder = 9998; // Vẽ đè lên hầu hết các UI khác

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f; // Bắt đầu ở trạng thái ẩn

        // 2. Tạo Hộp Nền (Background Box)
        GameObject bgObj = new GameObject("BackgroundBox");
        bgObj.transform.SetParent(canvasObj.transform, false);

        backgroundBox = bgObj.AddComponent<Image>();
        backgroundBox.color = new Color(0.05f, 0.05f, 0.05f, 0.75f); // Đen mờ 75%

        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0.5f, 0.28f); // Đặt ở chính giữa phía dưới màn hình
        bgRt.anchorMax = new Vector2(0.5f, 0.28f);
        bgRt.pivot = new Vector2(0.5f, 0.5f);
        bgRt.sizeDelta = new Vector2(620f, 120f); // Kích thước hộp hướng dẫn

        // 3. Tạo Viền Neon Mỏng (Màu xám/trắng)
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(bgObj.transform, false);
        Image borderImg = borderObj.AddComponent<Image>();
        borderImg.color = new Color(0.4f, 0.4f, 0.4f, 0.5f); // Viền xám nhạt
        RectTransform borderRt = borderObj.GetComponent<RectTransform>();
        borderRt.anchorMin = Vector2.zero;
        borderRt.anchorMax = Vector2.one;
        borderRt.offsetMin = new Vector2(-2, -2); // Tạo viền ngoài 2px
        borderRt.offsetMax = new Vector2(2, 2);
        borderObj.transform.SetAsFirstSibling(); // Cho nằm dưới nền để làm viền

        // 4. Tạo Text Hướng Dẫn
        GameObject textObj = new GameObject("TutorialText");
        textObj.transform.SetParent(bgObj.transform, false);

        tutorialText = textObj.AddComponent<Text>();
        tutorialText.alignment = TextAnchor.MiddleCenter;
        tutorialText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tutorialText.fontSize = 20;
        tutorialText.color = Color.white;
        tutorialText.lineSpacing = 1.3f;
        tutorialText.supportRichText = true;

        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(20, 10); // Padding xung quanh
        textRt.offsetMax = new Vector2(-20, -10);
    }
}
