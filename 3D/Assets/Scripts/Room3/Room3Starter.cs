using System.Collections;
using UnityEngine;

/// <summary>
/// Gắn vào bất kỳ GameObject nào trong scene.
/// Khi RoomManager chuyển sang Room 3:
///   - Đèn trắng (#1) bắt đầu nhấp nháy (flickerLight cần có LightFlicker component).
///   - InnerMonologue entry phát sau startDelay.
/// </summary>
public class Room3Starter : MonoBehaviour
{
    [Header("Monologue")]
    [Tooltip("InnerMonologue khi bước vào hành lang tối")]
    public InnerMonologue entryMonologue;

    [Tooltip("Giây chờ sau khi vào Room 3 trước khi monologue bắt đầu")]
    public float startDelay = 1.5f;

    [Header("Đèn nhấp nháy")]
    [Tooltip("Đèn trắng #1 — nhấp nháy ngay khi bước vào Room 3")]
    public LightFlicker flickerLight;

    [Tooltip("Giây chờ trước khi bắt đầu nhấp nháy (ngắn hơn startDelay để tạo atmosphere)")]
    public float flickerStartDelay = 0.3f;

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
            StartCoroutine(StartRoom3());
    }

    private IEnumerator StartRoom3()
    {
        // Đèn nhấp nháy trước
        if (flickerStartDelay > 0f)
            yield return new WaitForSeconds(flickerStartDelay);

        flickerLight?.StartFlicker();

        // Monologue sau
        float remaining = startDelay - flickerStartDelay;
        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);
        else
            yield return null;

        entryMonologue?.PlayManually();
    }
}
