using UnityEngine;

/// <summary>
/// Thêm ánh sáng mờ nhạt nhấp nháy chậm lên prop có thể nhặt.
/// Gắn cùng GO với BookPickup hoặc TornPagePickup.
/// Gọi StopGlow() khi player đã nhặt xong để tắt đèn.
/// </summary>
public class PickupGlow : MonoBehaviour
{
    [Header("Ánh sáng")]
    [Tooltip("Màu ánh sáng — để mặc định cho màu vàng nến ấm")]
    public Color glowColor = new Color(1f, 0.88f, 0.6f);

    [Tooltip("Cường độ sáng tối thiểu")]
    public float minIntensity = 0.3f;

    [Tooltip("Cường độ sáng tối đa")]
    public float maxIntensity = 0.75f;

    [Tooltip("Bán kính chiếu sáng")]
    public float lightRange = 2.5f;

    [Tooltip("Tốc độ nhấp nháy (chu kỳ mỗi giây)")]
    public float pulseSpeed = 0.7f;

    private Light glowLight;
    private bool stopped = false;

    private void Awake()
    {
        GameObject lightGO = new GameObject("_PickupGlow");
        lightGO.transform.SetParent(transform);
        lightGO.transform.localPosition = Vector3.up * 0.15f;

        glowLight = lightGO.AddComponent<Light>();
        glowLight.type = LightType.Point;
        glowLight.color = glowColor;
        glowLight.range = lightRange;
        glowLight.intensity = minIntensity;
        glowLight.shadows = LightShadows.None;
    }

    private void Update()
    {
        if (stopped || glowLight == null) return;

        // Sine wave nhấp nháy mượt mà
        float t = (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
        glowLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
    }

    /// <summary>Tắt ánh sáng khi player đã nhặt/đọc xong.</summary>
    public void StopGlow()
    {
        stopped = true;
        if (glowLight != null)
            glowLight.gameObject.SetActive(false);
    }

    /// <summary>Bật lại ánh sáng để báo hiệu có nội dung mới (ví dụ: trang mới mở khóa).</summary>
    public void RestartGlow()
    {
        stopped = false;
        if (glowLight != null)
            glowLight.gameObject.SetActive(true);
    }
}
