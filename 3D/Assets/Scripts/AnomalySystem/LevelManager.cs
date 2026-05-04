using UnityEngine;
using UnityEngine.Events;

namespace AnomalySystem
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [Header("References")]
        public AnomalyManager anomalyManager;
        public Transform player;
        [Tooltip("Điểm xuất phát của player đầu mỗi loop")]
        public Transform startPoint; 

        [Header("Game Progress")]
        public int currentLevel = 0;
        [Tooltip("Đạt level này thì thắng (vd Exit 8)")]
        public int targetLevel = 8; 

        [Header("Events")]
        [Tooltip("Dùng để update UI hiển thị level")]
        public UnityEvent<int> OnLevelChanged; 
        public UnityEvent OnGameWon;
        [Tooltip("Dùng để gọi Jumpscare hoặc âm thanh thua cuộc")]
        public UnityEvent OnGameLost; 

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Đọc màn chơi đã lưu từ máy tính (mặc định là 0 nếu chưa có)
            currentLevel = PlayerPrefs.GetInt("SavedLevel", 0);
            
            OnLevelChanged?.Invoke(currentLevel);
            StartNewLoop();
        }

        // Hàm được gọi bởi cánh cửa (DoorChoice)
        public void OnPlayerMakeChoice(bool choseAnomalyDoor)
        {
            bool isActuallyAnomaly = anomalyManager.isCurrentLevelAnomaly;

            if (choseAnomalyDoor == isActuallyAnomaly)
            {
                // Chọn ĐÚNG
                currentLevel++;
                
                // LƯU TIẾN ĐỘ GAME
                PlayerPrefs.SetInt("SavedLevel", currentLevel);
                PlayerPrefs.Save();

                Debug.Log($"[LevelManager] Lựa chọn ĐÚNG. Lên Level {currentLevel}");
                OnLevelChanged?.Invoke(currentLevel);

                if (currentLevel >= targetLevel)
                {
                    Debug.Log("[LevelManager] BẠN ĐÃ CHIẾN THẮNG!");
                    // Khi thắng, xoá dữ liệu lưu để có thể chơi lại từ đầu
                    PlayerPrefs.SetInt("SavedLevel", 0);
                    PlayerPrefs.Save();
                    OnGameWon?.Invoke();
                    return; // Ngừng vòng lặp
                }
            }
            else
            {
                // Chọn SAI
                currentLevel = 0;
                
                // LƯU TIẾN ĐỘ GAME (RESET về 0)
                PlayerPrefs.SetInt("SavedLevel", 0);
                PlayerPrefs.Save();

                Debug.Log("[LevelManager] Lựa chọn SAI. Reset Level về 0.");
                OnLevelChanged?.Invoke(currentLevel);
                OnGameLost?.Invoke();
            }

            // Bắt đầu loop mới
            StartNewLoop();
        }

        private void StartNewLoop()
        {
            // 1. Đưa người chơi về điểm xuất phát
            if (player != null && startPoint != null)
            {
                // Tắt CharacterController (nếu có) trước khi dịch chuyển
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                // Tắt Rigidbody velocity (nếu có) và dùng rb.position để dịch chuyển an toàn
                Rigidbody rb = player.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.position = startPoint.position;
                    rb.rotation = startPoint.rotation;
                    player.position = startPoint.position; // Dự phòng
                }
                else
                {
                    player.position = startPoint.position;
                    player.rotation = startPoint.rotation;
                }

                if (cc != null) cc.enabled = true;
            }

            // 2. Tạo màn chơi mới
            if (anomalyManager != null)
            {
                anomalyManager.GenerateNewLevel(currentLevel);
            }
        }
    }
}
