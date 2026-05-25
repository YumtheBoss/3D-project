using System.Collections;
using UnityEngine;

/// <summary>
/// Gắn vào con gấu bông trong cái cũi ở Room 4.
/// Tự tạo Point Light và cho nó nhịp nhàng khi Room 4 được vào.
/// </summary>
public class TeddyBearGlow : MonoBehaviour
{
    [Header("Màu sắc & cường độ")]
    public Color glowColor = new Color(1f, 0.75f, 0.35f);
    public float minIntensity = 0.2f;
    public float maxIntensity = 1.4f;
    [Tooltip("Tốc độ nhịp sáng (lần/giây)")]
    public float pulseSpeed = 1.2f;
    public float lightRange = 2.5f;

    [Header("Vị trí đèn (local offset từ gấu)")]
    public Vector3 lightOffset = new Vector3(0f, 0.3f, 0f);

    private Light glowLight;

    private void Awake()
    {
        GameObject lightObj = new GameObject("_BearGlow");
        lightObj.transform.SetParent(transform, false);
        lightObj.transform.localPosition = lightOffset;

        glowLight = lightObj.AddComponent<Light>();
        glowLight.type      = LightType.Point;
        glowLight.color     = glowColor;
        glowLight.range     = lightRange;
        glowLight.intensity = minIntensity;
        glowLight.shadows   = LightShadows.None;
        glowLight.enabled   = false;
    }

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
            glowLight.enabled = true;
            StartCoroutine(PulseRoutine());
        }
    }

    private IEnumerator PulseRoutine()
    {
        while (true)
        {
            float t = (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI) + 1f) * 0.5f;
            if (glowLight != null)
                glowLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
            yield return null;
        }
    }
}
