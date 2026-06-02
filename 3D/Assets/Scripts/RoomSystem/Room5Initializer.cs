using UnityEngine;
using System.Collections.Generic;

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

    [Tooltip("Cổng Dịch Chuyển Ánh Sáng dẫn tới kết thúc game. Gán GameObject tại đây.")]
    public GameObject victoryPortal;

    private void OnEnable()
    {
        Room5SealPurge.OnAllSealsCleared += HandleAllSealsCleared;
    }

    private void OnDisable()
    {
        Room5SealPurge.OnAllSealsCleared -= HandleAllSealsCleared;
    }

    private void HandleAllSealsCleared()
    {
        if (victoryPortal != null)
        {
            victoryPortal.SetActive(true);
            Debug.Log("[Room5Initializer] Cổng Dịch Chuyển Ánh Sáng đã được kích hoạt!");
        }
        else
        {
            Debug.LogWarning("[Room5Initializer] victoryPortal chưa được gán! Thử tìm tự động...");
            // Tìm trong tất cả các đối tượng (bao gồm cả inactive)
            GameObject portal = FindVictoryPortalRobust();
            if (portal != null)
            {
                victoryPortal = portal;
                victoryPortal.SetActive(true);
                Debug.Log($"[Room5Initializer] Đã tìm thấy và kích hoạt tự động Portal: {victoryPortal.name}");
            }
            else
            {
                Debug.LogError("[Room5Initializer] Không tìm thấy Cổng Dịch Chuyển trong scene!");
            }
        }
    }

    private GameObject FindVictoryPortalRobust()
    {
        GameObject portal = GameObject.Find("VictoryPortal");
        if (portal == null) portal = GameObject.Find("Room5VictoryPortal");
        if (portal == null)
        {
            // Dự phòng quét tất cả Transform (kể cả bị inactive)
            Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
            foreach (Transform t in allTransforms)
            {
                if ((t.gameObject.name == "VictoryPortal" || t.gameObject.name == "Room5VictoryPortal") && 
                    t.gameObject.hideFlags == HideFlags.None && !string.IsNullOrEmpty(t.gameObject.scene.name))
                {
                    return t.gameObject;
                }
            }
        }
        return portal;
    }

    private void Start()
    {
        // Tự tìm Portal lúc bắt đầu nếu chưa gán
        if (victoryPortal == null)
        {
            victoryPortal = FindVictoryPortalRobust();
        }

        // Quản lý trạng thái ẩn/hiện của Portal lúc bắt đầu
        if (victoryPortal != null)
        {
            bool cleared = Room5SealPurge.AllSealsCleared();
            victoryPortal.SetActive(cleared);
            Debug.Log($"[Room5Initializer] Khởi tạo Portal '{victoryPortal.name}'. Trạng thái hoạt động: {cleared}");
        }

        Transform spawnT = playerSpawnPoint;
        if (spawnT == null)
        {
            GameObject spawnGO = GameObject.FindGameObjectWithTag("Room5Spawn");
            if (spawnGO != null) spawnT = spawnGO.transform;
        }

        // Thực hiện dịch chuyển người chơi về Spawn Point của Room 5
        if (spawnT != null)
        {
            if (RoomManager.Instance != null)
            {
                RoomManager.Instance.RespawnToPoint(spawnT);
            }
            else
            {
                // Tự động dịch chuyển nếu chạy test trực tiếp Scene trong Editor (khi không có RoomManager)
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj == null) playerObj = GameObject.Find("Player");
                
                if (playerObj != null)
                {
                    // Tắt tạm CharacterController (nếu có) để tránh lỗi cản trở định vị
                    var cc = playerObj.GetComponent<CharacterController>();
                    if (cc != null) cc.enabled = false;

                    playerObj.transform.position = spawnT.position;
                    playerObj.transform.rotation = spawnT.rotation;

                    if (cc != null) cc.enabled = true;
                    Debug.Log($"[Room5Initializer] RoomManager is missing (direct scene testing). Directly teleported Player to: {spawnT.name}");
                }
            }
        }

        // Kích hoạt Room5 event nếu chưa được kích hoạt trước đó (tránh kích hoạt trùng lặp)
        if (RoomManager.Instance != null)
        {
            if (RoomManager.Instance.CurrentRoom != RoomManager.RoomState.Room5)
            {
                RoomManager.Instance.NotifyRoomEntered(RoomManager.RoomState.Room5);
            }
        }

        // Tự động tạo và phát hội thoại nội tâm (Inner Monologue) hướng dẫn người chơi câu đố phong ấn Room 5
        var monologue = gameObject.AddComponent<InnerMonologue>();
        monologue.fontSize = 28f;
        monologue.lines = GameTextConfig.GetMonologueLines("Room5_Entry");
        monologue.PlayManually();
    }

    private void OnGUI()
    {
        // Chỉ vẽ HUD khi đang ở Room 5
        if (RoomManager.Instance != null && RoomManager.Instance.CurrentRoom != RoomManager.RoomState.Room5)
            return;

        if (Time.timeScale <= 0f) return;
        if (GameObject.Find("_ChapterIntroCanvas_Auto") != null) return;

        // Vẽ hộp đen mờ (background) cho HUD ở góc trên cùng chính giữa màn hình
        float hudWidth = 460f;
        float hudHeight = 40f;
        float startX = (Screen.width - hudWidth) / 2f;
        float startY = 20f;

        // Tạo texture đen mờ vẽ nền
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.6f));
        texture.Apply();

        GUIStyle bgStyle = new GUIStyle();
        bgStyle.normal.background = texture;

        GUI.Box(new Rect(startX, startY, hudWidth, hudHeight), GUIContent.none, bgStyle);

        // Vẽ chữ hiển thị tiến trình
        GUIStyle textStyle = new GUIStyle();
        textStyle.fontSize = 18;
        textStyle.alignment = TextAnchor.MiddleCenter;

        if (!Room5SealPurge.AllSealsCleared())
        {
            textStyle.normal.textColor = new Color(1f, 0.4f, 0.4f); // Đỏ tà khí
            string text = $"<b>⚠️ LỐI THOÁT BỊ PHONG ẤN: Đã giải trừ {Room5SealPurge.GetProgressString()} đàn tế</b>";
            GUI.Label(new Rect(startX, startY, hudWidth, hudHeight), text, textStyle);
        }
        else
        {
            textStyle.normal.textColor = new Color(0.4f, 1f, 0.4f); // Xanh giải phóng
            string text = "<b>🔓 PHONG ẤN ĐÃ ĐƯỢC GIẢI TRỪ! Hãy bước vào Cổng Dịch Chuyển Ánh Sáng!</b>";
            GUI.Label(new Rect(startX, startY, hudWidth, hudHeight), text, textStyle);
        }
    }
}
