using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Hiển thị hội thoại nội tâm của nhân vật khi bước vào vùng trigger.
/// Gắn script này lên một Empty GameObject có BoxCollider (Is Trigger = true).
/// Có thể đặt nhiều instance trong scene cho từng khu vực khác nhau.
/// </summary>
public class InnerMonologue : MonoBehaviour
{
    [System.Serializable]
    public class MonologueLine
    {
        [TextArea(2, 4)]
        public string text;
        [Tooltip("Thời gian hiển thị (giây) trước khi tự chuyển dòng tiếp. 0 = chờ player nhấn bất kỳ phím")]
        public float autoAdvanceDelay = 0f;
    }

    [Header("Nội dung hội thoại")]
    public List<MonologueLine> lines = new List<MonologueLine>();

    [Header("Hiệu ứng chữ")]
    [Tooltip("Tốc độ gõ chữ (ký tự/giây)")]
    public float typewriterSpeed = 40f;
    [Tooltip("Thời gian text mờ dần trước khi dòng tiếp xuất hiện (giây)")]
    public float fadeDuration = 0.4f;
    [Tooltip("Cỡ chữ hiển thị (font size)")]
    public float fontSize = 32f;

    [Header("Chỉ chạy 1 lần")]
    public bool triggerOnce = true;

    [Header("Audio")]
    [Tooltip("Tiếng lật trang / tiếng gõ nhẹ theo từng ký tự (tuỳ chọn)")]
    public AudioClip typingSound;
    [Range(0f, 1f)] public float typingVolume = 0.3f;

    // ── UI — tự tạo bằng code ──
    private Canvas monoCanvas;
    private TextMeshProUGUI monoText;
    private AudioSource audioSource;

    private bool hasTriggered = false;
    private bool isRunning = false;
    public bool IsRunning => isRunning;
    private Coroutine activeRoutine;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        BuildUI();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (triggerOnce && hasTriggered) return;
        if (isRunning) return;

        hasTriggered = true;
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(PlayMonologue());
    }

    /// <summary>
    /// Gọi thủ công từ script khác (ví dụ Room0Starter) để kích hoạt monologue
    /// mà không cần player bước vào trigger — dùng khi player spawn ngay tại vị trí.
    /// </summary>
    public void PlayManually()
    {
        if (triggerOnce && hasTriggered) return;
        if (isRunning) return;

        hasTriggered = true;
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(PlayMonologue());
    }

    // ─────────────────────────────────────────────────────────
    //  Coroutine chính
    // ─────────────────────────────────────────────────────────

    private IEnumerator PlayMonologue()
    {
        isRunning = true;
        monoCanvas.gameObject.SetActive(true);

        foreach (var line in lines)
        {
            // Reset
            monoText.text = "";
            monoText.alpha = 1f;

            // Typewriter
            yield return TypewriterRoutine(line.text);

            // Chờ: tự động hoặc chờ input
            if (line.autoAdvanceDelay > 0f)
            {
                yield return new WaitForSeconds(line.autoAdvanceDelay);
            }
            else
            {
                // Hiện dấu nhấp nháy nhỏ ở cuối
                monoText.text = line.text + " <alpha=#88>▌";
                yield return WaitForAnyKey();
                monoText.text = line.text;
            }

            // Fade out dòng hiện tại
            yield return FadeTextOut(fadeDuration);
        }

        monoCanvas.gameObject.SetActive(false);
        isRunning = false;
    }

    private IEnumerator TypewriterRoutine(string fullText)
    {
        float interval = 1f / typewriterSpeed;
        string visible = "";
        foreach (char c in fullText)
        {
            visible += c;
            monoText.text = visible;

            // Phát tiếng gõ mỗi vài ký tự (không phải khoảng trắng)
            if (c != ' ' && c != '\n' && typingSound != null && audioSource != null)
                audioSource.PlayOneShot(typingSound, typingVolume);

            yield return new WaitForSeconds(interval);
        }
    }

    private IEnumerator WaitForAnyKey()
    {
        // Bỏ qua frame hiện tại để tránh nhận input thừa
        yield return null;
        yield return null;
        while (!Input.anyKeyDown) yield return null;
    }

    private IEnumerator FadeTextOut(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            monoText.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }
        monoText.alpha = 0f;
        monoText.text = "";
    }

    // ─────────────────────────────────────────────────────────
    //  Tự xây dựng UI
    // ─────────────────────────────────────────────────────────

    private void BuildUI()
    {
        GameObject canvasObj = new GameObject($"_MonologueCanvas_{gameObject.name}");
        canvasObj.transform.SetParent(transform);

        monoCanvas = canvasObj.AddComponent<Canvas>();
        monoCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        monoCanvas.sortingOrder = 100;

        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();

        CanvasGroup cg = canvasObj.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        // Panel nền mờ phía dưới màn hình
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(canvasObj.transform, false);
        var panelImg = panel.AddComponent<UnityEngine.UI.Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.55f);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0.28f);
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;

        // Text hội thoại
        GameObject textObj = new GameObject("MonologueText");
        textObj.transform.SetParent(panel.transform, false);
        monoText = textObj.AddComponent<TextMeshProUGUI>();
        monoText.fontSize = fontSize;
        monoText.fontStyle = FontStyles.Italic;
        monoText.color = new Color(0.92f, 0.88f, 0.82f, 1f);
        monoText.alignment = TextAlignmentOptions.BottomLeft;
        monoText.margin = new Vector4(40f, 15f, 40f, 15f);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;

        // AudioSource
        audioSource = canvasObj.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        monoCanvas.gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.25f);
        Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.8f);
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
}
