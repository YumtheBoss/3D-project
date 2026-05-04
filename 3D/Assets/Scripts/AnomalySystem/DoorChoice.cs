using UnityEngine;
using MobileControls;

namespace AnomalySystem
{
    public class DoorChoice : MonoBehaviour
    {
        [Tooltip("True: Cửa Bất Thường (quay lại). False: Cửa Bình Thường (đi tiếp).")]
        public bool isAnomalyDoor;


        [Tooltip("Khoảng cách để hiện chữ E (tính bằng mét)")]
        public float interactDistance = 3f;

        [Header("Audio Settings")]
        [Tooltip("Kéo file âm thanh tiếng mở cửa vào đây")]
        public AudioClip openSound;
        [Tooltip("Kéo AudioSource vào đây (nếu để trống, code sẽ tự tìm trên vật thể này)")]
        public AudioSource audioSource;

        private bool isPlayerNear = false;
        private Transform playerTransform;

        private void Start()
        {
            // Tự động tìm nhân vật trong game có gắn Tag "Player"
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }

            // Tự động tìm AudioSource nếu người dùng quên kéo vào
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        private void Update()
        {
            // Nếu chưa có nhân vật thì tìm lại
            if (playerTransform == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) playerTransform = playerObj.transform;
                else return; // Thoát nếu vẫn không tìm thấy
            }

            // Tính khoảng cách từ cửa đến người chơi
            // Việc xoay camera sẽ KHÔNG làm thay đổi khoảng cách này, nên chữ sẽ không bị mất
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            isPlayerNear = (distance <= interactDistance);

            // Kiểm tra nếu người chơi đang ở gần và bấm phím E (PC) hoặc nút Interact (Mobile)
            bool isInteractInput = Input.GetKeyDown(KeyCode.E) || MobileButtons.interactPressed;
            if (isPlayerNear && isInteractInput)
            {
                // 1. Phát tiếng mở cửa
                if (openSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(openSound);
                }

                // 2. Chuyển màn chơi
                if (LevelManager.Instance != null)
                {
                    // Đặt lại biến để tránh lỗi bấm nhiều lần
                    isPlayerNear = false;
                    LevelManager.Instance.OnPlayerMakeChoice(isAnomalyDoor);
                }
                else
                {
                    Debug.LogError("Không tìm thấy LevelManager.Instance trong scene!");
                }
            }
        }

        // Tạm thời dùng OnGUI để vẽ chữ lên màn hình cho bạn dễ test
        private void OnGUI()
        {
            if (isPlayerNear && Time.timeScale > 0f)
            {
                GUIStyle style = new GUIStyle();
                style.fontSize = 24;
                style.normal.textColor = Color.white;
                style.alignment = TextAnchor.MiddleCenter;
                
                // Vẽ dòng chữ ở giữa màn hình (thấp xuống một chút so với tâm)
                GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height / 2 + 50, 300, 50), "Nhấn [E] để Mở Cửa", style);
            }
        }
    }
}
