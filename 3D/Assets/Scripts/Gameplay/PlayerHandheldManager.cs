using UnityEngine;
using AnomalySystem;

/// <summary>
/// Quản lý vật phẩm cầm tay của người chơi (Gấu bông phát sáng).
/// Tự động sinh visual 3D dạng quả cầu năng lượng vàng ấm HDR và Point Light tại runtime.
/// Quét và đốt cháy các thực thể quỷ trong phạm vi 8m để bảo vệ người chơi (Warding Shield).
/// </summary>
public class PlayerHandheldManager : MonoBehaviour
{
    private GameObject equippedBearVisual;
    private Light bearLight;
    private float pulseSpeed = 1.5f;
    private float minIntensity = 1.8f;
    private float maxIntensity = 3.0f;

    private void Start()
    {
        // Đăng ký sự kiện thay đổi trạng thái trang bị túi đồ
        InventoryManager.OnEquippedStateChanged += RefreshEquippedVisual;
        RefreshEquippedVisual();
    }

    private void OnDestroy()
    {
        InventoryManager.OnEquippedStateChanged -= RefreshEquippedVisual;
    }

    private void Update()
    {
        if (equippedBearVisual == null || !equippedBearVisual.activeSelf) return;

        // 1. Hiệu ứng nhấp nháy/nhịp thở ánh sáng nhẹ nhàng
        float t = (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI) + 1f) * 0.5f;
        if (bearLight != null)
        {
            bearLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
        }

        // 2. Bảo vệ người chơi: quét và đốt cháy quỷ xung quanh
        ScanAndDamageDemons();
    }

    private void RefreshEquippedVisual()
    {
        bool isEquipped = InventoryManager.Instance != null && InventoryManager.Instance.IsTeddyBearEquipped;

        if (isEquipped)
        {
            if (equippedBearVisual == null)
            {
                CreateBearVisual();
            }
            equippedBearVisual.SetActive(true);
            Debug.Log("[PlayerHandheldManager] Đã kích hoạt quả cầu ánh sáng bảo vệ của Gấu Bông!");
        }
        else
        {
            if (equippedBearVisual != null)
            {
                equippedBearVisual.SetActive(false);
            }
        }
    }

    private void CreateBearVisual()
    {
        equippedBearVisual = new GameObject("_EquippedBearVisual");
        equippedBearVisual.transform.SetParent(transform, false); // Gắn con dưới Camera chính
        equippedBearVisual.transform.localPosition = new Vector3(0.35f, -0.28f, 0.48f); // Góc dưới phải màn hình
        equippedBearVisual.transform.localRotation = Quaternion.identity;

        // Tạo quả cầu phát sáng tượng trưng cho năng lượng của Gấu Bông
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(sphere.GetComponent<Collider>()); // Bỏ va chạm để không gây nhiễu raycast/vật lý
        sphere.transform.SetParent(equippedBearVisual.transform, false);
        sphere.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);

        Renderer r = sphere.GetComponent<Renderer>();
        if (r != null)
        {
            // Tạo material phát sáng vàng HDR
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(1f, 0.78f, 0.38f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(1f, 0.7f, 0.3f) * 2.5f);
            r.material = mat;
        }

        // Tạo Point Light phát ra xung quanh
        GameObject lightObj = new GameObject("BearPointLight");
        lightObj.transform.SetParent(equippedBearVisual.transform, false);
        lightObj.transform.localPosition = Vector3.zero;

        bearLight = lightObj.AddComponent<Light>();
        bearLight.type = LightType.Point;
        bearLight.color = new Color(1f, 0.75f, 0.35f); // Vàng ấm áp xua đuổi tà ác
        bearLight.range = 8f;
        bearLight.intensity = minIntensity;
        bearLight.shadows = LightShadows.Soft;
    }

    private void ScanAndDamageDemons()
    {
        if (bearLight == null) return;

        // Quét quỷ trong phạm vi 8m xung quanh người chơi
        Collider[] hits = Physics.OverlapSphere(transform.position, 8f);
        foreach (Collider col in hits)
        {
            // Quét quỷ Room 4 / Room 5 (navmesh chase)
            DemonController demon = col.GetComponent<DemonController>();
            if (demon != null)
            {
                // Tự động gây sát thương làm quỷ flinch và tan biến
                demon.OnLightHit(Time.deltaTime * 0.8f);
            }

            // Quét quỷ tầng chuyên dụng Room 5
            FloorDemonAI floorDemon = col.GetComponent<FloorDemonAI>();
            if (floorDemon != null)
            {
                floorDemon.OnLightHit(Time.deltaTime * 0.8f);
            }
        }
    }
}
