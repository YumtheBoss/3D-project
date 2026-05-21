using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Gắn script này vào trigger collider ở đầu hành lang Room 4.
// Khi player bước vào: con quỷ xuất hiện đằng sau và đuổi player về phía trước.
// Player phải chạy đến cửa cuối → advance sang Room 5.
// Nếu bị bắt kịp → bad ending.
[RequireComponent(typeof(Collider))]
public class Room4ChaseSequence : MonoBehaviour
{
    [Header("Demon")]
    [Tooltip("GameObject con quỷ ở hành lang (ban đầu inactive)")]
    public GameObject corridorDemon;
    [Tooltip("Vận tốc đuổi của quỷ (m/s)")]
    public float demonSpeed = 4.5f;
    [Tooltip("Khoảng cách bắt player (mét)")]
    public float catchDistance = 1.5f;

    [Header("Audio")]
    public AudioSource chaseAudioSource;
    [Tooltip("Nhạc chase căng thẳng")]
    public AudioClip chaseMusic;

    [Header("Exit")]
    [Tooltip("Trigger Collider ở cửa cuối hành lang → advance Room 5")]
    public Collider exitTrigger;

    private bool chaseActive = false;
    private bool triggered = false;
    private Transform playerTransform;
    private NavMeshAgent agent;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        if (corridorDemon != null) corridorDemon.SetActive(false);
        if (exitTrigger != null) exitTrigger.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;
        playerTransform = other.transform;
        StartCoroutine(BeginChase());
    }

    private IEnumerator BeginChase()
    {
        // Ngắn ngủi để player tiến vào trước
        yield return new WaitForSeconds(0.5f);

        // Kích hoạt quỷ
        if (corridorDemon != null)
        {
            corridorDemon.SetActive(true);
            agent = corridorDemon.GetComponent<NavMeshAgent>();
            if (agent != null) agent.speed = demonSpeed;
        }

        // Phát nhạc chase
        if (chaseAudioSource != null && chaseMusic != null)
        {
            chaseAudioSource.clip = chaseMusic;
            chaseAudioSource.loop = true;
            chaseAudioSource.Play();
        }

        chaseActive = true;

        // Mở trigger exit
        if (exitTrigger != null) exitTrigger.enabled = true;
    }

    private void Update()
    {
        if (!chaseActive || playerTransform == null || corridorDemon == null) return;

        // Di chuyển quỷ về phía player
        if (agent != null)
            agent.SetDestination(playerTransform.position);

        // Kiểm tra bắt kịp
        float dist = Vector3.Distance(corridorDemon.transform.position, playerTransform.position);
        if (dist <= catchDistance)
        {
            chaseActive = false;
            RoomManager.Instance?.TriggerBadEnding("caught_corridor");
        }
    }

    // Gọi hàm này từ trigger exit (dùng Room4ExitTrigger script hoặc gắn event)
    public void OnPlayerReachedExit()
    {
        if (!chaseActive) return;
        chaseActive = false;

        if (chaseAudioSource != null) chaseAudioSource.Stop();
        if (corridorDemon != null) corridorDemon.SetActive(false);

        RoomManager.Instance?.EnterRoom(RoomManager.RoomState.Room5);
    }
}
