using UnityEngine;
using UnityEngine.EventSystems;

namespace MobileControls
{
    public class MobileTouchCamera : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        public static Vector2 lookInput = Vector2.zero;
        
        [Header("Settings")]
        [Tooltip("Khu vực nhận cảm ứng (thường là nửa phải màn hình)")]
        public RectTransform touchArea;

        public void OnPointerDown(PointerEventData eventData)
        {
            // Bắt đầu tính toán xoay
        }

        public void OnDrag(PointerEventData eventData)
        {
            // deltaX, deltaY giống như Mouse X, Mouse Y
            lookInput = eventData.delta;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            lookInput = Vector2.zero;
        }

        private void Update()
        {
            // Reset input nếu không có chạm (để tránh camera tự xoay mãi mãi)
            if (Input.touchCount == 0 && !Input.GetMouseButton(0))
            {
                lookInput = Vector2.zero;
            }
        }
    }
}
