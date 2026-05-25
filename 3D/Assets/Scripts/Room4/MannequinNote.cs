using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tờ giấy trên tay ma nơ canh trong Room 4.
/// Player đến gần [E] → đọc nội dung → đóng → phát inner monologue.
/// </summary>
public class MannequinNote : MonoBehaviour
{
    [Header("Tương tác")]
    public float pickupRange = 2.5f;

    [Header("UI — kéo Panel từ Canvas vào đây")]
    public GameObject notePanelUI;
    public TextMeshProUGUI noteContentText;
    public Button closeButton;
    public TextMeshProUGUI hintText;

    [Header("Sau khi đọc xong")]
    [Tooltip("InnerMonologue phát sau khi player đóng tờ giấy")]
    public InnerMonologue postReadMonologue;

    [Header("Audio")]
    public AudioClip pickupSound;
    [Range(0f, 1f)] public float volume = 0.7f;

    // ── Nội dung tờ giấy Room 4 ──────────────────────────────────
    private const string NOTE_CONTENT =
        "<color=#FF4444><size=115%><b>— Cảnh báo —</b></size></color>\n\n" +

        "<i>Chúng tôi đã thử đến gần con gấu.\n" +
        "Mười hai người. Không ai trở lại.\n\n" +
        "Nó đang chờ.\n" +
        "Nó <b>luôn</b> chờ ở đó.</i>\n\n" +

        "<mark=#3A000080>" +
        "<color=#FF9999>" +
        "Nếu bạn muốn sống —\n" +
        "hãy chạy qua cánh cửa cuối\n" +
        "và <b>đừng nhìn lại.</b>\n\n" +
        "Dù bạn nghe thấy gì.\n" +
        "Dù bạn cảm thấy gì.\n\n" +
        "<size=85%><b>ĐỪNG NHÌN LẠI.</b></size>" +
        "</color>" +
        "</mark>\n\n" +
        "<color=#666666><size=75%>— Chữ viết run rẩy, mực nhòe —</size></color>";

    private bool isOpen = false;
    private bool hasBeenRead = false;
    private Transform playerTransform;
    private AudioSource audioSource;

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake  = false;
        audioSource.spatialBlend = 0.3f;

        if (notePanelUI != null) notePanelUI.SetActive(false);
        if (hintText != null)    hintText.gameObject.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(CloseNote);
    }

    private void Update()
    {
        if (isOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) CloseNote();
            return;
        }

        if (playerTransform == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        if (dist <= pickupRange)
        {
            ShowHint();
            if (Input.GetKeyDown(KeyCode.E)) OpenNote();
        }
        else
        {
            HideHint();
        }
    }

    private void OpenNote()
    {
        isOpen = true;
        HideHint();

        if (pickupSound != null && audioSource != null)
            audioSource.PlayOneShot(pickupSound, volume);

        if (noteContentText != null) noteContentText.text = NOTE_CONTENT;
        if (notePanelUI != null)     notePanelUI.SetActive(true);

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseNote()
    {
        if (notePanelUI != null) notePanelUI.SetActive(false);
        isOpen = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (!hasBeenRead)
        {
            hasBeenRead = true;
            postReadMonologue?.PlayManually();
        }
    }

    private void ShowHint()
    {
        if (hintText == null) return;
        hintText.gameObject.SetActive(true);
        hintText.text = "Nhấn <b>[E]</b> để đọc tờ giấy";
    }

    private void HideHint()
    {
        if (hintText != null) hintText.gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}
