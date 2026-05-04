using System.Collections.Generic;
using UnityEngine;

namespace AnomalySystem
{
    public class AnomalyManager : MonoBehaviour
    {
        [Header("Anomaly Settings")]
        [Tooltip("Tỷ lệ xuất hiện sự bất thường (0-100)")]
        [Range(0f, 100f)]
        public float anomalyProbability = 50f;

        [Tooltip("Danh sách tất cả các đồ vật có thể bị thay đổi")]
        public List<AnomalyObject> anomalyObjects = new List<AnomalyObject>();

        [Header("Testing / Debug")]
        [Tooltip("Kéo 1 Anomaly vào đây để ÉP game luôn hiện Anomaly này (dùng để test). Bỏ trống ô này để game chạy random bình thường.")]
        public AnomalyObject forceTestAnomaly = null;

        [Header("Debug/Info")]
        public bool isCurrentLevelAnomaly;
        public AnomalyObject activeAnomaly;
        [Tooltip("Số màn bình thường liên tiếp đã trôi qua")]
        public int consecutiveNormalRooms = 0;
        [Tooltip("Số màn bất thường liên tiếp đã trôi qua")]
        public int consecutiveAnomalyRooms = 0;

        // Danh sách các Anomaly CHƯA được hiển thị trong lượt chơi hiện tại
        private List<AnomalyObject> availableAnomalies = new List<AnomalyObject>();

        public void GenerateNewLevel(int currentLevel)
        {
            // 1. Đặt tất cả về trạng thái bình thường
            foreach (var anomaly in anomalyObjects)
            {
                if (anomaly != null) anomaly.SetNormal();
            }
            activeAnomaly = null;
            isCurrentLevelAnomaly = false;

            if (anomalyObjects.Count == 0 && forceTestAnomaly == null)
            {
                Debug.LogWarning("Không có AnomalyObject nào trong danh sách AnomalyManager!");
                return;
            }

            // 2. Nếu đang có vật thể trong ô Force Test, ÉP BUỘC chạy vật thể đó
            if (forceTestAnomaly != null)
            {
                isCurrentLevelAnomaly = true;
                activeAnomaly = forceTestAnomaly;
                activeAnomaly.SetAnomaly();
                Debug.Log($"[AnomalyManager] [CHẾ ĐỘ TEST] Đã ép buộc bật Anomaly ở: {activeAnomaly.gameObject.name}");
                return;
            }

            // 3. NẾU LÀ MÀN 0: Mặc định luôn BÌNH THƯỜNG (Không có Anomaly)
            if (currentLevel == 0)
            {
                consecutiveNormalRooms = 0; // Reset bộ đếm khi quay lại vạch xuất phát
                consecutiveAnomalyRooms = 0;
                
                // NẠP LẠI TOÀN BỘ ANOMALY MỚI KHI BẮT ĐẦU LƯỢT CHƠI
                availableAnomalies.Clear();
                availableAnomalies.AddRange(anomalyObjects);

                Debug.Log("[AnomalyManager] Màn chơi số 0 -> Mặc định BÌNH THƯỜNG. Đã làm mới danh sách Anomaly.");
                return;
            }

            // Kiểm tra xem đã đạt giới hạn 2 phòng bình thường liên tiếp chưa
            bool forceAnomaly = false;

            if (consecutiveNormalRooms >= 2)
            {
                forceAnomaly = true;
                Debug.Log("[AnomalyManager] Đã có 2 phòng BÌNH THƯỜNG liên tiếp -> ÉP BUỘC màn này phải CÓ Anomaly!");
            }

            // 4. Nếu không Test và không phải màn 0, quay random bình thường
            float randomValue = Random.Range(0f, 100f);
            
            // Chỉ xảy ra Anomaly nếu: Bị ép Anomaly HOẶC random trúng
            if (forceAnomaly || randomValue <= anomalyProbability)
            {
                isCurrentLevelAnomaly = true;
                consecutiveNormalRooms = 0; // Đã ra Anomaly thì reset bộ đếm bình thường
                consecutiveAnomalyRooms++;  // Tăng bộ đếm Anomaly

                // Nếu đã xài hết sạch Anomaly trong danh sách, nạp lại từ đầu
                if (availableAnomalies.Count == 0)
                {
                    Debug.Log("[AnomalyManager] Đã dùng hết toàn bộ Anomaly. Đang nạp lại danh sách mới...");
                    availableAnomalies.AddRange(anomalyObjects);
                }

                // 5. Bốc thăm 1 vật thể từ danh sách CÒN LẠI (chưa xuất hiện)
                int randomIndex = Random.Range(0, availableAnomalies.Count);
                activeAnomaly = availableAnomalies[randomIndex];
                
                // XOÁ vật thể này khỏi danh sách để các vòng sau không bốc trúng nữa
                availableAnomalies.RemoveAt(randomIndex);

                activeAnomaly.SetAnomaly();

                Debug.Log($"[AnomalyManager] Màn này CÓ bất thường ở vật thể: {activeAnomaly.gameObject.name}. (Còn lại {availableAnomalies.Count} Anomaly chưa xuất hiện)");
            }
            else
            {
                isCurrentLevelAnomaly = false;
                consecutiveAnomalyRooms = 0; // Đã ra bình thường thì reset bộ đếm Anomaly
                consecutiveNormalRooms++;    // Tăng bộ đếm bình thường
                Debug.Log($"[AnomalyManager] Màn này BÌNH THƯỜNG. Số phòng bình thường liên tiếp đang là: {consecutiveNormalRooms}");
            }
        }
    }
}
