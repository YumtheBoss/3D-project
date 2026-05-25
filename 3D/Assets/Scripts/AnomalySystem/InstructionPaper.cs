using UnityEngine;

namespace AnomalySystem
{
    public class InstructionPaper : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Khoảng cách để hiện chữ E (tính bằng mét)")]
        public float interactDistance = 3f;
        
        [Header("UI References")]
        [Tooltip("Gán Canvas chứa nội dung tờ giấy vào đây (sẽ hiện lên khi tương tác)")]
        public GameObject instructionUI;

        [Header("Light Effect")]
        [Tooltip("Ánh sáng đỏ nhạt phát ra từ tờ giấy")]
        public Light paperLight;

        private bool isPlayerNear = false;
        private Transform playerTransform;
        private bool isReading = false;

        private void Start()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }

            if (instructionUI != null)
            {
                instructionUI.SetActive(false);
            }
            
            // Tự động thêm Light nếu chưa có
            if (paperLight == null)
            {
                paperLight = gameObject.GetComponent<Light>();
                if (paperLight == null)
                {
                    paperLight = gameObject.AddComponent<Light>();
                }
                
                paperLight.type = LightType.Point;
                paperLight.color = new Color(1f, 0.4f, 0.4f); // Đỏ nhạt
                paperLight.intensity = 1.5f; // Giảm cường độ một chút
                paperLight.range = 0.5f; // Thu hẹp phạm vi sáng để không bị tràn xuống gầm bàn
                paperLight.shadows = LightShadows.Soft; // Bật bóng đổ để bị bàn cản lại
            }
            
            // Ẩn tờ giấy nếu không phải là lần chơi mới (level 0)
            if (LevelManager.Instance != null && LevelManager.Instance.currentLevel > 0)
            {
                gameObject.SetActive(false);
                if (paperLight != null) paperLight.enabled = false;
            }
            // Ẩn tờ giấy nếu người chơi đã nhặt và cất vào túi đồ
            else if (InventoryManager.Instance != null && InventoryManager.Instance.HasItem("InstructionPaper"))
            {
                gameObject.SetActive(false);
                if (paperLight != null) paperLight.enabled = false;
            }
            // Hỗ trợ trường hợp đọc từ PlayerPrefs nếu InventoryManager chưa kịp Awake
            else if (PlayerPrefs.GetString("SavedInventory", "").Contains("InstructionPaper"))
            {
                gameObject.SetActive(false);
                if (paperLight != null) paperLight.enabled = false;
            }
        }

        private void Update()
        {
            // Kiểm tra liên tục nếu sang level khác thì ẩn giấy
            if (LevelManager.Instance != null && LevelManager.Instance.currentLevel > 0)
            {
                gameObject.SetActive(false);
                if (paperLight != null) paperLight.enabled = false;
                return;
            }

            if (playerTransform == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) playerTransform = playerObj.transform;
                else return;
            }

            // Tính khoảng cách
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            isPlayerNear = (distance <= interactDistance);

            // Ẩn tính năng nếu màn hình Intro đang hiện
            if (GameObject.Find("_ChapterIntroCanvas_Auto") != null) 
            {
                return;
            }

            bool isInteractInput = Input.GetKeyDown(KeyCode.E);

            if (isPlayerNear && isInteractInput)
            {
                isReading = !isReading;
                if (instructionUI != null)
                {
                    instructionUI.SetActive(isReading);
                }
            }
            
            // Đóng giấy hoặc cất vào túi
            if (isReading)
            {
                // Nhấn F để cất vào túi đồ
                if (Input.GetKeyDown(KeyCode.F))
                {
                    TakePaperToInventory();
                    return;
                }

                bool hideInput = Input.GetKeyDown(KeyCode.Escape) || (isInteractInput && !isPlayerNear);
                if (hideInput || !isPlayerNear)
                {
                    isReading = false;
                    if (instructionUI != null)
                    {
                        instructionUI.SetActive(false);
                    }
                }
            }
        }

        // Gọi hàm này bằng nút trên màn hình Mobile hoặc phím F (PC)
        public void TakePaperToInventory()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.AddItem("InstructionPaper");
            }
            
            isReading = false;
            if (instructionUI != null) instructionUI.SetActive(false);
            
            // Xóa giấy khỏi cảnh
            gameObject.SetActive(false);
            
            // Tắt luôn đèn của tờ giấy (nếu đèn được tạo ở object khác không phải là child)
            if (paperLight != null) 
            {
                paperLight.enabled = false;
                paperLight.gameObject.SetActive(false);
            }
        }

        private void OnGUI()
        {
            if (GameObject.Find("_ChapterIntroCanvas_Auto") != null) return;

            GUIStyle style = new GUIStyle();
            style.fontSize = 24;
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleCenter;
            
            if (isPlayerNear && !isReading && Time.timeScale > 0f)
            {
                GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height / 2 + 50, 300, 50), "Nhấn [E] để Đọc Hướng Dẫn", style);
            }
            else if (isReading && Time.timeScale > 0f)
            {
                // Hiển thị gợi ý cất giấy khi đang đọc
                GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height / 2 + 250, 300, 50), "Nhấn [F] để Cất Vào Túi", style);
            }
        }
    }
}
