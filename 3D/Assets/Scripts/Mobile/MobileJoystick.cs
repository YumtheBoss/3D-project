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
            
            inputVector = Vector2.zero; // Đảm bảo reset lại khi mới vào scene
        }

        private void OnEnable()
        {
            inputVector = Vector2.zero;
        }

        private void OnDisable()
        {
            inputVector = Vector2.zero;
        }

        private void Update()
        {
            // Fix lỗi kẹt joystick khi chuột bị kéo thả ra ngoài cửa sổ Unity hoặc màn hình điện thoại
            if (!Application.isMobilePlatform && !Input.GetMouseButton(0) && inputVector != Vector2.zero)
            {
                OnPointerUp(null);
            }
            else if (Application.isMobilePlatform && Input.touchCount == 0 && inputVector != Vector2.zero)
            {
                OnPointerUp(null);
            }
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
