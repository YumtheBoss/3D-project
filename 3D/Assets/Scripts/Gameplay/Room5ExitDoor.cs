using System.Collections;
using UnityEngine;

// Gắn script này vào từng cửa thoát ở Room 5.
// Chỉ 1 cửa đặt isGoodEnding = true → Good Ending.
// Các cửa còn lại → Bad Ending sau 3 lần chọn sai.
public class Room5ExitDoor : MonoBehaviour
{
    [Tooltip("True: đây là cửa dẫn đến good ending. False: bad ending sau 3 lần.")]
    public bool isGoodEnding;

    [Header("Interaction")]
    public float interactDistance = 3f;

    [Header("Audio")]
    public AudioClip openSound;
    public AudioSource audioSource;

    // Dùng chung cho tất cả Room5ExitDoor trong scene
    private static int wrongAttempts = 0;
    private const int maxWrongAttempts = 3;

    private bool isPlayerNear = false;
    private bool used = false;
    private Transform playerTransform;

    // Reset khi Room 5 được vào lần đầu
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
        if (room == RoomManager.RoomState.Room5)
            wrongAttempts = 0;
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (used) return;

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
            else return;
        }

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        isPlayerNear = dist <= interactDistance;

        if (GameObject.Find("_ChapterIntroCanvas_Auto") != null) return;

        bool interactPressed = Input.GetKeyDown(KeyCode.E);
        if (isPlayerNear && interactPressed)
        {
            OnDoorInteract();
        }
    }

    private void OnDoorInteract()
    {
        // Khóa cửa cho đến khi giải trừ xong 3 phong ấn tà ác
        if (!Room5SealPurge.AllSealsCleared())
        {
            Debug.LogWarning("[Room5ExitDoor] Lối thoát đang bị phong ấn! Hãy giải trừ tất cả phong ấn bằng Gấu bông.");
            return;
        }

        // Cổng dịch chuyển đã mở, cửa vent bị chặn cứng do tà khí bùng nổ lúc thanh tẩy
        Debug.LogWarning("[Room5ExitDoor] Lối thoát hiểm này đã bị chặn cứng! Cần tìm Cổng Dịch Chuyển ở tâm phòng.");
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.PlaySafetyMonologue("Cánh cửa này đã bị tà khí bùng nổ làm sập hoàn toàn... Mình phải đi vào Cổng Dịch Chuyển Ánh Sáng ở tâm phòng!");
        }
    }

    private IEnumerator WrongDoorSequence()
    {
        JumpscareController jsc = FindAnyObjectByType<JumpscareController>();
        jsc?.TriggerJumpscare();

        yield return new WaitForSeconds(2.8f);

        // Respawn về giữa Room 5, còn cơ hội thử lại
        RoomManager.Instance?.RespawnInRoom5();
    }

    private IEnumerator WrongDoorFinalSequence()
    {
        JumpscareController jsc = FindAnyObjectByType<JumpscareController>();
        jsc?.TriggerJumpscare();

        yield return new WaitForSeconds(2.8f);

        // Đã sai 3 lần → mắc kẹt mãi mãi
        RoomManager.Instance?.TriggerBadEndingTrapped();
    }

    private void OnGUI()
    {
        if (used || !isPlayerNear || Time.timeScale <= 0f) return;
        if (GameObject.Find("_ChapterIntroCanvas_Auto") != null) return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 24;
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.MiddleCenter;

        // Hiển thị cảnh báo nếu vẫn còn phong ấn chưa giải
        if (!Room5SealPurge.AllSealsCleared())
        {
            style.normal.textColor = new Color(1f, 0.4f, 0.4f); // Đỏ cảnh báo phong ấn
            GUI.Label(new Rect(Screen.width / 2f - 400, Screen.height / 2f + 50, 800, 50),
                $"<b>Lối thoát hiểm đang bị phong ấn bởi tà khí! ({Room5SealPurge.GetProgressString()})</b>", style);
            return;
        }

        style.normal.textColor = new Color(1f, 0.6f, 0.2f); // Cam cảnh báo
        GUI.Label(new Rect(Screen.width / 2f - 250, Screen.height / 2f + 50, 500, 50),
            "<b>Cửa này đã bị chặn cứng! Hãy tìm Cổng Dịch Chuyển ở tâm phòng!</b>", style);
    }
}
