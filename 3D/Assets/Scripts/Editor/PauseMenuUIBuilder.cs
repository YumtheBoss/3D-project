#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace GameUI
{
    /// <summary>
    /// Công cụ Editor đặc biệt dành cho Unity:
    /// - Tự động thiết kế và tạo mới Canvas Pause Menu đồng bộ hoàn hảo với phong cách kinh dị của Main Menu ("Behind the Dark").
    /// - Tự động tìm kiếm và bảo lưu các bảng HUD, Canvas tương tác và Túi đồ cũ.
    /// - Tự động liên kết các sự kiện OnClick, Sliders với script PauseMenu.cs.
    /// </summary>
    public class PauseMenuUIBuilder : EditorWindow
    {
        [MenuItem("Tools/Generate Beautiful Pause Menu")]
        public static void BuildOrUpgradePauseMenu()
        {
            // 1. Tìm hoặc tạo PauseMenu Controller trong Scene
            PauseMenu pauseMenu = Object.FindAnyObjectByType<PauseMenu>();
            if (pauseMenu == null)
            {
                GameObject controller = GameObject.Find("PauseMenuController");
                if (controller == null) controller = GameObject.Find("PauseMenu");
                if (controller == null) controller = new GameObject("PauseMenuController");
                
                pauseMenu = controller.GetComponent<PauseMenu>();
                if (pauseMenu == null) pauseMenu = controller.AddComponent<PauseMenu>();
                
                Undo.RegisterCreatedObjectUndo(controller, "Create Pause Menu Controller");
                Debug.Log("[PauseMenuUIBuilder] Đã tạo mới đối tượng 'PauseMenuController'!");
            }

            // --- BẢO VỆ CÁC THAM CHIẾU CŨ ---
            GameObject preservedHud = pauseMenu.hudPanel;
            GameObject preservedInteraction = pauseMenu.interactionCanvas;
            GameObject preservedInventory = pauseMenu.inventoryPanel;

            // Tìm kiếm bổ sung bằng tên nếu tham chiếu bị null
            if (preservedHud == null) preservedHud = FindGameObjectInActiveScene("HUDPanel") ?? FindGameObjectInActiveScene("HUD Panel") ?? FindGameObjectInActiveScene("HUD");
            if (preservedInteraction == null) preservedInteraction = FindGameObjectInActiveScene("InteractionCanvas") ?? FindGameObjectInActiveScene("Interaction Canvas") ?? FindGameObjectInActiveScene("InteractionCanvas_Auto");
            if (preservedInventory == null) preservedInventory = FindGameObjectInActiveScene("InventoryPanel") ?? FindGameObjectInActiveScene("Inventory Panel") ?? FindGameObjectInActiveScene("Inventory");

            // --- TÌM VÀ XOÁ CANVAS PAUSE CŨ ---
            GameObject oldCanvas = null;
            if (pauseMenu.pauseMenuPanel != null)
            {
                Canvas c = pauseMenu.pauseMenuPanel.GetComponentInParent<Canvas>();
                if (c != null) oldCanvas = c.gameObject;
            }
            if (oldCanvas == null) oldCanvas = FindGameObjectInActiveScene("PauseMenuCanvas");
            if (oldCanvas == null) oldCanvas = FindGameObjectInActiveScene("PauseMenu Canvas");
            if (oldCanvas == null) oldCanvas = FindGameObjectInActiveScene("Pause Canvas");

            if (oldCanvas != null)
            {
                Debug.Log($"[PauseMenuUIBuilder] Đang xoá Canvas cũ: '{oldCanvas.name}'");
                Undo.DestroyObjectImmediate(oldCanvas);
            }

            // 2. Tạo mới hoàn toàn Canvas Pause Menu
            CreateNewBeautifulPauseMenu(pauseMenu, preservedHud, preservedInteraction, preservedInventory);

            // Đánh dấu Scene đã thay đổi
            EditorUtility.SetDirty(pauseMenu);
            if (pauseMenu.gameObject != null) EditorUtility.SetDirty(pauseMenu.gameObject);

            EditorUtility.DisplayDialog(
                "Thành Công!",
                "Đã tạo mới hệ thống Pause Menu thành công đồng bộ với Main Menu:\n" +
                "- Tiêu đề game: \"PAUSED\" phong cách chữ nghiêng đỏ máu.\n" +
                "- Đồng bộ giao diện tối tăm mờ ảo, các nút chữ trắng in hoa căn trái tinh tế.\n" +
                "- Tự động tích hợp hiệu ứng UIHoverEffect di chuột đổi màu đỏ máu và phóng to mượt mà.\n" +
                "- Đã tự động kết nối lại HUD, Interaction Canvas và Inventory Panel cũ.\n\n" +
                "Bạn có thể chạy thử để trải nghiệm sự chuyên nghiệp ngay lập tức!",
                "Tuyệt vời"
            );
        }

        private static void SetupPanelCanvasGroup(GameObject panel)
        {
            if (panel == null) return;

            CanvasGroup cg = panel.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = panel.AddComponent<CanvasGroup>();
                Undo.RegisterCreatedObjectUndo(cg, "Add CanvasGroup to Panel");
            }
            cg.alpha = panel.activeSelf ? 1f : 0f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        private static void CreateNewBeautifulPauseMenu(PauseMenu pauseMenu, GameObject hud, GameObject interaction, GameObject inventory)
        {
            Debug.Log("[PauseMenuUIBuilder] Đang tạo mới Pause Menu 'Behind the Dark' style...");

            // 0. Tạo hoặc tìm EventSystem trong Scene
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                eventSystemObj.AddComponent<StandaloneInputModule>();
#endif
                Undo.RegisterCreatedObjectUndo(eventSystemObj, "Create EventSystem");
                Debug.Log("[PauseMenuUIBuilder] Đã tự động tạo EventSystem trong Scene!");
            }

            // 1. Tạo Canvas gốc
            GameObject canvasObj = new GameObject("PauseMenuCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Pause Canvas");

            // 2. Tạo PauseMenu Panel (Nền tối mờ của Menu Pause)
            GameObject pauseMenuPanel = new GameObject("PauseMenu Panel");
            pauseMenuPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform rectPMP = pauseMenuPanel.AddComponent<RectTransform>();
            rectPMP.anchorMin = Vector2.zero;
            rectPMP.anchorMax = Vector2.one;
            rectPMP.sizeDelta = Vector2.zero;

            Image imgPMP = pauseMenuPanel.AddComponent<Image>();
            imgPMP.color = new Color(0f, 0f, 0f, 0.65f); // Làm mờ tối đen hành lang phía sau đẹp mắt

            pauseMenu.pauseMenuPanel = pauseMenuPanel;
            SetupPanelCanvasGroup(pauseMenuPanel);

            // 3. Tạo Tiêu đề (PAUSED)
            GameObject titleObj = new GameObject("PauseTitle");
            titleObj.transform.SetParent(pauseMenuPanel.transform, false);
            RectTransform rectTitle = titleObj.AddComponent<RectTransform>();
            rectTitle.anchorMin = new Vector2(0f, 1f);
            rectTitle.anchorMax = new Vector2(0f, 1f);
            rectTitle.pivot = new Vector2(0f, 1f);
            rectTitle.anchoredPosition = new Vector2(100f, -100f); // 100px từ góc trên bên trái
            rectTitle.sizeDelta = new Vector2(600f, 120f);

            TextMeshProUGUI textTitle = titleObj.AddComponent<TextMeshProUGUI>();
            textTitle.text = "PAUSED";
            textTitle.fontSize = 76;
            textTitle.fontStyle = FontStyles.Bold | FontStyles.Italic;
            textTitle.color = new Color(0.9f, 0.1f, 0.1f, 1f); // Màu đỏ máu
            textTitle.alignment = TextAlignmentOptions.Left;

            // 4. Tạo khu vực chứa nút bấm (Vertical Layout)
            GameObject buttonContainer = new GameObject("ButtonsGroup");
            buttonContainer.transform.SetParent(pauseMenuPanel.transform, false);
            RectTransform rectBC = buttonContainer.AddComponent<RectTransform>();
            rectBC.anchorMin = new Vector2(0f, 0.5f);
            rectBC.anchorMax = new Vector2(0f, 0.5f);
            rectBC.pivot = new Vector2(0f, 0.5f);
            rectBC.anchoredPosition = new Vector2(100f, -50f); // Dưới tiêu đề
            rectBC.sizeDelta = new Vector2(400f, 300f);

            VerticalLayoutGroup vlg = buttonContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 25;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;

            // 5. Sinh các nút: RESUME, SETTINGS, MAIN MENU
            CreateStyledButton(buttonContainer.transform, "ResumeButton", "RESUME", pauseMenu, "ResumeGame");
            CreateStyledButton(buttonContainer.transform, "SettingsButton", "SETTINGS", pauseMenu, "ShowSettings");
            CreateStyledButton(buttonContainer.transform, "MainMenuButton", "MAIN MENU", pauseMenu, "GoToMainMenu");

            // 6. Tạo Settings Panel
            GameObject settingsPanel = new GameObject("Setting Panel");
            settingsPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform rectSP = settingsPanel.AddComponent<RectTransform>();
            rectSP.anchorMin = Vector2.zero;
            rectSP.anchorMax = Vector2.one;
            rectSP.sizeDelta = Vector2.zero;

            Image imgSP = settingsPanel.AddComponent<Image>();
            imgSP.color = new Color(0f, 0f, 0f, 0.88f); // Tối hẳn khi vào settings

            pauseMenu.settingsPanel = settingsPanel;
            SetupPanelCanvasGroup(settingsPanel);
            settingsPanel.SetActive(false); // Ẩn mặc định

            // Tiêu đề Cài đặt
            GameObject sTitleObj = new GameObject("SettingsTitle");
            sTitleObj.transform.SetParent(settingsPanel.transform, false);
            RectTransform rectSTitle = sTitleObj.AddComponent<RectTransform>();
            rectSTitle.anchorMin = new Vector2(0.5f, 1f);
            rectSTitle.anchorMax = new Vector2(0.5f, 1f);
            rectSTitle.pivot = new Vector2(0.5f, 1f);
            rectSTitle.anchoredPosition = new Vector2(0f, -80f);
            rectSTitle.sizeDelta = new Vector2(400f, 80f);
            TextMeshProUGUI textSTitle = sTitleObj.AddComponent<TextMeshProUGUI>();
            textSTitle.text = "SETTINGS";
            textSTitle.fontSize = 48;
            textSTitle.fontStyle = FontStyles.Bold;
            textSTitle.color = Color.white;
            textSTitle.alignment = TextAlignmentOptions.Center;

            // Tạo Panel chứa các Slider cài đặt
            GameObject slidersGroup = new GameObject("SlidersGroup");
            slidersGroup.transform.SetParent(settingsPanel.transform, false);
            RectTransform rectSG = slidersGroup.AddComponent<RectTransform>();
            rectSG.anchorMin = new Vector2(0.5f, 0.5f);
            rectSG.anchorMax = new Vector2(0.5f, 0.5f);
            rectSG.pivot = new Vector2(0.5f, 0.5f);
            rectSG.anchoredPosition = Vector2.zero;
            rectSG.sizeDelta = new Vector2(600f, 350f);

            VerticalLayoutGroup vlgSG = slidersGroup.AddComponent<VerticalLayoutGroup>();
            vlgSG.spacing = 30;
            vlgSG.childForceExpandWidth = true;
            vlgSG.childForceExpandHeight = false;

            // Tạo các Sliders
            pauseMenu.masterVolumeSlider = CreateVolumeSlider(slidersGroup.transform, "Master Volume", "MasterVolumeSlider");
            pauseMenu.musicVolumeSlider = CreateVolumeSlider(slidersGroup.transform, "Music Volume", "MusicVolumeSlider");
            pauseMenu.sfxVolumeSlider = CreateVolumeSlider(slidersGroup.transform, "SFX Volume", "SFXVolumeSlider");
            pauseMenu.sensitivitySlider = CreateVolumeSlider(slidersGroup.transform, "Mouse Sensitivity", "SensitivitySlider", 0.1f, 10f, 2f);

            // Nút Back trong Cài đặt
            GameObject backBtnObj = new GameObject("BackButton");
            backBtnObj.transform.SetParent(settingsPanel.transform, false);
            RectTransform rectBack = backBtnObj.AddComponent<RectTransform>();
            rectBack.anchorMin = new Vector2(0.5f, 0f);
            rectBack.anchorMax = new Vector2(0.5f, 0f);
            rectBack.pivot = new Vector2(0.5f, 0f);
            rectBack.anchoredPosition = new Vector2(0f, 60f);
            rectBack.sizeDelta = new Vector2(250, 50);

            Image imgBack = backBtnObj.AddComponent<Image>();
            imgBack.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
            Button btnBack = backBtnObj.AddComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btnBack.onClick, pauseMenu.BackToPauseMenu);

            GameObject backTextObj = new GameObject("Text");
            backTextObj.transform.SetParent(backBtnObj.transform, false);
            RectTransform rectBackText = backTextObj.AddComponent<RectTransform>();
            rectBackText.anchorMin = Vector2.zero;
            rectBackText.anchorMax = Vector2.one;
            rectBackText.sizeDelta = Vector2.zero;
            TextMeshProUGUI textBack = backTextObj.AddComponent<TextMeshProUGUI>();
            textBack.text = "BACK TO PAUSE";
            textBack.fontSize = 24;
            textBack.color = Color.white;
            textBack.alignment = TextAlignmentOptions.Center;

            UIHoverEffect hoverBack = backBtnObj.AddComponent<UIHoverEffect>();
            hoverBack.targetText = textBack;
            hoverBack.hoverColor = new Color(0.9f, 0.1f, 0.1f, 1f);

            // Liên kết lại các thành phần khác
            pauseMenu.hudPanel = hud;
            pauseMenu.interactionCanvas = interaction;
            pauseMenu.inventoryPanel = inventory;
            
            Debug.Log("[PauseMenuUIBuilder] Đã hoàn thành nướng UI Pause Menu!");
        }

        private static void CreateStyledButton(Transform parent, string objName, string buttonText, PauseMenu pauseMenu, string methodName)
        {
            GameObject btnObj = new GameObject(objName);
            btnObj.transform.SetParent(parent, false);

            RectTransform rectBtn = btnObj.AddComponent<RectTransform>();
            rectBtn.sizeDelta = new Vector2(300, 60);

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.05f); // Trong suốt để thấy nền

            Button btn = btnObj.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;

            UnityEngine.Events.UnityAction action = null;
            if (methodName == "ResumeGame") action = pauseMenu.ResumeGame;
            else if (methodName == "ShowSettings") action = pauseMenu.ShowSettings;
            else if (methodName == "GoToMainMenu") action = pauseMenu.GoToMainMenu;

            if (action != null)
            {
                UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, action);
            }

            GameObject textObj = new GameObject("ButtonText");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform rectText = textObj.AddComponent<RectTransform>();
            rectText.anchorMin = Vector2.zero;
            rectText.anchorMax = Vector2.one;
            rectText.sizeDelta = Vector2.zero;

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = buttonText;
            text.fontSize = 32;
            text.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Left;

            UIHoverEffect hover = btnObj.AddComponent<UIHoverEffect>();
            hover.targetText = text;
            hover.hoverColor = new Color(0.9f, 0.1f, 0.1f, 1f); // Màu đỏ máu phát sáng
        }

        private static Slider CreateVolumeSlider(Transform parent, string labelText, string objName, float min = 0f, float max = 1f, float defValue = 1f)
        {
            GameObject container = new GameObject(objName + "_Container");
            container.transform.SetParent(parent, false);
            RectTransform rectCont = container.AddComponent<RectTransform>();
            rectCont.sizeDelta = new Vector2(500, 50);

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(container.transform, false);
            RectTransform rectLabel = labelObj.AddComponent<RectTransform>();
            rectLabel.anchorMin = new Vector2(0f, 0.5f);
            rectLabel.anchorMax = new Vector2(0.35f, 0.5f);
            rectLabel.anchoredPosition = Vector2.zero;
            rectLabel.sizeDelta = new Vector2(0, 40);
            TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = labelText;
            label.fontSize = 20;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Left;

            GameObject sliderObj = new GameObject(objName);
            sliderObj.transform.SetParent(container.transform, false);
            RectTransform rectSlider = sliderObj.AddComponent<RectTransform>();
            rectSlider.anchorMin = new Vector2(0.35f, 0.5f);
            rectSlider.anchorMax = new Vector2(1f, 0.5f);
            rectSlider.anchoredPosition = Vector2.zero;
            rectSlider.sizeDelta = new Vector2(0, 20);

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = defValue;

            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);
            RectTransform rectBg = bgObj.AddComponent<RectTransform>();
            rectBg.anchorMin = new Vector2(0f, 0.25f);
            rectBg.anchorMax = new Vector2(1f, 0.75f);
            rectBg.sizeDelta = Vector2.zero;
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform rectFA = fillArea.AddComponent<RectTransform>();
            rectFA.anchorMin = new Vector2(0f, 0.25f);
            rectFA.anchorMax = new Vector2(1f, 0.75f);
            rectFA.sizeDelta = new Vector2(-20, 0);

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform rectFill = fill.AddComponent<RectTransform>();
            rectFill.anchorMin = Vector2.zero;
            rectFill.anchorMax = new Vector2(0f, 1f);
            rectFill.sizeDelta = Vector2.zero;
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.8f, 0.1f, 0.1f, 1f);

            GameObject handleArea = new GameObject("Handle Area");
            handleArea.transform.SetParent(sliderObj.transform, false);
            RectTransform rectHA = handleArea.AddComponent<RectTransform>();
            rectHA.anchorMin = new Vector2(0f, 0f);
            rectHA.anchorMax = new Vector2(1f, 1f);
            rectHA.sizeDelta = new Vector2(-20, 0);

            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            RectTransform rectHandle = handle.AddComponent<RectTransform>();
            rectHandle.sizeDelta = new Vector2(20, 20);
            Image handleImg = handle.AddComponent<Image>();
            handleImg.color = Color.white;

            slider.fillRect = rectFill;
            slider.handleRect = rectHandle;

            return slider;
        }

        private static GameObject FindGameObjectInActiveScene(string name)
        {
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            foreach (var root in activeScene.GetRootGameObjects())
            {
                var found = FindRecursive(root, name);
                if (found != null) return found;
            }
            return null;
        }

        private static GameObject FindRecursive(GameObject parent, string name)
        {
            if (parent.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                return parent;

            foreach (Transform child in parent.transform)
            {
                var found = FindRecursive(child.gameObject, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
#endif
