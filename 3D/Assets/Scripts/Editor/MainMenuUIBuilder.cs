#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace GameUI
{
    /// <summary>
    /// Công cụ Editor đặc biệt dành cho Unity:
    /// - Tự động xoá toàn bộ Canvas cũ để dọn dẹp Scene gọn gàng.
    /// - Tạo mới một Canvas Main Menu cực kỳ đẹp mắt, đậm chất kinh dị (Theme tối + đỏ phát sáng) với tiêu đề "Behind the Dark".
    /// - Tự động liên kết các sự kiện OnClick, Sliders với script MainMenu.cs.
    /// </summary>
    public class MainMenuUIBuilder : EditorWindow
    {
        [MenuItem("Tools/Generate Beautiful Main Menu")]
        public static void BuildOrUpgradeMenu()
        {
            // 1. Tìm hoặc tạo MainMenu Controller trong Scene
            MainMenu mainMenu = Object.FindAnyObjectByType<MainMenu>();
            if (mainMenu == null)
            {
                GameObject controller = new GameObject("MainMenuController");
                mainMenu = controller.AddComponent<MainMenu>();
                Undo.RegisterCreatedObjectUndo(controller, "Create Main Menu Controller");
                Debug.Log("[MainMenuUIBuilder] Đã tạo mới đối tượng 'MainMenuController'!");
            }

            // --- BẢO VỆ CUTSCENE & LOADING PANELS TRƯỚC KHI XOÁ CANVAS ---
            CutsceneManager cutsceneMgr = Object.FindAnyObjectByType<CutsceneManager>(FindObjectsInactive.Include);
            GameObject preservedCutscenePanel = null;
            GameObject preservedLoadingPanel = null;
            UnityEngine.Video.VideoPlayer preservedVideoPlayer = null;

            if (cutsceneMgr != null)
            {
                preservedCutscenePanel = cutsceneMgr.cutscenePanel;
                preservedLoadingPanel = cutsceneMgr.loadingPanel;
                preservedVideoPlayer = cutsceneMgr.cutsceneVideoPlayer;
            }

            // Tìm kiếm bổ sung bằng tên nếu tham chiếu bị null (đề phòng chạy lại tool khi đã mất tham chiếu)
            if (preservedCutscenePanel == null)
            {
                preservedCutscenePanel = FindGameObjectInActiveScene("CutsencePanel");
                if (preservedCutscenePanel == null) preservedCutscenePanel = FindGameObjectInActiveScene("CutscencePanel");
                if (preservedCutscenePanel == null) preservedCutscenePanel = FindGameObjectInActiveScene("CutscenePanel");
            }
            if (preservedLoadingPanel == null)
            {
                preservedLoadingPanel = FindGameObjectInActiveScene("LoadingPanel");
                if (preservedLoadingPanel == null) preservedLoadingPanel = FindGameObjectInActiveScene("Loading Panel");
            }

            if (preservedCutscenePanel != null)
            {
                if (preservedVideoPlayer == null)
                {
                    preservedVideoPlayer = preservedCutscenePanel.GetComponentInChildren<UnityEngine.Video.VideoPlayer>(true);
                }
                // Tách khỏi Canvas cũ để không bị xoá
                Undo.SetTransformParent(preservedCutscenePanel.transform, null, "Preserve Cutscene Panel");
                Debug.Log($"[MainMenuUIBuilder] Đã bảo vệ và giữ lại panel Cutsence: '{preservedCutscenePanel.name}'");
            }
            if (preservedLoadingPanel != null)
            {
                // Tách khỏi Canvas cũ để không bị xoá
                Undo.SetTransformParent(preservedLoadingPanel.transform, null, "Preserve Loading Panel");
                Debug.Log($"[MainMenuUIBuilder] Đã bảo vệ và giữ lại panel Loading: '{preservedLoadingPanel.name}'");
            }
            // --- KẾT THÚC BẢO VỆ ---

            // 2. Xoá TOÀN BỘ Canvas cũ trong Scene để dọn dẹp sạch sẽ
            Canvas[] existingCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int deletedCanvasCount = 0;
            foreach (Canvas oldCanvas in existingCanvases)
            {
                Debug.Log($"[MainMenuUIBuilder] Đang xoá Canvas cũ: '{oldCanvas.name}'");
                Undo.DestroyObjectImmediate(oldCanvas.gameObject);
                deletedCanvasCount++;
            }

            // 3. Xoá các EventSystem cũ để tránh xung đột
            EventSystem[] existingEventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (EventSystem oldES in existingEventSystems)
            {
                Undo.DestroyObjectImmediate(oldES.gameObject);
            }

            // 4. Tạo mới hoàn toàn một Canvas mẫu tuyệt đẹp từ đầu
            CreateNewSpookyMenu(mainMenu, preservedCutscenePanel, preservedLoadingPanel, cutsceneMgr, preservedVideoPlayer);

            // Đánh dấu Scene đã thay đổi để Unity lưu lại
            EditorUtility.SetDirty(mainMenu);
            if (mainMenu.gameObject != null) EditorUtility.SetDirty(mainMenu.gameObject);
            
            // Hiện hộp thoại thông báo hoàn thành
            string infoMessage = deletedCanvasCount > 0 
                ? $"Đã xoá thành công {deletedCanvasCount} Canvas cũ và EventSystem cũ!\n\n"
                : "Không tìm thấy Canvas cũ. Đã khởi tạo mới từ đầu!\n\n";

            EditorUtility.DisplayDialog(
                "Thành Công!",
                infoMessage +
                "Đã tạo mới hệ thống Main Menu thành công:\n" +
                "- Tiêu đề game: \"Behind the Dark\"\n" +
                "- CanvasGroup đã được tự động thêm để hỗ trợ Fade mượt.\n" +
                "- Gắn hiệu ứng UIHoverEffect tự phóng to & đổi màu đỏ máu cho các nút.\n" +
                (preservedCutscenePanel != null ? "- Đã khôi phục thành công CutsencePanel!\n" : "- Không tìm thấy CutsencePanel cũ để khôi phục.\n") +
                (preservedLoadingPanel != null ? "- Đã khôi phục thành công LoadingPanel!\n" : "- Không tìm thấy LoadingPanel cũ để khôi phục.\n") +
                "\nNhấn Play trong Unity để chạy thử ngay!",
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
            
            // Đảm bảo bật tương tác
            cg.alpha = panel.activeSelf ? 1f : 0f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        private static void CreateNewSpookyMenu(MainMenu mainMenu, GameObject preservedCutscenePanel, GameObject preservedLoadingPanel, CutsceneManager cutsceneMgr, UnityEngine.Video.VideoPlayer preservedVideoPlayer)
        {
            Debug.Log("[MainMenuUIBuilder] Đang tạo mới Main Menu 'Behind the Dark'...");

            // 1. Tạo Canvas gốc
            GameObject canvasObj = new GameObject("MainMenuCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");

            // 2. Tạo MainMenu Panel (Menu chính)
            GameObject mainMenuPanel = new GameObject("MainMenu Panel");
            mainMenuPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform rectMMP = mainMenuPanel.AddComponent<RectTransform>();
            rectMMP.anchorMin = Vector2.zero;
            rectMMP.anchorMax = Vector2.one;
            rectMMP.sizeDelta = Vector2.zero;
            
            // Nền tối mờ góc trái để dễ đọc chữ trên nền 3D hành lang
            Image imgMMP = mainMenuPanel.AddComponent<Image>();
            imgMMP.color = new Color(0f, 0f, 0f, 0.45f); // Làm mờ nhẹ hành lang phía sau

            mainMenu.mainMenuPanel = mainMenuPanel;
            SetupPanelCanvasGroup(mainMenuPanel);

            // 3. Tạo Tiêu đề game (Game Title: Behind the Dark)
            GameObject titleObj = new GameObject("GameTitle");
            titleObj.transform.SetParent(mainMenuPanel.transform, false);
            RectTransform rectTitle = titleObj.AddComponent<RectTransform>();
            rectTitle.anchorMin = new Vector2(0f, 0.72f);
            rectTitle.anchorMax = new Vector2(1f, 0.92f);
            rectTitle.anchoredPosition = new Vector2(100f, 0f); // Lệch sang trái
            rectTitle.pivot = new Vector2(0f, 0.5f);

            TextMeshProUGUI textTitle = titleObj.AddComponent<TextMeshProUGUI>();
            textTitle.text = "Behind the Dark";
            textTitle.fontSize = 76;
            textTitle.fontStyle = FontStyles.Bold | FontStyles.Italic;
            textTitle.color = new Color(0.9f, 0.1f, 0.1f, 1f); // Màu đỏ máu kinh dị nổi bật
            textTitle.alignment = TextAlignmentOptions.Left;

            // 4. Tạo khu vực chứa nút bấm (Vertical Layout)
            GameObject buttonContainer = new GameObject("ButtonsGroup");
            buttonContainer.transform.SetParent(mainMenuPanel.transform, false);
            RectTransform rectBC = buttonContainer.AddComponent<RectTransform>();
            rectBC.anchorMin = new Vector2(0f, 0.15f);
            rectBC.anchorMax = new Vector2(0.4f, 0.65f);
            rectBC.anchoredPosition = new Vector2(100f, 0f);
            rectBC.pivot = new Vector2(0f, 0.5f);

            VerticalLayoutGroup vlg = buttonContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 20;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;

            // 5. Sinh các nút: Continue, New Game, Settings, Leaderboard, Exit
            CreateStyledButton(buttonContainer.transform, "ContinueButton", "CONTINUE", mainMenu, "ClickContinue");
            CreateStyledButton(buttonContainer.transform, "NewGameButton", "NEW GAME", mainMenu, "ClickNewGame");
            CreateStyledButton(buttonContainer.transform, "SettingsButton", "SETTINGS", mainMenu, "ShowSettings");
            CreateStyledButton(buttonContainer.transform, "LeaderboardButton", "LEADERBOARD", mainMenu, "ShowLeaderboard");
            CreateStyledButton(buttonContainer.transform, "ExitButton", "EXIT", mainMenu, "ClickExit");

            // 6. Tạo Settings Panel
            GameObject settingsPanel = new GameObject("Setting Panel");
            settingsPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform rectSP = settingsPanel.AddComponent<RectTransform>();
            rectSP.anchorMin = Vector2.zero;
            rectSP.anchorMax = Vector2.one;
            rectSP.sizeDelta = Vector2.zero;
            
            // Phủ nền tối hơn khi vào cài đặt để tập trung vào thanh điều khiển
            Image imgSP = settingsPanel.AddComponent<Image>();
            imgSP.color = new Color(0f, 0f, 0f, 0.88f);

            mainMenu.settingsPanel = settingsPanel;
            SetupPanelCanvasGroup(settingsPanel);
            settingsPanel.SetActive(false); // Ẩn mặc định

            // Tiêu đề Cài đặt
            GameObject sTitleObj = new GameObject("SettingsTitle");
            sTitleObj.transform.SetParent(settingsPanel.transform, false);
            RectTransform rectSTitle = sTitleObj.AddComponent<RectTransform>();
            rectSTitle.anchorMin = new Vector2(0.5f, 0.8f);
            rectSTitle.anchorMax = new Vector2(0.5f, 0.95f);
            rectSTitle.anchoredPosition = Vector2.zero;
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
            rectSG.anchorMin = new Vector2(0.2f, 0.25f);
            rectSG.anchorMax = new Vector2(0.8f, 0.75f);
            rectSG.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup vlgSG = slidersGroup.AddComponent<VerticalLayoutGroup>();
            vlgSG.spacing = 30;
            vlgSG.childForceExpandWidth = true;
            vlgSG.childForceExpandHeight = false;

            // Tạo các Sliders
            mainMenu.masterVolumeSlider = CreateVolumeSlider(slidersGroup.transform, "Master Volume", "MasterVolumeSlider");
            mainMenu.musicVolumeSlider = CreateVolumeSlider(slidersGroup.transform, "Music Volume", "MusicVolumeSlider");
            mainMenu.sfxVolumeSlider = CreateVolumeSlider(slidersGroup.transform, "SFX Volume", "SFXVolumeSlider");
            mainMenu.sensitivitySlider = CreateVolumeSlider(slidersGroup.transform, "Mouse Sensitivity", "SensitivitySlider", 0.1f, 10f, 2f);

            // Nút Back trong Cài đặt
            GameObject backBtnObj = new GameObject("BackButton");
            backBtnObj.transform.SetParent(settingsPanel.transform, false);
            RectTransform rectBack = backBtnObj.AddComponent<RectTransform>();
            rectBack.anchorMin = new Vector2(0.5f, 0.1f);
            rectBack.anchorMax = new Vector2(0.5f, 0.18f);
            rectBack.sizeDelta = new Vector2(250, 50);
            rectBack.anchoredPosition = Vector2.zero;

            Image imgBack = backBtnObj.AddComponent<Image>();
            imgBack.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
            Button btnBack = backBtnObj.AddComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btnBack.onClick, mainMenu.ShowMainMenu);

            GameObject backTextObj = new GameObject("Text");
            backTextObj.transform.SetParent(backBtnObj.transform, false);
            RectTransform rectBackText = backTextObj.AddComponent<RectTransform>();
            rectBackText.anchorMin = Vector2.zero;
            rectBackText.anchorMax = Vector2.one;
            rectBackText.sizeDelta = Vector2.zero;
            TextMeshProUGUI textBack = backTextObj.AddComponent<TextMeshProUGUI>();
            textBack.text = "BACK TO MENU";
            textBack.fontSize = 24;
            textBack.color = Color.white;
            textBack.alignment = TextAlignmentOptions.Center;

            UIHoverEffect hoverBack = backBtnObj.AddComponent<UIHoverEffect>();
            hoverBack.targetText = textBack;
            hoverBack.hoverColor = new Color(0.9f, 0.1f, 0.1f, 1f);

            // 6.5 Tạo Leaderboard Panel
            GameObject leaderboardPanel = new GameObject("Leaderboard Panel");
            leaderboardPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform rectLP = leaderboardPanel.AddComponent<RectTransform>();
            rectLP.anchorMin = Vector2.zero;
            rectLP.anchorMax = Vector2.one;
            rectLP.sizeDelta = Vector2.zero;
            
            Image imgLP = leaderboardPanel.AddComponent<Image>();
            imgLP.color = new Color(0f, 0f, 0f, 0.92f); // Nền tối sẫm, kinh dị

            mainMenu.leaderboardPanel = leaderboardPanel;
            SetupPanelCanvasGroup(leaderboardPanel);
            leaderboardPanel.SetActive(false); // Ẩn mặc định

            // Tiêu đề Bảng xếp hạng
            GameObject lTitleObj = new GameObject("LeaderboardTitle");
            lTitleObj.transform.SetParent(leaderboardPanel.transform, false);
            RectTransform rectLTitle = lTitleObj.AddComponent<RectTransform>();
            rectLTitle.anchorMin = new Vector2(0.5f, 0.85f);
            rectLTitle.anchorMax = new Vector2(0.5f, 0.95f);
            rectLTitle.anchoredPosition = Vector2.zero;
            TextMeshProUGUI textLTitle = lTitleObj.AddComponent<TextMeshProUGUI>();
            textLTitle.text = "＝ LEADERBOARD ＝";
            textLTitle.fontSize = 48;
            textLTitle.fontStyle = FontStyles.Bold | FontStyles.Italic;
            textLTitle.color = new Color(0.9f, 0.1f, 0.1f, 1f); // Màu đỏ máu phát sáng
            textLTitle.alignment = TextAlignmentOptions.Center;

            // Tạo nhãn tiêu đề cột: Rank | Player Name | Completion Time | Date
            GameObject headerObj = new GameObject("ColumnsHeader");
            headerObj.transform.SetParent(leaderboardPanel.transform, false);
            RectTransform rectHeader = headerObj.AddComponent<RectTransform>();
            rectHeader.anchorMin = new Vector2(0.15f, 0.73f);
            rectHeader.anchorMax = new Vector2(0.85f, 0.79f);
            rectHeader.offsetMin = rectHeader.offsetMax = Vector2.zero;

            HorizontalLayoutGroup hlgHeader = headerObj.AddComponent<HorizontalLayoutGroup>();
            hlgHeader.childForceExpandWidth = false;
            hlgHeader.childControlWidth = true;
            hlgHeader.childForceExpandHeight = true;
            hlgHeader.childControlHeight = true;
            
            CreateHeaderColumn(headerObj.transform, "RANK", 0.15f);
            CreateHeaderColumn(headerObj.transform, "PLAYER NAME", 0.45f);
            CreateHeaderColumn(headerObj.transform, "TIME", 0.2f);
            CreateHeaderColumn(headerObj.transform, "DATE", 0.2f);

            // Một đường phân cách ngang mỏng màu đỏ máu
            GameObject divider = new GameObject("Divider");
            divider.transform.SetParent(leaderboardPanel.transform, false);
            RectTransform rectDiv = divider.AddComponent<RectTransform>();
            rectDiv.anchorMin = new Vector2(0.15f, 0.72f);
            rectDiv.anchorMax = new Vector2(0.85f, 0.725f);
            rectDiv.offsetMin = rectDiv.offsetMax = Vector2.zero;
            Image divImg = divider.AddComponent<Image>();
            divImg.color = new Color(0.6f, 0.05f, 0.05f, 0.8f);

            // Tạo Scroll View chứa danh sách điểm số
            GameObject scrollViewObj = new GameObject("LeaderboardScrollView");
            scrollViewObj.transform.SetParent(leaderboardPanel.transform, false);
            RectTransform rectSV = scrollViewObj.AddComponent<RectTransform>();
            rectSV.anchorMin = new Vector2(0.15f, 0.22f);
            rectSV.anchorMax = new Vector2(0.85f, 0.7f);
            rectSV.offsetMin = rectSV.offsetMax = Vector2.zero;

            ScrollRect scrollRect = scrollViewObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 25f;

            // Viewport
            GameObject viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(scrollViewObj.transform, false);
            RectTransform rectVP = viewportObj.AddComponent<RectTransform>();
            rectVP.anchorMin = Vector2.zero;
            rectVP.anchorMax = Vector2.one;
            rectVP.offsetMin = rectVP.offsetMax = Vector2.zero;
            viewportObj.AddComponent<RectMask2D>();
            scrollRect.viewport = rectVP;

            // Content Container
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);
            RectTransform rectContent = contentObj.AddComponent<RectTransform>();
            rectContent.anchorMin = new Vector2(0f, 1f);
            rectContent.anchorMax = new Vector2(1f, 1f);
            rectContent.pivot = new Vector2(0.5f, 1f);
            rectContent.sizeDelta = new Vector2(0, 300);
            
            VerticalLayoutGroup vlgContent = contentObj.AddComponent<VerticalLayoutGroup>();
            vlgContent.spacing = 10;
            vlgContent.childForceExpandWidth = true;
            vlgContent.childForceExpandHeight = false;
            vlgContent.childControlWidth = true;
            vlgContent.childControlHeight = true;

            ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = rectContent;
            mainMenu.leaderboardContainer = rectContent; // Tự gán container chứa danh sách

            // Tạo nút Back trong Leaderboard Panel
            GameObject lBackBtnObj = new GameObject("LeaderboardBackButton");
            lBackBtnObj.transform.SetParent(leaderboardPanel.transform, false);
            RectTransform rectLBack = lBackBtnObj.AddComponent<RectTransform>();
            rectLBack.anchorMin = new Vector2(0.5f, 0.08f);
            rectLBack.anchorMax = new Vector2(0.5f, 0.16f);
            rectLBack.sizeDelta = new Vector2(250, 50);
            rectLBack.anchoredPosition = Vector2.zero;

            Image imgLBack = lBackBtnObj.AddComponent<Image>();
            imgLBack.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
            Button btnLBack = lBackBtnObj.AddComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btnLBack.onClick, mainMenu.CloseLeaderboard);

            GameObject lBackTextObj = new GameObject("Text");
            lBackTextObj.transform.SetParent(lBackBtnObj.transform, false);
            RectTransform rectLBackText = lBackTextObj.AddComponent<RectTransform>();
            rectLBackText.anchorMin = Vector2.zero;
            rectLBackText.anchorMax = Vector2.one;
            rectLBackText.sizeDelta = Vector2.zero;
            TextMeshProUGUI textLBack = lBackTextObj.AddComponent<TextMeshProUGUI>();
            textLBack.text = "BACK TO MENU";
            textLBack.fontSize = 24;
            textLBack.color = Color.white;
            textLBack.alignment = TextAlignmentOptions.Center;

            UIHoverEffect hoverLBack = lBackBtnObj.AddComponent<UIHoverEffect>();
            hoverLBack.targetText = textLBack;
            hoverLBack.hoverColor = new Color(0.9f, 0.1f, 0.1f, 1f);

            // Đảm bảo có EventSystem
            EnsureEventSystem();

            // Đưa các panel đã bảo tồn vào Canvas mới
            if (preservedCutscenePanel != null)
            {
                Undo.SetTransformParent(preservedCutscenePanel.transform, canvasObj.transform, "Parent Cutscene Panel");
                RectTransform rt = preservedCutscenePanel.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }
                preservedCutscenePanel.SetActive(false); // Đảm bảo ẩn lúc đầu
                Debug.Log($"[MainMenuUIBuilder] Đã đưa '{preservedCutscenePanel.name}' vào MainMenuCanvas mới!");
            }

            if (preservedLoadingPanel != null)
            {
                Undo.SetTransformParent(preservedLoadingPanel.transform, canvasObj.transform, "Parent Loading Panel");
                RectTransform rt = preservedLoadingPanel.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }
                preservedLoadingPanel.SetActive(false); // Đảm bảo ẩn lúc đầu
                Debug.Log($"[MainMenuUIBuilder] Đã đưa '{preservedLoadingPanel.name}' vào MainMenuCanvas mới!");
            }

            // Tái liên kết các tham chiếu trong CutsceneManager
            if (cutsceneMgr != null)
            {
                cutsceneMgr.cutscenePanel = preservedCutscenePanel;
                cutsceneMgr.loadingPanel = preservedLoadingPanel;
                cutsceneMgr.cutsceneVideoPlayer = preservedVideoPlayer;
                EditorUtility.SetDirty(cutsceneMgr);
                Debug.Log("[MainMenuUIBuilder] Đã tự động tái liên kết CutsencePanel và LoadingPanel vào CutsceneManager!");
            }

            // Gán CutsceneManager cho MainMenu
            mainMenu.cutsceneManager = cutsceneMgr;

            Debug.Log("[MainMenuUIBuilder] Đã thiết lập thành công Canvas mới cho 'Behind the Dark'!");
        }

        private static void CreateStyledButton(Transform parent, string objName, string buttonText, MainMenu mainMenu, string methodName)
        {
            GameObject btnObj = new GameObject(objName);
            btnObj.transform.SetParent(parent, false);

            RectTransform rectBtn = btnObj.AddComponent<RectTransform>();
            rectBtn.sizeDelta = new Vector2(300, 60);

            // Nền nút trong suốt hoặc rất tối để lộ 3D đằng sau
            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.05f); // Gần như trong suốt

            Button btn = btnObj.AddComponent<Button>();
            btn.transition = Selectable.Transition.None; // Sử dụng script custom
            
            UnityEngine.Events.UnityAction action = null;
            if (methodName == "ClickContinue") action = mainMenu.ClickContinue;
            else if (methodName == "ClickNewGame") action = mainMenu.ClickNewGame;
            else if (methodName == "ShowSettings") action = mainMenu.ShowSettings;
            else if (methodName == "ShowLeaderboard") action = mainMenu.ShowLeaderboard;
            else if (methodName == "CloseLeaderboard") action = mainMenu.CloseLeaderboard;
            else if (methodName == "ClickExit") action = mainMenu.ClickExit;
            else if (methodName == "ShowMainMenu") action = mainMenu.ShowMainMenu;

            if (action != null)
            {
                UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, action);
            }

            // Text của nút bấm
            GameObject textObj = new GameObject("ButtonText");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform rectText = textObj.AddComponent<RectTransform>();
            rectText.anchorMin = Vector2.zero;
            rectText.anchorMax = Vector2.one;
            rectText.sizeDelta = Vector2.zero;

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = buttonText;
            text.fontSize = 32;
            text.color = new Color(0.85f, 0.85f, 0.85f, 1f); // Trắng sáng nhẹ mặc định
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Left; // Căn trái kiểu hiện đại

            // Thêm hiệu ứng di chuột
            UIHoverEffect hover = btnObj.AddComponent<UIHoverEffect>();
            hover.targetText = text;
            hover.hoverColor = new Color(0.9f, 0.1f, 0.1f, 1f); // Đỏ thẫm phát sáng
        }

        private static Slider CreateVolumeSlider(Transform parent, string labelText, string objName, float min = 0f, float max = 1f, float defValue = 1f)
        {
            GameObject container = new GameObject(objName + "_Container");
            container.transform.SetParent(parent, false);
            RectTransform rectCont = container.AddComponent<RectTransform>();
            rectCont.sizeDelta = new Vector2(500, 50);

            // Label
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

            // Slider Object
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

            // Background of Slider
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);
            RectTransform rectBg = bgObj.AddComponent<RectTransform>();
            rectBg.anchorMin = new Vector2(0f, 0.25f);
            rectBg.anchorMax = new Vector2(1f, 0.75f);
            rectBg.sizeDelta = Vector2.zero;
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // Fill Area
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
            fillImg.color = new Color(0.8f, 0.1f, 0.1f, 1f); // Thanh kéo màu đỏ kinh dị

            // Handle Area
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

        private static void EnsureEventSystem()
        {
            EventSystem es = Object.FindAnyObjectByType<EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                
                // Tự động phát hiện và thêm InputModule tương thích
#if ENABLE_INPUT_SYSTEM
                var uiModule = esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                uiModule.actionsAsset = null; // Xoá sạch cấu hình cũ để đè dữ liệu mặc định chuẩn
                uiModule.AssignDefaultActions();
                Debug.Log("[MainMenuUIBuilder] Đã thêm InputSystemUIInputModule và tự động gán Default Actions!");
#else
                esObj.AddComponent<StandaloneInputModule>();
                Debug.Log("[MainMenuUIBuilder] Đã thêm StandaloneInputModule cho Old Input System!");
#endif
                Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
                Debug.Log("[MainMenuUIBuilder] Đã tạo mới đối tượng 'EventSystem' trong Scene!");
            }
            else
            {
#if ENABLE_INPUT_SYSTEM
                // Nếu EventSystem có sẵn nhưng dùng module cũ dưới New Input System, nâng cấp tự động
                StandaloneInputModule oldModule = es.GetComponent<StandaloneInputModule>();
                if (oldModule != null)
                {
                    Undo.DestroyObjectImmediate(oldModule);
                    var uiModule = es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                    uiModule.actionsAsset = null; // Xoá sạch cấu hình cũ để đè dữ liệu mặc định chuẩn
                    uiModule.AssignDefaultActions();
                    Debug.Log("[MainMenuUIBuilder] Đã tự động nâng cấp EventSystem hiện tại lên InputSystemUIInputModule và gán Default Actions!");
                }
                else
                {
                    // Nếu đã có InputSystemUIInputModule, bất kể actionsAsset có bị gán nhầm hay trống, ta đè mặc định chuẩn để nút bấm hoạt động
                    var uiModule = es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                    if (uiModule != null)
                    {
                        uiModule.actionsAsset = null; // Xoá bỏ asset cấu hình lỗi/trống trong inspector
                        uiModule.AssignDefaultActions();
                        Debug.Log("[MainMenuUIBuilder] Đã ghi đè cưỡng bức Default Actions chuẩn cho EventSystem!");
                    }
                }
#endif
            }
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

        private static void CreateHeaderColumn(Transform parent, string label, float flexWidth)
        {
            GameObject col = new GameObject(label + "_Col");
            col.transform.SetParent(parent, false);
            LayoutElement le = col.AddComponent<LayoutElement>();
            le.preferredWidth = 800f * flexWidth;
            le.flexibleWidth = flexWidth;

            TextMeshProUGUI txt = col.AddComponent<TextMeshProUGUI>();
            txt.text = label;
            txt.fontSize = 22;
            txt.fontStyle = FontStyles.Bold;
            txt.color = new Color(0.7f, 0.6f, 0.5f, 1f);
            txt.alignment = TextAlignmentOptions.Center;
        }
    }
}
#endif

