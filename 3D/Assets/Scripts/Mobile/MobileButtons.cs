using UnityEngine;

namespace MobileControls
{
    public class MobileButtons : MonoBehaviour
    {
        // Các biến static để các script khác truy cập
        public static bool isSprinting = false;
        public static bool interactPressed = false;
        public static bool pausePressed = false;

        // Các hàm để gán vào sự kiện OnClick của Button trong Unity
        
        public void OnSprintDown()
        {
            isSprinting = true;
        }

        public void OnSprintUp()
        {
            isSprinting = false;
        }

        public void OnInteractClick()
        {
            interactPressed = true;
            // Tự động tắt sau 1 frame (giống GetKeyDown)
            Invoke("ResetInteract", 0.1f);
        }

        public void OnPauseClick()
        {
            pausePressed = true;
            Invoke("ResetPause", 0.1f);
        }

        private void ResetInteract() { interactPressed = false; }
        private void ResetPause() { pausePressed = false; }

        private void Start()
        {
            // Tự động ẩn UI mobile nếu đang ở PC (tùy chọn)
            // gameObject.SetActive(Application.isMobilePlatform);
        }
    }
}
