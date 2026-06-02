using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

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
        [Tooltip("Kéo Inventory Panel vào đây để bật/tắt bằng phím Tab/I")]
        public GameObject inventoryPanel;

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
        private bool isInventoryOpen = false;
        
        // Tham chiếu đến FirstPersonController để đổi tốc độ chuột ngay lập tức
        private FirstPersonController fpc;

        private void Awake()
        {
            // Tự động kiểm tra và tạo EventSystem nếu thiếu trong Scene (Self-Healing)
            EnsureEventSystemExists();

            // Tự động tìm kiếm và liên kết các thành phần bị thiếu (Self-Healing System)
            SelfHealReferences();
        }

        private void EnsureEventSystemExists()
        {
            if (EventSystem.current == null && Object.FindAnyObjectByType<EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem_Auto");
                eventSystemObj.AddComponent<EventSystem>();
                
#if ENABLE_INPUT_SYSTEM
                eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                eventSystemObj.AddComponent<StandaloneInputModule>();
#endif
                Debug.Log("[PauseMenu Self-Heal] Phát hiện thiếu EventSystem trong Scene! Đã tự động tạo 'EventSystem_Auto'.");
            }
        }

        private void Start()
        {
            // Tự động tìm nhân vật
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) fpc = player.GetComponent<FirstPersonController>();

            // Ẩn Menu khi mới vào game (Bổ sung null-check an toàn chống UnassignedReferenceException)
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (inventoryPanel != null) inventoryPanel.SetActive(false);

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
            // DEBUG: Kiểm tra xem script có đang chạy không
            if (Input.anyKeyDown) Debug.Log($"[PauseMenu] Phím vừa nhấn: {Input.inputString}");

            // Nhấn phím ESC (PC) hoặc nút Pause (Mobile)
            bool isPauseInput = Input.GetKeyDown(KeyCode.Escape);
            if (isPauseInput)
            {
                Debug.Log("Đã bấm phím ESC!");
                if (isInventoryOpen)
                {
                    CloseInventory();
                }
                else if (isPaused) 
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

            // Nhấn phím Tab hoặc I để mở Túi Đồ
            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I))
            {
                if (isInventoryOpen)
                {
                    CloseInventory();
                }
                else if (!isPaused) // Không cho phép mở túi đồ khi đang Pause
                {
                    OpenInventory();
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

        public void OpenInventory()
        {
            isInventoryOpen = true;
            Time.timeScale = 0f;
            
            if (fpc != null) fpc.cameraCanMove = false;
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            FirstPersonController.IsUIOpen = true;

            if (hudPanel != null) hudPanel.SetActive(false);
            if (interactionCanvas != null) interactionCanvas.SetActive(false);

            if (inventoryPanel != null) inventoryPanel.SetActive(true);

            // Dừng bộ đếm thời gian chơi trong RoomManager
            if (RoomManager.Instance != null)
            {
                RoomManager.Instance.isTimerPaused = true;
            }
        }

        public void CloseInventory()
        {
            isInventoryOpen = false;
            Time.timeScale = 1f;
            
            if (fpc != null) 
            {
                fpc.cameraCanMove = true;
                fpc.enabled = true; // Tự động khôi phục hoạt động của nhân vật (Self-Healing)
            }
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            FirstPersonController.IsUIOpen = false;

            if (inventoryPanel != null) inventoryPanel.SetActive(false);

            if (hudPanel != null) hudPanel.SetActive(true);
            if (interactionCanvas != null) interactionCanvas.SetActive(true);

            // Tiếp tục bộ đếm thời gian chơi trong RoomManager
            if (RoomManager.Instance != null)
            {
                RoomManager.Instance.isTimerPaused = false;
            }
        }

        private void EnsurePausePanelChildrenActive()
        {
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(true);
                
                // Đảm bảo CanvasGroup luôn hiện và nhận tương tác (Self-Healing)
                CanvasGroup cg = pauseMenuPanel.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 1f;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }

                foreach (Transform child in pauseMenuPanel.transform)
                {
                    child.gameObject.SetActive(true);

                    CanvasGroup childCg = child.GetComponent<CanvasGroup>();
                    if (childCg != null)
                    {
                        childCg.alpha = 1f;
                        childCg.interactable = true;
                        childCg.blocksRaycasts = true;
                    }

                    foreach (Transform grandChild in child)
                    {
                        grandChild.gameObject.SetActive(true);
                        foreach (Transform greatGrandChild in grandChild)
                        {
                            greatGrandChild.gameObject.SetActive(true);
                        }
                    }
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
            FirstPersonController.IsUIOpen = true;

            // Ẩn HUD và chữ tương tác khi Pause
            if (hudPanel != null) hudPanel.SetActive(false);
            if (interactionCanvas != null) interactionCanvas.SetActive(false);

            EnsurePausePanelChildrenActive();
            if (settingsPanel != null) settingsPanel.SetActive(false);

            // Dừng bộ đếm thời gian chơi trong RoomManager
            if (RoomManager.Instance != null)
            {
                RoomManager.Instance.isTimerPaused = true;
            }
        }

        public void ResumeGame()
        {
            Debug.Log("[PauseMenu] Nút RESUME đã được BẤM!");
            isPaused = false;
            Time.timeScale = 1f; // Tiếp tục thời gian
            
            // Bật lại xoay camera và khôi phục hoạt động của nhân vật (Self-Healing)
            if (fpc != null) 
            {
                fpc.cameraCanMove = true;
                fpc.enabled = true; 
            }
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            FirstPersonController.IsUIOpen = false;

            pauseMenuPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);

            // Hiện lại HUD và chữ tương tác
            if (hudPanel != null) hudPanel.SetActive(true);
            if (interactionCanvas != null) interactionCanvas.SetActive(true);

            // Tiếp tục bộ đếm thời gian chơi trong RoomManager
            if (RoomManager.Instance != null)
            {
                RoomManager.Instance.isTimerPaused = false;
            }
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
            EnsurePausePanelChildrenActive();
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

        // ========== TỰ ĐỘNG LIÊN KẾT (SELF-HEALING) ==========

        private void SelfHealReferences()
        {
            // 1. Tìm Canvas của Pause Menu hoặc tự quét trong scene
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null)
            {
                parentCanvas = Object.FindAnyObjectByType<Canvas>();
            }

            if (parentCanvas != null)
            {
                // Tìm pauseMenuPanel nếu trống
                if (pauseMenuPanel == null)
                {
                    pauseMenuPanel = FindChildRecursive(parentCanvas.gameObject, "PauseMenu Panel") 
                                     ?? FindChildRecursive(parentCanvas.gameObject, "PauseMenuPanel") 
                                     ?? FindChildRecursive(parentCanvas.gameObject, "Pause Panel")
                                     ?? FindChildRecursive(parentCanvas.gameObject, "PausePanel");
                    if (pauseMenuPanel != null) Debug.Log($"[PauseMenu Self-Heal] Đã tự tìm thấy pauseMenuPanel: '{pauseMenuPanel.name}'!");
                }

                // Tìm settingsPanel nếu trống
                if (settingsPanel == null)
                {
                    settingsPanel = FindChildRecursive(parentCanvas.gameObject, "Setting Panel") 
                                    ?? FindChildRecursive(parentCanvas.gameObject, "SettingPanel") 
                                    ?? FindChildRecursive(parentCanvas.gameObject, "SettingsPanel")
                                    ?? FindChildRecursive(parentCanvas.gameObject, "Settings Panel");
                    if (settingsPanel != null) Debug.Log($"[PauseMenu Self-Heal] Đã tự tìm thấy settingsPanel: '{settingsPanel.name}'!");
                }
            }

            // 2. Tìm HUD panel nếu trống
            if (hudPanel == null)
            {
                hudPanel = GameObject.Find("HUDPanel") ?? GameObject.Find("HUD Panel") ?? GameObject.Find("HUD");
                if (hudPanel != null) Debug.Log($"[PauseMenu Self-Heal] Đã tự tìm thấy hudPanel: '{hudPanel.name}'!");
            }

            // 3. Tìm interactionCanvas nếu trống
            if (interactionCanvas == null)
            {
                interactionCanvas = GameObject.Find("InteractionCanvas") ?? GameObject.Find("Interaction Canvas") ?? GameObject.Find("InteractionCanvas_Auto");
                if (interactionCanvas != null) Debug.Log($"[PauseMenu Self-Heal] Đã tự tìm thấy interactionCanvas: '{interactionCanvas.name}'!");
            }

            // 4. Tìm inventoryPanel nếu trống
            if (inventoryPanel == null)
            {
                inventoryPanel = GameObject.Find("InventoryPanel") ?? GameObject.Find("Inventory Panel") ?? GameObject.Find("Inventory") ?? GameObject.Find("InventoryPanel_Auto");
                if (inventoryPanel != null) Debug.Log($"[PauseMenu Self-Heal] Đã tự tìm thấy inventoryPanel: '{inventoryPanel.name}'!");
            }
        }

        private GameObject FindChildRecursive(GameObject parent, string name)
        {
            if (parent.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                return parent;
            
            foreach (Transform child in parent.transform)
            {
                GameObject found = FindChildRecursive(child.gameObject, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
