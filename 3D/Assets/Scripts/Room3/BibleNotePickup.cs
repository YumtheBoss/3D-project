using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Mảnh ghi chú trong Room 3 — trích từ Kinh Thánh về ánh sáng vs bóng tối.
/// Không nói thẳng "chiếu đèn pin giết quỷ" — dùng ngôn ngữ kinh thánh và
/// ghi chú bút mực hấp tấp ở lề để người chơi tự suy luận cơ chế.
/// </summary>
public class BibleNotePickup : MonoBehaviour
{
    [Header("Tương tác")]
    public float pickupRange = 2.5f;

    [Header("UI — kéo Panel từ Canvas vào đây")]
    public GameObject notePanelUI;
    public TextMeshProUGUI noteContentText;
    public Button closeButton;

    [Header("Hint")]
    public TextMeshProUGUI hintText;

    [Header("Audio")]
    public AudioClip pickupSound;
    [Range(0f, 1f)] public float volume = 0.7f;

    [Header("Đèn thứ 2 — thay thế sau khi đọc ghi chú")]
    [Tooltip("Đèn bị tắt sau khi đóng ghi chú")]
    public Light lightToReplace;

    [Tooltip("Đèn thay thế bật lên (màu đỏ/cam tối — tạo atmosphere mới)")]
    public Light replacementLight;

    [Tooltip("Monologue phát sau khi player đóng ghi chú (\"Ánh sáng là vũ khí...\")")]
    public InnerMonologue postNoteMonologue;

    // ── Nội dung ghi chú Room 3 ─────────────────────────────────
    private const string NOTE_CONTENT =
        "<color=#FFCC88><size=110%><b>✝  Kinh Thánh  ✝</b></size></color>\n\n" +

        "<i>\"Sự sáng chiếu trong tối tăm,\n" +
        "và tối tăm <b>không tiếp nhận</b> sự sáng.\"</i>\n" +
        "<color=#777777><size=80%>— Giăng 1:5 —</size></color>\n\n" +

        "<i>\"Đức Giê-hô-va là <b>sự sáng</b> và sự cứu rỗi của tôi;\n" +
        "tôi sẽ sợ ai?\"</i>\n" +
        "<color=#777777><size=80%>— Thi Thiên 27:1 —</size></color>\n\n" +

        "<i>\"Hãy mặc lấy mọi khí giới của Đức Chúa Trời,\n" +
        "để được đứng vững mà địch cùng mưu kế của ma quỷ.\"</i>\n" +
        "<color=#777777><size=80%>— Ê-phê-sô 6:11 —</size></color>\n\n" +

        "<mark=#1A1A0080>" +
        "<color=#DDCC88><size=90%><b>[ Ghi chú bút chì — nét chữ run rẩy ]</b></size></color>\n\n" +
        "<color=#CCBB77><i>" +
        "Bóng tối không thể tồn tại khi có ánh sáng.\n" +
        "Giữ lấy nguồn sáng — đó là vũ khí.\n\n" +
        "Cần đủ lâu. Nhưng sau đó...\n" +
        "đèn sẽ tắt một lúc.\n\n" +
        "<b>Đừng hoảng loạn.</b>" +
        "</i></color>" +
        "</mark>";

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

        if (hasBeenPickedUp || playerTransform == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        if (dist <= pickupRange)
        {
            ShowHint();
            bool input = Input.GetKeyDown(KeyCode.E);
            if (input)
            {
                OpenNote();
            }
        }
        else
        {
            HideHint();
        }
    }

    private void OpenNote()
    {
        hasBeenPickedUp = true;
        isOpen = true;
        HideHint();

        if (pickupSound != null && audioSource != null)
            audioSource.PlayOneShot(pickupSound, volume);

        if (notePanelUI == null)
        {
            Debug.LogWarning("[BibleNotePickup] Chưa gán notePanelUI!");
            return;
        }

        if (noteContentText != null)
            noteContentText.text = NOTE_CONTENT;

        notePanelUI.SetActive(true);
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

        // Thay thế đèn: tắt đèn cũ, bật đèn mới (chỉ chạy lần đầu khi hasBeenPickedUp)
        if (lightToReplace != null)    lightToReplace.gameObject.SetActive(false);
        if (replacementLight != null)  replacementLight.gameObject.SetActive(true);

        postNoteMonologue?.PlayManually();
    }

    private void ShowHint()
    {
        if (hintText == null) return;
        hintText.gameObject.SetActive(true);
        hintText.text = "Nhấn <b>[E]</b> để nhặt ghi chú";
    }

    private void HideHint()
    {
        if (hintText != null) hintText.gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.9f, 0.8f, 0.3f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}
