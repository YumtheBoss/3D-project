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
        "<color=#FF4444><size=115%><b>— Ghi chép khẩn thiết —</b></size></color>\n\n" +
        "<i>Mười hai người... không ai có thể thoát ra ngoài.\n" +
        "Gấu bông phát sáng là lá chắn tâm linh duy nhất giúp bạn chống chọi với lũ quỷ.\n\n" +
        "Khi soi hào quang bảo vệ của Gấu bông vào các nguồn <b>phong ấn tà ác</b> ở căn phòng tiếp theo, phong ấn sẽ bị thanh tẩy.\n\n" +
        "<mark=#3A000080><color=#FF9999>" +
        "Nhưng hãy cẩn thận!\n" +
        "Tà khí bùng phát khi thanh tẩy sẽ đánh động và thu hút quỷ dữ ở cả 2 tầng lao thẳng tới bạn!\n" +
        "Hãy giữ vững lá chắn, giải phóng cả 3 đàn tế phong ấn thì lối thoát hiểm mới mở!</color></mark></i>";

    private bool isOpen = false;
    private bool hasBeenRead = false;
    private Transform playerTransform;
    private AudioSource audioSource;
    private bool isHintShown = false;

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
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        if (isOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) CloseNote();
            return;
        }

        if (playerTransform == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        if (dist <= pickupRange)
        {
            if (!isHintShown)
            {
                isHintShown = true;
                ShowHint();
            }

            if (Input.GetKeyDown(KeyCode.E)) OpenNote();
        }
        else
        {
            if (isHintShown)
            {
                isHintShown = false;
                HideHint();
            }
        }
    }

    private void OpenNote()
    {
        isOpen = true;
        HideHint();

        // Thêm Tờ Giấy Cảnh Báo vào túi đồ
        AnomalySystem.InventoryManager.Instance?.AddItem("MannequinNote_Room4");

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
        if (!isOpen) return;

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

        // Biến mất hoàn toàn sau khi nhặt/đọc xong
        gameObject.SetActive(false);
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
