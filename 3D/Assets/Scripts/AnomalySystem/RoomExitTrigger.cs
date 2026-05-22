using UnityEngine;

/// <summary>
/// Đặt BoxCollider (Is Trigger = true) ở lối ra của mỗi phòng.
/// Khi player bước vào vùng này, sẽ tiến sang phòng tiếp theo.
/// Dùng cho Room0, Room1, Room3, Room4.
/// Room2 dùng DoorChoice thay thế.
/// </summary>
public class RoomExitTrigger : MonoBehaviour
{
    [Tooltip("Hiển thị gợi ý '[E] Đi tiếp' khi đứng gần lối ra")]
    public bool showPrompt = true;

    [Tooltip("Khoảng cách hiện prompt (mét)")]
    public float promptDistance = 2.5f;

    [Header("Audio")]
    public AudioClip doorOpenSound;

    private bool triggered = false;
    private Transform playerTransform;
    private AudioSource audioSource;
    private bool isNear = false;

    private void Start()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0.5f;
        audioSource.playOnAwake = false;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;
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
        if (!showPrompt) Activate(); // Tự động tiến nếu không cần nhấn E
    }

    private void Activate()
    {
        if (triggered) return;
        triggered = true;

        if (doorOpenSound != null && audioSource != null)
            audioSource.PlayOneShot(doorOpenSound);

        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.AdvanceToNextRoom();
        else
            Debug.LogError("[RoomExitTrigger] Không tìm thấy GameFlowManager!");
    }

    private void OnGUI()
    {
        if (!isNear || triggered || Time.timeScale == 0f) return;

        GUIStyle style = new GUIStyle
        {
            fontSize = 22,
            alignment = TextAnchor.MiddleCenter
        };
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(Screen.width / 2f - 150, Screen.height / 2f + 50, 300, 40),
                  "Nhấn [E] để đi tiếp", style);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.3f, 0.35f);
        Collider col = GetComponent<Collider>();
        if (col != null)
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
    }
}
