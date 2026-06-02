using UnityEngine;

/// <summary>
/// Đặt lên cánh cửa ra duy nhất ở cuối map Room 0/1/2.
/// Khi player nhấn [E]:
///   Room 0 → Room 1: teleport về đầu map (loop lần 1)
///   Room 1 → Room 2: teleport về đầu map (loop lần 2, đồ đạc xáo trộn)
///   Room 2: script này bị tắt — Room2Door xử lý thay
/// Tự reset sau mỗi lần loop để có thể kích hoạt lại lần sau.
/// </summary>
public class RoomExitTrigger : MonoBehaviour
{
    [Tooltip("Hiển thị gợi ý '[E] Đi tiếp' khi đứng gần")]
    public bool showPrompt = true;
    public float promptDistance = 2.5f;

    [Header("Audio")]
    public AudioClip doorOpenSound;

    private bool triggered = false;
    private Transform playerTransform;
    private AudioSource audioSource;
    private bool isNear = false;
    private bool isInsideTrigger = false;

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
        if (room == RoomManager.RoomState.Room0 || room == RoomManager.RoomState.Room1)
        {
            // Đảm bảo bật lại khi quay lại Room 0 hoặc Room 1 sau restart
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = true;
            triggered = false;
        }
        else if (room == RoomManager.RoomState.Room2)
        {
            // Reset để có thể fire lại lần tiếp theo khi player đi qua lần nữa (nếu không dùng DoorChoice)
            triggered = false;
        }

        // Room 2 dùng DoorChoice hoặc Room2Door thay thế → vô hiệu hóa trigger này đi (nhưng giữ Script Enabled để nhận event)
        if (room == RoomManager.RoomState.Room2)
        {
            var doorChoice = FindAnyObjectByType<AnomalySystem.DoorChoice>();
            var room2Door = FindAnyObjectByType<Room2Door>();
            if (doorChoice != null || room2Door != null)
            {
                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
                triggered = true; // Khóa tương tác của Update và OnTriggerEnter
                return;
            }
            // Nếu không có DoorChoice/Room2Door, RoomExitTrigger vẫn hoạt động bình thường
            Debug.Log("[RoomExitTrigger] Room 2: Không có DoorChoice hoặc Room2Door, giữ RoomExitTrigger hoạt động.");
        }
    }

    private void FindPlayerRobust()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) p = GameObject.Find("Player");
        if (p == null)
        {
            CharacterController cc = FindAnyObjectByType<CharacterController>();
            if (cc != null) p = cc.gameObject;
        }
        if (p != null) playerTransform = p.transform;
    }

    private void Start()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0.5f;
        audioSource.playOnAwake = false;

        FindPlayerRobust();

        // Nếu game bắt đầu ở Room 2, ẩn trigger ngay (nếu có DoorChoice hoặc Room2Door)
        if (RoomManager.Instance != null && RoomManager.Instance.CurrentRoom == RoomManager.RoomState.Room2)
        {
            var doorChoice = FindAnyObjectByType<AnomalySystem.DoorChoice>();
            var room2Door = FindAnyObjectByType<Room2Door>();
            if (doorChoice != null || room2Door != null)
            {
                if (col != null) col.enabled = false;
                triggered = true; // Khóa tương tác
            }
        }
    }

    private void Update()
    {
        if (triggered) return;

        if (playerTransform == null)
        {
            FindPlayerRobust();
            if (playerTransform == null) return;
        }

        // Đo khoảng cách chính xác đến tâm collider để miễn nhiễm lỗi lệch pivot
        float dist = Vector3.Distance(transform.position, playerTransform.position);
        Collider myCol = GetComponent<Collider>();
        if (myCol != null)
        {
            dist = Vector3.Distance(myCol.bounds.center, playerTransform.position);
        }
        
        bool isClose = dist <= promptDistance;
        isNear = isInsideTrigger || isClose;

        if (isNear && Input.GetKeyDown(KeyCode.E))
            Activate();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player") && !other.name.Contains("Player")) return;
        isInsideTrigger = true;
        if (!showPrompt) Activate();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player") && !other.name.Contains("Player")) return;
        isInsideTrigger = false;
    }

    private void Activate()
    {
        if (triggered) return;

        // Khóa an toàn check
        if (RoomManager.Instance != null)
        {
            if (!RoomManager.Instance.CheckAndPlaySafetyLockMonologue())
            {
                // Bị khóa -> Không mở cửa và giữ nguyên trạng thái cho lần tương tác sau
                return;
            }
        }

        triggered = true;

        if (doorOpenSound != null && audioSource != null)
            audioSource.PlayOneShot(doorOpenSound);

        if (RoomManager.Instance != null)
            RoomManager.Instance.AdvanceToNextRoom();
        else
            Debug.LogError("[RoomExitTrigger] Không tìm thấy RoomManager!");
    }

    private void OnGUI()
    {
        if (!showPrompt || !isNear || triggered || Time.timeScale == 0f) return;

        GUIStyle style = new GUIStyle { fontSize = 22, alignment = TextAnchor.MiddleCenter };
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(Screen.width / 2f - 150, Screen.height / 2f + 50, 300, 40),
                  "Nhấn [E] để đi tiếp", style);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.3f, 0.35f);
        Collider col = GetComponent<Collider>();
        if (col != null) Gizmos.DrawCube(col.bounds.center, col.bounds.size);
    }
}
