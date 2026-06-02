using UnityEngine;

/// <summary>
/// Script tương tác cửa đa năng cho Room 3, Room 4 và các phòng khác.
/// Hỗ trợ hiển thị UI gợi ý, phát âm thanh mở cửa,
/// và ngay lập tức dịch chuyển người chơi sang phòng tiếp theo (room4spawn) qua RoomManager.
/// </summary>
public class InteractiveDoor : MonoBehaviour
{
    [Header("Cấu hình chuyển phòng")]
    [Tooltip("Nếu tích chọn, khi nhấn mở cửa sẽ dịch chuyển người chơi sang phòng đích tương ứng qua RoomManager")]
    public bool triggerRoomTransition = true;
    [Tooltip("Phòng đích muốn chuyển tới (ví dụ: Room4)")]
    public RoomManager.RoomState targetRoom = RoomManager.RoomState.Room4;
    [Tooltip("Tên GameObject spawn point tùy chọn. Nếu gán, người chơi sẽ được dịch chuyển tới đây thay vì spawn point mặc định của RoomManager.")]
    public string customSpawnPointName;

    [Header("Hiệu ứng chuyển cảnh")]
    [Tooltip("Nếu tích chọn, màn hình sẽ tự động fade tối đen lại trước khi dịch chuyển, sau đó sáng dần lên ở vị trí spawn mới.")]
    public bool useFadeTransition = true;

    [Header("Hành vi vật lý")]
    [Tooltip("Nếu tích chọn, vật thể cửa này sẽ tự ẩn đi (SetActive(false)) sau khi mở để người chơi có thể tự đi qua.")]
    public bool deactivateOnOpen = false;

    [Header("Tương tác")]
    [Tooltip("Khoảng cách tối đa để có thể tương tác mở cửa")]
    public float interactDistance = 3f;
    [Tooltip("Phím tương tác")]
    public KeyCode interactKey = KeyCode.E;

    [Header("Âm thanh")]
    [Tooltip("Tiếng mở cửa")]
    public AudioClip openSound;
    [Tooltip("AudioSource để phát âm thanh (Nếu trống sẽ tự tìm trên vật thể này)")]
    public AudioSource audioSource;

    private bool isPlayerNear = false;
    private bool isOpened = false;
    private Transform playerTransform;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        // 1. Kiểm tra đối tượng người chơi
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
            else return;
        }

        // 2. Tính khoảng cách
        float dist = Vector3.Distance(transform.position, playerTransform.position);
        isPlayerNear = dist <= interactDistance;

        // 3. Nhận tương tác
        if (isPlayerNear && !isOpened && Input.GetKeyDown(interactKey))
        {
            OpenDoor();
        }
    }

    public void OpenDoor()
    {
        if (isOpened) return;

        // Nếu người chơi đang ở Room 3 và cố tình quay lại cửa vào (InteractiveDoor hướng về Room 0, 1, 2)
        if (RoomManager.Instance != null && RoomManager.Instance.CurrentRoom == RoomManager.RoomState.Room3)
        {
            if (targetRoom == RoomManager.RoomState.Room0 || targetRoom == RoomManager.RoomState.Room1 || targetRoom == RoomManager.RoomState.Room2)
            {
                string blockText = "Cánh cửa này đã bị khóa chặt từ bên ngoài... Mình cảm giác có điều gì đó không lành ở phía sau. Mình phải đi tiếp qua cánh cửa đang chiếu đèn đỏ ở đằng kia.";
                RoomManager.Instance.PlaySafetyMonologue(blockText);
                return;
            }
        }

        // Khóa an toàn check
        if (RoomManager.Instance != null)
        {
            if (!RoomManager.Instance.CheckAndPlaySafetyLockMonologue())
            {
                // Bị khóa -> Không mở cửa và giữ nguyên trạng thái cho lần tương tác sau
                return;
            }
        }

        isOpened = true;

        // Phát âm thanh tiếng mở cửa
        if (openSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(openSound);
        }

        // Kích hoạt dịch chuyển tức thời qua RoomManager (sẽ đưa player tới Room4Spawn tương ứng)
        if (triggerRoomTransition)
        {
            string spawnName = !string.IsNullOrEmpty(customSpawnPointName) ? customSpawnPointName : "";
            
            if (useFadeTransition)
            {
                RoomManager.Instance?.EnterRoomWithFadeTransition(targetRoom, spawnName);
            }
            else
            {
                if (!string.IsNullOrEmpty(spawnName))
                {
                    RoomManager.Instance?.EnterRoomWithCustomSpawn(targetRoom, spawnName);
                }
                else
                {
                    RoomManager.Instance?.EnterRoom(targetRoom);
                }
            }
        }

        // Tự động ẩn vật thể cửa đi nếu tùy chọn này được kích hoạt
        if (deactivateOnOpen)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnGUI()
    {
        if (isOpened || !isPlayerNear || Time.timeScale <= 0f) return;

        // Tránh hiện đè lên Canvas giới thiệu chương
        if (GameObject.Find("_ChapterIntroCanvas_Auto") != null) return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 24;
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(Screen.width / 2f - 150, Screen.height / 2f + 50, 300, 50),
            $"Nhấn [{interactKey}] để Mở Cửa", style);
    }
}
