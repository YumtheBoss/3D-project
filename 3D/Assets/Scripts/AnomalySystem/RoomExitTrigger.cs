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
        if (room == RoomManager.RoomState.Room1 || room == RoomManager.RoomState.Room2)
        {
            // Reset để có thể fire lại lần tiếp theo khi player đi qua lần nữa
            triggered = false;
        }

        // Room 2 dùng Room2Door thay thế → ẩn trigger này đi
        gameObject.SetActive(room != RoomManager.RoomState.Room2);
    }

    private void Start()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0.5f;
        audioSource.playOnAwake = false;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;

        // Nếu game bắt đầu ở Room 2, ẩn trigger ngay
        if (RoomManager.Instance != null && RoomManager.Instance.CurrentRoom == RoomManager.RoomState.Room2)
            gameObject.SetActive(false);
    }

    private void Update()
    {
        if (triggered || playerTransform == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        isNear = dist <= promptDistance;

        if (isNear && Input.GetKeyDown(KeyCode.E))
            Activate();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;
        if (!showPrompt) Activate();
    }

    private void Activate()
    {
        if (triggered) return;
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
