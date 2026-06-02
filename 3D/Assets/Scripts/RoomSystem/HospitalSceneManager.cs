using System.Collections;
using UnityEngine;

/// <summary>
/// Scene-local manager cho Hospital scene (Room 3 + Room 4).
/// Đặt trên bất kỳ GO nào trong Hospital scene — KHÔNG DontDestroyOnLoad.
/// Thay thế Room3Starter + Room3Initializer.
///
/// Room 3: tự kích hoạt khi scene load (flicker + monologue).
/// Room 4: gọi EnterRoom4() từ RoomTrigger ở cửa vào Room 4.
///         TeddyBearGlow và Room4Environment tự phản ứng qua OnRoomEntered event.
/// </summary>
public class HospitalSceneManager : MonoBehaviour
{
    [Header("── Room 3 ──────────────────────────")]
    [Tooltip("Đèn trắng nhấp nháy đầu hành lang — cần LightFlicker component")]
    public LightFlicker r3FlickerLight;

    [Tooltip("Giây chờ trước khi đèn bắt đầu nhấp nháy")]
    public float r3FlickerDelay = 0.3f;

    [Tooltip("InnerMonologue phát khi bước vào hành lang")]
    public InnerMonologue r3EntryMonologue;

    [Tooltip("Giây chờ trước khi monologue bắt đầu (nên >= r3FlickerDelay)")]
    public float r3MonologueDelay = 1.5f;

    [Header("── Room 4 ──────────────────────────")]
    [Tooltip("(tuỳ chọn) InnerMonologue phát thêm khi vào Room 4")]
    public InnerMonologue r4EntryMonologue;

    // ─────────────────────────────────────────────────────────

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
        if (room == RoomManager.RoomState.Room4)
        {
            if (r4EntryMonologue != null)
            {
                r4EntryMonologue.lines = GameTextConfig.GetMonologueLines("Room4_Entry");
                r4EntryMonologue.PlayManually();
            }
        }
    }

    private void Start()
    {
        // Dọn dẹp các HintText mặc định hoặc Canvas dư thừa do copy-paste trong Editor (như TornPageCanvas)
        CleanUpDuplicatePrompts();

        // Chỉ NotifyRoomEntered nếu RoomManager chưa ở Room3 (tránh double-fire khi cross-scene)
        if (RoomManager.Instance != null && RoomManager.Instance.CurrentRoom != RoomManager.RoomState.Room3)
        {
            RoomManager.Instance.NotifyRoomEntered(RoomManager.RoomState.Room3);
        }
        StartCoroutine(InitRoom3());
    }

    private void CleanUpDuplicatePrompts()
    {
        // 1. Tắt TornPageCanvas bị thừa trong scene Hospital
        GameObject tornPageCanvas = GameObject.Find("TornPageCanvas");
        if (tornPageCanvas != null)
        {
            Debug.Log("[HospitalSceneManager] Phát hiện TornPageCanvas dư thừa, tiến hành tắt hoạt động.");
            tornPageCanvas.SetActive(false);
        }

        // 2. Tìm và tắt các HintText có chữ "Ấn E để đọc" hoặc tương tự đang hiển thị mặc định
        var hintTexts = FindObjectsOfType<TMPro.TextMeshProUGUI>(true);
        foreach (var hint in hintTexts)
        {
            if (hint.gameObject.name == "HintText" && hint.gameObject.activeSelf)
            {
                string text = hint.text.Trim();
                if (text.Contains("Ấn E") || text.Contains("đọc") || text.Contains("Nhấn [E]"))
                {
                    Debug.Log($"[HospitalSceneManager] Phát hiện HintText mặc định '{text}' đang bật, tắt hoạt động.");
                    hint.gameObject.SetActive(false);
                }
            }
        }
    }


    private IEnumerator InitRoom3()
    {
        if (r3FlickerDelay > 0f)
            yield return new WaitForSeconds(r3FlickerDelay);

        r3FlickerLight?.StartFlicker();

        float remaining = r3MonologueDelay - r3FlickerDelay;
        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);
        else
            yield return null;

        if (r3EntryMonologue != null)
        {
            r3EntryMonologue.lines = GameTextConfig.GetMonologueLines("Room3_Entry");
            r3EntryMonologue.PlayManually();
        }
    }

    /// <summary>
    /// Gắn vào RoomTrigger ở cửa vào Room 4 (UnityEvent hoặc gọi thẳng).
    /// Room4Environment và TeddyBearGlow tự kích hoạt qua OnRoomEntered.
    /// </summary>
    public void EnterRoom4()
    {
        RoomManager.Instance?.NotifyRoomEntered(RoomManager.RoomState.Room4);
    }
}
