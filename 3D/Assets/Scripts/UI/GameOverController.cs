using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Màn hình Game Over / Bad Ending / Good Ending.
/// Gắn script này vào 1 GameObject trong scene.
/// Sẽ tự tạo Canvas UI đầy đủ bằng code — không cần setup thủ công.
/// </summary>
public class GameOverController : MonoBehaviour
{
    public enum EndingType { GameOver, BadEnding, GoodEnding }

    [Header("Audio")]
    [Tooltip("Tiếng zombie nhai ngấu nghiến (Game Over / Bad Ending)")]
    public AudioClip zombieChewingAudio;
    [Tooltip("Âm thanh siêu thoát (Good Ending)")]
    public AudioClip goodEndingAudio;

    [Header("Cài đặt")]
    public string mainMenuSceneName = "MainMenu";
    [Tooltip("Thời gian text xuất hiện dần (giây)")]
    public float textFadeInDuration = 2f;

    // ── Nội bộ ──
    private Canvas endingCanvas;
    private TextMeshProUGUI mainText;
    private TextMeshProUGUI subText;
    private UnityEngine.UI.Image backgroundImage;
    private AudioSource audioSource;
    private bool isShowing = false;

    private void Awake()
    {
        BuildUI();
    }

    // ─────────────────────────────────────────────────────────
    //  API công khai — gọi từ GameFlowManager Events
    // ─────────────────────────────────────────────────────────

    public void ShowGameOver()  => StartCoroutine(ShowEndingRoutine(EndingType.GameOver));
    public void ShowBadEnding() => StartCoroutine(ShowEndingRoutine(EndingType.BadEnding));
    public void ShowGoodEnding()=> StartCoroutine(ShowEndingRoutine(EndingType.GoodEnding));

    // ─────────────────────────────────────────────────────────
    //  Logic hiển thị
    // ─────────────────────────────────────────────────────────

    private IEnumerator ShowEndingRoutine(EndingType type)
    {
        if (isShowing) yield break;
        isShowing = true;

        Time.timeScale = 0f;
        endingCanvas.gameObject.SetActive(true);

        switch (type)
        {
            case EndingType.GameOver:
                backgroundImage.color = new Color(0.02f, 0f, 0f, 1f);
                mainText.text = "GAME OVER";
                mainText.color = new Color(0.8f, 0f, 0f, 0f);
                subText.text = "Bạn đã bị bắt.\nBấm [R] để thử lại — [M] để về Menu.";
                PlayAudio(zombieChewingAudio);
                break;

            case EndingType.BadEnding:
                backgroundImage.color = new Color(0.02f, 0f, 0f, 1f);
                mainText.text = "BẠN MẮC KẸT MÃI MÃI";
                mainText.color = new Color(0.8f, 0f, 0f, 0f);
                subText.text = "Không lối thoát.\nLinh hồn bạn thuộc về nơi này...";
                PlayAudio(zombieChewingAudio);
                break;

            case EndingType.GoodEnding:
                backgroundImage.color = Color.white;
                mainText.text = "BẠN ĐÃ THOÁT KHỎI BÓI TỐI";
                mainText.color = new Color(0.9f, 0.9f, 0.9f, 0f);
                subText.text = "Ánh sáng đã dẫn đường cho bạn.\nBình an vĩnh cửu.";
                subText.color = new Color(0.7f, 0.7f, 0.7f, 0f);
                PlayAudio(goodEndingAudio);
                break;
        }

        // Fade in text
        yield return FadeTextIn(mainText, textFadeInDuration);
        yield return new WaitForSecondsRealtime(0.5f);
        yield return FadeTextIn(subText, textFadeInDuration * 0.7f);

        // Chờ input
        while (true)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                yield break;
            }
            if (Input.GetKeyDown(KeyCode.M))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(mainMenuSceneName);
                yield break;
            }
            yield return null;
        }
    }

    private IEnumerator FadeTextIn(TextMeshProUGUI txt, float duration)
    {
        if (txt == null) yield break;
        float elapsed = 0f;
        Color c = txt.color;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Clamp01(elapsed / duration);
            txt.color = c;
            yield return null;
        }
        c.a = 1f;
        txt.color = c;
    }

    private void PlayAudio(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.Play();
    }

    // ─────────────────────────────────────────────────────────
    //  Tự xây dựng UI bằng code
    // ─────────────────────────────────────────────────────────

    private void BuildUI()
    {
        GameObject canvasObj = new GameObject("_EndingCanvas");
        DontDestroyOnLoad(canvasObj);

        endingCanvas = canvasObj.AddComponent<Canvas>();
        endingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        endingCanvas.sortingOrder = 9998;

        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();

        CanvasGroup cg = canvasObj.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        // Nền
        GameObject bgObj = new GameObject("BG");
        bgObj.transform.SetParent(canvasObj.transform, false);
        backgroundImage = bgObj.AddComponent<UnityEngine.UI.Image>();
        backgroundImage.color = new Color(0.02f, 0f, 0f, 1f);
        Stretch(bgObj);

        // Text chính
        GameObject mainTextObj = new GameObject("MainText");
        mainTextObj.transform.SetParent(canvasObj.transform, false);
        mainText = mainTextObj.AddComponent<TextMeshProUGUI>();
        mainText.fontSize = 52;
        mainText.fontStyle = FontStyles.Bold;
        mainText.alignment = TextAlignmentOptions.Center;
        mainText.color = new Color(0.8f, 0f, 0f, 0f);
        RectTransform mrt = mainTextObj.GetComponent<RectTransform>();
        mrt.anchorMin = new Vector2(0f, 0.55f);
        mrt.anchorMax = new Vector2(1f, 0.75f);
        mrt.offsetMin = mrt.offsetMax = Vector2.zero;

        // Sub text
        GameObject subTextObj = new GameObject("SubText");
        subTextObj.transform.SetParent(canvasObj.transform, false);
        subText = subTextObj.AddComponent<TextMeshProUGUI>();
        subText.fontSize = 22;
        subText.alignment = TextAlignmentOptions.Center;
        subText.color = new Color(0.6f, 0.6f, 0.6f, 0f);
        RectTransform srt = subTextObj.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.1f, 0.3f);
        srt.anchorMax = new Vector2(0.9f, 0.52f);
        srt.offsetMin = srt.offsetMax = Vector2.zero;

        // Hint nhỏ phía dưới
        GameObject hintObj = new GameObject("HintText");
        hintObj.transform.SetParent(canvasObj.transform, false);
        var hintText = hintObj.AddComponent<TextMeshProUGUI>();
        hintText.text = "[R] Thử lại    [M] Menu";
        hintText.fontSize = 16;
        hintText.alignment = TextAlignmentOptions.Center;
        hintText.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        RectTransform hrt = hintObj.GetComponent<RectTransform>();
        hrt.anchorMin = new Vector2(0.2f, 0.08f);
        hrt.anchorMax = new Vector2(0.8f, 0.18f);
        hrt.offsetMin = hrt.offsetMax = Vector2.zero;

        // AudioSource
        audioSource = canvasObj.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 0.85f;

        endingCanvas.gameObject.SetActive(false);
    }

    private void Stretch(GameObject obj)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt == null) rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
