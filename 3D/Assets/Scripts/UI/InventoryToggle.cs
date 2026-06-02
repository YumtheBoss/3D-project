using UnityEngine;
using GameUI;

namespace GameUI
{
    /// <summary>
    /// Script gắn vào Canvas hoặc một GameObject luôn Active trong Scene để lắng nghe phím bấm đóng/mở túi đồ.
    /// Mặc định dùng phím Tab hoặc phím I để mở.
    /// </summary>
    public class InventoryToggle : MonoBehaviour
    {
        [Header("Giao diện")]
        [Tooltip("Kéo thả GameObject Panel túi đồ (InventoryPanel) vào đây")]
        public GameObject inventoryPanel;

        [Header("Phím bấm")]
        [Tooltip("Phím chính để mở túi đồ")]
        public KeyCode toggleKey = KeyCode.Tab;
        [Tooltip("Phím phụ để mở túi đồ")]
        public KeyCode alternativeKey = KeyCode.I;

        private bool isOpen = false;
        private FirstPersonController playerController;

        private void Start()
        {
            // Tự động kiểm tra và tránh xung đột với PauseMenu (Self-Healing System)
            if (FindAnyObjectByType<PauseMenu>() != null)
            {
                Debug.Log("[InventoryToggle] Phát hiện script PauseMenu trong Scene. Tự động vô hiệu hóa InventoryToggle để tránh xung đột phím bấm desync!");
                enabled = false;
                return;
            }

            // Self-healing: Tự động tìm kiếm InventoryPanel trong scene nếu người dùng quên kéo thả hoặc bị mất liên kết (Missing)
            if (inventoryPanel == null)
            {
                InventoryUI ui = FindAnyObjectByType<InventoryUI>(FindObjectsInactive.Include);
                if (ui != null)
                {
                    inventoryPanel = ui.gameObject;
                    Debug.Log($"[InventoryToggle] Tự động tìm thấy và liên kết InventoryPanel tại runtime: {inventoryPanel.name}");
                }
            }

            // Ẩn bảng túi đồ khi bắt đầu game
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(false);
            }

            // Tìm sẵn FirstPersonController trong màn chơi
            playerController = FindAnyObjectByType<FirstPersonController>();
        }

        private void Update()
        {
            // Lắng nghe sự kiện nhấn phím Tab hoặc I
            if (Input.GetKeyDown(toggleKey) || Input.GetKeyDown(alternativeKey))
            {
                ToggleInventory();
            }
        }

        /// <summary>
        /// Đóng hoặc mở túi đồ, tự động xử lý khóa/mở chuột và dừng di chuyển của nhân vật.
        /// </summary>
        public void ToggleInventory()
        {
            if (inventoryPanel == null)
            {
                Debug.LogWarning("[InventoryToggle] Chưa kéo thả InventoryPanel vào trường Inspector!");
                return;
            }

            isOpen = !isOpen;
            inventoryPanel.SetActive(isOpen);

            // Tìm lại nhân vật nếu trước đó chưa tìm thấy (đề phòng chuyển scene hoặc spawn muộn)
            if (playerController == null)
            {
                playerController = FindAnyObjectByType<FirstPersonController>();
            }

            if (isOpen)
            {
                // 1. Mở khóa chuột và hiển thị con trỏ để người chơi tương tác với túi đồ
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                // 2. Dừng di chuyển và khóa xoay camera của nhân vật
                if (playerController != null)
                {
                    playerController.enabled = false;
                }

                // 3. Làm mới giao diện để hiển thị các vật phẩm mới nhất vừa nhặt được
                InventoryUI ui = inventoryPanel.GetComponent<InventoryUI>();
                if (ui != null)
                {
                    ui.RefreshUI();
                }
            }
            else
            {
                // 1. Khóa và ẩn con trỏ chuột quay lại góc màn hình để chơi game tiếp
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                // 2. Cho phép nhân vật di chuyển và xoay camera bình thường
                if (playerController != null)
                {
                    playerController.enabled = true;
                }
            }
        }
    }
}
