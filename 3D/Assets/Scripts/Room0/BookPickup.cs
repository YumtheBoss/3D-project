using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quyển sổ bị rách trang trên bàn ở Room 0.
/// Trang 1 đọc được ngay. Trang 2-3 yêu cầu nhặt TornPagePickup tương ứng trước.
/// Khi tìm được trang mới, sổ hiện lại trên bàn để player quay về đọc.
/// </summary>
public class BookPickup : MonoBehaviour
{
    // ── Static events & unlock system ────────────────────────────
    /// <summary>Phát khi player mở sổ lần đầu — dùng để unlock TornPagePickup.</summary>
    public static event System.Action OnBookRead;

    /// <summary>Phát khi một trang mới được mở khóa.</summary>
    public static event System.Action OnPageUnlocked;

    private static int s_unlockedPages = 1;

    /// <summary>Gọi từ TornPagePickup khi player nhặt được trang xé.</summary>
    public static void UnlockNextPage()
    {
        s_unlockedPages++;
        OnPageUnlocked?.Invoke();
    }

    // ── Inspector fields ─────────────────────────────────────────
    [Header("Tương tác")]
    public float pickupRange = 2f;

    [Header("UI")]
    public GameObject bookPanelUI;
    public TextMeshProUGUI pageContentText;
    public TextMeshProUGUI pageNumberText;
    public Button nextPageButton;
    public Button closeButton;

    [Header("Hint")]
    public TextMeshProUGUI hintText;

    [Header("Font")]
    [Tooltip("Kéo TMP Font Asset vào đây để áp dụng font riêng cho nội dung sổ")]
    public TMP_FontAsset pageFont;

    [Header("Audio")]
    public AudioClip pageFlipSound;
    public AudioClip pickupSound;
    [Range(0f, 1f)] public float volume = 0.8f;

    // ── Nội dung từng trang ──────────────────────────────────────
    private static readonly string[] PAGES = new string[]
    {
        // Trang 1
        "<size=115%><b>— Nhật ký —</b></size>\n\n" +
        "Tôi không biết mình đã ở đây bao lâu rồi.\n\n" +
        "Khi tỉnh dậy, tôi thấy mình nằm trong căn phòng này.\n" +
        "Không có cửa sổ. Không có đồng hồ.\n" +
        "Và cánh cửa... nó dẫn đến đúng căn phòng này.\n\n" +
        "<color=#AAAAAA><i>— Nhiều dòng bị xé mất —</i></color>",

        // Trang 2
        "<size=115%><b>— Nhật ký (tiếp) —</b></size>\n\n" +
        "Tôi đã đếm. Đây là lần thứ mười bảy tôi bước qua cánh cửa đó.\n\n" +
        "Nhưng lần này... có gì đó <i>khác</i>.\n" +
        "Chiếc ghế. Nó không ở chỗ cũ.\n\n" +
        "<mark=#FFFF00AA><b>→ Căn phòng thay đổi giữa các lần đi qua.\n" +
        "Phải quan sát thật kỹ.</b></mark>\n\n" +
        "<color=#AAAAAA><i>— Trang bị xé không đều, mực nhòe —</i></color>",

        // Trang 3
        "<size=115%><b>— Nhật ký (tiếp) —</b></size>\n\n" +
        "Tôi nghĩ mình hiểu ra rồi.\n\n" +
        "Khi có <b>sự thay đổi</b> — phải quay lại.\n" +
        "Khi <b>mọi thứ vẫn như cũ</b> — tiếp tục đi.\n\n" +
        "<color=#CC0000><i>Đừng để nó đánh lừa bạn.\n" +
        "Nó rất giỏi giả vờ bình thường.</i></color>\n\n" +
        "<color=#AAAAAA><size=75%>— Phần còn lại của quyển sổ bị xé sạch —</size></color>",
    };

    // ── Private state ────────────────────────────────────────────
    private int currentPage = 0;
    private bool bookEventFired = false;
    private bool isOpen = false;
    private bool hasBeenPickedUp = false;
    private Transform playerTransform;
    private AudioSource audioSource;
    private PickupGlow pickupGlow;

    // ── Lifecycle ────────────────────────────────────────────────

    private void OnEnable()
    {
        OnPageUnlocked += HandlePageUnlocked;
        RoomManager.OnRoomEntered += HandleRoomEntered;
    }

    private void OnDisable()
    {
        OnPageUnlocked -= HandlePageUnlocked;
        RoomManager.OnRoomEntered -= HandleRoomEntered;
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.3f;

        pickupGlow = GetComponent<PickupGlow>();

        if (bookPanelUI != null) bookPanelUI.SetActive(false);
        if (hintText != null)    hintText.gameObject.SetActive(false);
        if (nextPageButton != null) nextPageButton.onClick.AddListener(NextPage);
        if (closeButton != null)    closeButton.onClick.AddListener(CloseBook);
    }

    // ── Event handlers ───────────────────────────────────────────

    private void HandlePageUnlocked()
    {
        if (isOpen)
        {
            // Làm mới nút Next ngay lập tức nếu đang đọc sổ
            RefreshNextButton();
        }
        else if (RoomManager.Instance?.CurrentRoom == RoomManager.RoomState.Room0)
        {
            // Chỉ hiện lại sổ nếu đang ở Room 0 — báo hiệu có trang mới
            SetMeshVisible(true);
            hasBeenPickedUp = false;
            pickupGlow?.RestartGlow();
        }
    }

    private void HandleRoomEntered(RoomManager.RoomState room)
    {
        if (room == RoomManager.RoomState.Room0)
        {
            // Bắt đầu game mới — hiện lại sổ, reset toàn bộ
            s_unlockedPages = 1;
            bookEventFired = false;
            hasBeenPickedUp = false;
            SetMeshVisible(true);
            pickupGlow?.RestartGlow();
        }
        else if (room == RoomManager.RoomState.Room1 || room == RoomManager.RoomState.Room2)
        {
            // Ẩn sổ khi rời Room 0 — sổ chỉ tồn tại trong Room 0
            if (!hasBeenPickedUp)
            {
                hasBeenPickedUp = true;
                pickupGlow?.StopGlow();
            }
            SetMeshVisible(false);
        }
    }

    private void SetMeshVisible(bool visible)
    {
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = visible;
    }

    // ── Update ───────────────────────────────────────────────────

    private void Update()
    {
        if (isOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) CloseBook();
            return;
        }

        if (hasBeenPickedUp || playerTransform == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        if (dist <= pickupRange)
        {
            ShowHint();
            bool input = Input.GetKeyDown(KeyCode.E);
            if (input)
            {
                OpenBook();
            }
        }
        else
        {
            HideHint();
        }
    }

    // ── Book logic ───────────────────────────────────────────────

    private void OpenBook()
    {
        hasBeenPickedUp = true;
        isOpen = true;
        currentPage = 0;

        HideHint();
        PlaySound(pickupSound);
        pickupGlow?.StopGlow();

        if (!bookEventFired)
        {
            bookEventFired = true;
            OnBookRead?.Invoke();
        }

        SetMeshVisible(false);

        if (bookPanelUI == null)
        {
            Debug.LogWarning("[BookPickup] Chưa gán bookPanelUI!");
            return;
        }

        bookPanelUI.SetActive(true);
        ShowPage(currentPage);

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ShowPage(int index)
    {
        if (pageContentText != null)
        {
            if (pageFont != null) pageContentText.font = pageFont;
            pageContentText.text = PAGES[index];
        }

        if (pageNumberText != null)
            pageNumberText.text = $"Trang {index + 1} / {s_unlockedPages}";

        RefreshNextButton();
        PlaySound(pageFlipSound);
    }

    private void RefreshNextButton()
    {
        if (nextPageButton == null) return;

        bool hasNext = currentPage < PAGES.Length - 1;
        bool nextUnlocked = (currentPage + 1) < s_unlockedPages;

        nextPageButton.gameObject.SetActive(hasNext);
        nextPageButton.interactable = nextUnlocked;

        // Đổi nhãn nút để player biết cần tìm thêm trang
        var label = nextPageButton.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
            label.text = nextUnlocked ? "Trang tiếp →" : "Tìm thêm trang...";
    }

    private void NextPage()
    {
        if (currentPage < PAGES.Length - 1 && (currentPage + 1) < s_unlockedPages)
        {
            currentPage++;
            ShowPage(currentPage);
        }
    }

    public void CloseBook()
    {
        if (bookPanelUI != null) bookPanelUI.SetActive(false);
        isOpen = false;
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // ── UI helpers ───────────────────────────────────────────────

    private void ShowHint()
    {
        if (hintText == null) return;
        hintText.gameObject.SetActive(true);
        hintText.text = "Nhấn <b>[E]</b> để đọc quyển sổ";
    }

    private void HideHint()
    {
        if (hintText != null) hintText.gameObject.SetActive(false);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip, volume);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}
