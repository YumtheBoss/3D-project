using UnityEngine;
using TMPro;
using AnomalySystem;

namespace GameUI
{
    public class LevelDisplay : MonoBehaviour
    {
        [Tooltip("Kéo chữ TextMeshPro từ trên Hierarchy xuống đây")]
        public TextMeshProUGUI levelText;

        [Tooltip("Chữ hiển thị kèm theo, ví dụ nhập 'Exit ' thì sẽ hiện ra 'Exit 1'")]
        public string prefix = "Exit ";

        private void Start()
        {
            // Tự động kết nối với hệ thống để lắng nghe sự kiện đổi Level
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.OnLevelChanged.AddListener(UpdateLevelText);
                
                // Cập nhật số Level ngay khi vừa vào game
                UpdateLevelText(LevelManager.Instance.currentLevel);
            }
        }

        // Hàm này sẽ tự động chạy mỗi khi biến currentLevel trong LevelManager bị thay đổi
        public void UpdateLevelText(int newLevel)
        {
            if (levelText != null)
            {
                levelText.text = prefix + newLevel.ToString();
            }
        }

        private void OnDestroy()
        {
            // Huỷ lắng nghe khi người chơi thoát màn để tránh lỗi rò rỉ
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.OnLevelChanged.RemoveListener(UpdateLevelText);
            }
        }
    }
}
