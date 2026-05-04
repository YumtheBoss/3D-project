using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameUI
{
    public class MainMenu : MonoBehaviour
    {
        public string gameSceneName = "SampleScene"; // Tên scene chứa game

        [Header("Panels")]
        public GameObject mainMenuPanel;
        public GameObject settingsPanel;

        [Header("Settings UI - Volume")]
        [Tooltip("Thanh chỉnh âm lượng tổng")]
        public Slider masterVolumeSlider;
        [Tooltip("Thanh chỉnh âm lượng nhạc nền")]
        public Slider musicVolumeSlider;
        [Tooltip("Thanh chỉnh âm lượng hiệu ứng (SFX)")]
        public Slider sfxVolumeSlider;

        [Header("Settings UI - Sensitivity")]
        public Slider sensitivitySlider;

        private void Start()
        {
            // Bật Main Menu, Tắt Settings
            ShowMainMenu();

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
                // Mặc định tốc độ chuột là 2
                sensitivitySlider.value = PlayerPrefs.GetFloat("MouseSensitivity", 2f);
                sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
            }
            
            // Mở khoá con trỏ chuột ở Main Menu để bấm nút
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // ========== NÚT BẤM ==========

        public void ClickContinue()
        {
            PlayClickSound();
            SceneManager.LoadScene(gameSceneName);
        }

        public void ClickNewGame()
        {
            PlayClickSound();
            // Xoá file save (đặt lại level 0) rồi mới load game
            PlayerPrefs.SetInt("SavedLevel", 0);
            PlayerPrefs.Save();
            SceneManager.LoadScene(gameSceneName);
        }

        public void ShowSettings()
        {
            PlayClickSound();
            mainMenuPanel.SetActive(false);
            settingsPanel.SetActive(true);
        }

        public void ShowMainMenu()
        {
            PlayClickSound();
            settingsPanel.SetActive(false);
            mainMenuPanel.SetActive(true);
        }

        public void ClickExit()
        {
            PlayClickSound();
            Debug.Log("Đã thoát Game!");
            Application.Quit();
        }

        // ========== SETTINGS ==========

        public void SetMasterVolume(float value)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetMasterVolume(value);
            else
                AudioListener.volume = value; // Fallback nếu không có AudioManager
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
        }

        // ========== TIỆN ÍCH ==========

        private void PlayClickSound()
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayButtonClick();
        }
    }
}
