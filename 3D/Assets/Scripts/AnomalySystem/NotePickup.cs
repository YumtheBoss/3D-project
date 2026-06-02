using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gắn vào một GameObject (mẩu giấy 3D) đặt trong R3.
/// Khi người chơi đến gần và có nguồn sáng → có thể nhặt mẩu giấy.
/// Hiện popup UI với nội dung nhật ký kinh dị, có phần highlight.
/// </summary>
public class NotePickup : MonoBehaviour
{
    [Header("Cài đặt tương tác")]
    [Tooltip("Khoảng cách tối đa để tương tác với mẩu giấy (mét)")]
    public float pickupRange = 2.5f;

    [Header("Yêu cầu nguồn sáng")]
    [Tooltip("Script R3FlickerEvent trong scene — dùng để biết đèn phòng có đang sáng không")]
    public AnomalySystem.R3FlickerEvent r3FlickerEvent;
    [Tooltip("Script FlashlightController trên Player — dùng để biết đèn pin có bật không")]
    public FlashlightController flashlightController;

    [Header("UI - Panel mẩu giấy")]
    [Tooltip("Panel UI chứa toàn bộ nội dung mẩu giấy (tạo trong Canvas)")]
    public GameObject notePanelUI;
    [Tooltip("TextMeshPro chứa nội dung chính của mẩu giấy")]
    public TextMeshProUGUI noteContentText;
    [Tooltip("Nút 'Đóng' trong panel")]
    public Button closeButton;

    [Header("UI - Thông báo nhỏ")]
    [Tooltip("TextMeshPro hiển thị chữ 'Nhấn F để nhặt' / 'Quá tối...' khi gần mẩu giấy")]
    public TextMeshProUGUI hintText;

    [Header("Âm thanh")]
    [Tooltip("Tiếng nhặt giấy (xào xạc)")]
    public AudioClip pickupSound;
    [Range(0f, 1f)]
    public float pickupVolume = 0.8f;

    // ---- Trạng thái ----
    private bool hasBeenPickedUp = false;
    private bool isNoteOpen = false;
    private Transform playerTransform;
    private AudioSource audioSource;

    // ---- Nội dung mẩu giấy (nhật ký kinh dị tiếng Việt) ----
    // Dùng Rich Text của TextMeshPro: <mark=yellow>...</mark> để highlight
    private const string NOTE_CONTENT =
        "<size=110%><b>— Nhật ký của người đi trước —</b></size>\n\n" +
        "Ngày... tôi không còn nhớ là ngày mấy nữa.\n\n" +
        "Tôi đã cố chạy trong bóng tối. Đó là sai lầm lớn nhất.\n\n" +
        "<i>Nó không nhìn thấy bạn bằng mắt.</i>\n" +
        "<i>Nó cảm nhận sự sợ hãi. Nó cảm nhận bóng tối.</i>\n\n" +
        "Khi tôi tắt đèn để trốn — nó đến gần hơn.\n" +
        "Khi tôi bật đèn lên — nó... dừng lại.\n\n" +
        "<mark=#FFFF00AA><b>→ PHẢI CÓ NGUỒN SÁNG.</b>\n" +
        "Đèn phòng. Đèn pin. Bất cứ thứ gì.\n" +
        "Đừng bao giờ để mình chìm vào bóng tối hoàn toàn.</mark>\n\n" +
        "Tôi không biết mình còn bao nhiêu thời gian.\n" +
        "Nếu ai đó tìm thấy tờ giấy này...\n\n" +
        "<color=#CC0000><i>...hãy đừng tắt đèn.</i></color>\n\n" +
        "<size=70%><color=#888888>— Chữ viết bị nhòe, không đọc được tên người viết —</color></size>";

    private void Start()
    {
        // Tìm player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;

            // Tự tìm FlashlightController nếu chưa gán
            if (flashlightController == null)
                flashlightController = playerObj.GetComponentInChildren<FlashlightController>();
        }

        // AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.5f; // Bán 3D để tiếng xào xạc tự nhiên

        // Ẩn panel khi mới vào
        if (notePanelUI != null) notePanelUI.SetActive(false);
        if (hintText != null) hintText.gameObject.SetActive(false);

        // Gán nội dung cho Text
        if (noteContentText != null)
            noteContentText.text = NOTE_CONTENT;

        // Gán sự kiện nút Đóng
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseNote);

        // Tự tìm R3FlickerEvent nếu chưa gán
        if (r3FlickerEvent == null)
            r3FlickerEvent = FindAnyObjectByType<AnomalySystem.R3FlickerEvent>();
    }

    private void Update()
    {
        // Đã nhặt rồi thì không cần xử lý
        if (hasBeenPickedUp) return;

        // Nếu note đang mở, chặn input khác
        if (isNoteOpen)
        {
            // Nhấn phím bất kỳ hoặc chạm màn hình để đóng
            if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.E) ||
                Input.GetKeyDown(KeyCode.Escape))
            {
                CloseNote();
            }
            return;
        }

        // Kiểm tra khoảng cách đến player
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                playerTransform = playerObj.transform;
            return;
        }

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        bool isNear = dist <= pickupRange;

        if (isNear)
        {
            bool hasLight = CheckHasLightSource();
            ShowHint(hasLight);

            bool pickupInput = Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.E);

            if (pickupInput)
            {
                if (hasLight)
                {
                    PickUpNote();
                }
                else
                {
                    // Thông báo "quá tối"
                    StartCoroutine(ShowTooDarkMessage());
                }
            }
        }
        else
        {
            HideHint();
        }
    }

    // ────── Kiểm tra nguồn sáng ──────
    private bool CheckHasLightSource()
    {
        // 1. Đèn pin đang bật?
        if (flashlightController != null && flashlightController.IsOn())
            return true;

        // 2. Đèn phòng trong R3 đang không bị sự kiện tắt hoàn toàn?
        //    (R3FlickerEvent nhấp nháy nên đèn vẫn có thể sáng một phần)
        //    Ta dùng cách đơn giản: kiểm tra ánh sáng ambient đủ sáng không
        float ambientBrightness = RenderSettings.ambientLight.grayscale;
        if (ambientBrightness > 0.05f)
            return true;

        // 3. Kiểm tra trực tiếp đèn trong R3 có đang sáng ≥ 20% không
        if (r3FlickerEvent != null && r3FlickerEvent.r3Lights != null)
        {
            foreach (var light in r3FlickerEvent.r3Lights)
            {
                if (light != null && light.gameObject.activeSelf && light.intensity > 0.2f)
                    return true;
            }
        }

        return false;
    }

    // ────── Hành động nhặt giấy ──────
    private void PickUpNote()
    {
        hasBeenPickedUp = true;
        isNoteOpen = true;

        // Ẩn gợi ý
        HideHint();

        // Phát tiếng xào xạc
        if (pickupSound != null && audioSource != null)
            audioSource.PlayOneShot(pickupSound, pickupVolume);

        // Ẩn vật thể 3D trong thế giới (đã nhặt lên rồi)
        // Chỉ ẩn Renderer và Collider, không destroy ngay để audio còn phát
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Hiện panel mẩu giấy
        OpenNote();

        Debug.Log("[NotePickup] Người chơi đã nhặt mẩu giấy trong R3.");
    }

    // ────── UI mẩu giấy ──────
    private void OpenNote()
    {
        if (notePanelUI == null)
        {
            Debug.LogWarning("[NotePickup] Chưa gán notePanelUI! Hãy tạo Panel UI và kéo vào Inspector.");
            return;
        }

        notePanelUI.SetActive(true);
        isNoteOpen = true;

        // Dừng thời gian khi đang đọc giấy (tuỳ chọn)
        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        FirstPersonController.IsUIOpen = true;
    }

    public void CloseNote()
    {
        if (notePanelUI != null)
            notePanelUI.SetActive(false);

        isNoteOpen = false;

        // Tiếp tục thời gian
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        FirstPersonController.IsUIOpen = false;

        Debug.Log("[NotePickup] Đóng mẩu giấy.");
    }

    // ────── Hiển thị hint ──────
    private void ShowHint(bool hasLight)
    {
        if (hintText == null) return;
        hintText.gameObject.SetActive(true);

        if (hasLight)
            hintText.text = "Nhấn <b>[F]</b> / <b>[E]</b> để nhặt mẩu giấy";
        else
            hintText.text = "<color=#FF6666>Quá tối để đọc... cần có nguồn sáng</color>";
    }

    private void HideHint()
    {
        if (hintText != null)
            hintText.gameObject.SetActive(false);
    }

    private IEnumerator ShowTooDarkMessage()
    {
        if (hintText == null) yield break;

        hintText.text = "<color=#FF4444><b>Tối quá, không thể đọc được!</b></color>";
        yield return new WaitForSeconds(2f);
        hintText.text = "<color=#FF6666>Quá tối để đọc... cần có nguồn sáng</color>";
    }

    // Vẽ phạm vi tương tác trong Editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}
