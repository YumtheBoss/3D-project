using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using AnomalySystem;

namespace GameUI
{
    public class MainMenu : MonoBehaviour
    {
        public string gameSceneName = "SampleScene"; // Tên scene chứa game

        [Header("Panels")]
        public GameObject mainMenuPanel;
        public GameObject settingsPanel;
        public GameObject leaderboardPanel;
        public Transform leaderboardContainer;
        public GameObject leaderboardEntryPrefab;

        [Header("Transition Settings")]
        [Tooltip("Thời gian chuyển đổi (fade) giữa các panel")]
        public float fadeDuration = 0.3f;

        [Header("Cinematic Transition (Horror Fly-in)")]
        [Tooltip("Điểm đích mà Camera sẽ bay tới khi nhấn Play (đặt sát hoặc xuyên qua cánh cửa gỗ)")]
        public Transform cameraFlyTarget;
        [Tooltip("Thời gian Camera bay vào hành lang (giây)")]
        public float flyDuration = 2.0f;
        [Tooltip("Đường cong gia tốc cho chuyển động bay của Camera")]
        public AnimationCurve flyCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Cutscene")]
        public CutsceneManager cutsceneManager;

        [Header("Settings UI - Volume")]
        [Tooltip("Thanh chỉnh âm lượng tổng")]
        public Slider masterVolumeSlider;
        [Tooltip("Thanh chỉnh âm lượng nhạc nền")]
        public Slider musicVolumeSlider;
        [Tooltip("Thanh chỉnh âm lượng hiệu ứng (SFX)")]
        public Slider sfxVolumeSlider;

        [Header("Settings UI - Sensitivity")]
        public Slider sensitivitySlider;

        [Header("Camera Settings")]
        [Tooltip("Camera chính được sử dụng cho hiệu ứng bay. Nếu trống, script tự động quét tìm camera.")]
        public Camera mainCamera;
        [Tooltip("Bật hiệu ứng bay Camera 3D vào cửa trước khi vào Cutscene.")]
        public bool enableCameraFlyIn = true;

        [Header("Transition Effects")]
        [Tooltip("Hình ảnh dùng để fade màn hình. Nếu trống sẽ tự sinh.")]
        public UnityEngine.UI.Image fadeOverlayImage;
        [Tooltip("Thời gian fade tối màn hình (giây)")]
        public float fadeToBlackDuration = 0.8f;

        [Header("Menu Buttons")]
        [Tooltip("Nút Continue - Nếu trống sẽ tự tìm theo tên 'ContinueButton'")]
        public Button continueButton;
        [Tooltip("Nút New Game - Nếu trống sẽ tự tìm theo tên 'NewGameButton'")]
        public Button newGameButton;
        [Tooltip("Nút Settings - Nếu trống sẽ tự tìm theo tên 'SettingsButton'")]
        public Button settingsButton;
        [Tooltip("Nút Leaderboard - Nếu trống sẽ tự tìm theo tên 'LeaderboardButton'")]
        public Button leaderboardButton;
        [Tooltip("Nút Exit - Nếu trống sẽ tự tìm theo tên 'ExitButton'")]
        public Button exitButton;
        [Tooltip("Nút Back trong Settings - Nếu trống sẽ tự tìm theo tên 'BackButton' dưới SettingsPanel")]
        public Button backButton;
        [Tooltip("Nút Back trong Leaderboard - Nếu trống sẽ tự tìm theo tên 'LeaderboardBackButton' dưới LeaderboardPanel")]
        public Button leaderboardBackButton;

        private Coroutine fadeCoroutine;
        private bool isStartingGame = false;
        private bool isNewGameSession = false;

        private void Awake()
        {
            CleanupPersistentGameplayObjects();

            // Tự động tìm kiếm và liên kết các thành phần bị thiếu (Self-Healing System)
            SelfHealReferences();
        }

        private void CleanupPersistentGameplayObjects()
        {
            Debug.Log("[MainMenu] Bắt đầu dọn dẹp các đối tượng persistent của lượt chơi trước để tránh lỗi xung đột...");

            // 1. Huỷ đối tượng người chơi persistent (FirstPersonController)
            if (FirstPersonController.Instance != null)
            {
                Destroy(FirstPersonController.Instance.gameObject);
            }
            else
            {
                GameObject oldPlayer = GameObject.FindWithTag("Player");
                if (oldPlayer == null) oldPlayer = GameObject.Find("Player");
                if (oldPlayer != null)
                {
                    Destroy(oldPlayer);
                }
            }

            // 2. Huỷ các Managers/Controllers persistent liên quan đến gameplay
            if (RoomManager.Instance != null) Destroy(RoomManager.Instance.gameObject);
            if (InventoryManager.Instance != null) Destroy(InventoryManager.Instance.gameObject);
            if (PlayerTracker.Instance != null) Destroy(PlayerTracker.Instance.gameObject);
            if (TutorialHUDManager.Instance != null) Destroy(TutorialHUDManager.Instance.gameObject);
            if (EndingController.Instance != null) Destroy(EndingController.Instance.gameObject);
            if (GameEndUIController.Instance != null) Destroy(GameEndUIController.Instance.gameObject);

            // Tìm và huỷ thêm các đối tượng tự sinh ra nếu còn sót lại (dự phòng)
            GameObject oldTransition = GameObject.Find("_RoomTransitionScreen_Auto");
            if (oldTransition != null) Destroy(oldTransition);

            GameObject oldJumpscare = GameObject.Find("_JumpscareCanvas_Auto");
            if (oldJumpscare != null) Destroy(oldJumpscare);

            // Khôi phục các trạng thái toàn cục quan trọng
            Time.timeScale = 1f;
            FirstPersonController.IsUIOpen = false;
            
            // Hiện con trỏ chuột ở Main Menu
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void SelfHealReferences()
        {
            // 1. Tự tìm MainCamera nếu bị trống trong Inspector
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    mainCamera = Object.FindAnyObjectByType<Camera>(FindObjectsInactive.Include);
                    if (mainCamera != null)
                    {
                        Debug.Log($"[MainMenu Self-Heal] Đã tìm thấy MainCamera bằng Type: '{mainCamera.name}'!");
                    }
                }
            }

            // 2. Tự động tìm hoặc tạo điểm đích Camera Fly Target ảo nếu bị trống và có bật bay camera
            if (enableCameraFlyIn && cameraFlyTarget == null)
            {
                GameObject targetObj = GameObject.Find("CameraFlyTarget");
                if (targetObj == null) targetObj = GameObject.Find("Camera Target");
                if (targetObj == null) targetObj = GameObject.Find("FlyTarget");
                if (targetObj == null) targetObj = GameObject.Find("DoorTarget");
                
                if (targetObj != null)
                {
                    cameraFlyTarget = targetObj.transform;
                    Debug.Log($"[MainMenu Self-Heal] Đã tự động tìm và gán Camera Fly Target từ '{targetObj.name}'!");
                }
                else
                {
                    // Quét các đối tượng cửa phổ biến trong scene để tạo điểm đích ảo
                    GameObject doorObj = GameObject.Find("Door_A_Grp");
                    if (doorObj == null) doorObj = GameObject.Find("Doorway Wall With Door");
                    if (doorObj == null) doorObj = GameObject.Find("Door");
                    
                    if (doorObj != null)
                    {
                        GameObject tempTarget = GameObject.Find("AutoCreated_CameraFlyTarget");
                        if (tempTarget == null)
                        {
                            tempTarget = new GameObject("AutoCreated_CameraFlyTarget");
                            // Đặt điểm đích dừng cách cánh cửa 2.2m để có góc nhìn điện ảnh rộng và đẹp hơn, không bị đâm sát vào cửa
                            tempTarget.transform.position = doorObj.transform.position - doorObj.transform.forward * 2.2f;
                            tempTarget.transform.rotation = doorObj.transform.rotation;
                        }
                        cameraFlyTarget = tempTarget.transform;
                        Debug.Log($"[MainMenu Self-Heal] Tự động tạo điểm Camera Fly Target ảo tại vị trí của '{doorObj.name}'!");
                    }
                }
            }

            // 3. Tìm CutsceneManager
            if (cutsceneManager == null)
            {
                cutsceneManager = Object.FindAnyObjectByType<CutsceneManager>(FindObjectsInactive.Include);
            }

            // 4. Tìm các Panels trong Canvas
            if (mainMenuPanel == null)
            {
                mainMenuPanel = FindGameObjectLocal("MainMenu Panel");
                if (mainMenuPanel == null) mainMenuPanel = FindGameObjectLocal("MainMenuPanel");
            }

            if (settingsPanel == null)
            {
                settingsPanel = FindGameObjectLocal("Setting Panel");
                if (settingsPanel == null) settingsPanel = FindGameObjectLocal("SettingsPanel");
            }

            if (leaderboardPanel == null)
            {
                leaderboardPanel = FindGameObjectLocal("Leaderboard Panel");
                if (leaderboardPanel == null) leaderboardPanel = FindGameObjectLocal("LeaderboardPanel");
            }

            if (leaderboardContainer == null && leaderboardPanel != null)
            {
                leaderboardContainer = FindRecursive(leaderboardPanel, "Content");
                if (leaderboardContainer == null) leaderboardContainer = FindRecursive(leaderboardPanel, "LeaderboardContainer");
            }

            // 5. Tìm các Sliders trong Settings Panel
            if (settingsPanel != null)
            {
                if (masterVolumeSlider == null) masterVolumeSlider = FindComponentInChild<Slider>(settingsPanel, "MasterVolumeSlider");
                if (musicVolumeSlider == null) musicVolumeSlider = FindComponentInChild<Slider>(settingsPanel, "MusicVolumeSlider");
                if (sfxVolumeSlider == null) sfxVolumeSlider = FindComponentInChild<Slider>(settingsPanel, "SFXVolumeSlider");
                if (sensitivitySlider == null) sensitivitySlider = FindComponentInChild<Slider>(settingsPanel, "SensitivitySlider");
            }

            // 6. Tự động gắn các nút bấm và sự kiện từ Inspector hoặc tự dò tìm
            if (continueButton == null) continueButton = FindComponentInCanvas<Button>("ContinueButton");
            if (newGameButton == null) newGameButton = FindComponentInCanvas<Button>("NewGameButton");
            if (settingsButton == null) settingsButton = FindComponentInCanvas<Button>("SettingsButton");
            if (leaderboardButton == null) leaderboardButton = FindComponentInCanvas<Button>("LeaderboardButton");
            if (exitButton == null) exitButton = FindComponentInCanvas<Button>("ExitButton");
            if (backButton == null && settingsPanel != null) backButton = FindComponentInChild<Button>(settingsPanel, "BackButton");
            if (leaderboardBackButton == null && leaderboardPanel != null) leaderboardBackButton = FindComponentInChild<Button>(leaderboardPanel, "LeaderboardBackButton");

            // Đăng ký sự kiện an toàn cho các nút
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(ClickContinue);
                continueButton.onClick.AddListener(ClickContinue);
            }
            if (newGameButton != null)
            {
                newGameButton.onClick.RemoveListener(ClickNewGame);
                newGameButton.onClick.AddListener(ClickNewGame);
            }
            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(ShowSettings);
                settingsButton.onClick.AddListener(ShowSettings);
            }
            if (leaderboardButton != null)
            {
                leaderboardButton.onClick.RemoveListener(ShowLeaderboard);
                leaderboardButton.onClick.AddListener(ShowLeaderboard);
            }
            if (exitButton != null)
            {
                exitButton.onClick.RemoveListener(ClickExit);
                exitButton.onClick.AddListener(ClickExit);
            }
            if (backButton != null)
            {
                backButton.onClick.RemoveListener(ShowMainMenu);
                backButton.onClick.AddListener(ShowMainMenu);
            }
            if (leaderboardBackButton != null)
            {
                leaderboardBackButton.onClick.RemoveListener(CloseLeaderboard);
                leaderboardBackButton.onClick.AddListener(CloseLeaderboard);
            }

            // 7. Tự động tìm hoặc tạo Fade Overlay Image nếu bị trống
            if (fadeOverlayImage == null)
            {
                fadeOverlayImage = FindComponentInCanvas<UnityEngine.UI.Image>("FadeOverlay");
                if (fadeOverlayImage == null) fadeOverlayImage = FindComponentInCanvas<UnityEngine.UI.Image>("FadePanel");
                if (fadeOverlayImage == null) fadeOverlayImage = FindComponentInCanvas<UnityEngine.UI.Image>("FadeOverlayImage");
                
                if (fadeOverlayImage == null)
                {
                    CreateAutoFadeOverlay();
                }
            }
        }

        private void CreateAutoFadeOverlay()
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                Debug.LogWarning("[MainMenu] Không tìm thấy Canvas nào trong scene để tự sinh Fade Overlay!");
                return;
            }

            GameObject overlayObj = new GameObject("AutoCreated_FadeOverlay");
            overlayObj.transform.SetParent(canvas.transform, false);
            overlayObj.transform.SetAsLastSibling(); // Vẽ trên cùng

            fadeOverlayImage = overlayObj.AddComponent<UnityEngine.UI.Image>();
            fadeOverlayImage.color = new Color(0f, 0f, 0f, 0f); // Mặc định trong suốt

            RectTransform rect = overlayObj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            CanvasGroup cg = overlayObj.AddComponent<CanvasGroup>();
            cg.interactable = false;
            cg.blocksRaycasts = false;
            
            Debug.Log("[MainMenu Self-Heal] Đã tự động tạo một Fade Overlay Image màu đen trên Canvas!");
        }

        private T FindComponentInCanvas<T>(string name) where T : Component
        {
            GameObject obj = FindGameObjectLocal(name);
            if (obj != null) return obj.GetComponent<T>();
            return null;
        }

        private GameObject FindGameObjectLocal(string name)
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                Transform t = FindRecursive(canvas.gameObject, name);
                if (t != null) return t.gameObject;
            }
            return GameObject.Find(name);
        }

        private Transform FindRecursive(GameObject parent, string name)
        {
            if (parent.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                return parent.transform;
            
            foreach (Transform child in parent.transform)
            {
                var found = FindRecursive(child.gameObject, name);
                if (found != null) return found;
            }
            return null;
        }

        private T FindComponentInChild<T>(GameObject parent, string childName) where T : Component
        {
            Transform t = FindRecursive(parent, childName);
            if (t != null) return t.GetComponent<T>();
            return null;
        }

        private void BindButtonAction(string buttonName, UnityEngine.Events.UnityAction action)
        {
            GameObject btnObj = FindGameObjectLocal(buttonName);
            if (btnObj != null)
            {
                Button btn = btnObj.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveListener(action); // Tránh trùng lặp listener
                    btn.onClick.AddListener(action);
                    Debug.Log($"[MainMenu Self-Heal] Đã liên kết nút '{buttonName}' thành công!");
                }
            }
        }

        private void BindButtonActionInParent(GameObject parent, string buttonName, UnityEngine.Events.UnityAction action)
        {
            Transform t = FindRecursive(parent, buttonName);
            if (t != null)
            {
                Button btn = t.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveListener(action);
                    btn.onClick.AddListener(action);
                    Debug.Log($"[MainMenu Self-Heal] Đã liên kết nút con '{buttonName}' thành công!");
                }
            }
        }

        private void Start()
        {
            mainCamera = Camera.main;

            // Đảm bảo các tham chiếu được thiết lập nếu bị trễ
            SelfHealReferences();

            // Hiển thị Main Menu ngay lập tức lúc đầu, không chạy hiệu ứng fade
            ShowMainMenuInstant();

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

            // KIỂM TRA VÀ HIỂN THỊ BẢNG XẾP HẠNG NẾU ĐƯỢC CHUYỂN HƯỚNG TỪ GAMEOVER SUBMIT
            if (PlayerPrefs.GetInt("ShowLeaderboardOnStart", 0) == 1)
            {
                PlayerPrefs.SetInt("ShowLeaderboardOnStart", 0);
                PlayerPrefs.Save();
                ShowLeaderboard();
            }
        }

        // ========== NÚT BẤM ==========

        private string GetSceneForRoom(int roomVal)
        {
            switch (roomVal)
            {
                case 0:
                case 1:
                case 2:
                    return "SampleScene";
                case 3:
                case 4:
                    return "Hospital";
                case 5:
                    return "LevelTst";
                default:
                    return "SampleScene";
            }
        }

        public void ClickContinue()
        {
            if (isStartingGame) return;
            PlayClickSound();

            // Đánh dấu đây là phiên chơi Tiếp tục (không phải New Game) để bỏ qua Cutscene video
            isNewGameSession = false;

            // Xác định scene đích dựa trên phòng lưu trong PlayerPrefs
            int savedRoom = PlayerPrefs.GetInt("CurrentRoom", 0);
            gameSceneName = GetSceneForRoom(savedRoom);
            Debug.Log($"[MainMenu] ClickContinue: Loaded saved room {savedRoom}, redirecting to scene: {gameSceneName}");

            StartGameTransition();
        }

        public void ClickNewGame()
        {
            if (isStartingGame) return;
            PlayClickSound();
            
            // Đánh dấu đây là phiên chơi mới hoàn toàn để chiếu Cutscene video
            isNewGameSession = true;

            // Xóa sạch dữ liệu đã lưu trong PlayerPrefs
            PlayerPrefs.SetString("SavedInventory", "");
            PlayerPrefs.SetInt("IsTeddyBearEquipped", 0);
            PlayerPrefs.SetInt("SavedLevel", 0);
            PlayerPrefs.SetFloat("SavedPlayTime", 0f);
            PlayerPrefs.SetInt("CurrentRoom", 0);
            PlayerPrefs.Save();

            // Reset trạng thái trong bộ nhớ nếu các Singletons tồn tại sẵn
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.ClearInventory();
                InventoryManager.Instance.SetTeddyBearEquipped(false);
            }

            if (RoomManager.Instance != null)
            {
                RoomManager.Instance.ResetProgress();
            }

            // Đối với New Game, bắt đầu ở Room 0 thuộc scene SampleScene
            gameSceneName = GetSceneForRoom(0);
            Debug.Log($"[MainMenu] ClickNewGame: Resetting all save data, starting fresh in scene: {gameSceneName}");
            
            StartGameTransition();
        }

        private void StartGameTransition()
        {
            isStartingGame = true;

            // Ẩn/tắt tương tác UI ngay lập tức
            CanvasGroup menuCG = mainMenuPanel != null ? mainMenuPanel.GetComponent<CanvasGroup>() : null;
            if (menuCG != null)
            {
                menuCG.interactable = false;
                menuCG.blocksRaycasts = false;
            }

            // Ưu tiên chạy hiệu ứng bay Camera 3D vào cửa trước tiên (nếu có thiết lập và được bật trong Inspector)
            if (enableCameraFlyIn && cameraFlyTarget != null && mainCamera != null)
            {
                StartCoroutine(CameraFlyInRoutine());
            }
            // Nếu không có bay camera nhưng có Cutscene video, chạy thẳng video (Chỉ chạy khi là New Game)
            else if (isNewGameSession && cutsceneManager != null)
            {
                if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
                // Vì không có bay camera, đặt đen hoàn toàn luôn
                if (fadeOverlayImage != null)
                {
                    fadeOverlayImage.color = new Color(0f, 0f, 0f, 1f);
                }
                cutsceneManager.PlayCutscene(gameSceneName, fadeOverlayImage);
            }
            // Fallback hoặc Tiếp tục (Continue): Tải scene trực tiếp
            else
            {
                SceneManager.LoadScene(gameSceneName);
            }
        }

        private IEnumerator CameraFlyInRoutine()
        {
            // 1. Tắt script CameraSway (lắc lư) để camera chuyển động thẳng ổn định
            CameraSway swayScript = mainCamera.GetComponent<CameraSway>();
            if (swayScript != null)
            {
                swayScript.enabled = false;
            }

            Vector3 startPos = mainCamera.transform.position;
            Quaternion startRot = mainCamera.transform.rotation;

            Vector3 endPos = cameraFlyTarget.position;
            Quaternion endRot = cameraFlyTarget.rotation;

            CanvasGroup menuCG = mainMenuPanel != null ? mainMenuPanel.GetComponent<CanvasGroup>() : null;

            float elapsed = 0f;
            while (elapsed < flyDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / flyDuration;
                float curveT = flyCurve.Evaluate(t); // Sử dụng Animation Curve để di chuyển mượt mà hơn

                // Di chuyển và xoay camera
                mainCamera.transform.position = Vector3.Lerp(startPos, endPos, curveT);
                mainCamera.transform.rotation = Quaternion.Slerp(startRot, endRot, curveT);

                // Đồng thời fade mờ dần chữ Menu về 0
                if (menuCG != null)
                {
                    menuCG.alpha = Mathf.Lerp(1f, 0f, curveT);
                }

                // Fade tối dần màn hình màu đen ở đoạn cuối của quá trình camera di chuyển
                if (fadeOverlayImage != null)
                {
                    float fadeStartDelay = Mathf.Max(0f, flyDuration - fadeToBlackDuration);
                    if (elapsed >= fadeStartDelay)
                    {
                        float fadeElapsed = elapsed - fadeStartDelay;
                        float fadeT = Mathf.Clamp01(fadeElapsed / fadeToBlackDuration);
                        fadeOverlayImage.color = new Color(0f, 0f, 0f, fadeT);
                    }
                }

                // Giảm dần âm thanh nhạc nền để chuyển cảnh mượt
                if (AudioManager.Instance != null && AudioManager.Instance.musicSource != null)
                {
                    AudioManager.Instance.musicSource.volume = Mathf.Lerp(PlayerPrefs.GetFloat("MusicVolume", 1f), 0f, curveT);
                }

                yield return null;
            }

            // Đảm bảo camera đạt chính xác đích
            mainCamera.transform.position = endPos;
            mainCamera.transform.rotation = endRot;

            // Đảm bảo màn hình đã đen hoàn toàn
            if (fadeOverlayImage != null)
            {
                fadeOverlayImage.color = new Color(0f, 0f, 0f, 1f);
            }

            // Ẩn panel menu chính sau khi camera đã bay xong
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(false);
            }

            // Sau khi camera chạm đích, mới kích hoạt Cutscene (nếu có và là New Game) hoặc load scene trực tiếp
            if (isNewGameSession && cutsceneManager != null)
            {
                cutsceneManager.PlayCutscene(gameSceneName, fadeOverlayImage);
            }
            else
            {
                SceneManager.LoadScene(gameSceneName);
            }
        }

        public void ShowSettings()
        {
            if (isStartingGame) return;
            PlayClickSound();
            FadeToPanel(mainMenuPanel, settingsPanel);
        }

        public void ShowMainMenu()
        {
            if (isStartingGame) return;
            PlayClickSound();
            FadeToPanel(settingsPanel, mainMenuPanel);
        }

        private void ShowMainMenuInstant()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
                CanvasGroup settingsCG = settingsPanel.GetComponent<CanvasGroup>();
                if (settingsCG != null) settingsCG.alpha = 0f;
            }

            if (leaderboardPanel != null)
            {
                leaderboardPanel.SetActive(false);
                CanvasGroup leaderboardCG = leaderboardPanel.GetComponent<CanvasGroup>();
                if (leaderboardCG != null) leaderboardCG.alpha = 0f;
            }

            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(true);
                CanvasGroup menuCG = mainMenuPanel.GetComponent<CanvasGroup>();
                if (menuCG != null)
                {
                    menuCG.alpha = 1f;
                    menuCG.interactable = true;
                    menuCG.blocksRaycasts = true;
                }
            }
        }

        public void ClickExit()
        {
            if (isStartingGame) return;
            PlayClickSound();
            Debug.Log("Đã thoát Game!");
            Application.Quit();
        }

        // ========== HIỆU ỨNG CHUYỂN PANEL (FADE) ==========

        private void FadeToPanel(GameObject fromPanel, GameObject toPanel)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadePanelRoutine(fromPanel, toPanel));
        }

        private IEnumerator FadePanelRoutine(GameObject fromPanel, GameObject toPanel)
        {
            CanvasGroup fromCG = fromPanel != null ? fromPanel.GetComponent<CanvasGroup>() : null;
            CanvasGroup toCG = toPanel != null ? toPanel.GetComponent<CanvasGroup>() : null;

            // Nếu thiếu một trong hai CanvasGroup, thực hiện bật/tắt lập tức làm fallback
            if (fromCG == null || toCG == null)
            {
                if (fromPanel != null) fromPanel.SetActive(false);
                if (toPanel != null) toPanel.SetActive(true);
                yield break;
            }

            // Chuẩn bị cho panel đích xuất hiện
            toPanel.SetActive(true);
            toCG.alpha = 0f;
            toCG.interactable = false;
            toCG.blocksRaycasts = false;

            // Khoá tương tác panel nguồn
            fromCG.interactable = false;
            fromCG.blocksRaycasts = false;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / fadeDuration;
                
                fromCG.alpha = Mathf.Lerp(1f, 0f, t);
                toCG.alpha = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }

            fromCG.alpha = 0f;
            fromPanel.SetActive(false);

            toCG.alpha = 1f;
            toCG.interactable = true;
            toCG.blocksRaycasts = true;
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

        // ========== LEADERBOARD ==========

        public void ShowLeaderboard()
        {
            if (isStartingGame) return;
            PlayClickSound();

            if (leaderboardPanel != null)
            {
                FadeToPanel(mainMenuPanel, leaderboardPanel);

                // Clear the container first and show loading message
                if (leaderboardContainer != null)
                {
                    foreach (Transform child in leaderboardContainer)
                    {
                        Destroy(child.gameObject);
                    }

                    // Create a "LOADING..." text row
                    GameObject loadingRow = new GameObject("LoadingRow");
                    loadingRow.transform.SetParent(leaderboardContainer, false);
                    
                    var txt = loadingRow.AddComponent<TMPro.TextMeshProUGUI>();
                    txt.text = "LOADING HIGHSCORES...";
                    txt.fontSize = 28;
                    txt.fontStyle = TMPro.FontStyles.Italic;
                    txt.color = new Color(0.9f, 0.7f, 0.2f, 0.8f);
                    txt.alignment = TMPro.TextAlignmentOptions.Center;
                    
                    var le = loadingRow.AddComponent<LayoutElement>();
                    le.minHeight = 60f;
                }

                // Query database
                if (FirebaseDatabaseManager.Instance != null)
                {
                    FirebaseDatabaseManager.Instance.GetLeaderboardScores(PopulateLeaderboard);
                }
                else
                {
                    Debug.LogWarning("[MainMenu] FirebaseDatabaseManager.Instance is null!");
                    ShowLeaderboardError("FirebaseManager is missing.");
                }
            }
        }

        public void CloseLeaderboard()
        {
            if (isStartingGame) return;
            PlayClickSound();
            FadeToPanel(leaderboardPanel, mainMenuPanel);
        }

        private void ShowLeaderboardError(string errMsg)
        {
            if (leaderboardContainer == null) return;
            foreach (Transform child in leaderboardContainer)
            {
                Destroy(child.gameObject);
            }

            GameObject errorRow = new GameObject("ErrorRow");
            errorRow.transform.SetParent(leaderboardContainer, false);
            var txt = errorRow.AddComponent<TMPro.TextMeshProUGUI>();
            txt.text = $"ERROR: {errMsg.ToUpper()}";
            txt.fontSize = 28;
            txt.fontStyle = TMPro.FontStyles.Bold;
            txt.color = new Color(0.9f, 0.1f, 0.1f, 0.9f);
            txt.alignment = TMPro.TextAlignmentOptions.Center;
        }

        private void PopulateLeaderboard(System.Collections.Generic.List<LeaderboardEntry> scores)
        {
            if (leaderboardContainer == null) return;

            // Clear loading text
            foreach (Transform child in leaderboardContainer)
            {
                Destroy(child.gameObject);
            }

            if (scores == null || scores.Count == 0)
            {
                GameObject emptyRow = new GameObject("EmptyRow");
                emptyRow.transform.SetParent(leaderboardContainer, false);
                var txt = emptyRow.AddComponent<TMPro.TextMeshProUGUI>();
                txt.text = "NO HIGH SCORES SUBMITTED YET.";
                txt.fontSize = 28;
                txt.fontStyle = TMPro.FontStyles.Italic;
                txt.color = new Color(0.7f, 0.6f, 0.5f, 0.8f);
                txt.alignment = TMPro.TextAlignmentOptions.Center;
                return;
            }

            // Populate up to 100 top entries
            int rank = 1;
            foreach (var entry in scores)
            {
                if (rank > 100) break;

                // Create a row item dynamically
                GameObject rowObj = new GameObject($"EntryRow_{rank}");
                rowObj.transform.SetParent(leaderboardContainer, false);

                // Add HorizontalLayoutGroup for columns spacing
                var hlg = rowObj.AddComponent<HorizontalLayoutGroup>();
                hlg.childForceExpandWidth = false;
                hlg.childControlWidth = true;
                hlg.childForceExpandHeight = true;
                hlg.childControlHeight = true;

                var le = rowObj.AddComponent<LayoutElement>();
                le.minHeight = 50f;

                // Color coding for top ranks
                Color rankColor = Color.white;
                if (rank == 1) rankColor = new Color(0.95f, 0.75f, 0.2f); // Gold
                else if (rank == 2) rankColor = new Color(0.75f, 0.75f, 0.75f); // Silver
                else if (rank == 3) rankColor = new Color(0.6f, 0.45f, 0.35f); // Bronze
                else rankColor = new Color(0.85f, 0.85f, 0.85f); // Normal

                // 1. Rank Col
                CreateRowColumn(rowObj.transform, $"#{rank}", 0.15f, rankColor, rank <= 3);
                // 2. Name Col
                CreateRowColumn(rowObj.transform, entry.playerName.ToUpper(), 0.45f, rankColor, rank <= 3);
                
                // Formatted Time Col
                int minutes = Mathf.FloorToInt(entry.completion_time_seconds / 60F);
                int seconds = Mathf.FloorToInt(entry.completion_time_seconds % 60F);
                string formattedTime = string.Format("{0:00}:{1:00}", minutes, seconds);
                CreateRowColumn(rowObj.transform, formattedTime, 0.2f, rankColor, rank <= 3);

                // 4. Date Col
                CreateRowColumn(rowObj.transform, entry.timestamp, 0.2f, rankColor, rank <= 3);

                rank++;
            }
        }

        private void CreateRowColumn(Transform parent, string value, float flexWidth, Color color, bool isBold)
        {
            GameObject col = new GameObject("Col");
            col.transform.SetParent(parent, false);
            
            var le = col.AddComponent<LayoutElement>();
            le.preferredWidth = 800f * flexWidth;
            le.flexibleWidth = flexWidth;

            var txt = col.AddComponent<TMPro.TextMeshProUGUI>();
            txt.text = value;
            txt.fontSize = 24;
            txt.color = color;
            if (isBold) txt.fontStyle = TMPro.FontStyles.Bold;
            txt.alignment = TMPro.TextAlignmentOptions.Center;
        }

        // ========== TIỆN ÍCH ==========

        private void PlayClickSound()
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayButtonClick();
        }
    }
}


