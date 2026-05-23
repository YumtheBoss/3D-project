using System.Collections;
using UnityEngine;

/// <summary>
/// Gắn script này vào bất kỳ GameObject nào trong scene.
/// Khi RoomManager chuyển sang Room 3, tự động phát monologue "hành lang tối — linh cảm xấu"
/// sau một khoảng delay ngắn. Room 3 là nơi đèn pin bắt đầu có tác dụng với quỷ.
/// </summary>
public class Room3Starter : MonoBehaviour
{
    [Tooltip("InnerMonologue khi bước vào hành lang tối — cảm giác bị theo dõi")]
    public InnerMonologue entryMonologue;

    [Tooltip("Giây chờ sau khi vào Room 3 trước khi monologue bắt đầu")]
    public float startDelay = 1.5f;

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
        if (room == RoomManager.RoomState.Room3)
            StartCoroutine(PlayAfterDelay());
    }

    private IEnumerator PlayAfterDelay()
    {
        yield return new WaitForSeconds(startDelay);
        entryMonologue?.PlayManually();
    }
}
