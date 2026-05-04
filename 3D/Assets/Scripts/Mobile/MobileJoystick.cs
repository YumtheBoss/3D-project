using UnityEngine;
using UnityEngine.EventSystems;

namespace MobileControls
{
    public class MobileJoystick : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
    {
        [Header("Settings")]
        public RectTransform joystickBackground;
        public RectTransform joystickHandle;
        public float joystickRange = 100f;

        public static Vector2 inputVector = Vector2.zero;

        private Vector2 pos;

        private void Start()
        {
            if (joystickBackground == null) joystickBackground = GetComponent<RectTransform>();
            if (joystickHandle == null) joystickHandle = transform.GetChild(0).GetComponent<RectTransform>();
            
            // Ẩn joystick nếu không phải mobile (tùy chọn)
            // if (!Application.isMobilePlatform) gameObject.SetActive(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(joystickBackground, eventData.position, eventData.pressEventCamera, out pos))
            {
                pos.x = (pos.x / joystickBackground.sizeDelta.x);
                pos.y = (pos.y / joystickBackground.sizeDelta.y);

                inputVector = new Vector2(pos.x * 2, pos.y * 2);
                inputVector = (inputVector.magnitude > 1.0f) ? inputVector.normalized : inputVector;

                // Di chuyển Handle
                joystickHandle.anchoredPosition = new Vector2(inputVector.x * (joystickBackground.sizeDelta.x / 2), inputVector.y * (joystickBackground.sizeDelta.y / 2));
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            inputVector = Vector2.zero;
            joystickHandle.anchoredPosition = Vector2.zero;
        }
    }
}
