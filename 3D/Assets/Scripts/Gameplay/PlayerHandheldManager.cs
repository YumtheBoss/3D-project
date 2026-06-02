using UnityEngine;
using AnomalySystem;

/// <summary>
/// Quản lý vật phẩm cầm tay của người chơi (Gấu bông phát sáng).
/// Tự động sinh visual 3D dạng quả cầu năng lượng vàng ấm HDR và Point Light tại runtime.
/// Quét và đốt cháy các thực thể quỷ trong phạm vi 8m để bảo vệ người chơi (Warding Shield).
/// </summary>
public class PlayerHandheldManager : MonoBehaviour
{
    [Header("Cấu hình Gấu Bông")]
    [Tooltip("Phím dùng để trang bị nhanh hoặc cất Gấu Bông")]
    public KeyCode quickEquipKey = KeyCode.Q;
    [Tooltip("Phím dùng để bật/tắt hào quang bảo vệ của Gấu Bông")]
    public KeyCode toggleKey = KeyCode.G;

    [Header("Hiển thị mô hình 3D thực tế")]
    [Tooltip("Prefab của gấu bông (Voodoo Doll) để hiển thị trên tay người chơi")]
    public GameObject teddyBearPrefab;
    [Tooltip("Vị trí hiển thị của gấu bông so với Camera (X: Trái/Phải, Y: Trên/Dưới, Z: Trước/Sau)")]
    public Vector3 bearPositionOffset = new Vector3(0f, -0.2f, 0.35f); // Đặt chính diện bên dưới camera
    [Tooltip("Góc xoay của gấu bông so với Camera")]
    public Vector3 bearRotationOffset = new Vector3(0f, 180f, 0f);
    [Tooltip("Tỷ lệ thu phóng (Scale) mong muốn của gấu bông khi trang bị")]
    public float bearScaleMultiplier = 0.08f;

    [Header("Âm thanh Bật/Tắt (Tùy chọn)")]
    [Tooltip("Âm thanh sạc năng lượng khi bật hào quang")]
    public AudioClip turnOnSound;
    [Tooltip("Âm thanh xì tắt năng lượng khi tắt hào quang")]
    public AudioClip turnOffSound;

    public bool IsShieldActive => isBearLightActive && equippedBearVisual != null && equippedBearVisual.activeSelf && currentScale > 0.01f;

    private GameObject equippedBearVisual;
    private GameObject bearInstance; // Chú gấu bông thực tế (nếu có prefab)
    private GameObject bearInstanceForScale; // Đối tượng helper để thực hiện co giãn mượt mà
    private Vector3 bearOriginalScale = Vector3.one; // Lưu scale ban đầu của prefab
    private GameObject bearSphere; // Quả cầu primitive sphere đại diện cho hào quang (nếu không có prefab)
    private Light bearLight; // Spot Light chiếu trước
    private Light bearGlowLight; // Point Light tỏa sáng xung quanh gấu
    
    private float pulseSpeed = 1.5f;
    private float minIntensity = 1.8f;
    private float maxIntensity = 3.0f;
    private bool isBearLightActive = true; // Trạng thái bật/tắt của hào quang gấu

    // Các biến phục vụ Lerp mượt mà
    private float currentScale = 0f;
    private float targetScale = 0f;
    private float currentIntensity = 0f;
    private float targetIntensity = 0f;

    private Transform GetCameraTransform()
    {
        // 1. Thử lấy camera từ FirstPersonController
        if (FirstPersonController.Instance != null && FirstPersonController.Instance.playerCamera != null)
        {
            return FirstPersonController.Instance.playerCamera.transform;
        }
        // 2. Thử lấy Camera.main
        Camera cam = Camera.main;
        if (cam != null) return cam.transform;
        
        // 3. Thử tìm Camera ở các đối tượng con
        Camera childCam = GetComponentInChildren<Camera>();
        if (childCam != null) return childCam.transform;
        
        // 4. Thử tìm Camera ở các đối tượng cha
        Camera parentCam = GetComponentInParent<Camera>();
        if (parentCam != null) return parentCam.transform;

        // 5. Thử tìm bất kỳ Camera nào trong scene
        Camera anyCam = FindAnyObjectByType<Camera>();
        if (anyCam != null) return anyCam.transform;

        return transform; // Fallback cuối cùng
    }

    private void Start()
    {
        // Tự động tối ưu hóa kích thước và góc quay nếu giá trị scale cũ hoặc vị trí lệch cũ bị giữ lại trong Unity Editor
        if (teddyBearPrefab != null && (bearScaleMultiplier == 1.0f || bearPositionOffset.x == 0.2f || bearPositionOffset.x == 0.35f))
        {
            bearScaleMultiplier = 0.08f;
            bearPositionOffset = new Vector3(0f, -0.2f, 0.35f);
            bearRotationOffset = new Vector3(0f, 180f, 0f);
            Debug.Log("[PlayerHandheldManager] Đã tự động tối ưu cấu hình hiển thị của gấu bông 3D về chính diện bên dưới!");
        }

        // Đăng ký sự kiện thay đổi trạng thái trang bị túi đồ
        InventoryManager.OnEquippedStateChanged += RefreshEquippedVisual;
        RefreshEquippedVisual();
    }

    private void OnDestroy()
    {
        InventoryManager.OnEquippedStateChanged -= RefreshEquippedVisual;
        if (equippedBearVisual != null)
        {
            Destroy(equippedBearVisual);
        }
    }

    private void Update()
    {
        // 1. Phím tắt trang bị nhanh (Q)
        if (Input.GetKeyDown(quickEquipKey))
        {
            // Kiểm tra sở hữu vật phẩm trực tiếp từ PlayerPrefs để tránh lỗi desync Singleton khi test
            bool hasTeddy = PlayerPrefs.GetString("SavedInventory", "").Contains("TeddyBear");
            if (hasTeddy)
            {
                bool currentlyEquipped = PlayerPrefs.GetInt("IsTeddyBearEquipped", 0) == 1;
                if (!currentlyEquipped)
                {
                    // Tự động trang bị gấu bông và bật hào quang lên
                    isBearLightActive = true;
                    if (InventoryManager.Instance != null)
                    {
                        InventoryManager.Instance.SetTeddyBearEquipped(true);
                    }
                    else
                    {
                        PlayerPrefs.SetInt("IsTeddyBearEquipped", 1);
                        PlayerPrefs.Save();
                    }
                    PlaySound(turnOnSound);
                    Debug.Log("[PlayerHandheldManager] Phím Q: Tự động trang bị Gấu Bông và BẬT hào quang bảo vệ!");
                }
                else
                {
                    // Tự động cất gấu bông đi (tháo trang bị)
                    if (InventoryManager.Instance != null)
                    {
                        InventoryManager.Instance.SetTeddyBearEquipped(false);
                    }
                    else
                    {
                        PlayerPrefs.SetInt("IsTeddyBearEquipped", 0);
                        PlayerPrefs.Save();
                    }
                    PlaySound(turnOffSound);
                    Debug.Log("[PlayerHandheldManager] Phím Q: Tự động cất Gấu Bông!");
                }
            }
            else
            {
                Debug.LogWarning("[PlayerHandheldManager] Không thể rút Gấu Bông vì bạn chưa nhặt nó trong game!");
            }
        }

        // 2. Phím bật/tắt hào quang bảo vệ (G)
        if (PlayerPrefs.GetInt("IsTeddyBearEquipped", 0) == 1)
        {
            if (Input.GetKeyDown(toggleKey))
            {
                isBearLightActive = !isBearLightActive;
                PlaySound(isBearLightActive ? turnOnSound : turnOffSound);
                Debug.Log($"[PlayerHandheldManager] Phím G: Đã {(isBearLightActive ? "BẬT" : "TẮT")} hào quang bảo vệ!");
            }
        }

        bool isEquipped = PlayerPrefs.GetInt("IsTeddyBearEquipped", 0) == 1;

        // 3. Tính toán mục tiêu co giãn quả cầu và cường độ ánh sáng
        if (isEquipped && isBearLightActive)
        {
            targetScale = 1.0f; // Tiến trình scale chạy từ 0 -> 1.0f

            // Kiểm tra xem có đang thanh tẩy phong ấn nào ở Room 5 không (Tăng tốc độ nhịp đập và độ sáng)
            bool isPurging = Room5SealPurge.IsAnySealCurrentlyPurging();
            float currentPulseSpeed = isPurging ? 5.0f : pulseSpeed;
            float currentMin = isPurging ? 2.5f : minIntensity;
            float currentMax = isPurging ? 4.5f : maxIntensity;

            // Hiệu ứng nhịp thở/nhịp đập năng lượng
            float t = (Mathf.Sin(Time.time * currentPulseSpeed * Mathf.PI) + 1f) * 0.5f;
            targetIntensity = Mathf.Lerp(currentMin, currentMax, t);
        }
        else
        {
            // Co nhỏ gấu/quả cầu về 0 và tắt ánh sáng
            targetScale = 0f;
            targetIntensity = 0f;
        }

        // 4. Nội suy (Lerp) mượt mà các thông số visual
        currentScale = Mathf.MoveTowards(currentScale, targetScale, Time.deltaTime * 4f); // Co giãn mượt mà trong ~0.25s
        
        if (bearSphere != null)
        {
            // Quả cầu primitive giữ scale nhỏ gọn 0.08f mặc định để không che mắt người chơi
            float s = 0.08f * currentScale;
            bearSphere.transform.localScale = new Vector3(s, s, s);
        }
        if (bearInstanceForScale != null)
        {
            // Gấu bông 3D nhân thêm hệ số điều chỉnh bearScaleMultiplier
            bearInstanceForScale.transform.localScale = bearOriginalScale * bearScaleMultiplier * currentScale;
        }

        currentIntensity = Mathf.MoveTowards(currentIntensity, targetIntensity, Time.deltaTime * 12f);
        if (bearLight != null)
        {
            bearLight.intensity = currentIntensity;
            bearLight.enabled = currentIntensity > 0.01f;
        }
        if (bearGlowLight != null)
        {
            bearGlowLight.intensity = currentIntensity * 0.8f;
            bearGlowLight.enabled = currentIntensity > 0.01f;
        }

        // 5. Tự động ẩn hoàn toàn GameObject visual sau khi hiệu ứng co nhỏ kết thúc và không còn trang bị
        if (equippedBearVisual != null && equippedBearVisual.activeSelf)
        {
            if (!isEquipped && currentScale <= 0.001f)
            {
                equippedBearVisual.SetActive(false);
                Debug.Log("[PlayerHandheldManager] Đã ẩn hoàn toàn visual gấu bông sau khi co nhỏ về 0.");
            }
        }

        if (equippedBearVisual == null || !equippedBearVisual.activeSelf || currentScale <= 0.01f) return;

        // 6. Bảo vệ người chơi: quét và thiêu đốt quỷ xung quanh
        ScanAndDamageDemons();
    }

    private void RefreshEquippedVisual()
    {
        bool isEquipped = PlayerPrefs.GetInt("IsTeddyBearEquipped", 0) == 1;

        if (isEquipped)
        {
            if (equippedBearVisual == null)
            {
                CreateBearVisual();
            }
            equippedBearVisual.SetActive(true); // Bật GameObject lên để chạy hiệu ứng phình to dần
            Debug.Log("[PlayerHandheldManager] Đã kích hoạt visual gấu bông!");
        }
        // Khi không trang bị, Update sẽ tự co nhỏ quả cầu về 0 rồi tắt Active sau để mượt mà
    }

    private void CreateBearVisual()
    {
        // Tự động tìm kiếm teddyBearPrefab từ InventoryManager database nếu bị null
        if (teddyBearPrefab == null && InventoryManager.Instance != null)
        {
            var bearData = InventoryManager.Instance.GetItemData("TeddyBear");
            if (bearData != null && bearData.itemPrefab != null)
            {
                teddyBearPrefab = bearData.itemPrefab;
                Debug.Log("[PlayerHandheldManager] Tự động tìm thấy TeddyBear Prefab từ InventoryManager Database!");
            }
        }

        Transform playerCameraTransform = GetCameraTransform();

        equippedBearVisual = new GameObject("_EquippedBearVisual");
        equippedBearVisual.transform.SetParent(playerCameraTransform, false); // Gắn dưới Camera chính để xoay theo camera
        equippedBearVisual.transform.localPosition = Vector3.zero; // Căn chỉnh tương đối từ tâm camera
        equippedBearVisual.transform.localRotation = Quaternion.identity;

        // Tạo Spot Light phát ra phía trước (đèn thanh tẩy quỷ) đặt trùng tâm Camera
        GameObject lightObj = new GameObject("BearSpotLight");
        lightObj.transform.SetParent(equippedBearVisual.transform, false);
        lightObj.transform.localPosition = Vector3.zero;
        lightObj.transform.localRotation = Quaternion.identity;

        bearLight = lightObj.AddComponent<Light>();
        bearLight.type = LightType.Spot;
        bearLight.spotAngle = 35f;
        bearLight.innerSpotAngle = 20f;
        bearLight.color = new Color(1f, 0.75f, 0.35f); // Vàng ấm áp xua đuổi tà ác
        bearLight.range = 15f; // Tầm xa 15m
        bearLight.intensity = 0f; // Khởi tạo bằng 0
        bearLight.shadows = LightShadows.Soft;

        if (teddyBearPrefab != null)
        {
            // Sinh mô hình gấu bông thực tế
            bearInstance = Instantiate(teddyBearPrefab);
            
            // Khử tất cả va chạm vật lý của gấu bông để tránh va chạm với player camera hoặc raycast
            Collider[] colliders = bearInstance.GetComponentsInChildren<Collider>(true);
            foreach (var col in colliders)
            {
                Destroy(col);
            }

            // Tắt/Hủy Animator để tránh việc Animation ghi đè tọa độ của gấu trên tay
            Animator anim = bearInstance.GetComponent<Animator>();
            if (anim == null) anim = bearInstance.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.enabled = false;
                Destroy(anim);
            }

            // Hủy tất cả các script tự chế trên gấu bông để tránh chúng tự di chuyển hoặc xử lý vị trí của gấu
            MonoBehaviour[] scripts = bearInstance.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var script in scripts)
            {
                if (script != null)
                {
                    Destroy(script);
                }
            }
            
            // Tính toán tâm hình học để triệt tiêu mọi pivot offset sai lệch của file 3D
            Vector3 localCenter = Vector3.zero;
            Renderer[] renderers = bearInstance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
                localCenter = bearInstance.transform.InverseTransformPoint(bounds.center);
            }

            // Tạo pivot helper để căn chỉnh và co giãn chuẩn xác
            GameObject pivotHelper = new GameObject("_BearPivotHelper");
            pivotHelper.transform.SetParent(equippedBearVisual.transform, false);
            pivotHelper.transform.localPosition = bearPositionOffset;
            pivotHelper.transform.localRotation = Quaternion.Euler(bearRotationOffset);
            pivotHelper.transform.localScale = Vector3.zero;
            
            // Gắn gấu bông làm con của helper và dịch chuyển tâm của nó về gốc tọa độ của helper
            bearInstance.transform.SetParent(pivotHelper.transform, false);
            bearInstance.transform.localPosition = -localCenter;
            bearInstance.transform.localRotation = Quaternion.identity;
            bearInstance.transform.localScale = Vector3.one; // Giữ scale gốc của prefab
            
            bearOriginalScale = Vector3.one; // Vì co giãn trên pivotHelper nên scale gốc là 1
            bearInstanceForScale = pivotHelper;
            
            bearSphere = null;

            // Tạo Point Light phụ để chiếu sáng gấu bông
            GameObject glowLightObj = new GameObject("BearGlowLight");
            glowLightObj.transform.SetParent(equippedBearVisual.transform, false);
            glowLightObj.transform.localPosition = bearPositionOffset + new Vector3(-0.05f, 0.1f, -0.1f);
            
            bearGlowLight = glowLightObj.AddComponent<Light>();
            bearGlowLight.type = LightType.Point;
            bearGlowLight.color = new Color(1f, 0.75f, 0.35f);
            bearGlowLight.range = 3f;
            bearGlowLight.intensity = 0f;
        }
        else
        {
            // Tạo quả cầu primitive sphere đại diện cho hào quang
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(sphere.GetComponent<Collider>()); // Bỏ va chạm
            sphere.transform.SetParent(equippedBearVisual.transform, false);
            sphere.transform.localPosition = bearPositionOffset;
            sphere.transform.localScale = Vector3.zero; // Khởi tạo bằng 0 để phình to dần mượt mà
            bearSphere = sphere;

            // Đảm bảo quả cầu tắt bóng đổ để không cản camera
            Renderer r = sphere.GetComponent<Renderer>();
            if (r != null)
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;

                // Sử dụng chính material mặc định của Primitive để đảm bảo tương thích 100% với URP/HDRP/Standard (không bị đốm tím)
                Material mat = r.material;
                if (mat != null)
                {
                    Color goldColor = new Color(1f, 0.78f, 0.38f);
                    if (mat.HasProperty("_Color")) mat.color = goldColor;
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", goldColor);
                    
                    mat.EnableKeyword("_EMISSION");
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.SetColor("_EmissionColor", new Color(1f, 0.7f, 0.3f) * 2.5f);
                    }
                }
            }

            bearInstance = null;
            bearInstanceForScale = null;
            bearSphere = sphere;
            bearGlowLight = null;
        }
    }

    private void ScanAndDamageDemons()
    {
        if (bearLight == null) return;
        Transform playerCameraTransform = GetCameraTransform();
        if (playerCameraTransform == null) return;

        // Đọc trạng thái zoom từ FirstPersonController hoặc phím giữ chuột phải
        bool isZooming = (FirstPersonController.Instance != null && FirstPersonController.Instance.IsZoomed) || Input.GetKey(KeyCode.Mouse1);
        float multiplier = isZooming ? 3.0f : 1.0f;

        // Quét quỷ trong phạm vi và hình nón sáng của bearLight (Spot Light) từ vị trí Camera
        float range = bearLight.range;
        Collider[] hits = Physics.OverlapSphere(playerCameraTransform.position, range);
        foreach (Collider col in hits)
        {
            // Sử dụng tâm hình học của Collider (bounds.center) thay vì chân quái giúp tránh lệch góc khi quái lại gần
            Vector3 targetCenter = col.bounds.center;
            Vector3 dir = (targetCenter - playerCameraTransform.position).normalized;
            float angle = Vector3.Angle(playerCameraTransform.forward, dir);

            // Chỉ thiêu đốt nếu quỷ nằm trong góc chiếu nón sáng của Gấu Bông
            if (angle <= bearLight.spotAngle * 0.5f)
            {
                // Quét quỷ Room 4 / Room 5 (navmesh chase)
                DemonController demon = col.GetComponentInParent<DemonController>();
                if (demon != null)
                {
                    demon.OnLightHit(Time.deltaTime * 0.8f * multiplier, isZooming);
                }

                // Quét quỷ tầng chuyên dụng Room 5
                FloorDemonAI floorDemon = col.GetComponentInParent<FloorDemonAI>();
                if (floorDemon != null)
                {
                    floorDemon.OnLightHit(Time.deltaTime * 0.8f * multiplier, isZooming);
                }
            }
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        
        if (bearLight != null)
        {
            AudioSource source = bearLight.GetComponent<AudioSource>();
            if (source == null)
            {
                source = bearLight.gameObject.AddComponent<AudioSource>();
                source.spatialBlend = 0f; // Âm thanh stereo phẳng 2D cho tiếng bật/tắt gần tai
            }
            
            // Lấy âm lượng SFX toàn cục
            float globalSfxVol = AudioManager.Instance != null ? AudioManager.Instance.GetSFXVolume() : PlayerPrefs.GetFloat("SFXVolume", 1f);
            source.PlayOneShot(clip, globalSfxVol);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, GetCameraTransform().position);
        }
    }
}
