using UnityEngine;
using TMPro; // Nếu dùng TextMeshPro
using System.Collections.Generic;
using AnomalySystem;

namespace GameUI
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("Left Panel - Description")]
        [Tooltip("Text hiển thị tên vật phẩm")]
        public TextMeshProUGUI itemNameText;
        [Tooltip("Text hiển thị mô tả chi tiết")]
        public TextMeshProUGUI itemDescriptionText;

        [Header("Right Panel - Slots")]
        [Tooltip("Transform chứa Grid Layout Group (nơi sinh ra các ô)")]
        public Transform slotsParent;
        [Tooltip("Prefab của ô vật phẩm (đã gắn script InventorySlot)")]
        public GameObject slotPrefab;

        private InventorySlot currentSelectedSlot;

        private void OnEnable()
        {
            RefreshUI();
        }

        [Tooltip("Tổng số ô trong túi đồ (bao gồm cả ô trống)")]
        public int maxSlots = 16;

        public void RefreshUI()
        {
            // 1. Clear thông tin bên trái
            ClearDescription();
            currentSelectedSlot = null;

            if (InventoryManager.Instance == null) return;
            
            List<string> items = InventoryManager.Instance.inventoryList;

            // 2. Tìm tất cả các ô đồ ĐÃ ĐƯỢC TẠO SẴN trong slotsParent
            // Cú pháp này sẽ lấy toàn bộ các object con đang có chứa script InventorySlot
            InventorySlot[] allSlots = slotsParent.GetComponentsInChildren<InventorySlot>(true);

            // 3. Đổ dữ liệu vật phẩm vào các ô đã tạo sẵn
            for (int i = 0; i < allSlots.Length; i++)
            {
                if (i < items.Count)
                {
                    // Nếu người chơi có đồ ở vị trí i, lấy dữ liệu và hiển thị
                    ItemData data = InventoryManager.Instance.GetItemData(items[i]);
                    allSlots[i].Setup(data, this);
                }
                else
                {
                    // Ô trống, giấu Icon đi
                    allSlots[i].Setup(null, this);
                }
            }
        }

        // Hàm được gọi từ InventorySlot khi người dùng click vào
        public void OnSlotSelected(InventorySlot slot, ItemData data)
        {
            // Tắt highlight của slot cũ (nếu có)
            if (currentSelectedSlot != null)
            {
                currentSelectedSlot.SetHighlight(false);
            }

            // Bật highlight cho slot mới
            currentSelectedSlot = slot;
            currentSelectedSlot.SetHighlight(true);

            // Cập nhật text bên trái
            if (itemNameText != null) itemNameText.text = data.itemName;
            if (itemDescriptionText != null) itemDescriptionText.text = data.itemDescription;
        }

        private void ClearDescription()
        {
            if (itemNameText != null) itemNameText.text = "Chọn vật phẩm";
            if (itemDescriptionText != null) itemDescriptionText.text = "";
        }
    }
}
