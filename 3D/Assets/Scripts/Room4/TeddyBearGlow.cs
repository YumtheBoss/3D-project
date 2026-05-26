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

    [Header("Tương tác với Gấu (Mới)")]
    [Tooltip("Khoảng cách tối đa để tương tác")]
    public float interactDistance = 2.5f;
    [Tooltip("Tiếng cười kinh dị hoặc âm thanh rợn người khi tương tác")]
    public AudioClip laughSound;
    [Tooltip("Monologue phát khi player kiểm tra con gấu")]
    public InnerMonologue interactMonologue;
    [Tooltip("ID của vật phẩm Gấu bông trong hệ thống Inventory (túi đồ)")]
    public string bearItemID = "TeddyBear";

    private bool isPlayerNear = false;
    private bool hasInteracted = false;
    private Transform playerTransform;
    private AudioSource audioSource;

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

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.8f; // Âm thanh 3D rùng rợn phát từ con gấu
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

    private void Update()
    {
        // Chỉ hoạt động khi ở Room 4
        if (RoomManager.Instance == null || RoomManager.Instance.CurrentRoom != RoomManager.RoomState.Room4)
        {
            isPlayerNear = false;
            return;
        }

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
            else return;
        }

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        isPlayerNear = dist <= interactDistance;

        if (isPlayerNear && !hasInteracted && Input.GetKeyDown(KeyCode.E))
        {
            InteractWithBear();
        }
    }

    private void InteractWithBear()
    {
        hasInteracted = true;

        // 1. Phát tiếng cười/âm thanh kinh dị 3D từ gấu bông
        if (laughSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(laughSound);
        }

        // 2. Tăng cường hiệu ứng phát sáng rùng rợn (chớp nháy nhanh hơn)
        pulseSpeed = 4.0f;
        maxIntensity = 2.5f;

        // 3. Phát độc thoại nội tâm rùng rợn của người chơi
        interactMonologue?.PlayManually();

        // 4. Thêm gấu bông vào túi đồ (Inventory) làm trang bị nguồn sáng
        if (AnomalySystem.InventoryManager.Instance != null)
        {
            AnomalySystem.InventoryManager.Instance.AddItem(bearItemID);
        }

        // 5. Bắt đầu coroutine để con gấu biến mất sau khi nhặt
        StartCoroutine(PickupRoutine());
    }

    private IEnumerator PickupRoutine()
    {
        // Chờ 1.5s để tiếng cười phát xong và monologue bắt đầu
        yield return new WaitForSeconds(1.5f);
        gameObject.SetActive(false);
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

    private void OnGUI()
    {
        if (hasInteracted || !isPlayerNear || Time.timeScale <= 0f) return;
        if (RoomManager.Instance == null || RoomManager.Instance.CurrentRoom != RoomManager.RoomState.Room4) return;
        if (GameObject.Find("_ChapterIntroCanvas_Auto") != null) return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 24;
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(Screen.width / 2f - 150, Screen.height / 2f + 50, 300, 50),
            "Nhấn [E] để Kiểm Tra", style);
    }
}
