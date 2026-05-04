using UnityEngine;

/// <summary>
/// Gắn script này vào cùng GameObject với Player.
/// Tự động phát tiếng bước chân lặp lại khi nhân vật di chuyển.
/// </summary>
public class FootstepAudio : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Kéo AudioSource của Player vào đây (để trống thì tự tìm/tự tạo)")]
    public AudioSource audioSource;

    [Header("Footstep Clips")]
    [Tooltip("Danh sách các file âm thanh bước chân khi ĐI (sẽ phát ngẫu nhiên luân phiên)")]
    public AudioClip[] walkClips;
    [Tooltip("Danh sách các file âm thanh bước chân khi CHẠY (để trống thì dùng chung với walk)")]
    public AudioClip[] sprintClips;

    [Header("Volume Settings")]
    [Tooltip("Âm lượng tiếng bước chân khi đi bình thường")]
    [Range(0f, 1f)]
    public float walkVolume = 0.8f;
    [Tooltip("Âm lượng tiếng bước chân khi chạy nhanh")]
    [Range(0f, 1f)]
    public float sprintVolume = 1f;

    [Header("Step Rate (Khoảng cách giữa 2 bước)")]
    [Tooltip("Thời gian giữa 2 bước chân khi ĐI (giây)")]
    public float walkStepInterval = 0.5f;
    [Tooltip("Thời gian giữa 2 bước chân khi CHẠY (giây)")]
    public float sprintStepInterval = 0.32f;

    // ---- Biến nội bộ ----
    private float stepTimer = 0f;
    private int lastClipIndex = -1;

    private void Awake()
    {
        // Tự động lấy hoặc tạo AudioSource
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Cấu hình AudioSource cho tiếng bước chân 2D
        audioSource.spatialBlend = 0f;    // 2D sound - nghe rõ dù xa gần
        audioSource.playOnAwake = false;
        audioSource.loop = false;         // Mình tự quản lý vòng lặp bằng timer
    }

    private void Update()
    {
        HandleFootsteps();
    }

    private void HandleFootsteps()
    {
        if (audioSource == null) return;

        // Phát hiện di chuyển bằng phím bấm - cách đáng tin cậy nhất
        bool isMovingForward  = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        bool isMovingBackward = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        bool isMovingLeft     = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
        bool isMovingRight    = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
        bool isMoving = isMovingForward || isMovingBackward || isMovingLeft || isMovingRight;

        bool isSprinting = isMoving && Input.GetKey(KeyCode.LeftShift);

        if (isMoving)
        {
            stepTimer -= Time.deltaTime;

            if (stepTimer <= 0f)
            {
                // Chọn bộ clip và âm lượng phù hợp
                AudioClip[] clips = (isSprinting && sprintClips != null && sprintClips.Length > 0)
                    ? sprintClips
                    : walkClips;

                float volume = isSprinting ? sprintVolume : walkVolume;

                PlayRandomStep(clips, volume);

                // Đặt lại bộ đếm thời gian
                stepTimer = isSprinting ? sprintStepInterval : walkStepInterval;
            }
        }
        else
        {
            // Khi dừng lại: reset timer về 0 để bước đầu tiên phát ngay khi đi tiếp
            stepTimer = 0f;
        }
    }

    private void PlayRandomStep(AudioClip[] clips, float volume)
    {
        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning("[FootstepAudio] Chưa có clip nào trong danh sách! Hãy kéo file âm thanh vào ô Walk Clips.");
            return;
        }

        // Chọn ngẫu nhiên, đảm bảo không phát lại clip vừa xong
        int index = 0;
        if (clips.Length == 1)
        {
            index = 0;
        }
        else
        {
            do { index = Random.Range(0, clips.Length); }
            while (index == lastClipIndex);
        }

        lastClipIndex = index;

        if (clips[index] != null)
        {
            audioSource.PlayOneShot(clips[index], volume);
            Debug.Log($"[FootstepAudio] Đang phát: {clips[index].name} | Volume: {volume}");
        }
    }
}
