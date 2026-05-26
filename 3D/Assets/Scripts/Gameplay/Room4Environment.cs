using UnityEngine;

// Gắn script này vào bất kỳ GameObject nào trong scene.
// Tự động kích hoạt hiệu ứng Room 4 khi RoomManager chuyển sang Room4:
//   - Đổi màu ánh sáng sang đỏ thẫm
//   - Hoán đổi tranh tường thành thánh giá ngược
//   - Bật các thực thể lúc ẩn lúc hiện
//   - Phát nhạc aggressive
public class Room4Environment : MonoBehaviour
{
    [Header("Lighting")]
    [Tooltip("Directional Light chính của scene")]
    public Light mainLight;
    [Tooltip("Màu ánh sáng khi vào Room 4")]
    public Color room4LightColor = new Color(0.6f, 0.05f, 0.05f);
    [Tooltip("Màu ambient khi vào Room 4")]
    public Color room4AmbientColor = new Color(0.15f, 0.02f, 0.02f);

    [Header("Wall Pictures")]
    [Tooltip("Tag gắn cho tất cả tranh tường trong scene")]
    public string wallPictureTag = "WallPicture";
    [Tooltip("Material thay thế cho thánh giá ngược")]
    public Material invertedCrossMaterial;
    [Tooltip("Prefab thánh giá ngược (nếu muốn swap object thay vì material)")]
    public GameObject invertedCrossPrefab;

    [Header("Entities")]
    [Tooltip("Các thực thể lúc ẩn lúc hiện trong Room 4 (EntityIdleEffect)")]
    public GameObject[] room4Entities;

    [Header("Audio")]
    [Tooltip("AudioSource để phát nhạc Room 4")]
    public AudioSource audioSource;
    [Tooltip("Nhạc aggressive cho Room 4")]
    public AudioClip aggressiveMusic;
    [Range(0f, 1f)]
    public float musicVolume = 0.9f;

    private Color originalLightColor;
    private Color originalAmbientColor;
    private float originalLightIntensity;

    private void OnEnable()
    {
        RoomManager.OnRoomEntered += HandleRoomEntered;
    }

    private void OnDisable()
    {
        RoomManager.OnRoomEntered -= HandleRoomEntered;
    }

    private void Start()
    {
        if (mainLight != null)
        {
            originalLightColor = mainLight.color;
            originalLightIntensity = mainLight.intensity;
        }
        originalAmbientColor = RenderSettings.ambientLight;

        // Tắt tất cả entity Room 4 lúc đầu
        foreach (var entity in room4Entities)
            if (entity != null) entity.SetActive(false);
    }

    private void HandleRoomEntered(RoomManager.RoomState room)
    {
        if (room == RoomManager.RoomState.Room4)
            ActivateRoom4();
    }

    private void ActivateRoom4()
    {
        // 1. Đổi màu ánh sáng
        if (mainLight != null)
        {
            mainLight.color = room4LightColor;
            mainLight.intensity = 0.4f;
        }
        RenderSettings.ambientLight = room4AmbientColor;

        // 2. Đổi tranh tường
        SwapWallPictures();

        // 3. Bật entities
        foreach (var entity in room4Entities)
            if (entity != null) entity.SetActive(true);

        // 4. Bật nhạc aggressive
        if (audioSource != null && aggressiveMusic != null)
        {
            audioSource.clip = aggressiveMusic;
            audioSource.loop = true;
            audioSource.volume = musicVolume;
            audioSource.Play();
        }
    }

    private void SwapWallPictures()
    {
        if (string.IsNullOrEmpty(wallPictureTag)) return;

        try
        {
            GameObject[] pictures = GameObject.FindGameObjectsWithTag(wallPictureTag);
            foreach (GameObject pic in pictures)
            {
                if (invertedCrossPrefab != null)
                {
                    // Thay thế bằng prefab mới ở cùng vị trí/rotation
                    Instantiate(invertedCrossPrefab, pic.transform.position, pic.transform.rotation, pic.transform.parent);
                    pic.SetActive(false);
                }
                else if (invertedCrossMaterial != null)
                {
                    Renderer r = pic.GetComponent<Renderer>();
                    if (r != null) r.material = invertedCrossMaterial;
                }
            }
        }
        catch (UnityEngine.UnityException e)
        {
            Debug.LogWarning($"[Room4Environment] Bỏ qua đổi tranh vì tag '{wallPictureTag}' chưa được tạo/định nghĩa trong Unity Editor: {e.Message}");
        }
    }
}
