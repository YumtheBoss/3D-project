using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MobileControls;

/// <summary>
/// Quyển sổ bị rách trang trên bàn ở Room 0.
/// Không yêu cầu nguồn sáng. Hiện từng trang một khi player đọc.
/// </summary>
public class BookPickup : MonoBehaviour
{
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

    [Header("Audio")]
    public AudioClip pageFlipSound;
    public AudioClip pickupSound;
    [Range(0f, 1f)] public float volume = 0.8f;

    // ── Nội dung từng trang ──
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

    private int currentPage = 0;
    private bool hasBeenPickedUp = false;
    private bool isOpen = false;
    private Transform playerTransform;
    private AudioSource audioSource;

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.3f;

        if (bookPanelUI != null) bookPanelUI.SetActive(false);
        if (hintText != null)    hintText.gameObject.SetActive(false);

        if (nextPageButton != null) nextPageButton.onClick.AddListener(NextPage);
        if (closeButton != null)    closeButton.onClick.AddListener(CloseBook);
    }

    private void Update()
    {
        if (hasBeenPickedUp && !isOpen) return;

        if (isOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) CloseBook();
            return;
        }

        if (playerTransform == null) return;
        float dist = Vector3.Distance(transform.position, playerTransform.position);

        if (dist <= pickupRange)
        {
            ShowHint();
            bool input = Input.GetKeyDown(KeyCode.E) || MobileButtons.interactPressed;
            if (input) OpenBook();
        }
        else
        {
            HideHint();
        }
    }

    private void OpenBook()
    {
        hasBeenPickedUp = true;
        isOpen = true;
        currentPage = 0;

        HideHint();
        PlaySound(pickupSound);

        // Ẩn mesh 3D
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;

        if (bookPanelUI == null)
        {
            Debug.LogWarning("[BookPickup] Chưa gán bookPanelUI!");
            return;
        }

        bookPanelUI.SetActive(true);
        ShowPage(currentPage);

        Time.timeScale = 0f;
        if (!Application.isMobilePlatform)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void ShowPage(int index)
    {
        if (pageContentText != null)
            pageContentText.text = PAGES[index];

        if (pageNumberText != null)
            pageNumberText.text = $"Trang {index + 1} / {PAGES.Length}";

        // Nút Next ẩn ở trang cuối
        if (nextPageButton != null)
            nextPageButton.gameObject.SetActive(index < PAGES.Length - 1);

        PlaySound(pageFlipSound);
    }

    private void NextPage()
    {
        if (currentPage < PAGES.Length - 1)
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

        if (!Application.isMobilePlatform)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void ShowHint()
    {
        if (hintText == null) return;
        hintText.gameObject.SetActive(true);
        hintText.text = "Nhấn <b>[E]</b> để nhặt quyển sổ";
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
