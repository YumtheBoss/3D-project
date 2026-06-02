using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script đèn pin điện thoại (single source of truth).
/// - Tự tạo Spot Light gắn trên camera.
/// - Toggle: Phím F (PC) | Nút flashlight (Mobile).
/// - Room 3+: quét DemonController trong vùng sáng; chiếu 3s → quỷ tan biến → khóa 5s.
/// </summary>
public class FlashlightController : MonoBehaviour
{
    // ─── Cài đặt đèn pin ───────────────────────────────────────
    [Header("Đèn pin")]
    [Tooltip("Camera của người chơi. Để trống thì tự tìm Camera.main")]
    public Camera playerCamera;
    [Range(10f, 60f)] public float spotAngle = 25f;
    public float lightRange = 15f;
    public float lightIntensity = 3f;
    public Color lightColor = new Color(1f, 0.98f, 0.9f);
    public bool startsOn = false;

    [Header("Hiệu ứng bật/tắt")]
    public AudioClip toggleSound;
    [Range(0f, 1f)] public float toggleSoundVolume = 0.6f;

    // ─── Demon detection (Room 3+) ─────────────────────────────
    [Header("Demon Detection (Room 3+)")]
    [Tooltip("Layer chứa các Demon để scan (để trống = dùng Physics.DefaultRaycastLayers)")]
    public LayerMask demonLayer = Physics.DefaultRaycastLayers;
    [Tooltip("Giây chiếu liên tục để quỷ tan biến")]
    public float lightVanishTime = 3f;
    [Tooltip("Giây khóa đèn sau khi quỷ tan biến")]
    public float cooldownDuration = 5f;
    [Tooltip("Text UI hiển thị đếm ngược cooldown (tùy chọn)")]
    public Text cooldownText;

    // ─── State (đọc từ bên ngoài) ──────────────────────────────
    [Header("Debug / Info")]
    public bool isFlashlightOn = false;
    public bool IsFlashlightOn => isFlashlightOn;
    public bool IsOnCooldown { get; private set; }

    // ─── Nội bộ ────────────────────────────────────────────────
    private Light flashlight;
    private AudioSource audioSource;
    private bool inputCooldown = false;
    private float cooldownRemaining;
    private bool demonScanActive = false;

    // ═══════════════════════════════════════════════════════════
    // UNITY LIFECYCLE
    // ═══════════════════════════════════════════════════════════

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera == null)
        {
            Debug.LogError("[FlashlightController] Không tìm thấy Camera!");
            return;
        }

        // Tạo Spot Light con của Camera
        GameObject lightObj = new GameObject("_Flashlight");
        lightObj.transform.SetParent(playerCamera.transform, false);
        // Đặt nguồn sáng trùng khít với tâm Camera chính để triệt tiêu hoàn toàn lệch góc (parallax offset)
        lightObj.transform.localPosition = new Vector3(0f, 0f, 0f);
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

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        UpdateCooldownUI(0f);
    }

    private void OnEnable()
    {
        RoomManager.OnRoomEntered += HandleRoomEntered;
    }

    private void OnDisable()
    {
        RoomManager.OnRoomEntered -= HandleRoomEntered;
    }

    private void HandleRoomEntered(RoomManager.RoomState room)
    {
        // Demon scan chỉ hoạt động từ Room 3 trở đi
        demonScanActive = room >= RoomManager.RoomState.Room3;
    }

    private void Update()
    {
        // ── Cooldown đếm ngược ──
        if (IsOnCooldown)
        {
            cooldownRemaining -= Time.deltaTime;
            UpdateCooldownUI(cooldownRemaining);
            if (cooldownRemaining <= 0f)
            {
                IsOnCooldown = false;
                cooldownRemaining = 0f;
                UpdateCooldownUI(0f);
            }
            return; // Không cho toggle khi đang cooldown
        }

        // ── Toggle đèn ──
        if (!inputCooldown)
        {
            bool toggleInput = Input.GetKeyDown(KeyCode.F);
            if (toggleInput)
            {
                ToggleFlashlight();
                StartCoroutine(InputCooldownRoutine());
            }
        }

        // ── Quét Demon khi đèn bật ──
        if (isFlashlightOn && demonScanActive)
            ScanForDemons();
    }

    // ═══════════════════════════════════════════════════════════
    // PUBLIC API
    // ═══════════════════════════════════════════════════════════

    public void ToggleFlashlight()
    {
        if (flashlight == null) return;
        isFlashlightOn = !isFlashlightOn;
        flashlight.gameObject.SetActive(isFlashlightOn);
        if (toggleSound != null) audioSource.PlayOneShot(toggleSound, toggleSoundVolume);
    }

    /// <summary>Dùng bởi NotePickup để kiểm tra "có nguồn sáng không".</summary>
    public bool IsOn() => isFlashlightOn;

    /// <summary>Gọi bởi DemonController khi quỷ tan biến.</summary>
    public void StartCooldown()
    {
        isFlashlightOn = false;
        if (flashlight != null) flashlight.gameObject.SetActive(false);
        IsOnCooldown = true;
        cooldownRemaining = cooldownDuration;
        UpdateCooldownUI(cooldownRemaining);
        Debug.Log($"[FlashlightController] Đèn bị khóa {cooldownDuration}s.");
    }

    // ═══════════════════════════════════════════════════════════
    // DEMON DETECTION
    // ═══════════════════════════════════════════════════════════

    private void ScanForDemons()
    {
        if (playerCamera == null)
        {
            if (FirstPersonController.Instance != null && FirstPersonController.Instance.playerCamera != null)
                playerCamera = FirstPersonController.Instance.playerCamera;
            else
                playerCamera = Camera.main;
        }
        if (flashlight == null || playerCamera == null) return;

        Collider[] hits = Physics.OverlapSphere(
            playerCamera.transform.position, lightRange, demonLayer);

        // Tăng gấp 3 lần sát thương/tốc độ đốt quỷ khi người chơi zoom camera
        bool isZooming = (FirstPersonController.Instance != null && FirstPersonController.Instance.IsZoomed) || Input.GetKey(KeyCode.Mouse1);
        float multiplier = isZooming ? 3.0f : 1.0f;

        foreach (Collider col in hits)
        {
            // Sử dụng tâm hình học của Collider (bounds.center - thường ở ngực quái) thay vì chân quái (col.transform.position)
            // giúp tránh hiện tượng lệch góc chúc xuống khi quái lại gần làm trượt nón sáng
            Vector3 targetCenter = col.bounds.center;
            Vector3 dir = (targetCenter - playerCamera.transform.position).normalized;
            float angle = Vector3.Angle(playerCamera.transform.forward, dir);
            if (angle <= spotAngle * 0.5f)
            {
                DemonController demon = col.GetComponentInParent<DemonController>();
                demon?.OnLightHit(Time.deltaTime * multiplier, isZooming);

                FloorDemonAI floorDemon = col.GetComponentInParent<FloorDemonAI>();
                floorDemon?.OnLightHit(Time.deltaTime * multiplier, isZooming);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // INTERNAL
    // ═══════════════════════════════════════════════════════════

    private void UpdateCooldownUI(float remaining)
    {
        if (cooldownText == null) return;
        cooldownText.gameObject.SetActive(remaining > 0f);
        if (remaining > 0f)
            cooldownText.text = $"Đèn hồi phục: {remaining:F1}s";
    }

    private IEnumerator InputCooldownRoutine()
    {
        inputCooldown = true;
        yield return new WaitForSeconds(0.2f);
        inputCooldown = false;
    }
}
