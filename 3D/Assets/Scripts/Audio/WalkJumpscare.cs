using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn script này vào bất kỳ GameObject nào trong Scene.
/// Khi người chơi giữ phím W liên tục 5 giây -> hiện hình ảnh kèm âm thanh.
/// </summary>
public class WalkJumpscare : MonoBehaviour
{
    [Header("Cài đặt thời gian")]
    [Tooltip("Số giây người chơi phải giữ W liên tục trước khi bị hù")]
    public float holdDuration = 5f;

    [Header("Hình ảnh Jumpscare")]
    [Tooltip("Kéo cái Image (con ma) trong Hierarchy vào đây")]
    public GameObject jumpscareImage;

    [Header("Âm thanh Jumpscare")]
    [Tooltip("Kéo AudioSource (nằm trên GameObject cha luôn bật) vào đây")]
    public AudioSource audioSource;
    [Tooltip("File âm thanh tiếng hét")]
    public AudioClip jumpscareSound;

    [Header("Thời gian hiển thị")]
    [Tooltip("Hình ảnh sẽ tự biến mất sau bao nhiêu giây (0 = không tự tắt)")]
    public float displayDuration = 2f;

    // ---- Biến nội bộ ----
    private float holdTimer = 0f;
    private bool hasTriggered = false;
    private float hideTimer = 0f;
    private bool isShowing = false;

    private void Update()
    {
        HandleHoldDetection();
        HandleAutoHide();
    }

    private void HandleHoldDetection()
    {
        // Nếu đã kích hoạt rồi thì không đếm nữa
        if (hasTriggered) return;

        if (Input.GetKey(KeyCode.W))
        {
            holdTimer += Time.deltaTime;

            // Đủ thời gian -> kích hoạt jumpscare
            if (holdTimer >= holdDuration)
            {
                TriggerJumpscare();
            }
        }
        else
        {
            // Người chơi nhả W -> reset bộ đếm
            holdTimer = 0f;
        }
    }

    private void TriggerJumpscare()
    {
        hasTriggered = true;
        isShowing = true;

        // Hiện hình ảnh
        if (jumpscareImage != null)
        {
            jumpscareImage.SetActive(true);
        }

        // Phát âm thanh
        if (audioSource != null && jumpscareSound != null)
        {
            audioSource.PlayOneShot(jumpscareSound);
        }

        // Bắt đầu đếm ngược để tự ẩn
        if (displayDuration > 0f)
        {
            hideTimer = displayDuration;
        }

        Debug.Log("[WalkJumpscare] Jumpscare kích hoạt!");
    }

    private void HandleAutoHide()
    {
        if (!isShowing || displayDuration <= 0f) return;

        hideTimer -= Time.deltaTime;

        if (hideTimer <= 0f)
        {
            isShowing = false;

            // Ẩn hình ảnh
            if (jumpscareImage != null)
            {
                jumpscareImage.SetActive(false);
            }

            // Cho phép kích hoạt lại (tuỳ chọn: bỏ dòng này nếu chỉ muốn xảy ra 1 lần)
            hasTriggered = false;
            holdTimer = 0f;
        }
    }
}
