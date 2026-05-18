using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameEndUIController : MonoBehaviour
{
    public static GameEndUIController Instance { get; private set; }

    [Header("UI Settings")]
    [Tooltip("Kéo font chữ (nếu có) vào đây, nếu để trống sẽ dùng font mặc định")]
    public Font customFont;

    private Canvas endCanvas;
    private Text endText;
    
    private float completionTime = 0f;
    private int currentStep = 0; // 0: Đang ẩn, 1: Cảm ơn, 2: Thời gian

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void CreateUI()
    {
        // 1. Tạo Canvas
        GameObject canvasObj = new GameObject("_GameEndCanvas_Auto");
        canvasObj.transform.SetParent(transform); // Cùng DontDestroyOnLoad với manager
        
        endCanvas = canvasObj.AddComponent<Canvas>();
        endCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        endCanvas.sortingOrder = 9999; // Hiển thị trên cùng

        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>(); // Để bắt sự kiện click

        // 2. Tạo Background đen
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image background = bgObj.AddComponent<Image>();
        background.color = new Color(0, 0, 0, 1f); // Nền đen tuyền
        
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        // (Đã xóa Button vì sẽ dùng hàm Update để bắt sự kiện thay thế cho an toàn, không phụ thuộc EventSystem)

        // 3. Tạo Text hiển thị nội dung
        GameObject textObj = new GameObject("MessageText");
        textObj.transform.SetParent(canvasObj.transform, false);
        endText = textObj.AddComponent<Text>();
        endText.alignment = TextAnchor.MiddleCenter;
        endText.color = Color.white;
        endText.fontSize = 60;
        
        if (customFont != null) 
            endText.font = customFont;
        else 
            endText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(50, 50);
        textRt.offsetMax = new Vector2(-50, -50);

        // Mặc định ẩn
        canvasObj.SetActive(false);
    }

    /// <summary>
    /// Gọi hàm này khi người chơi chiến thắng
    /// </summary>
    public void ShowEndScreen(float timeInSeconds)
    {
        completionTime = timeInSeconds;
        currentStep = 1;
        
        endText.text = "CẢM ƠN BẠN ĐÃ CHƠI THỬ!";
        endCanvas.gameObject.SetActive(true);
        
        // Tạm dừng thời gian game để mọi thứ đứng yên
        Time.timeScale = 0f;
        
        // Bắt buộc hiện và mở khóa chuột
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        // Nếu màn hình đang hiện (currentStep > 0) và người chơi nhấn chuột trái
        if (currentStep > 0 && Input.GetMouseButtonDown(0))
        {
            OnScreenClicked();
        }
    }

    private void OnScreenClicked()
    {
        if (currentStep == 1)
        {
            // Bước 2: Hiển thị thời gian
            currentStep = 2;
            int minutes = Mathf.FloorToInt(completionTime / 60F);
            int seconds = Mathf.FloorToInt(completionTime - minutes * 60);
            
            endText.text = $"THỜI GIAN HOÀN THÀNH\n<size=80><b>{minutes} phút {seconds} giây</b></size>\n\n<size=40><i>(Nhấn vào màn hình để quay lại Menu)</i></size>";
        }
        else if (currentStep == 2)
        {
            // Bước 3: Đóng màn hình, khôi phục game, quay về Menu
            currentStep = 0;
            endCanvas.gameObject.SetActive(false);
            
            Time.timeScale = 1f; // Khôi phục thời gian
            
            // Xóa rác và load lại Scene 0 (Menu chính)
            SceneManager.LoadScene(0); 
        }
    }
}