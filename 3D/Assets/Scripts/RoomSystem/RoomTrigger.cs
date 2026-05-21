using UnityEngine;

// Đặt vào một GameObject có Collider (Is Trigger = true) ở cửa ra mỗi phòng.
// Khi player bước vào, chuyển sang phòng tiếp theo qua RoomManager.
public class RoomTrigger : MonoBehaviour
{
    [SerializeField] private RoomManager.RoomState targetRoom;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;
        RoomManager.Instance?.EnterRoom(targetRoom);
        gameObject.SetActive(false);
    }
}
