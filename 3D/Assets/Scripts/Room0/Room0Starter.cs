using System.Collections;
using UnityEngine;

/// <summary>
/// Gắn script này vào bất kỳ GameObject nào trong scene Room 0.
/// Khi RoomManager chuyển sang Room0, tự động phát monologue "thức dậy"
/// sau một khoảng delay ngắn để player có thời gian nhìn xung quanh trước.
/// </summary>
public class Room0Starter : MonoBehaviour
{
    [Tooltip("InnerMonologue ở vị trí spawn (lúc thức dậy)")]
    public InnerMonologue wakeUpMonologue;

    [Tooltip("Giây chờ sau khi vào Room 0 trước khi monologue bắt đầu")]
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
        if (room == RoomManager.RoomState.Room0)
            StartCoroutine(PlayAfterDelay());
    }

    private void Start()
    {
        // Nếu game bắt đầu ngay ở Room 0 (không qua RoomManager event),
        // kiểm tra và kích hoạt luôn.
        if (RoomManager.Instance == null || RoomManager.Instance.CurrentRoom == RoomManager.RoomState.Room0)
            StartCoroutine(PlayAfterDelay());
    }

    private IEnumerator PlayAfterDelay()
    {
        yield return new WaitForSeconds(startDelay);
        wakeUpMonologue?.PlayManually();
    }
}
