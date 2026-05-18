using UnityEngine;

namespace AnomalySystem
{
    [CreateAssetMenu(fileName = "NewItemData", menuName = "Inventory/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Tooltip("ID độc nhất của vật phẩm (ví dụ: InstructionPaper)")]
        public string itemID;
        
        [Tooltip("Tên hiển thị trong UI")]
        public string itemName;
        
        [Tooltip("Mô tả dài hiển thị bên trái")]
        [TextArea(3, 10)]
        public string itemDescription;
        
        [Tooltip("Hình ảnh Icon hiển thị trong ô vật phẩm (Dành cho ảnh 2D)")]
        public Sprite itemIcon;

        [Tooltip("Prefab hiển thị bên trong ô (Dành cho trường hợp bạn muốn nhét 1 Prefab vào ô thay vì ảnh 2D)")]
        public GameObject itemPrefab;
    }
}
