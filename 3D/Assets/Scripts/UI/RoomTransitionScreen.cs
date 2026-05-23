using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Màn hình chuyển cảnh giữa các phòng.
/// Hiển thị "ROOM" cố định + số phòng cuộn lên như đồng hồ cơ học.
/// Tự tạo toàn bộ UI — chỉ cần gắn script vào bất kỳ GameObject nào.
/// </summary>
public class RoomTransitionScreen : MonoBehaviour
{
    [Header("Thời lượng (giây)")]
    public float fadeInDuration  = 0.3f;
    public float rollDuration    = 1.6f;   // tăng để chậm hơn, tạo kịch tính
    public float holdDuration    = 1.2f;
    public float fadeOutDuration = 0.6f;

    [Header("Easing — chỉnh đường cong cuộn số")]
    [Tooltip("Trục X = thời gian (0→1). Trục Y = vị trí (0→1).\n" +
             "Đường cong dốc đứng = lăn nhanh. Nằm ngang = lăn chậm/dừng.\n" +
             "Mặc định: bắt đầu chậm → nhanh → phanh mạnh ở cuối (horror).")]
    public AnimationCurve rollCurve = new AnimationCurve(
        new Keyframe(0f,   0f,   0f,  2.5f),   // bắt đầu chậm
        new Keyframe(0.35f, 0.75f, 3f,  1.5f),  // tăng tốc giữa
        new Keyframe(0.72f, 0.97f, 0.4f, 0f),   // gần đến đích — phanh mạnh
        new Keyframe(1f,   1f,   0f,  0f)        // dừng hẳn
    );

    [Header("Âm thanh")]
    [Tooltip("Tiếng cộc cơ học khi số bắt đầu lăn")]
    public AudioClip rollSound;
    [Tooltip("Tiếng thình nặng khi số dừng lại")]
    public AudioClip landSound;
    [Range(0f, 1f)] public float rollVolume = 0.75f;
    [Range(0f, 1f)] public float landVolume = 1f;

    [Header("Ngắt audio thừa")]
    [Tooltip("Tắt toàn bộ âm thanh game khi màn chuyển cảnh hiện ra.\n" +
             "Audio của transition vẫn phát trước và sau khoảng im lặng.")]
    public bool muteWorldAudio = true;
    [Tooltip("Thời gian fade âm lượng thế giới về 0 (giây)")]
    public float audioFadeOutDuration = 0.25f;
    [Tooltip("Thời gian khôi phục âm lượng thế giới (giây)")]
    public float audioFadeInDuration  = 0.4f;

    [Header("Font (tuỳ chọn)")]
    public TMP_FontAsset displayFont;

    // ── Constants ──────────────────────────────────────────────
    private const float DIGIT_H   = 90f;   // chiều cao 1 ô số
    private const float DIGIT_W   = 100f;  // chiều rộng ô số
    private const float LABEL_W   = 200f;  // chiều rộng chữ "ROOM"
    private const float GAP       = 16f;   // khoảng cách label ↔ counter

    // ── UI references ──────────────────────────────────────────
    private CanvasGroup   canvasGroup;
    private RectTransform digitStrip;
    private AudioSource   audioSource;

    // ── State ──────────────────────────────────────────────────
    private Coroutine activeRoutine;
    private float     savedListenerVolume = 1f;

    // ══════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ══════════════════════════════════════════════════════════

    private void Awake()
    {
        BuildUI();
    }

    private void OnEnable()
    {
        RoomManager.OnRoomEntered += HandleRoomEntered;
    }

    private void OnDisable()
    {
        RoomManager.OnRoomEntered -= HandleRoomEntered;
    }

    private void HandleRoomEntered(RoomManager.RoomState room)
    {
        // Room 5 load scene riêng — không dùng transition này
        if (room == RoomManager.RoomState.Room5) return;

        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(PlayTransition((int)room));
    }

    // ══════════════════════════════════════════════════════════
    //  TRANSITION COROUTINE
    // ══════════════════════════════════════════════════════════

    private IEnumerator PlayTransition(int roomIndex)
    {
        float targetStripY = roomIndex * DIGIT_H;
        canvasGroup.blocksRaycasts = true;
        savedListenerVolume = AudioListener.volume;

        // 1. Tiếng cộc phát trước khi thế giới bị tắt tiếng
        PlaySound(rollSound, rollVolume);

        // 2. Fade in canvas + fade out world audio đồng thời
        yield return FadeInWithAudioMute(fadeInDuration);

        // 3. Cuộn số trong im lặng
        float startY = digitStrip.anchoredPosition.y;
        yield return RollStrip(startY, targetStripY, rollDuration);

        // 4. Khôi phục âm lượng → tiếng dừng vang lên
        if (muteWorldAudio)
            yield return FadeListenerVolume(0f, savedListenerVolume, audioFadeInDuration);

        PlaySound(landSound, landVolume);

        // 5. Giữ nguyên
        yield return new WaitForSecondsRealtime(holdDuration);

        // 6. Fade out canvas
        yield return FadeAlpha(1f, 0f, fadeOutDuration);
        canvasGroup.blocksRaycasts = false;
    }

    private IEnumerator FadeInWithAudioMute(float duration)
    {
        float t = 0f;
        float startVol = AudioListener.volume;
        canvasGroup.alpha = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(t / duration);
            canvasGroup.alpha = progress;
            if (muteWorldAudio)
                AudioListener.volume = Mathf.Lerp(startVol, 0f, progress);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        if (muteWorldAudio) AudioListener.volume = 0f;
    }

    private IEnumerator FadeListenerVolume(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            AudioListener.volume = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        AudioListener.volume = to;
    }

    private IEnumerator FadeAlpha(float from, float to, float duration)
    {
        float t = 0f;
        canvasGroup.alpha = from;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    private IEnumerator RollStrip(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(t / duration);
            float ease = rollCurve.Evaluate(normalized);
            digitStrip.anchoredPosition = new Vector2(0f, Mathf.Lerp(from, to, ease));
            yield return null;
        }
        digitStrip.anchoredPosition = new Vector2(0f, to);
    }

    private void PlaySound(AudioClip clip, float volume)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip, volume);
    }

    // ══════════════════════════════════════════════════════════
    //  UI BUILDER
    // ══════════════════════════════════════════════════════════

    private void BuildUI()
    {
        // ── Root Canvas ────────────────────────────────────────
        var root = new GameObject("_RoomTransitionCanvas");
        DontDestroyOnLoad(root);

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 700;
        root.AddComponent<CanvasScaler>();

        canvasGroup = root.AddComponent<CanvasGroup>();
        canvasGroup.alpha           = 0f;
        canvasGroup.blocksRaycasts  = false;
        canvasGroup.interactable    = false;

        audioSource = root.AddComponent<AudioSource>();
        audioSource.playOnAwake  = false;
        audioSource.spatialBlend = 0f;

        // ── Nền tối toàn màn hình ─────────────────────────────
        var bg = NewImage(root.transform, "BG", new Color(0.04f, 0.04f, 0.04f, 1f));
        Stretch(bg.rectTransform);

        // ── Khung trung tâm ───────────────────────────────────
        float totalW = LABEL_W + GAP + DIGIT_W;
        var center = NewRect(root.transform, "Center");
        center.anchorMin = center.anchorMax = new Vector2(0.5f, 0.5f);
        center.sizeDelta = new Vector2(totalW, DIGIT_H);
        center.anchoredPosition = Vector2.zero;

        // ── Chữ "ROOM" ────────────────────────────────────────
        var lbl = NewTMP(center, "LabelROOM", "ROOM");
        lbl.rectTransform.anchorMin = new Vector2(0f, 0f);
        lbl.rectTransform.anchorMax = new Vector2(0f, 1f);
        lbl.rectTransform.sizeDelta      = new Vector2(LABEL_W, 0f);
        lbl.rectTransform.anchoredPosition = new Vector2(LABEL_W * 0.5f, 0f);
        lbl.fontSize   = 58f;
        lbl.fontStyle  = FontStyles.Bold;
        lbl.color      = new Color(0.87f, 0.87f, 0.87f);
        lbl.alignment  = TextAlignmentOptions.MidlineRight;

        // ── Hộp đếm số (nền + mask) ───────────────────────────
        var box = NewRect(center, "CounterBox");
        box.anchorMin = new Vector2(1f, 0f);
        box.anchorMax = new Vector2(1f, 1f);
        box.sizeDelta       = new Vector2(DIGIT_W, 0f);
        box.anchoredPosition = new Vector2(-(DIGIT_W * 0.5f), 0f);

        var boxBg = box.gameObject.AddComponent<Image>();
        boxBg.color = new Color(0.08f, 0.08f, 0.09f, 1f);
        box.gameObject.AddComponent<RectMask2D>();

        // Đường kẻ viền trên / dưới
        AddEdgeLine(box, true);
        AddEdgeLine(box, false);

        // Đường kẻ ngang giữa (bóng cơ học)
        AddShadowLine(box);

        // ── Dải số (cuộn dọc) ─────────────────────────────────
        var strip = NewRect(box, "DigitStrip");
        strip.anchorMin = new Vector2(0f, 0.5f);
        strip.anchorMax = new Vector2(1f, 0.5f);
        strip.sizeDelta       = new Vector2(0f, DIGIT_H * 9f);
        strip.anchoredPosition = new Vector2(0f, -DIGIT_H); // bắt đầu ở blank slot
        digitStrip = strip;

        // Blank slot phía trên số 0 (hiển thị trước lần đầu)
        //   index = -1  →  strip local Y = +DIGIT_H
        AddDigitCell(strip, -1, "");

        // Số 0-5
        for (int i = 0; i <= 5; i++)
            AddDigitCell(strip, i, i.ToString());
    }

    // ── Digit cell ─────────────────────────────────────────────
    // index = -1 → Y = +DIGIT_H  (blank)
    // index =  0 → Y = 0
    // index =  N → Y = -N * DIGIT_H
    // strip.anchoredPosition.y để hiện số N = N * DIGIT_H
    private void AddDigitCell(RectTransform parent, int index, string text)
    {
        var rt = NewRect(parent, $"D{index}");
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.sizeDelta       = new Vector2(0f, DIGIT_H);
        rt.anchoredPosition = new Vector2(0f, -index * DIGIT_H);

        var tmp = NewTMP(rt, "T", text);
        Stretch(tmp.rectTransform);
        tmp.fontSize  = 70f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    // ── Đường viền trên/dưới hộp số ───────────────────────────
    private void AddEdgeLine(RectTransform parent, bool isTop)
    {
        var rt = NewRect(parent, isTop ? "EdgeTop" : "EdgeBot");
        rt.anchorMin = new Vector2(0f, isTop ? 1f : 0f);
        rt.anchorMax = new Vector2(1f, isTop ? 1f : 0f);
        rt.sizeDelta       = new Vector2(0f, 2f);
        rt.anchoredPosition = Vector2.zero;
        var img = rt.gameObject.AddComponent<Image>();
        img.color = new Color(0.42f, 0.42f, 0.42f);
    }

    // Đường bóng ngang giữa hộp — mô phỏng bề mặt cơ học
    private void AddShadowLine(RectTransform parent)
    {
        var rt = NewRect(parent, "ShadowLine");
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.sizeDelta       = new Vector2(0f, 1f);
        rt.anchoredPosition = Vector2.zero;
        var img = rt.gameObject.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.6f);
    }

    // ══════════════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════════════

    private RectTransform NewRect(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    private Image NewImage(Transform parent, string name, Color color)
    {
        var rt  = NewRect(parent, name);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private TextMeshProUGUI NewTMP(Transform parent, string name, string text)
    {
        var rt  = NewRect(parent, name);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        if (displayFont != null) tmp.font = displayFont;
        return tmp;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.offsetMin  = rt.offsetMax = Vector2.zero;
    }
}
