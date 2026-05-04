using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script tự tạo Canvas và Image Jumpscare bằng code.
/// Không cần setup Canvas trong Unity - chỉ cần kéo AudioClip vào là dùng được!
/// Gắn script này vào bất kỳ GameObject nào trong Scene.
/// </summary>
public class JumpscareController : MonoBehaviour
{
    [Header("Hình ảnh Jumpscare")]
    [Tooltip("Kéo hình ảnh (Sprite) của con ma vào đây")]
    public Sprite jumpscareSprite;

    [Header("Âm thanh")]
    [Tooltip("Kéo file âm thanh tiếng hét vào đây")]
    public AudioClip jumpscareSound;
    [Range(0f, 1f)]
    public float soundVolume = 1f;

    [Header("Thời gian")]
    [Tooltip("Hình ảnh tự ẩn sau bao nhiêu giây")]
    public float displayDuration = 2f;

    // ---- Tự tạo bằng code ----
    private Canvas jumpscareCanvas;
    private Image jumpscareImage;
    private AudioSource audioSource;
    private bool isReady = false;

    private void Awake()
    {
        CreateJumpscareUI();
    }

    private void CreateJumpscareUI()
    {
        // Tạo Canvas mới hoàn toàn bằng code
        GameObject canvasObj = new GameObject("_JumpscareCanvas_Auto");
        DontDestroyOnLoad(canvasObj); // Giữ xuyên suốt các Scene
        
        jumpscareCanvas = canvasObj.AddComponent<Canvas>();
        jumpscareCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        jumpscareCanvas.sortingOrder = 9999; // Luôn nằm trên cùng

        canvasObj.AddComponent<CanvasScaler>();
        // KHÔNG thêm GraphicRaycaster — Canvas này chỉ hiển thị, không cần nhận click

        // Thêm CanvasGroup để đảm bảo KHÔNG chặn click chuột của các Canvas khác
        CanvasGroup cg = canvasObj.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        // Tạo Image con ma bên trong Canvas
        GameObject imageObj = new GameObject("JumpscareImage");
        imageObj.transform.SetParent(canvasObj.transform, false);

        jumpscareImage = imageObj.AddComponent<Image>();
        jumpscareImage.raycastTarget = false; // Image cũng không chặn click
        
        // Kéo full màn hình
        RectTransform rt = imageObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        if (jumpscareSprite != null)
            jumpscareImage.sprite = jumpscareSprite;
        else
            jumpscareImage.color = new Color(0, 0, 0, 0.9f); // Màn đen nếu không có hình

        // Tạo AudioSource
        audioSource = canvasObj.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        // Ẩn đi từ đầu
        canvasObj.SetActive(false);

        isReady = true;
        Debug.Log("[JumpscareController] Đã tạo Canvas Jumpscare tự động thành công!");
    }

    /// <summary>
    /// Gọi hàm này để kích hoạt Jumpscare (dùng từ AnomalyObject Custom Event hoặc code khác)
    /// </summary>
    public void TriggerJumpscare()
    {
        if (!isReady) return;
        StartCoroutine(ShowJumpscareRoutine());
    }

    private IEnumerator ShowJumpscareRoutine()
    {
        // Hiện màn hình
        if (jumpscareCanvas != null)
            jumpscareCanvas.gameObject.SetActive(true);

        // Phát âm thanh
        if (audioSource != null && jumpscareSound != null)
            audioSource.PlayOneShot(jumpscareSound, soundVolume);

        Debug.Log("[JumpscareController] Jumpscare HIỆN! Chờ " + displayDuration + " giây rồi tắt.");

        yield return new WaitForSeconds(displayDuration);

        // Ẩn màn hình
        if (jumpscareCanvas != null)
            jumpscareCanvas.gameObject.SetActive(false);

        Debug.Log("[JumpscareController] Jumpscare ĐÃ ẨN.");
    }

    /// <summary>
    /// Ẩn Jumpscare ngay lập tức (dùng khi reset màn chơi)
    /// </summary>
    public void HideJumpscare()
    {
        if (jumpscareCanvas != null)
            jumpscareCanvas.gameObject.SetActive(false);
    }
}
