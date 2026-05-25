using UnityEngine;

// Gắn script này vào GameObject đại diện trang sổ / mảnh giấy trong game.
// Khi player đến gần và bấm E, hiện UI nội dung. Bấm E lần nữa hoặc rời đi để đóng.
// Có thể cài autoShowOnEnter = true để tự hiện khi bước vào trigger zone (dùng cho monologue).
public class NotebookPage : MonoBehaviour
{
    [Header("Interaction")]
    [Tooltip("Khoảng cách để hiện prompt tương tác")]
    public float interactDistance = 3f;
    [Tooltip("Tự động hiện UI khi bước vào collider trigger (dùng cho monologue Room 1)")]
    public bool autoShowOnEnter = false;

    [Header("UI")]
    [Tooltip("Canvas chứa nội dung trang giấy (designer tự thiết kế)")]
    public GameObject pageUI;

    [Header("Content (tùy chọn - nếu dùng UI tự tạo)")]
    [Tooltip("Các dòng nội dung hiển thị trên trang. Designer kéo Text component vào pageUI.")]
    [TextArea(3, 8)]
    public string[] pageLines;

    [Header("Inventory")]
    [Tooltip("Bật nếu muốn player có thể cất trang này vào túi bằng phím F")]
    public bool canPickUp = false;
    [Tooltip("ItemID để lưu vào InventoryManager (cần canPickUp = true)")]
    public string inventoryItemId = "";

    private bool isPlayerNear = false;
    private bool isReading = false;
    private Transform playerTransform;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;

        if (pageUI != null) pageUI.SetActive(false);
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
            else return;
        }

        if (autoShowOnEnter) return; // trigger-based, không dùng distance check

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        isPlayerNear = dist <= interactDistance;

        // Ẩn chức năng khi intro đang hiện
        if (GameObject.Find("_ChapterIntroCanvas_Auto") != null) return;

        bool interactPressed = Input.GetKeyDown(KeyCode.E);

        if (isPlayerNear && interactPressed)
        {
            ToggleReading();
        }

        if (isReading)
        {
            if (canPickUp && Input.GetKeyDown(KeyCode.F))
            {
                PickUp();
                return;
            }

            bool shouldClose = Input.GetKeyDown(KeyCode.Escape) || (!isPlayerNear);
            if (shouldClose)
            {
                SetReading(false);
            }
        }
    }

    // Dùng cho autoShowOnEnter = true (gắn trigger collider vào cùng GameObject hoặc child)
    private void OnTriggerEnter(Collider other)
    {
        if (!autoShowOnEnter) return;
        if (!other.CompareTag("Player")) return;
        SetReading(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!autoShowOnEnter) return;
        if (!other.CompareTag("Player")) return;
        SetReading(false);
    }

    private void ToggleReading()
    {
        SetReading(!isReading);
    }

    private void SetReading(bool value)
    {
        isReading = value;
        if (pageUI != null) pageUI.SetActive(isReading);
    }

    private void PickUp()
    {
        if (!string.IsNullOrEmpty(inventoryItemId) && AnomalySystem.InventoryManager.Instance != null)
        {
            AnomalySystem.InventoryManager.Instance.AddItem(inventoryItemId);
        }
        SetReading(false);
        gameObject.SetActive(false);
    }

    private void OnGUI()
    {
        if (autoShowOnEnter) return;
        if (playerTransform == null) return;
        if (GameObject.Find("_ChapterIntroCanvas_Auto") != null) return;
        if (Time.timeScale <= 0f) return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 24;
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.MiddleCenter;

        Rect labelRect = new Rect(Screen.width / 2f - 150, Screen.height / 2f + 50, 300, 50);

        if (isPlayerNear && !isReading)
        {
            GUI.Label(labelRect, "Nhấn [E] để Đọc", style);
        }
        else if (isReading && canPickUp)
        {
            GUI.Label(new Rect(Screen.width / 2f - 150, Screen.height / 2f + 250, 300, 50),
                "Nhấn [F] để Cất Vào Túi", style);
        }
    }
}
