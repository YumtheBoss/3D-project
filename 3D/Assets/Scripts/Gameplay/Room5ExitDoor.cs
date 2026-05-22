using System.Collections;
using UnityEngine;
using MobileControls;

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

        bool interactPressed = Input.GetKeyDown(KeyCode.E) || MobileButtons.interactPressed;
        if (isPlayerNear && interactPressed)
        {
            MobileButtons.interactPressed = false;
            OnDoorInteract();
        }
    }

    private void OnDoorInteract()
    {
        if (openSound != null && audioSource != null)
            audioSource.PlayOneShot(openSound);

        if (isGoodEnding)
        {
            used = true;
            RoomManager.Instance?.TriggerGoodEnding();
        }
        else
        {
            wrongAttempts++;
            if (wrongAttempts >= maxWrongAttempts)
            {
                used = true;
                StartCoroutine(WrongDoorFinalSequence());
            }
            else
            {
                StartCoroutine(WrongDoorSequence());
            }
        }
    }

    private IEnumerator WrongDoorSequence()
    {
        JumpscareController jsc = FindObjectOfType<JumpscareController>();
        jsc?.TriggerJumpscare();

        yield return new WaitForSeconds(2.8f);

        // Respawn về giữa Room 5, còn cơ hội thử lại
        RoomManager.Instance?.RespawnInRoom5();
    }

    private IEnumerator WrongDoorFinalSequence()
    {
        JumpscareController jsc = FindObjectOfType<JumpscareController>();
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

        GUI.Label(new Rect(Screen.width / 2f - 200, Screen.height / 2f + 50, 400, 50),
            "Nhấn [E] để Chui Qua Vent", style);
    }
}
