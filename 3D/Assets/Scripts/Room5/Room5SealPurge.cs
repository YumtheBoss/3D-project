using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý cơ chế giải câu đố "Đàn tế Phong ấn Lá chắn Gấu Bông" tại Room 5.
/// Người chơi cần đứng gần đàn tế (< activeDistance) và BẬT Lá chắn của Gấu bông phát sáng để thanh tẩy phong ấn.
/// Trong lúc thanh tẩy, quỷ dữ sẽ được đánh động và liên tục rượt đuổi người chơi!
/// </summary>
public class Room5SealPurge : MonoBehaviour
{
    public static System.Action OnAllSealsCleared;

    [Header("Cấu hình câu đố")]
    [Tooltip("Thời gian cần thiết để thanh tẩy phong ấn này (giây)")]
    public float purgeDuration = 12f;
    [Tooltip("Khoảng cách tối đa để tương tác thanh tẩy (mét)")]
    public float activeDistance = 3.5f;

    [Header("Hiệu ứng Visual & Âm thanh")]
    [Tooltip("Nguồn sáng hoặc Particle của Phong ấn (sẽ tắt hoặc đổi màu khi hoàn thành)")]
    public Light sealLight;
    [Tooltip("Màu sắc của nguồn sáng khi đang thanh tẩy")]
    public Color purgingColor = new Color(1f, 0.5f, 0f); // Cam bùng cháy
    [Tooltip("Màu sắc của nguồn sáng khi đã giải trừ xong")]
    public Color clearedColor = new Color(0.2f, 1f, 0.2f); // Xanh lá cây tâm linh

    [Header("Audio")]
    [Tooltip("Nguồn phát âm thanh (tự động thêm nếu để trống)")]
    public AudioSource audioSource;
    [Range(0f, 1f)]
    [Tooltip("Điều chỉnh âm lượng to/nhỏ tùy ý cho riêng đàn tế này")]
    public float volumeScale = 0.8f;
    [Tooltip("Âm thanh kêu rú, tà khí bùng phát khi đang thanh tẩy (loop)")]
    public AudioClip purgingSound;
    [Tooltip("Âm thanh nổ vang rực rỡ báo hiệu giải trừ thành công")]
    public AudioClip clearedSound;

    // ---- Trạng thái ----
    [HideInInspector] public bool isCleared = false;
    [HideInInspector] public bool isCurrentlyPurging = false; // Đổi thành public để các script khác đọc trạng thái
    private float purgeProgress = 0f;
    private Transform player;
    private PlayerHandheldManager handheldManager;
    private bool isPlayerInside = false;
    private float alertTimer = 0f;

    private Color initialColor = Color.red;

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        handheldManager = FindAnyObjectByType<PlayerHandheldManager>();

        // Tự động kiểm tra và khởi tạo AudioSource nếu người dùng để trống (Self-Healing System)
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // Cấu hình âm thanh 3D sống động để phát ra tại đúng vị trí cột đàn tế
        if (audioSource != null)
        {
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // Âm thanh 3D lập thể đầy đủ
            audioSource.minDistance = 2f;
            audioSource.maxDistance = 15f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }

        if (sealLight != null)
        {
            initialColor = sealLight.color;
        }
    }

    private void Update()
    {
        if (isCleared) return;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            else return;
        }

        float dist = Vector3.Distance(transform.position, player.position);
        isPlayerInside = dist <= activeDistance;

        // Kiểm tra xem gấu bông có được trang bị và đang BẬT hào quang không
        bool isTeddyActive = handheldManager != null && handheldManager.isActiveAndEnabled && handheldManager.IsShieldActive;

        if (isPlayerInside && isTeddyActive)
        {
            // Bắt đầu hoặc tiếp tục thanh tẩy
            if (!isCurrentlyPurging)
            {
                isCurrentlyPurging = true;
                if (audioSource != null && purgingSound != null)
                {
                    audioSource.clip = purgingSound;
                    audioSource.Play();
                }
                Debug.Log($"[Room5SealPurge] Bắt đầu thanh tẩy phong ấn: {gameObject.name}");
            }

            // Cập nhật âm lượng động theo thời gian thực (đáp ứng sfxVolume và volumeScale tùy chỉnh)
            if (audioSource != null)
            {
                float globalSfxVol = AudioManager.Instance != null ? AudioManager.Instance.GetSFXVolume() : PlayerPrefs.GetFloat("SFXVolume", 1f);
                audioSource.volume = volumeScale * globalSfxVol;
            }

            // Tăng tiến trình
            purgeProgress += Time.deltaTime;

            // Đổi màu đèn đàn tế sang cam bùng cháy để hiển thị phản ứng
            if (sealLight != null)
            {
                sealLight.color = Color.Lerp(initialColor, purgingColor, purgeProgress / purgeDuration);
                // Tạo hiệu ứng nhấp nháy mạnh mẽ lúc thanh tẩy
                sealLight.intensity = Mathf.PingPong(Time.time * 6f, 3f) + 1.5f;
            }

            // ĐÁNH ĐỘNG QUỶ: Mỗi 1.5 giây, phát ra sóng xung kích tâm linh cảnh báo toàn bộ quỷ
            alertTimer += Time.deltaTime;
            if (alertTimer >= 1.5f)
            {
                alertTimer = 0f;
                AlertAllDemons();
            }

            // Hoàn thành
            if (purgeProgress >= purgeDuration)
            {
                ClearSeal();
            }
        }
        else
        {
            // Dừng thanh tẩy nếu người chơi ra ngoài hoặc tắt gấu bông
            if (isCurrentlyPurging)
            {
                isCurrentlyPurging = false;
                if (audioSource != null) audioSource.Stop();
                
                if (sealLight != null)
                {
                    sealLight.color = initialColor;
                    sealLight.intensity = 1.5f;
                }
                Debug.Log($"[Room5SealPurge] Bị gián đoạn thanh tẩy phong ấn: {gameObject.name}");
            }

            // Giảm nhẹ tiến trình thanh tẩy theo thời gian nếu bị bỏ hoang (tránh người chơi nhặt nhạnh tiến trình quá dễ)
            if (purgeProgress > 0f)
            {
                purgeProgress = Mathf.Max(0f, purgeProgress - Time.deltaTime * 0.3f);
            }
        }
    }

    private void AlertAllDemons()
    {
        FloorDemonAI[] demons = Object.FindObjectsByType<FloorDemonAI>(FindObjectsSortMode.None);
        foreach (var demon in demons)
        {
            if (demon != null && demon.isActiveAndEnabled)
            {
                // Khiến quỷ dữ biết vị trí đàn tế đang bị xâm phạm và lao tới đuổi Player
                demon.AlertDemon(player.position);
            }
        }
        Debug.Log("[Room5SealPurge] Tà khí bùng phát! Toàn bộ quỷ dữ đã được cảnh báo vị trí của bạn!");
    }

    private void ClearSeal()
    {
        isCleared = true;
        isCurrentlyPurging = false;

        if (audioSource != null)
        {
            audioSource.Stop();
            if (clearedSound != null)
            {
                audioSource.PlayOneShot(clearedSound, 1f);
            }
        }

        if (sealLight != null)
        {
            sealLight.color = clearedColor;
            sealLight.intensity = 2f;
            // Cho phép sáng dịu vĩnh viễn báo hiệu an toàn
        }

        Debug.Log($"[Room5SealPurge] ĐÃ GIẢI TRỪ THÀNH CÔNG PHONG ẤN: {gameObject.name}!");

        // Nếu tất cả phong ấn đã được giải phóng, làm choáng toàn bộ quỷ và phát sự kiện cổng dịch chuyển
        if (AllSealsCleared())
        {
            PurgeAllDemons();
            OnAllSealsCleared?.Invoke();
        }
    }

    private void PurgeAllDemons()
    {
        FloorDemonAI[] demons = Object.FindObjectsByType<FloorDemonAI>(FindObjectsSortMode.None);
        foreach (var demon in demons)
        {
            if (demon != null && demon.isActiveAndEnabled)
            {
                // Gọi OnLightHit với sát thương cực lớn để tiêu diệt quỷ vĩnh viễn (Vanishing)
                demon.OnLightHit(100f, true);
            }
        }
        Debug.Log("[Room5SealPurge] TẤT CẢ PHONG ẤN ĐÃ ĐƯỢC GIẢI PHÓNG! Toàn bộ tà khí bị tiêu diệt, quỷ dữ tan biến vĩnh viễn!");
    }

    /// <summary>
    /// Kiểm tra xem toàn bộ phong ấn trong Room 5 đã được giải trừ chưa.
    /// </summary>
    public static bool AllSealsCleared()
    {
        Room5SealPurge[] seals = Object.FindObjectsByType<Room5SealPurge>(FindObjectsSortMode.None);
        if (seals == null || seals.Length == 0) return true;
        foreach (var seal in seals)
        {
            if (!seal.isCleared) return false;
        }
        return true;
    }

    /// <summary>
    /// Kiểm tra xem có phong ấn nào đang được thanh tẩy thực tế tại Room 5 không.
    /// </summary>
    public static bool IsAnySealCurrentlyPurging()
    {
        Room5SealPurge[] seals = Object.FindObjectsByType<Room5SealPurge>(FindObjectsSortMode.None);
        if (seals == null || seals.Length == 0) return false;
        foreach (var seal in seals)
        {
            if (seal.isCurrentlyPurging) return true;
        }
        return false;
    }

    /// <summary>
    /// Lấy số lượng phong ấn đã giải trừ / Tổng số phong ấn để hiển thị UI
    /// </summary>
    public static string GetProgressString()
    {
        Room5SealPurge[] seals = Object.FindObjectsByType<Room5SealPurge>(FindObjectsSortMode.None);
        if (seals == null || seals.Length == 0) return "0/0";
        int cleared = 0;
        foreach (var seal in seals)
        {
            if (seal.isCleared) cleared++;
        }
        return $"{cleared}/{seals.Length}";
    }

    private void OnGUI()
    {
        if (isCleared || Time.timeScale <= 0f) return;
        if (GameObject.Find("_ChapterIntroCanvas_Auto") != null) return;

        // Chỉ hiển thị gợi ý trên màn hình khi người chơi đứng gần
        if (isPlayerInside)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 22;
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleCenter;

            bool isTeddyActive = handheldManager != null && handheldManager.isActiveAndEnabled && handheldManager.IsShieldActive;

            string text;
            if (!isTeddyActive)
            {
                style.normal.textColor = new Color(1f, 0.4f, 0.4f); // Đỏ cảnh báo
                text = "<b>CẦN TRANG BỊ VÀ BẬT HÀO QUANG GẤU BÔNG [G] để thanh tẩy phong ấn!</b>";
            }
            else
            {
                style.normal.textColor = new Color(1f, 0.8f, 0.3f); // Vàng sáng
                int percent = Mathf.FloorToInt((purgeProgress / purgeDuration) * 100f);
                text = $"<b>ĐANG THANH TẨY PHONG ẤN: {percent}%... (Đang thu hút quỷ dữ!)</b>";
            }

            GUI.Label(new Rect(Screen.width / 2f - 400, Screen.height / 2f + 100, 800, 50), text, style);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, activeDistance);
    }
}
