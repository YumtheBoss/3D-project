using System.Collections;
using UnityEngine;

// Gắn script này vào từng cánh cửa ở Room 2.
// Một cửa đặt isCorrectDoor = true (phòng có bất thường → chọn đúng → qua Room 3).
// Cửa còn lại isCorrectDoor = false (chọn sai → jumpscare + game over).
public class Room2Door : MonoBehaviour
{
    [Tooltip("True: cửa đúng (đồ đạc bất thường → đi tiếp). False: cửa sai → bad ending.")]
    public bool isCorrectDoor;

    [Header("Interaction")]
    public float interactDistance = 3f;

    [Header("Audio")]
    public AudioClip openSound;
    public AudioSource audioSource;

    // Thời gian chờ sau jumpscare trước khi hiện màn bad ending
    private const float jumpscareDelay = 2.8f;

    private bool isPlayerNear = false;
    private bool used = false;
    private Transform playerTransform;
    private float cooldownTimer = 0f; // Thời gian chờ sau khi vào Room 2 trước khi cho phép tương tác

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
        // Reset mỗi khi vào Room 2 để player có thể tương tác lại nếu retry
        if (room == RoomManager.RoomState.Room2)
        {
            used = false;
            cooldownTimer = 1.0f; // Chờ 1 giây sau khi vào Room 2 mới cho phép mở cửa
        }
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

        // Đếm ngược cooldown
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            return;
        }

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
            else return;
        }

        // Chỉ hoạt động khi đang ở Room 2
        if (RoomManager.Instance == null ||
            RoomManager.Instance.CurrentRoom != RoomManager.RoomState.Room2)
        {
            isPlayerNear = false;
            return;
        }

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        isPlayerNear = dist <= interactDistance;

        if (GameObject.Find("_ChapterIntroCanvas_Auto") != null) return;

        bool interactPressed = Input.GetKeyDown(KeyCode.E);
        if (isPlayerNear && interactPressed)
        {
            used = true;
            OnDoorInteract();
        }
    }

    private void OnDoorInteract()
    {
        if (openSound != null && audioSource != null)
            audioSource.PlayOneShot(openSound);

        if (isCorrectDoor)
        {
            RoomManager.Instance?.EnterRoom(RoomManager.RoomState.Room3);
        }
        else
        {
            StartCoroutine(WrongChoiceSequence());
        }
    }

    private IEnumerator WrongChoiceSequence()
    {
        JumpscareController jsc = FindObjectOfType<JumpscareController>();
        jsc?.TriggerJumpscare();

        yield return new WaitForSeconds(jumpscareDelay);

        RoomManager.Instance?.TriggerBadEnding("wrong_room2_door");
    }

    private void OnGUI()
    {
        if (used || !isPlayerNear || Time.timeScale <= 0f) return;
        if (GameObject.Find("_ChapterIntroCanvas_Auto") != null) return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 24;
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(Screen.width / 2f - 150, Screen.height / 2f + 50, 300, 50),
            "Nhấn [E] để Mở Cửa", style);
    }
}
