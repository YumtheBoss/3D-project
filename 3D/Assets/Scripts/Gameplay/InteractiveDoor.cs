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
        isOpened = true;

        // Phát âm thanh tiếng mở cửa
        if (openSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(openSound);
        }

        // Kích hoạt dịch chuyển tức thời qua RoomManager (sẽ đưa player tới Room4Spawn tương ứng)
        if (triggerRoomTransition)
        {
            RoomManager.Instance?.EnterRoom(targetRoom);
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
