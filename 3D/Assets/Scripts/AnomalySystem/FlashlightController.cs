using System.Collections;
using UnityEngine;
using MobileControls;

/// <summary>
/// Script đèn pin điện thoại. Gắn vào Player hoặc Camera.
/// Tự động tạo một Spot Light gắn trên camera và toggle bật/tắt.
/// PC: Phím F | Mobile: Nút FlashlightButton
/// </summary>
public class FlashlightController : MonoBehaviour
{
    [Header("Cài đặt đèn pin")]
    [Tooltip("Camera của người chơi. Để trống thì script tự tìm Camera.main")]
    public Camera playerCamera;

    [Tooltip("Góc chùm sáng của đèn pin (độ)")]
    [Range(10f, 60f)]
    public float spotAngle = 25f;

    [Tooltip("Tầm sáng tối đa (mét)")]
    public float lightRange = 15f;

    [Tooltip("Cường độ đèn pin")]
    public float lightIntensity = 3f;

    [Tooltip("Màu ánh sáng đèn pin (trắng hơi vàng cho thực tế hơn)")]
    public Color lightColor = new Color(1f, 0.98f, 0.9f);

    [Header("Hiệu ứng bật/tắt")]
    [Tooltip("Bật đèn pin khi bắt đầu game")]
    public bool startsOn = false;

    [Tooltip("Âm thanh click khi bật/tắt đèn pin")]
    public AudioClip toggleSound;
    [Range(0f, 1f)]
    public float toggleSoundVolume = 0.6f;

    [Header("Debug / Info")]
    [Tooltip("Trạng thái hiện tại của đèn pin (chỉ đọc)")]
    public bool isFlashlightOn = false;

    // ---- Nội bộ ----
    private Light flashlight;
    private AudioSource audioSource;
    private bool inputCooldown = false; // Tránh toggle liên tục

    private void Awake()
    {
        // Tìm camera nếu chưa gán
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera == null)
        {
            Debug.LogError("[FlashlightController] Không tìm thấy Camera! Hãy kéo Camera vào Inspector.");
            return;
        }

        // Tạo Spot Light con của Camera
        GameObject lightObj = new GameObject("_Flashlight");
        lightObj.transform.SetParent(playerCamera.transform, false);
        lightObj.transform.localPosition = new Vector3(0.15f, -0.1f, 0.3f); // Lệch phải nhẹ giống đèn pin thực
        lightObj.transform.localRotation = Quaternion.identity;

        flashlight = lightObj.AddComponent<Light>();
        flashlight.type = LightType.Spot;
        flashlight.spotAngle = spotAngle;
        flashlight.innerSpotAngle = spotAngle * 0.6f;
        flashlight.range = lightRange;
        flashlight.intensity = lightIntensity;
        flashlight.color = lightColor;
        flashlight.shadows = LightShadows.Soft;
        flashlight.gameObject.SetActive(startsOn);
        isFlashlightOn = startsOn;

        // AudioSource để phát tiếng click
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        Debug.Log("[FlashlightController] Đèn pin đã được tạo và gắn vào camera.");
    }

    private void Update()
    {
        if (inputCooldown) return;

        // PC: Phím F | Mobile: flashlightPressed
        bool toggleInput = Input.GetKeyDown(KeyCode.F) || MobileButtons.flashlightPressed;

        if (toggleInput)
        {
            ToggleFlashlight();
            StartCoroutine(InputCooldownRoutine());
        }
    }

    /// <summary>
    /// Bật/Tắt đèn pin. Có thể gọi từ bên ngoài (ví dụ từ nút UI mobile).
    /// </summary>
    public void ToggleFlashlight()
    {
        if (flashlight == null) return;

        isFlashlightOn = !isFlashlightOn;
        flashlight.gameObject.SetActive(isFlashlightOn);

        // Phát tiếng click
        if (toggleSound != null && audioSource != null)
            audioSource.PlayOneShot(toggleSound, toggleSoundVolume);

        Debug.Log($"[FlashlightController] Đèn pin: {(isFlashlightOn ? "BẬT" : "TẮT")}");
    }

    /// <summary>
    /// Kiểm tra đèn pin hiện có đang bật không.
    /// Dùng cho NotePickup để check "có nguồn sáng không".
    /// </summary>
    public bool IsOn() => isFlashlightOn;

    private IEnumerator InputCooldownRoutine()
    {
        inputCooldown = true;
        yield return new WaitForSeconds(0.2f);
        inputCooldown = false;
    }
}
