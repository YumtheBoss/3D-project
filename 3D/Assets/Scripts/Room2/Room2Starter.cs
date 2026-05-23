using System.Collections;
using UnityEngine;

/// <summary>
/// Gắn script này vào bất kỳ GameObject nào trong scene.
/// Khi RoomManager chuyển sang Room 2, tự động phát monologue "nhận ra đồ đạc xáo trộn"
/// sau một khoảng delay ngắn.
/// </summary>
public class Room2Starter : MonoBehaviour
{
    [Tooltip("InnerMonologue khi nhận ra phòng đã thay đổi — gợi ý player quan sát")]
    public InnerMonologue entryMonologue;

    [Tooltip("Giây chờ sau khi vào Room 2 trước khi monologue bắt đầu")]
    public float startDelay = 1.2f;

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
        if (room == RoomManager.RoomState.Room2)
            StartCoroutine(PlayAfterDelay());
    }

    private IEnumerator PlayAfterDelay()
    {
        yield return new WaitForSeconds(startDelay);
        entryMonologue?.PlayManually();
    }
}
