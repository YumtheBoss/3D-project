using UnityEngine;

namespace GameUI
{
    /// <summary>
    /// Script tạo hiệu ứng chuyển động lắc lư (sway) nhẹ nhàng cho Camera bằng Perlin Noise.
    /// Giúp cho màn hình Main Menu 3D trông có hồn, sống động như phim điện ảnh.
    /// Gắn script này trực tiếp vào "Main Camera" của cảnh MainMenu.
    /// </summary>
    public class CameraSway : MonoBehaviour
    {
        [Header("Position Sway (Di chuyển nhẹ)")]
        [Tooltip("Bật/tắt lắc lư vị trí")]
        public bool swayPosition = true;
        [Tooltip("Biên độ lắc lư theo các trục X, Y, Z")]
        public Vector3 positionAmount = new Vector3(0.04f, 0.04f, 0f);
        [Tooltip("Tốc độ chuyển động vị trí")]
        public float positionSpeed = 0.5f;

        [Header("Rotation Sway (Quay nhẹ)")]
        [Tooltip("Bật/tắt lắc lư góc quay")]
        public bool swayRotation = true;
        [Tooltip("Biên độ xoay theo góc Pitch, Yaw, Roll")]
        public Vector3 rotationAmount = new Vector3(0.5f, 0.8f, 0.3f);
        [Tooltip("Tốc độ chuyển động xoay")]
        public float rotationSpeed = 0.4f;

        // Vị trí và góc quay ban đầu
        private Vector3 startPosition;
        private Quaternion startRotation;

        // Điểm bắt đầu tính toán Perlin Noise ngẫu nhiên để không bị trùng lặp
        private float noiseOffsetX;
        private float noiseOffsetY;
        private float noiseOffsetZ;

        private void Start()
        {
            // Lưu lại vị trí và góc quay mặc định
            startPosition = transform.localPosition;
            startRotation = transform.localRotation;

            // Sinh offset ngẫu nhiên cho thuật toán Noise
            noiseOffsetX = Random.Range(0f, 100f);
            noiseOffsetY = Random.Range(100f, 200f);
            noiseOffsetZ = Random.Range(200f, 300f);
        }

        private void Update()
        {
            float time = Time.unscaledTime; // Sử dụng unscaledTime để hiệu ứng vẫn chạy tốt ngay cả khi game bị Pause
 
            // 1. Tính toán chuyển động lắc lư vị trí (sử dụng các hàm Sin/Cos kết hợp có tần số khác nhau để mô phỏng chuyển động tự nhiên, siêu nhẹ cho CPU)
            if (swayPosition)
            {
                float posX = Mathf.Sin(time * positionSpeed * 1.3f) * Mathf.Cos(time * positionSpeed * 0.9f);
                float posY = Mathf.Sin(time * positionSpeed * 0.8f) * Mathf.Cos(time * positionSpeed * 1.4f);
                float posZ = Mathf.Sin(time * positionSpeed * 1.1f) * Mathf.Cos(time * positionSpeed * 0.7f);
 
                Vector3 targetPosOffset = new Vector3(
                    posX * positionAmount.x,
                    posY * positionAmount.y,
                    posZ * positionAmount.z
                );
 
                transform.localPosition = startPosition + targetPosOffset;
            }
 
            // 2. Tính toán chuyển động xoay
            if (swayRotation)
            {
                float rotX = Mathf.Sin(time * rotationSpeed * 1.2f) * Mathf.Cos(time * rotationSpeed * 0.8f);
                float rotY = Mathf.Sin(time * rotationSpeed * 0.7f) * Mathf.Cos(time * rotationSpeed * 1.3f);
                float rotZ = Mathf.Sin(time * rotationSpeed * 1.0f) * Mathf.Cos(time * rotationSpeed * 0.9f);
 
                Vector3 targetRotOffset = new Vector3(
                    rotX * rotationAmount.x,
                    rotY * rotationAmount.y,
                    rotZ * rotationAmount.z
                );
 
                transform.localRotation = startRotation * Quaternion.Euler(targetRotOffset);
            }
        }
    }
}
