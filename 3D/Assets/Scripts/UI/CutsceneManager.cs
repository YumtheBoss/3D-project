using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using TMPro;

namespace GameUI
{
    public class CutsceneManager : MonoBehaviour
    {
        public enum VideoRenderSetupMode
        {
            KeepInspectorSettings,
            ForceCameraNearPlane,
            AutoDetect
        }

        [Header("Cutscene Components")]
        public GameObject cutscenePanel;
        public VideoPlayer cutsceneVideoPlayer;
        
        [Tooltip("Cấu hình Render Mode cho Video:\n- KeepInspectorSettings: Sử dụng cấu hình trong Inspector VideoPlayer.\n- ForceCameraNearPlane: Ép buộc đè lên Camera Near Plane.\n- AutoDetect: Tự động dùng Camera Near Plane nếu RenderTexture bị thiếu Texture.")]
        public VideoRenderSetupMode renderSetupMode = VideoRenderSetupMode.AutoDetect;

        [Tooltip("Nút Skip Cutscene. Nếu trống, script tự động quét tìm trong CutsencePanel.")]
        public UnityEngine.UI.Button skipButton;

        private string sceneToLoadAfterCutscene;
        private UnityEngine.UI.Image activeFadeOverlay;

        [Header("Loading Screen")]
        public GameObject loadingPanel;

        private void Awake()
        {
            SelfHealReferences();
        }

        private void SelfHealReferences()
        {
            if (cutscenePanel == null)
            {
                cutscenePanel = FindGameObjectLocal("CutsencePanel");
                if (cutscenePanel == null) cutscenePanel = FindGameObjectLocal("CutscencePanel");
                if (cutscenePanel == null) cutscenePanel = FindGameObjectLocal("CutscenePanel");
            }

            if (loadingPanel == null)
            {
                loadingPanel = FindGameObjectLocal("LoadingPanel");
                if (loadingPanel == null) loadingPanel = FindGameObjectLocal("Loading Panel");
                
                if (loadingPanel == null)
                {
                    CreateAutoLoadingPanel();
                }
            }

            if (cutscenePanel != null && cutsceneVideoPlayer == null)
            {
                cutsceneVideoPlayer = cutscenePanel.GetComponentInChildren<VideoPlayer>(true);
            }
            
            // Tự động gắn sự kiện nút Skip hoặc tự tạo
            if (skipButton == null && cutscenePanel != null)
            {
                Transform skipBtn = FindRecursive(cutscenePanel, "SkipButton");
                if (skipBtn == null) skipBtn = FindRecursive(cutscenePanel, "Skip Button");
                if (skipBtn != null)
                {
                    skipButton = skipBtn.GetComponent<UnityEngine.UI.Button>();
                }
                else
                {
                    CreateAutoSkipButton();
                }
            }

            if (skipButton != null)
            {
                skipButton.onClick.RemoveListener(SkipCutscene);
                skipButton.onClick.AddListener(SkipCutscene);
                Debug.Log("[CutsceneManager] Đã liên kết nút Skip thành công!");
            }
        }

        private void CreateAutoLoadingPanel()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null) return;

            loadingPanel = new GameObject("AutoCreated_LoadingPanel");
            loadingPanel.transform.SetParent(canvas.transform, false);
            loadingPanel.transform.SetAsLastSibling();

            // Phủ kín màn hình
            RectTransform panelRect = loadingPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Nền tối mượt mà (sleek dark mode)
            var img = loadingPanel.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.05f, 0.05f, 0.05f, 0.95f);

            // Container chứa Spinner và Text ở giữa
            GameObject container = new GameObject("Container");
            container.transform.SetParent(loadingPanel.transform, false);
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.sizeDelta = new Vector2(400f, 200f);

            // Spinner (Vòng tròn quay)
            GameObject spinnerObj = new GameObject("Spinner");
            spinnerObj.transform.SetParent(container.transform, false);
            RectTransform spinnerRect = spinnerObj.AddComponent<RectTransform>();
            spinnerRect.anchorMin = new Vector2(0.5f, 0.65f);
            spinnerRect.anchorMax = new Vector2(0.5f, 0.65f);
            spinnerRect.pivot = new Vector2(0.5f, 0.5f);
            spinnerRect.sizeDelta = new Vector2(60f, 60f);

            // Sử dụng hình tròn mặc định của Unity (Knob) để vẽ vòng xoay
            var spinnerImg = spinnerObj.AddComponent<UnityEngine.UI.Image>();
            spinnerImg.color = new Color(0.9f, 0.1f, 0.1f, 0.8f); // Đỏ horror rực rỡ quyến rũ
            
            Sprite knobSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
            if (knobSprite != null) spinnerImg.sprite = knobSprite;

            // Tạo hiệu ứng quay spinner
            StartCoroutine(RotateSpinnerRoutine(spinnerObj.transform));

            // Text Loading bên dưới
            GameObject textObj = new GameObject("LoadingText");
            textObj.transform.SetParent(container.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.25f);
            textRect.anchorMax = new Vector2(0.5f, 0.25f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(400f, 50f);

            var txt = textObj.AddComponent<TextMeshProUGUI>();
            txt.text = "LOADING CUTSCENE...";
            txt.fontSize = 22;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            
            // Tạo hiệu ứng nhấp nháy chữ (pulsate)
            StartCoroutine(PulsateTextRoutine(txt));

            loadingPanel.SetActive(false);
            Debug.Log("[CutsceneManager Self-Heal] Đã tự động tạo Màn hình Loading kính tối cực kỳ đẹp mắt!");
        }

        private void CreateAutoSkipButton()
        {
            if (cutscenePanel == null) return;

            GameObject skipBtnObj = new GameObject("AutoCreated_SkipButton");
            skipBtnObj.transform.SetParent(cutscenePanel.transform, false);

            RectTransform rect = skipBtnObj.AddComponent<RectTransform>();
            // Anchor ở góc dưới bên phải
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-40f, 40f); // Lùi vào trong góc 40px
            rect.sizeDelta = new Vector2(180f, 50f);

            // Nền đen kính mờ (semi-transparent dark)
            var buttonImage = skipBtnObj.AddComponent<UnityEngine.UI.Image>();
            buttonImage.color = new Color(0.08f, 0.08f, 0.08f, 0.8f);
            
            // Dùng InputFieldBackground để có bo góc mượt mà
            Sprite btnSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/InputFieldBackground.psd");
            if (btnSprite != null)
            {
                buttonImage.sprite = btnSprite;
                buttonImage.type = UnityEngine.UI.Image.Type.Sliced;
            }

            // Component Button
            skipButton = skipBtnObj.AddComponent<UnityEngine.UI.Button>();

            // Cài đặt Transition màu nút bấm
            UnityEngine.UI.ColorBlock colors = skipButton.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 1f);
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            colors.selectedColor = new Color(1f, 1f, 1f, 1f);
            skipButton.colors = colors;

            // Chữ text hiển thị bằng TMPro
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(skipBtnObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var txt = textObj.AddComponent<TextMeshProUGUI>();
            txt.text = "SKIP BỎ QUA >>";
            txt.fontSize = 18f;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = new Color(0.85f, 0.85f, 0.85f, 1f);

            // Gắn hiệu ứng tương tác UIHoverEffect (Premium Micro-animations)
            var hoverEffect = skipBtnObj.AddComponent<UIHoverEffect>();
            hoverEffect.targetText = txt;
            hoverEffect.hoverColor = new Color(0.9f, 0.1f, 0.1f, 1f); // Màu đỏ kinh dị khi rê chuột vào
            hoverEffect.hoverScale = new Vector3(1.05f, 1.05f, 1.05f); // Phóng to nhẹ

            // Gắn sự kiện click
            skipButton.onClick.RemoveListener(SkipCutscene);
            skipButton.onClick.AddListener(SkipCutscene);

            Debug.Log("[CutsceneManager Self-Heal] Đã tự động tạo Nút Skip kính mờ cao cấp ở góc dưới bên phải Canvas!");
        }

        private IEnumerator RotateSpinnerRoutine(Transform t)
        {
            while (true)
            {
                if (t != null && t.gameObject.activeInHierarchy)
                {
                    t.Rotate(0f, 0f, -250f * Time.deltaTime);
                }
                yield return null;
            }
        }

        private IEnumerator PulsateTextRoutine(TextMeshProUGUI txt)
        {
            float elapsed = 0f;
            while (true)
            {
                if (txt != null && txt.gameObject.activeInHierarchy)
                {
                    elapsed += Time.deltaTime * 3.5f;
                    float alpha = Mathf.Lerp(0.3f, 1.0f, (Mathf.Sin(elapsed) + 1f) / 2f);
                    txt.color = new Color(txt.color.r, txt.color.g, txt.color.b, alpha);
                }
                yield return null;
            }
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

        private void Start()
        {
            // Đảm bảo các tham chiếu được tự động dò tìm trước
            SelfHealReferences();

            // Tắt loading và panel video lúc bắt đầu
            if (loadingPanel != null)
                loadingPanel.SetActive(false);

            if (cutscenePanel != null) 
                cutscenePanel.SetActive(false);

            // Đăng ký sự kiện khi video kết thúc và cấu hình VideoPlayer an toàn
            if (cutsceneVideoPlayer != null)
            {
                cutsceneVideoPlayer.playOnAwake = false; // Ngăn chặn tự động phát trước khi chuẩn bị xong
                cutsceneVideoPlayer.loopPointReached -= OnCutsceneFinished; // Tránh đăng ký lặp
                cutsceneVideoPlayer.loopPointReached += OnCutsceneFinished;
            }
        }

        public void PlayCutscene(string targetScene, UnityEngine.UI.Image fadeOverlay = null)
        {
            activeFadeOverlay = fadeOverlay;
            if (cutscenePanel != null && cutsceneVideoPlayer != null)
            {
                sceneToLoadAfterCutscene = targetScene;
                cutscenePanel.SetActive(true);
                StartCoroutine(PlayCutsceneRoutine());
            }
            else
            {
                Debug.LogWarning("[CutsceneManager] Thiếu thành phần Cutscene! Tải thẳng gameplay.");
                SceneManager.LoadScene(targetScene);
            }
        }

        private IEnumerator PlayCutsceneRoutine()
        {
            Debug.Log("[CutsceneManager] Đang nạp trước Video Cutscene...");
            
            // Dừng luồng cũ để tránh lỗi bất đồng bộ
            cutsceneVideoPlayer.Stop();

            // Tự động tìm camera trong scene
            Camera cam = Camera.main;
            if (cam == null)
            {
                cam = Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
            }

            // Xử lý Render Mode theo tuỳ chọn của người dùng trên Inspector:
            if (renderSetupMode == VideoRenderSetupMode.ForceCameraNearPlane && cam != null)
            {
                cutsceneVideoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
                cutsceneVideoPlayer.targetCamera = cam;
                cutsceneVideoPlayer.aspectRatio = VideoAspectRatio.FitHorizontally;
                Debug.Log($"[CutsceneManager] Đã ép buộc Render Mode sang Camera Near Plane của camera: '{cam.name}' để hiển thị hình ảnh!");
            }
            else if (renderSetupMode == VideoRenderSetupMode.AutoDetect)
            {
                if (cutsceneVideoPlayer.renderMode == VideoRenderMode.RenderTexture && cutsceneVideoPlayer.targetTexture == null && cam != null)
                {
                    cutsceneVideoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
                    cutsceneVideoPlayer.targetCamera = cam;
                    cutsceneVideoPlayer.aspectRatio = VideoAspectRatio.FitHorizontally;
                    Debug.Log("[CutsceneManager] RenderTexture bị thiếu Target Texture! Đã tự động đổi sang Camera Near Plane.");
                }
                else if ((cutsceneVideoPlayer.renderMode == VideoRenderMode.CameraNearPlane || cutsceneVideoPlayer.renderMode == VideoRenderMode.CameraFarPlane) && cutsceneVideoPlayer.targetCamera == null && cam != null)
                {
                    cutsceneVideoPlayer.targetCamera = cam;
                    Debug.Log($"[CutsceneManager] Camera Near/Far Plane bị thiếu camera chỉ định! Đã tự động gán camera: '{cam.name}'.");
                }
            }
            else // KeepInspectorSettings
            {
                // Vẫn tự động khắc phục lỗi nếu thiết lập CameraNear/FarPlane nhưng bỏ trống Target Camera
                if ((cutsceneVideoPlayer.renderMode == VideoRenderMode.CameraNearPlane || cutsceneVideoPlayer.renderMode == VideoRenderMode.CameraFarPlane) && cutsceneVideoPlayer.targetCamera == null && cam != null)
                {
                    cutsceneVideoPlayer.targetCamera = cam;
                    Debug.Log($"[CutsceneManager] Camera Near/Far Plane bị thiếu camera! Tự động gán camera: '{cam.name}'.");
                }
            }

            // Nếu phát trên Camera Near Plane hoặc Camera Far Plane, tự động làm trong suốt hình nền đục của Canvas panel để tránh che mất video
            if (cutsceneVideoPlayer.renderMode == VideoRenderMode.CameraNearPlane || cutsceneVideoPlayer.renderMode == VideoRenderMode.CameraFarPlane)
            {
                var panelImage = cutscenePanel.GetComponent<UnityEngine.UI.Image>();
                if (panelImage != null)
                {
                    panelImage.color = new Color(panelImage.color.r, panelImage.color.g, panelImage.color.b, 0f);
                    Debug.Log("[CutsceneManager] Đã làm trong suốt Image nền của Cutscene Panel để lộ video phía sau.");
                }
                var panelRawImage = cutscenePanel.GetComponent<UnityEngine.UI.RawImage>();
                if (panelRawImage != null)
                {
                    panelRawImage.color = new Color(panelRawImage.color.r, panelRawImage.color.g, panelRawImage.color.b, 0f);
                    Debug.Log("[CutsceneManager] Đã làm trong suốt RawImage nền của Cutscene Panel để lộ video phía sau.");
                }
            }
            
            // Bật Loading Panel trong lúc chuẩn bị video để không bị ló mặt sau hoặc đơ màn hình
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(true);
            }

            // Gọi Prepare để nạp video dưới nền
            cutsceneVideoPlayer.Prepare();

            float maxWaitTime = 5f; // Chờ tối đa 5 giây để nạp video
            float elapsed = 0f;
            while (!cutsceneVideoPlayer.isPrepared && elapsed < maxWaitTime)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Nạp xong video (hoặc hết thời gian chờ) -> tắt Loading Panel
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }

            if (cutsceneVideoPlayer.isPrepared)
            {
                Debug.Log("[CutsceneManager] Video đã chuẩn bị xong. Bắt đầu phát!");
                cutsceneVideoPlayer.Play();

                // Fade out mượt mà màn hình tối sang trong suốt để hiện video
                if (activeFadeOverlay != null)
                {
                    StartCoroutine(FadeOutOverlayRoutine(activeFadeOverlay, 0.6f));
                }
            }
            else
            {
                Debug.LogWarning("[CutsceneManager] Không thể nạp video (Timeout)! Tự động bỏ qua cutscene sang gameplay.");
                StartCoroutine(LoadSceneAsync());
            }
        }

        private IEnumerator FadeOutOverlayRoutine(UnityEngine.UI.Image overlay, float duration)
        {
            if (overlay == null) yield break;

            CanvasGroup cg = overlay.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }

            float elapsed = 0f;
            Color startColor = overlay.color;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                overlay.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(1f, 0f, t));
                yield return null;
            }

            overlay.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
            overlay.gameObject.SetActive(false); // Tắt hẳn để tránh tốn năng năng/cản raycast
        }

        private void OnCutsceneFinished(VideoPlayer vp)
        {
            // Chỉ kết thúc chuyển cảnh nếu video đã được nạp chuẩn chỉnh và hoàn thành tự nhiên
            if (vp.isPrepared)
            {
                Debug.Log("[CutsceneManager] Cutscene kết thúc bình thường. Tiến hành chuyển cảnh.");
                StartCoroutine(LoadSceneAsync());
            }
        }

        // Hàm này sẽ được gán vào nút Skip
        public void SkipCutscene()
        {
            StartCoroutine(LoadSceneAsync());
        }

        private IEnumerator LoadSceneAsync()
        {
            if (cutsceneVideoPlayer != null)
            {
                cutsceneVideoPlayer.Stop(); // Dừng video
                cutsceneVideoPlayer.enabled = false; // Tắt luồng video 
            }

            // Hiển thị màn hình Loading
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(true);
            }

            // Đợi vài frame để màn hình Loading kịp vẽ ra trước khi quá trình load làm lag game
            yield return null;
            yield return null;
            yield return null;

            // Tải scene mới dưới nền
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToLoadAfterCutscene);
            
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }
    }
}
