using UnityEngine;

/// <summary>
/// Đặt script này vào một GameObject trong Room5 scene.
/// Khi scene load xong, script sẽ:
///   1. Teleport player đến spawn point trong Room5
///   2. Kích hoạt sự kiện Room5 để DemonController và FlashlightController hoạt động
/// </summary>
public class Room5Initializer : MonoBehaviour
{
    [Tooltip("Vị trí xuất hiện của player khi vào Room5. Gán Transform tại đây.")]
    [SerializeField] private Transform playerSpawnPoint;

    private void Start()
    {
        // Teleport player về spawn point của Room5
        if (playerSpawnPoint != null)
            RoomManager.Instance?.RespawnToPoint(playerSpawnPoint);
        else
        {
            // Fallback: tìm tag Room5Spawn
            GameObject spawnGO = GameObject.FindGameObjectWithTag("Room5Spawn");
            if (spawnGO != null)
                RoomManager.Instance?.RespawnToPoint(spawnGO.transform);
        }

        // Kích hoạt Room5 event — DemonControllers đã subscribe từ OnEnable()
        RoomManager.Instance?.EnterRoom(RoomManager.RoomState.Room5);
    }
}
