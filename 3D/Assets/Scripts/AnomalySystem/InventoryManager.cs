using UnityEngine;
using System.Collections.Generic;

namespace AnomalySystem
{
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        public List<string> inventoryList = new List<string>();
        
        [Header("Item Database")]
        [Tooltip("Kéo thả tất cả các ScriptableObject ItemData vào đây")]
        public List<ItemData> itemDatabase = new List<ItemData>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            LoadInventory();
        }

        public void AddItem(string itemID)
        {
            if (!inventoryList.Contains(itemID))
            {
                inventoryList.Add(itemID);
                SaveInventory();
                Debug.Log("[Inventory] Đã thêm vật phẩm: " + itemID);
            }
        }

        public bool HasItem(string itemID)
        {
            return inventoryList.Contains(itemID);
        }

        public void RemoveItem(string itemID)
        {
            if (inventoryList.Contains(itemID))
            {
                inventoryList.Remove(itemID);
                SaveInventory();
            }
        }

        public void ClearInventory()
        {
            inventoryList.Clear();
            SaveInventory();
        }

        private void SaveInventory()
        {
            string data = string.Join(",", inventoryList);
            PlayerPrefs.SetString("SavedInventory", data);
            PlayerPrefs.Save();
        }

        public void LoadInventory()
        {
            string data = PlayerPrefs.GetString("SavedInventory", "");
            inventoryList = new List<string>();
            if (!string.IsNullOrEmpty(data))
            {
                inventoryList.AddRange(data.Split(','));
            }
        }

        // Lấy thông tin chi tiết của vật phẩm từ ID
        public ItemData GetItemData(string itemID)
        {
            foreach (var item in itemDatabase)
            {
                if (item != null && item.itemID == itemID)
                {
                    return item;
                }
            }
            return null;
        }
    }
}
