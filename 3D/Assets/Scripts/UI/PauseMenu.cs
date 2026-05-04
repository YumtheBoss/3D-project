using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using MobileControls;

namespace GameUI
{
    public class PauseMenu : MonoBehaviour
    {
        public string mainMenuSceneName = "MainMenu"; // Tên scene của màn hình khởi động

        [Header("Panels")]
        public GameObject pauseMenuPanel;
        public GameObject settingsPanel;
        [Tooltip("Kéo HUD Panel vào đây để ẩn khi Pause")]
        public GameObject hudPanel;
        [Tooltip("Kéo Canvas chứa chữ 'Ấn E' vào đây để ẩn khi Pause")]
        public GameObject interactionCanvas;

        [Header("Settings UI - Volume")]
        [Tooltip("Thanh chỉnh âm lượng tổng")]
        public Slider masterVolumeSlider;
        [Tooltip("Thanh chỉnh âm lượng nhạc nền")]
        public Slider musicVolumeSlider;
        [Tooltip("Thanh chỉnh âm lượng hiệu ứng (SFX)")]
        public Slider sfxVolumeSlider;

        [Header("Settings UI - Sensitivity")]
        public Slider sensitivitySlider;

        private bool isPaused = false;
        
        // Tham chiếu đến FirstPersonController để đổi tốc độ chuột ngay lập tức
        private FirstPersonController fpc;

        private void Start()
        {
            // Tự động tìm nhân vật
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) fpc = player.GetComponent<FirstPersonController>();

            // Ẩn Menu khi mới vào game
            pauseMenuPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);

            // Đọc cài đặt cũ lên thanh trượt
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
                masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
                musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
                sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
            }

            if (sensitivitySlider != null)
            {
                sensitivitySlider.value = PlayerPrefs.GetFloat("MouseSensitivity", 2f);
                sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
            }

            // Áp dụng âm lượng đã lưu ngay khi vào game
            AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        }

        private void Update()
        {
            // Nhấn phím ESC (PC) hoặc nút Pause (Mobile)
            bool isPauseInput = Input.GetKeyDown(KeyCode.Escape) || MobileButtons.pausePressed;
            if (isPauseInput)
            {
                Debug.Log("Đã bấm phím ESC!");
                if (isPaused) 
                {
                    Debug.Log("Đang tắt Pause Menu...");
                    ResumeGame();
                }
                else 
                {
                    Debug.Log("Đang bật Pause Menu...");
                    PauseGame();
                }
            }

            // DEBUG: Kiểm tra click chuột trái
            if (isPaused && Input.GetMouseButtonDown(0))
            {
                Debug.Log("[DEBUG] Click chuột trái đã phát hiện!");

                // Kiểm tra EventSystem
                if (EventSystem.current == null)
                {
                    Debug.LogError("[DEBUG] EventSystem.current = NULL! Không có EventSystem trong Scene!");
                }
                else
                {
                    // Kiểm tra xem click đang trúng vào UI element nào
                    PointerEventData pointerData = new PointerEventData(EventSystem.current);
                    pointerData.position = Input.mousePosition;

                    List<RaycastResult> results = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(pointerData, results);

                    if (results.Count == 0)
                    {
                        Debug.LogWarning("[DEBUG] Click KHÔNG trúng vào bất kỳ UI nào! Raycast trả về rỗng.");
                        Debug.LogWarning($"[DEBUG] Mouse position: {Input.mousePosition}");
                    }
                    else
                    {
                        foreach (var result in results)
                        {
                            Debug.Log($"[DEBUG] Click trúng: {result.gameObject.name} (Layer: {LayerMask.LayerToName(result.gameObject.layer)})");
                        }
                    }

                    // Kiểm tra Input Module
                    var inputModule = EventSystem.current.currentInputModule;
                    if (inputModule == null)
                        Debug.LogError("[DEBUG] Không có Input Module trên EventSystem!");
                    else
                        Debug.Log($"[DEBUG] Input Module: {inputModule.GetType().Name}");
                }
            }
        }

        public void PauseGame()
        {
            isPaused = true;
            Time.timeScale = 0f;
            
            if (fpc != null) fpc.cameraCanMove = false;
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Ẩn HUD và chữ tương tác khi Pause
            if (hudPanel != null) hudPanel.SetActive(false);
            if (interactionCanvas != null) interactionCanvas.SetActive(false);

            pauseMenuPanel.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        public void ResumeGame()
        {
            Debug.Log("[PauseMenu] Nút RESUME đã được BẤM!");
            isPaused = false;
            Time.timeScale = 1f; // Tiếp tục thời gian
            
            // Bật lại xoay camera
            if (fpc != null) fpc.cameraCanMove = true;
            
            // Ẩn và khoá chuột
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            pauseMenuPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);

            // Hiện lại HUD và chữ tương tác
            if (hudPanel != null) hudPanel.SetActive(true);
            if (interactionCanvas != null) interactionCanvas.SetActive(true);
        }

        public void ShowSettings()
        {
            Debug.Log("[PauseMenu] Nút SETTINGS đã được BẤM!");
            pauseMenuPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        public void BackToPauseMenu()
        {
            Debug.Log("[PauseMenu] Nút BACK đã được BẤM!");
            if (settingsPanel != null) settingsPanel.SetActive(false);
            pauseMenuPanel.SetActive(true);
        }

        public void GoToMainMenu()
        {
            Debug.Log("[PauseMenu] Nút MAIN MENU đã được BẤM!");
            // RẤT QUAN TRỌNG: Phải đưa thời gian chạy lại bình thường trước khi load Scene
            Time.timeScale = 1f; 
            SceneManager.LoadScene(mainMenuSceneName);
        }

        // ========== SETTINGS ==========

        public void SetMasterVolume(float value)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetMasterVolume(value);
            else
                AudioListener.volume = value;
        }

        public void SetMusicVolume(float value)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetMusicVolume(value);
        }

        public void SetSFXVolume(float value)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetSFXVolume(value);
        }

        public void SetSensitivity(float value)
        {
            PlayerPrefs.SetFloat("MouseSensitivity", value);
            // Cập nhật ngay lập tức cho nhân vật đang chơi (không cần load lại game)
            if (fpc != null)
            {
                fpc.mouseSensitivity = value;
            }
        }

        // ========== TIỆN ÍCH ==========

        private void PlayClickSound()
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayButtonClick();
        }
    }
}
