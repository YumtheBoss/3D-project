using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using AnomalySystem;

namespace GameUI
{
    public class InventorySlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI Components")]
        [Tooltip("Ảnh hiển thị icon của vật phẩm (Tự động tìm nếu để trống)")]
        public Image iconImage;
        
        [Tooltip("Hình ảnh Highlight tùy chỉnh của bạn (chỉ hiện khi click)")]
        public GameObject highlightAsset;
        
        [Tooltip("Hình ảnh viền khi rê chuột vào (tùy chọn)")]
        public GameObject hoverAsset;

        [Header("Màu sắc")]
        [Tooltip("Màu nền khi ô trống")]
        public Color emptyColor = new Color(0.15f, 0.15f, 0.2f, 0.8f); // Xám đen mờ
        [Tooltip("Màu nền khi ô có đồ")]
        public Color filledColor = new Color(0.25f, 0.25f, 0.3f, 0.9f); // Xám nhạt hơn

        private ItemData currentItem;
        private InventoryUI inventoryUI;
        private GameObject spawnedPrefab;
        private Image backgroundImage; // Ảnh nền của chính cái ô này

        private void Awake()
        {
            // Tự động tìm các thành phần con nếu chưa gán trong Inspector
            AutoFindReferences();
        }

        /// <summary>
        /// Tự động tìm IconImage và HighlightAsset nếu người dùng chưa kéo thả trong Inspector.
        /// </summary>
        private void AutoFindReferences()
        {
            // Lấy Image nền của chính ô này (để đổi màu)
            backgroundImage = GetComponent<Image>();
            
            // Đảm bảo Raycast Target được bật (để click chuột hoạt động!)
            if (backgroundImage != null)
            {
                backgroundImage.raycastTarget = true;
            }

            // Tự động tìm IconImage nếu chưa gán
            if (iconImage == null)
            {
                Transform iconTf = transform.Find("IconImage");
                if (iconTf != null)
                {
                    iconImage = iconTf.GetComponent<Image>();
                }
                else
                {
                    // Nếu không tìm thấy con tên "IconImage", tìm Image đầu tiên ở con
                    foreach (Transform child in transform)
                    {
                        Image img = child.GetComponent<Image>();
                        if (img != null && child.gameObject != gameObject)
                        {
                            // Bỏ qua nếu đây là HighlightAsset hoặc HoverAsset
                            if (child.gameObject == highlightAsset || child.gameObject == hoverAsset) continue;
                            iconImage = img;
                            break;
                        }
                    }
                }
            }

            // Tự động tìm HighlightAsset nếu chưa gán
            if (highlightAsset == null)
            {
                Transform hlTf = transform.Find("HighlightAsset");
                if (hlTf != null)
                {
                    highlightAsset = hlTf.gameObject;
                }
            }

            // Tự động tìm HoverAsset nếu chưa gán
            if (hoverAsset == null)
            {
                Transform hvTf = transform.Find("HoverAsset");
                if (hvTf != null)
                {
                    hoverAsset = hvTf.gameObject;
                }
            }
        }

        public void Setup(ItemData item, InventoryUI ui)
        {
            currentItem = item;
            inventoryUI = ui;

            // Đảm bảo đã tìm references
            if (backgroundImage == null) AutoFindReferences();

            // Xóa prefab cũ nếu ô này trước đó có chứa đồ
            if (spawnedPrefab != null)
            {
                Destroy(spawnedPrefab);
            }

            if (item != null)
            {
                Debug.Log($"[InventorySlot] Đổ vật phẩm '{item.itemName}' vào ô {gameObject.name}");

                // Đổi nền thành màu "có đồ"
                if (backgroundImage != null) backgroundImage.color = filledColor;

                // Ưu tiên 1: Dùng Prefab
                if (item.itemPrefab != null)
                {
                    if (iconImage != null) iconImage.gameObject.SetActive(false);
                    spawnedPrefab = Instantiate(item.itemPrefab, transform);
                    
                    RectTransform rt = spawnedPrefab.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchorMin = new Vector2(0.1f, 0.1f);
                        rt.anchorMax = new Vector2(0.9f, 0.9f);
                        rt.offsetMin = Vector2.zero;
                        rt.offsetMax = Vector2.zero;
                    }
                }
                // Ưu tiên 2: Dùng Icon 2D
                else if (item.itemIcon != null && iconImage != null)
                {
                    iconImage.sprite = item.itemIcon;
                    iconImage.gameObject.SetActive(true);
                }
                else
                {
                    if (iconImage != null) iconImage.gameObject.SetActive(false);
                }
            }
            else
            {
                // Ô trống → đổi nền thành màu tối
                if (backgroundImage != null) backgroundImage.color = emptyColor;
                if (iconImage != null) iconImage.gameObject.SetActive(false);
            }

            SetHighlight(false);
            if (hoverAsset != null) hoverAsset.SetActive(false);
        }

        public void SetHighlight(bool isHighlighted)
        {
            if (highlightAsset != null)
            {
                highlightAsset.SetActive(isHighlighted);
            }
        }

        // --- Các sự kiện EventSystem ---

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log($"[InventorySlot] Click ô '{gameObject.name}' | Có đồ: {currentItem != null}");
            if (currentItem != null && inventoryUI != null)
            {
                inventoryUI.OnSlotSelected(this, currentItem);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Đổi màu nền sáng hơn khi rê chuột vào
            if (backgroundImage != null)
            {
                backgroundImage.color = currentItem != null 
                    ? new Color(filledColor.r + 0.1f, filledColor.g + 0.1f, filledColor.b + 0.1f, 1f)
                    : new Color(emptyColor.r + 0.1f, emptyColor.g + 0.1f, emptyColor.b + 0.1f, 1f);
            }
            if (currentItem != null && hoverAsset != null)
            {
                hoverAsset.SetActive(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Trả lại màu nền cũ
            if (backgroundImage != null)
            {
                backgroundImage.color = currentItem != null ? filledColor : emptyColor;
            }
            if (hoverAsset != null)
            {
                hoverAsset.SetActive(false);
            }
        }
    }
}

