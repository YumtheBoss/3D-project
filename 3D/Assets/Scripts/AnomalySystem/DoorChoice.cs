using UnityEngine;

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

            // Ẩn chức năng và chữ nếu màn hình Intro Chương 1 đang hiện
            if (GameObject.Find("_ChapterIntroCanvas_Auto") != null) 
            {
                return;
            }

            // Theo yêu cầu: Chặn người chơi không cho đi qua cửa gắn isAnomaly
            if (isAnomalyDoor)
            {
                return;
            }

            // Kiểm tra nếu người chơi đang ở gần và bấm phím E (PC) hoặc nút Interact (Mobile)
            bool isInteractInput = Input.GetKeyDown(KeyCode.E);
            if (isPlayerNear && isInteractInput)
            {
                // 1. Phát tiếng mở cửa
                if (openSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(openSound);
                }

                // 2. Chuyển màn chơi
                isPlayerNear = false;

                // Ưu tiên dùng GameFlowManager nếu đang hoạt động
                if (GameFlowManager.Instance != null && GameFlowManager.Instance.enabled)
                {
                    GameFlowManager.Instance.OnRoom2DoorChoice(isAnomalyDoor);
                }
                // Fallback: dùng RoomManager (hệ thống mới)
                else if (RoomManager.Instance != null)
                {
                    // Kiểm tra chọn đúng cửa thông qua AnomalyManager
                    AnomalyManager am = FindAnyObjectByType<AnomalyManager>();
                    if (am != null)
                    {
                        bool correct = (isAnomalyDoor == am.isCurrentLevelAnomaly);
                        if (correct)
                        {
                            Debug.Log("[DoorChoice] Chọn đúng cửa! Chuyển sang Room 3 qua RoomManager.");
                            RoomManager.Instance.EnterRoom(RoomManager.RoomState.Room3);
                        }
                        else
                        {
                            Debug.Log("[DoorChoice] Chọn sai cửa! Game Over.");
                            RoomManager.Instance.TriggerBadEnding("Chọn sai cửa ở Room 2");
                        }
                    }
                    else
                    {
                        // Không có AnomalyManager → cửa bình thường đi thẳng
                        Debug.Log("[DoorChoice] Không có AnomalyManager, chuyển thẳng sang Room 3.");
                        RoomManager.Instance.EnterRoom(RoomManager.RoomState.Room3);
                    }
                }
                else
                {
                    Debug.LogError("[DoorChoice] Không tìm thấy GameFlowManager hoặc RoomManager!");
                }
            }
        }

        // Tạm thời dùng OnGUI để vẽ chữ lên màn hình cho bạn dễ test
        private void OnGUI()
        {
            if (isPlayerNear && Time.timeScale > 0f)
            {
                // Ẩn chữ nếu Intro đang hiện
                if (GameObject.Find("_ChapterIntroCanvas_Auto") != null) return;

                // Ẩn chữ tương tác nếu đây là cửa isAnomaly (bị chặn)
                if (isAnomalyDoor) return;

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
