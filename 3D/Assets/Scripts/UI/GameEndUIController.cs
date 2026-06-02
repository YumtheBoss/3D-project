using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameEndUIController : MonoBehaviour
{
    public static GameEndUIController Instance { get; private set; }

    [Header("UI Settings")]
    [Tooltip("Kéo font chữ tùy chỉnh vào đây, nếu để trống sẽ dùng font hệ thống mặc định")]
    public Font customFont;

    private Canvas endCanvas;
    private InputField nameInputField;
    private float completionTime = 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateScoreBoardUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void CreateScoreBoardUI()
    {
        Font font = customFont != null 
            ? customFont 
            : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 1. Tạo Canvas gốc
        GameObject canvasObj = new GameObject("_GameEndCanvas_Auto");
        canvasObj.transform.SetParent(transform, false);
        
        endCanvas = canvasObj.AddComponent<Canvas>();
        endCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        endCanvas.sortingOrder = 9999; // Hiển thị trên tất cả các UI khác

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // 2. Nền tối mờ toàn màn hình (Background)
        GameObject bgObj = new GameObject("BackgroundOverlay");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image background = bgObj.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.85f);
        
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        // 3. Khung điểm số cổ kính ở giữa (Score Card Panel)
        GameObject cardObj = new GameObject("ScoreCard");
        cardObj.transform.SetParent(canvasObj.transform, false);
        Image cardImg = cardObj.AddComponent<Image>();
        cardImg.color = new Color(0.22f, 0.16f, 0.11f, 0.95f); // Tông màu da/nâu cổ kính ấm áp
        
        RectTransform cardRt = cardObj.GetComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.sizeDelta = new Vector2(650, 750);
        cardRt.anchoredPosition = Vector2.zero;

        // Viền vàng sang trọng cho card
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(cardObj.transform, false);
        Image borderImg = borderObj.AddComponent<Image>();
        borderImg.color = new Color(0.9f, 0.72f, 0.23f, 1f); // Màu vàng gold sang chảnh
        RectTransform borderRt = borderObj.GetComponent<RectTransform>();
        borderRt.anchorMin = Vector2.zero;
        borderRt.anchorMax = Vector2.one;
        borderRt.sizeDelta = new Vector2(-16, -16); // Thụt vào một chút để tạo viền
        
        // Thêm mặt nạ để đè màu nền da
        GameObject innerCardObj = new GameObject("InnerCard");
        innerCardObj.transform.SetParent(borderObj.transform, false);
        Image innerCardImg = innerCardObj.AddComponent<Image>();
        innerCardImg.color = new Color(0.18f, 0.13f, 0.09f, 1f); // Tối hơn viền
        RectTransform innerCardRt = innerCardObj.GetComponent<RectTransform>();
        innerCardRt.anchorMin = Vector2.zero;
        innerCardRt.anchorMax = Vector2.one;
        innerCardRt.sizeDelta = new Vector2(-8, -8);

        // 4. TIÊU ĐỀ: SCORE (Vàng gold to đậm)
        GameObject titleObj = new GameObject("ScoreTitle");
        titleObj.transform.SetParent(innerCardObj.transform, false);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "＝ SCORE ＝";
        titleText.font = font;
        titleText.fontSize = 55;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(0.95f, 0.75f, 0.2f, 1f);
        
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 0.85f);
        titleRt.anchorMax = new Vector2(1f, 0.95f);
        titleRt.offsetMin = titleRt.offsetMax = Vector2.zero;

        // 5. HIỂN THỊ THỜI GIAN TO TRÊN CÙNG
        GameObject timeObj = new GameObject("TimeText");
        timeObj.transform.SetParent(innerCardObj.transform, false);
        Text timeText = timeObj.AddComponent<Text>();
        timeText.text = "00:00";
        timeText.font = font;
        timeText.fontSize = 90; // Cực kỳ to và prominent
        timeText.fontStyle = FontStyle.Bold;
        timeText.alignment = TextAnchor.MiddleCenter;
        timeText.color = new Color(0.95f, 0.85f, 0.1f, 1f); // Màu vàng tươi
        
        RectTransform timeRt = timeObj.GetComponent<RectTransform>();
        timeRt.anchorMin = new Vector2(0f, 0.62f);
        timeRt.anchorMax = new Vector2(1f, 0.82f);
        timeRt.offsetMin = timeRt.offsetMax = Vector2.zero;

        // Nhãn nhỏ: Your Best Score!
        GameObject labelObj = new GameObject("TimeLabel");
        labelObj.transform.SetParent(innerCardObj.transform, false);
        Text labelText = labelObj.AddComponent<Text>();
        labelText.text = "Your Completion Time!!!";
        labelText.font = font;
        labelText.fontSize = 24;
        labelText.fontStyle = FontStyle.Italic;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = new Color(0.7f, 0.6f, 0.5f, 1f);
        
        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 0.55f);
        labelRt.anchorMax = new Vector2(1f, 0.62f);
        labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;

        // 6. THỐNG KÊ CHI TIẾT (BẢNG STATS)
        GameObject statsObj = new GameObject("StatsTable");
        statsObj.transform.SetParent(innerCardObj.transform, false);
        RectTransform statsRt = statsObj.AddComponent<RectTransform>();
        statsRt.anchorMin = new Vector2(0.1f, 0.35f);
        statsRt.anchorMax = new Vector2(0.9f, 0.52f);
        statsRt.offsetMin = statsRt.offsetMax = Vector2.zero;

        VerticalLayoutGroup vlg = statsObj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 15;
        vlg.childForceExpandHeight = vlg.childForceExpandWidth = false;
        vlg.childControlHeight = vlg.childControlWidth = true;

        CreateStatRow(statsObj.transform, "Loop Level:", "Room 5 (ESCAPED)", font);
        CreateStatRow(statsObj.transform, "Player Status:", "VICTORIOUS", font, new Color(0.2f, 0.85f, 0.2f));

        // 7. Ô NHẬP TÊN (InputField) CỰC XỊN
        GameObject inputCont = new GameObject("InputField_Container");
        inputCont.transform.SetParent(innerCardObj.transform, false);
        RectTransform inputContRt = inputCont.AddComponent<RectTransform>();
        inputContRt.anchorMin = new Vector2(0.15f, 0.2f);
        inputContRt.anchorMax = new Vector2(0.85f, 0.3f);
        inputContRt.offsetMin = inputContRt.offsetMax = Vector2.zero;

        Image inputBg = inputCont.AddComponent<Image>();
        inputBg.color = new Color(0.08f, 0.06f, 0.04f, 1f); // Nền tối đen sẫm
        
        // Bo viền đen mỏng cho InputField
        GameObject inputBorder = new GameObject("InputBorder");
        inputBorder.transform.SetParent(inputCont.transform, false);
        Image inputBorderImg = inputBorder.AddComponent<Image>();
        inputBorderImg.color = new Color(0.4f, 0.3f, 0.2f, 1f);
        RectTransform inputBorderRt = inputBorder.GetComponent<RectTransform>();
        inputBorderRt.anchorMin = Vector2.zero;
        inputBorderRt.anchorMax = Vector2.one;
        inputBorderRt.sizeDelta = new Vector2(-4, -4);

        GameObject inputInner = new GameObject("InputInner");
        inputInner.transform.SetParent(inputBorder.transform, false);
        Image inputInnerImg = inputInner.AddComponent<Image>();
        inputInnerImg.color = new Color(0.08f, 0.06f, 0.04f, 1f);
        RectTransform inputInnerRt = inputInner.GetComponent<RectTransform>();
        inputInnerRt.anchorMin = Vector2.zero;
        inputInnerRt.anchorMax = Vector2.one;
        inputInnerRt.sizeDelta = new Vector2(-4, -4);

        // Tạo Text con để hiện chữ gõ
        GameObject inputTextObj = new GameObject("Text");
        inputTextObj.transform.SetParent(inputInner.transform, false);
        Text inpText = inputTextObj.AddComponent<Text>();
        inpText.font = font;
        inpText.fontSize = 24;
        inpText.color = Color.white;
        inpText.alignment = TextAnchor.MiddleLeft;
        RectTransform inpTextRt = inputTextObj.GetComponent<RectTransform>();
        inpTextRt.anchorMin = new Vector2(0.05f, 0f);
        inpTextRt.anchorMax = new Vector2(0.95f, 1f);
        inpTextRt.offsetMin = inpTextRt.offsetMax = Vector2.zero;

        // Tạo Text Placeholder
        GameObject placeholderTextObj = new GameObject("Placeholder");
        placeholderTextObj.transform.SetParent(inputInner.transform, false);
        Text placeText = placeholderTextObj.AddComponent<Text>();
        placeText.text = "ENTER NICKNAME...";
        placeText.font = font;
        placeText.fontSize = 24;
        placeText.fontStyle = FontStyle.Italic;
        placeText.color = new Color(0.5f, 0.4f, 0.3f, 0.8f);
        placeText.alignment = TextAnchor.MiddleLeft;
        RectTransform placeTextRt = placeholderTextObj.GetComponent<RectTransform>();
        placeTextRt.anchorMin = new Vector2(0.05f, 0f);
        placeTextRt.anchorMax = new Vector2(0.95f, 1f);
        placeTextRt.offsetMin = placeTextRt.offsetMax = Vector2.zero;

        // Gắn component InputField chính
        nameInputField = inputCont.AddComponent<InputField>();
        nameInputField.textComponent = inpText;
        nameInputField.placeholder = placeText;
        nameInputField.characterLimit = 12; // Giới hạn 12 ký tự để bảng xếp hạng không bị tràn chữ

        // 8. NÚT SUBMIT & EXIT (Màu đỏ máu nổi bật)
        GameObject submitBtnObj = new GameObject("SubmitButton");
        submitBtnObj.transform.SetParent(innerCardObj.transform, false);
        RectTransform submitRt = submitBtnObj.AddComponent<RectTransform>();
        submitRt.anchorMin = new Vector2(0.25f, 0.06f);
        submitRt.anchorMax = new Vector2(0.75f, 0.15f);
        submitRt.offsetMin = submitRt.offsetMax = Vector2.zero;

        Image submitImg = submitBtnObj.AddComponent<Image>();
        submitImg.color = new Color(0.55f, 0.1f, 0.1f, 1f); // Đỏ thẫm máu rực rỡ
        
        Button submitBtn = submitBtnObj.AddComponent<Button>();
        submitBtn.onClick.AddListener(OnSubmitClicked);

        // Hiệu ứng đổi màu đỏ tươi khi hover qua nút bấm
        ColorBlock cb = submitBtn.colors;
        cb.highlightedColor = new Color(0.75f, 0.1f, 0.1f, 1f);
        cb.pressedColor = new Color(0.4f, 0.05f, 0.05f, 1f);
        submitBtn.colors = cb;

        GameObject submitTextObj = new GameObject("Text");
        submitTextObj.transform.SetParent(submitBtnObj.transform, false);
        Text submitText = submitTextObj.AddComponent<Text>();
        submitText.text = "SUBMIT & EXIT";
        submitText.font = font;
        submitText.fontSize = 28;
        submitText.fontStyle = FontStyle.Bold;
        submitText.color = Color.white;
        submitText.alignment = TextAnchor.MiddleCenter;
        RectTransform subTextRt = submitTextObj.GetComponent<RectTransform>();
        subTextRt.anchorMin = Vector2.zero;
        subTextRt.anchorMax = Vector2.one;
        subTextRt.offsetMin = subTextRt.offsetMax = Vector2.zero;

        // Mặc định ẩn UI đi
        canvasObj.SetActive(false);
    }

    private void CreateStatRow(Transform parent, string label, string val, Font font, Color? valColor = null)
    {
        GameObject row = new GameObject("StatRow");
        row.transform.SetParent(parent, false);
        RectTransform rowRt = row.AddComponent<RectTransform>();
        rowRt.sizeDelta = new Vector2(0, 30);

        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(row.transform, false);
        Text labelText = labelObj.AddComponent<Text>();
        labelText.text = label;
        labelText.font = font;
        labelText.fontSize = 22;
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.color = new Color(0.75f, 0.65f, 0.55f, 1f);
        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 0f);
        labelRt.anchorMax = new Vector2(0.45f, 1f);
        labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;

        // Value
        GameObject valObj = new GameObject("Value");
        valObj.transform.SetParent(row.transform, false);
        Text valText = valObj.AddComponent<Text>();
        valText.text = val;
        valText.font = font;
        valText.fontSize = 22;
        valText.fontStyle = FontStyle.Bold;
        valText.alignment = TextAnchor.MiddleRight;
        valText.color = valColor ?? new Color(0.95f, 0.75f, 0.2f, 1f);
        RectTransform valRt = valObj.GetComponent<RectTransform>();
        valRt.anchorMin = new Vector2(0.45f, 0f);
        valRt.anchorMax = new Vector2(1f, 1f);
        valRt.offsetMin = valRt.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// Kích hoạt màn hình Bảng Điểm Số và đệm thời gian chơi thực tế
    /// </summary>
    public void ShowEndScreen(float timeInSeconds)
    {
        completionTime = timeInSeconds;

        // Định dạng thời gian thành dạng MM:SS
        int minutes = Mathf.FloorToInt(completionTime / 60F);
        int seconds = Mathf.FloorToInt(completionTime % 60F);
        string formattedTime = string.Format("{0:00}:{1:00}", minutes, seconds);

        // Tìm đối tượng hiển thị thời gian để điền số
        Text timeTextComp = endCanvas.transform.Find("ScoreCard/Border/InnerCard/TimeText").GetComponent<Text>();
        if (timeTextComp != null)
        {
            timeTextComp.text = formattedTime;
        }

        // Bật Canvas hiển thị
        endCanvas.gameObject.SetActive(true);

        // Dừng thời gian chạy trong game
        Time.timeScale = 0f;

        // Bắt buộc mở khóa con trỏ chuột để nhập tên và bấm nút
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnSubmitClicked()
    {
        // 1. Thu thập tên người chơi nhập vào
        string name = nameInputField != null ? nameInputField.text.Trim() : "";
        
        // Nếu người chơi để trống, tự sinh tên mặc định đáng yêu kèm số ngẫu nhiên
        if (string.IsNullOrEmpty(name))
        {
            name = "Runner_" + Random.Range(1000, 9999);
        }

        Debug.Log($"[GameEndUIController] Bắt đầu đẩy điểm số của người chơi '{name}' lên Firebase...");

        // 2. Lưu điểm số lên bảng xếp hạng của Firebase
        if (FirebaseDatabaseManager.Instance != null)
        {
            FirebaseDatabaseManager.Instance.SaveLeaderboardScore(name, completionTime, true);
        }
        else
        {
            Debug.LogError("[GameEndUIController] FirebaseDatabaseManager.Instance = NULL! Không thể đẩy dữ liệu xếp hạng.");
        }

        // 3. Khôi phục lại tốc độ thời gian chuẩn trước khi đổi scene
        Time.timeScale = 1f;

        // 4. Ẩn Canvas
        endCanvas.gameObject.SetActive(false);

        // 5. Tải lại Menu chính (Scene 0)
        SceneManager.LoadScene(0);
    }
}