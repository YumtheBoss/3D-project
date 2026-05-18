using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

namespace GameUI
{
    public class CutsceneManager : MonoBehaviour
    {
        [Header("Cutscene Components")]
        public GameObject cutscenePanel;
        public VideoPlayer cutsceneVideoPlayer;
        private string sceneToLoadAfterCutscene;

        [Header("Loading Screen")]
        public GameObject loadingPanel;

        [Header("Intro Dialogues")]
        [Tooltip("Các câu thoại xuất hiện ở đầu game. Nếu có nhiều câu, nó sẽ tự động chạy lần lượt.")]
        [TextArea(2, 5)]
        public string[] introDialogues = new string[] { "Tại sao tôi lại ở đây..." };

        private void Start()
        {
            // Tắt loading và panel video lúc bắt đầu
            if (loadingPanel != null)
                loadingPanel.SetActive(false);

            if (cutscenePanel != null) 
                cutscenePanel.SetActive(false);

            // Đăng ký sự kiện khi video kết thúc
            if (cutsceneVideoPlayer != null)
            {
                cutsceneVideoPlayer.loopPointReached += OnCutsceneFinished;
            }
        }

        public void PlayCutscene(string targetScene)
        {
            if (cutscenePanel != null && cutsceneVideoPlayer != null)
            {
                sceneToLoadAfterCutscene = targetScene;
                cutscenePanel.SetActive(true);
                cutsceneVideoPlayer.Play();
            }
            else
            {
                Debug.LogWarning("Cutscene components are missing. Loading scene directly.");
                SceneManager.LoadScene(targetScene);
            }
        }

        private void OnCutsceneFinished(VideoPlayer vp)
        {
            StartCoroutine(LoadSceneAsync());
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

            // 1. Hiển thị màn hình Loading
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(true);
            }

            yield return null;
            yield return null;

            // 2. Tải scene mới dưới nền nhưng KÌM LẠI chưa cho kích hoạt ngay
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToLoadAfterCutscene);
            asyncLoad.allowSceneActivation = false; // Ngăn chặn chuyển scene tự động
            
            // Đợi cho đến khi load xong (progress = 0.9 là load xong hoàn toàn nhưng đang bị kìm lại)
            while (asyncLoad.progress < 0.9f)
            {
                yield return null;
            }

            // 3. Scene đã load xong. Giờ tắt màn loading đi
            if (loadingPanel != null) loadingPanel.SetActive(false);

            // 4. Tạo màn hình "Chương 1" bằng code
            GameObject introCanvasObj = new GameObject("_ChapterIntroCanvas_Auto");
            DontDestroyOnLoad(introCanvasObj); // Để sống sót qua quá trình chuyển scene
            
            Canvas canvas = introCanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999; // Trên cùng
            introCanvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();

            // Nền đen - Mí trên
            GameObject topBgObj = new GameObject("TopBg");
            topBgObj.transform.SetParent(canvas.transform, false);
            UnityEngine.UI.Image topBg = topBgObj.AddComponent<UnityEngine.UI.Image>();
            topBg.color = Color.black;
            RectTransform topRt = topBgObj.GetComponent<RectTransform>();
            topRt.anchorMin = new Vector2(0, 0.5f); topRt.anchorMax = new Vector2(1, 1);
            topRt.offsetMin = Vector2.zero; topRt.offsetMax = Vector2.zero;

            // Nền đen - Mí dưới
            GameObject botBgObj = new GameObject("BotBg");
            botBgObj.transform.SetParent(canvas.transform, false);
            UnityEngine.UI.Image botBg = botBgObj.AddComponent<UnityEngine.UI.Image>();
            botBg.color = Color.black;
            RectTransform botRt = botBgObj.GetComponent<RectTransform>();
            botRt.anchorMin = new Vector2(0, 0); botRt.anchorMax = new Vector2(1, 0.5f);
            botRt.offsetMin = Vector2.zero; botRt.offsetMax = Vector2.zero;

            // Chữ
            GameObject textObj = new GameObject("Txt");
            textObj.transform.SetParent(canvas.transform, false);
            UnityEngine.UI.Text txt = textObj.AddComponent<UnityEngine.UI.Text>();
            txt.text = "CHƯƠNG 1\n\nCĂN PHÒNG VÔ HẠN";
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 60;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            RectTransform txtRt = textObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;

            // Thêm script tự hủy sau 3 giây
            ChapterIntroDestroyer destroyer = introCanvasObj.AddComponent<ChapterIntroDestroyer>();
            destroyer.dialogueLines = this.introDialogues;

            // 5. Kích hoạt Scene Gameplay! Lúc này màn hình "Chương 1" vẫn đang che đè lên trên
            asyncLoad.allowSceneActivation = true;
        }
    }

    /// <summary>
    /// Script phụ trợ để điều khiển hiệu ứng mắt mở và hội thoại
    /// </summary>
    public class ChapterIntroDestroyer : MonoBehaviour
    {
        public string[] dialogueLines;

        private IEnumerator Start()
        {
            RectTransform topRt = transform.Find("TopBg").GetComponent<RectTransform>();
            RectTransform botRt = transform.Find("BotBg").GetComponent<RectTransform>();
            UnityEngine.UI.Text txt = transform.Find("Txt").GetComponent<UnityEngine.UI.Text>();

            // 1. Giữ màn hình đen + Chữ "Chương 1" trong 2.5 giây
            yield return new WaitForSeconds(2.5f);

            // 2. Mờ chữ đi
            float elapsed = 0;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                txt.color = new Color(1, 1, 1, 1f - elapsed);
                yield return null;
            }
            txt.color = new Color(1, 1, 1, 0);

            // 3. Hiệu ứng nháy mắt (Mở mí lên rồi nhắm lại 1 chút rồi mở hẳn)
            // Lần 1: Mở hé (20%)
            yield return StartCoroutine(OpenEyes(topRt, botRt, 0.2f, 0.3f));
            // Nhắm lại
            yield return StartCoroutine(OpenEyes(topRt, botRt, 0f, 0.15f)); 
            
            // Lần 2: Mở hẳn (100%)
            yield return StartCoroutine(OpenEyes(topRt, botRt, 1f, 1.2f));

            // Xóa mí mắt và chữ cho nhẹ bộ nhớ
            Destroy(topRt.gameObject);
            Destroy(botRt.gameObject);
            Destroy(txt.gameObject);

            // TỪ ĐÂY TRỞ ĐI: Đổi tên Canvas để bỏ giới hạn tương tác (ẩn chữ E) ở các cửa
            // Giúp người chơi có thể di chuyển và tương tác bình thường trong lúc nghe hội thoại
            gameObject.name = "_ChapterIntroCanvas_Dialog";

            // 4. Tạo Dialog UI ở cạnh dưới màn hình
            GameObject dialogPanel = new GameObject("DialogPanel");
            dialogPanel.transform.SetParent(transform, false);
            UnityEngine.UI.Image panelImg = dialogPanel.AddComponent<UnityEngine.UI.Image>();
            panelImg.color = new Color(0, 0, 0, 0.7f); // Nền đen mờ
            RectTransform panelRt = dialogPanel.GetComponent<RectTransform>();
            // Neo ở dưới cùng màn hình
            panelRt.anchorMin = new Vector2(0.1f, 0.05f); 
            panelRt.anchorMax = new Vector2(0.9f, 0.2f); 
            panelRt.offsetMin = Vector2.zero; 
            panelRt.offsetMax = Vector2.zero;

            GameObject dialogText = new GameObject("DialogText");
            dialogText.transform.SetParent(dialogPanel.transform, false);
            UnityEngine.UI.Text dTxt = dialogText.AddComponent<UnityEngine.UI.Text>();
            dTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            dTxt.fontSize = 35;
            dTxt.color = Color.white;
            dTxt.alignment = TextAnchor.MiddleLeft;
            dTxt.text = ""; // Ban đầu trống rỗng
            RectTransform dTxtRt = dialogText.GetComponent<RectTransform>();
            // Lùi lề vào trong cho đẹp
            dTxtRt.anchorMin = Vector2.zero; dTxtRt.anchorMax = Vector2.one;
            dTxtRt.offsetMin = new Vector2(40, 10); dTxtRt.offsetMax = new Vector2(-40, -10);

            // Đợi nửa giây cho người chơi định hình quang cảnh
            yield return new WaitForSeconds(0.5f);

            // 5. Hiệu ứng gõ phím cho TẤT CẢ các câu thoại
            if (dialogueLines != null && dialogueLines.Length > 0)
            {
                foreach (string line in dialogueLines)
                {
                    dTxt.text = "";
                    for (int i = 0; i <= line.Length; i++)
                    {
                        dTxt.text = line.Substring(0, i);
                        yield return new WaitForSeconds(0.04f); // Tốc độ gõ phím
                    }
                    // Chờ người chơi đọc
                    yield return new WaitForSeconds(3.0f);
                }
            }

            // Xóa thời gian chờ thừa vì vòng lặp ở trên đã có hàm chờ

            // 6. Mờ Dialog
            elapsed = 0;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                panelImg.color = new Color(0, 0, 0, 0.7f * (1f - elapsed));
                dTxt.color = new Color(1, 1, 1, 1f - elapsed);
                yield return null;
            }

            // 7. Hoàn tất toàn bộ chuỗi intro, tự hủy
            Destroy(gameObject);
        }

        // Hàm phụ trợ tạo hiệu ứng kéo 2 mí mắt
        // percent: 0 = nhắm kín, 1 = mở to (mí trên kéo lên trên, mí dưới kéo xuống dưới)
        private IEnumerator OpenEyes(RectTransform top, RectTransform bot, float targetPercent, float duration)
        {
            // Lấy độ mở hiện tại (0 tới 1)
            float startOpen = (top.anchorMin.y - 0.5f) * 2f; 
            
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float currentOpen = Mathf.Lerp(startOpen, targetPercent, elapsed / duration);
                
                // Mí trên: kéo dần lề dưới lên đỉnh
                top.anchorMin = new Vector2(0, 0.5f + (currentOpen * 0.5f));
                // Mí dưới: kéo dần lề trên xuống đáy
                bot.anchorMax = new Vector2(1, 0.5f - (currentOpen * 0.5f));

                yield return null;
            }
            
            top.anchorMin = new Vector2(0, 0.5f + (targetPercent * 0.5f));
            bot.anchorMax = new Vector2(1, 0.5f - (targetPercent * 0.5f));
        }
    }
}
