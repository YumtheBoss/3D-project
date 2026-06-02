using UnityEngine;

/// <summary>
/// Quản lý Cổng Dịch Chuyển Ánh Sáng ở Room 5.
/// Khi người chơi chạm vào cổng dịch chuyển, kích hoạt Good Ending.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Room5Portal : MonoBehaviour
{
    [Header("Audio & Effects")]
    [Tooltip("Nguồn âm thanh phát ra khi người chơi đi vào cổng dịch chuyển")]
    public AudioSource audioSource;
    [Tooltip("Âm thanh khi bước qua cổng dịch chuyển")]
    public AudioClip enterPortalSound;

    private bool triggered = false;

    private void Start()
    {
        // Đảm bảo Collider được thiết lập là Trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // Tự tìm AudioSource nếu chưa được gán
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        // Kiểm tra xem đối tượng va chạm có phải là Player hay không
        if (other.CompareTag("Player") || other.name.Contains("Player"))
        {
            triggered = true;
            Debug.Log("[Room5Portal] Player đã đi vào Cổng Dịch Chuyển! Kích hoạt Good Ending...");

            // Phát âm thanh nếu có
            if (audioSource != null && enterPortalSound != null)
            {
                audioSource.PlayOneShot(enterPortalSound);
            }

            // Gọi RoomManager kích hoạt kết thúc game (Good Ending)
            if (RoomManager.Instance != null)
            {
                RoomManager.Instance.TriggerGoodEnding();
            }
            else
            {
                Debug.LogWarning("[Room5Portal] Không tìm thấy RoomManager.Instance! (Đang chạy test trực tiếp Scene trong Editor). Tiến hành kích hoạt Ending trực tiếp thông qua EndingController...");
                
                // Tự động kiểm tra và tạo EndingController để hiển thị màn hình kết thúc nếu chưa có
                if (EndingController.Instance == null && FindAnyObjectByType<EndingController>() == null)
                {
                    GameObject endingCtrlObj = new GameObject("EndingController_Auto");
                    endingCtrlObj.AddComponent<EndingController>();
                    Debug.Log("[Room5Portal Self-Heal] Đã tự động tạo 'EndingController_Auto' cho chế độ test scene.");
                }
                
                // Kích hoạt hiển thị màn hình chiến thắng
                if (EndingController.Instance != null)
                {
                    EndingController.Instance.ShowGoodEnding(0f); // Truyền 0 giây khi chơi thử
                }
                else
                {
                    Debug.LogError("[Room5Portal] Không thể tự động tạo hoặc tìm thấy EndingController!");
                }
            }
        }
    }
}
