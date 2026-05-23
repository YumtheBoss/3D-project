using System.Collections;
using UnityEngine;

/// <summary>
/// Gắn script này vào bất kỳ GameObject nào trong scene.
/// Khi RoomManager chuyển sang Room 1, tự động phát monologue "bối rối — vòng lặp"
/// sau một khoảng delay ngắn.
/// </summary>
public class Room1Starter : MonoBehaviour
{
    [Tooltip("InnerMonologue bối rối khi nhận ra đang bị loop")]
    public InnerMonologue entryMonologue;

    [Tooltip("Giây chờ sau khi vào Room 1 trước khi monologue bắt đầu")]
    public float startDelay = 1.0f;

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
        if (room == RoomManager.RoomState.Room1)
            StartCoroutine(PlayAfterDelay());
    }

    private IEnumerator PlayAfterDelay()
    {
        yield return new WaitForSeconds(startDelay);
        entryMonologue?.PlayManually();
    }
}
