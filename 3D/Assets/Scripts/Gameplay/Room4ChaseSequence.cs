using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Gắn script này vào trigger collider ở đầu hành lang Room 4.
/// Khi player bước vào: quỷ xuất hiện đằng sau và đuổi về phía trước.
/// Player chạy đến cửa cuối → Room 5.
/// Bị bắt kịp → animation ATTACK + camera zoom → bad ending.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Room4ChaseSequence : MonoBehaviour
{
    // ─── Demon ──────────────────────────────────────────────────
    [Header("Demon")]
    [Tooltip("GameObject quỷ hành lang (ban đầu inactive)")]
    public GameObject corridorDemon;
    [Tooltip("Vận tốc đuổi (m/s)")]
    public float demonSpeed = 4.5f;
    [Tooltip("Khoảng cách bắt player (mét)")]
    public float catchDistance = 1.5f;

    // ─── Audio ──────────────────────────────────────────────────
    [Header("Audio")]
    public AudioSource chaseAudioSource;
    [Tooltip("Nhạc chase căng thẳng")]
    public AudioClip chaseMusic;

    // ─── Exit ───────────────────────────────────────────────────
    [Header("Exit")]
    [Tooltip("Trigger Collider ở cửa cuối hành lang")]
    public Collider exitTrigger;

    // ─── Attack Sequence ────────────────────────────────────────
    [Header("Attack Sequence")]
    [Tooltip("Giây chờ animation ATTACK trước khi hiện game over")]
    public float attackAnimDuration = 1.5f;
    [Tooltip("Giây camera zoom vào quỷ")]
    public float cameraZoomTime = 0.4f;
    [Tooltip("Khoảng cách camera tính từ quỷ khi zoom (mét)")]
    public float cameraZoomDistance = 2f;
    [Tooltip("Script điều khiển nhân vật — sẽ bị tắt trong lúc attack sequence. Để trống nếu không cần.")]
    public MonoBehaviour playerController;

    // ─── Private ────────────────────────────────────────────────
    private bool         chaseActive    = false;
    private bool         triggered      = false;
    private Transform    playerTransform;
    private NavMeshAgent agent;
    private Animator     demonAnimator;

    // Animator param names — phải khớp với mons_anim.controller
    private const string P_Run       = "is_idie_to_running";
    private const string P_WalkToRun = "is_walk_to_running";
    private const string P_AtkToRun  = "is_attack_to_running";

    // ═══════════════════════════════════════════════════════════
    // UNITY LIFECYCLE
    // ═══════════════════════════════════════════════════════════

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        if (corridorDemon != null) corridorDemon.SetActive(false);
        if (exitTrigger   != null) exitTrigger.enabled = false;
    }

    private void Start()
    {
        if (playerController == null)
        {
            playerController = FindObjectOfType<FirstPersonController>();
            if (playerController != null)
            {
                Debug.Log("[Room4ChaseSequence] Automatically assigned playerController dynamically!");
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // TRIGGER — player bước vào hành lang
    // ═══════════════════════════════════════════════════════════

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered       = true;
        playerTransform = other.transform;
        StartCoroutine(BeginChase());
    }

    private IEnumerator BeginChase()
    {
        yield return new WaitForSeconds(0.5f);

        if (corridorDemon != null)
        {
            corridorDemon.SetActive(true);

            agent         = corridorDemon.GetComponent<NavMeshAgent>();
            demonAnimator = corridorDemon.GetComponent<Animator>();

            // DemonController.Awake() ẩn renderer và tắt agent — ghi đè lại
            var dc = corridorDemon.GetComponent<DemonController>();
            if (dc != null)
                dc.ForceActivate();
            else
            {
                // Fallback: bật thủ công nếu không có DemonController
                if (agent != null) agent.enabled = true;
                foreach (var r in corridorDemon.GetComponentsInChildren<Renderer>())
                    r.enabled = true;
            }

            if (agent != null) agent.speed = demonSpeed;

            // Bật animation RUN
            SetDemonRun(true);
        }

        // Phát nhạc chase
        if (chaseAudioSource != null && chaseMusic != null)
        {
            chaseAudioSource.clip  = chaseMusic;
            chaseAudioSource.loop  = true;
            chaseAudioSource.Play();
        }

        chaseActive = true;
        if (exitTrigger != null) exitTrigger.enabled = true;
    }

    // ═══════════════════════════════════════════════════════════
    // UPDATE — kiểm tra bắt kịp
    // ═══════════════════════════════════════════════════════════

    private void Update()
    {
        if (!chaseActive || playerTransform == null || corridorDemon == null) return;

        if (agent != null)
            agent.SetDestination(playerTransform.position);

        float dist = Vector3.Distance(corridorDemon.transform.position, playerTransform.position);
        if (dist <= catchDistance)
        {
            chaseActive = false;
            StartCoroutine(AttackAndBadEnding());
        }
    }

    // ═══════════════════════════════════════════════════════════
    // ATTACK SEQUENCE — bắt được player
    // ═══════════════════════════════════════════════════════════

    private IEnumerator AttackAndBadEnding()
    {
        // Dừng quỷ di chuyển
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled   = false;
        }

        // Tắt điều khiển nhân vật (nếu có gán)
        if (playerController != null) playerController.enabled = false;

        // Chuyển từ RUN → ATTACK
        SetDemonRun(false);
        if (demonAnimator != null) demonAnimator.CrossFade("ATTACK", 0.25f);

        // Camera zoom vào quỷ
        yield return StartCoroutine(ZoomCameraTowardDemon());

        // Chờ animation ATTACK chạy xong
        yield return new WaitForSeconds(attackAnimDuration);

        if (chaseAudioSource != null) chaseAudioSource.Stop();
        RoomManager.Instance?.TriggerBadEnding("caught_corridor");
    }

    private IEnumerator ZoomCameraTowardDemon()
    {
        Camera cam = Camera.main;
        if (cam == null || corridorDemon == null) yield break;

        Transform demonT  = corridorDemon.transform;
        // Đứng giữa camera hiện tại và quỷ, nhìn về phía quỷ
        Vector3 dir       = (cam.transform.position - demonT.position).normalized;
        Vector3 targetPos = demonT.position + dir * cameraZoomDistance;
        Quaternion targetRot = Quaternion.LookRotation(demonT.position - targetPos, Vector3.up);

        Vector3    startPos = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;
        float elapsed = 0f;

        while (elapsed < cameraZoomTime)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.SmoothStep(0f, 1f, elapsed / cameraZoomTime);
            cam.transform.position = Vector3.Lerp(startPos, targetPos, t);
            cam.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // EXIT — player chạy thoát thành công
    // ═══════════════════════════════════════════════════════════

    /// <summary>Gọi từ Room4ExitTrigger khi player chạm cửa cuối.</summary>
    public void OnPlayerReachedExit()
    {
        if (!chaseActive) return;
        chaseActive = false;

        if (chaseAudioSource != null) chaseAudioSource.Stop();

        SetDemonRun(false);
        if (corridorDemon != null) corridorDemon.SetActive(false);

        RoomManager.Instance?.EnterRoom(RoomManager.RoomState.Room5);
    }

    // ═══════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════

    private void SetDemonRun(bool run)
    {
        if (demonAnimator == null) return;
        demonAnimator.SetBool(P_Run,       run);
        demonAnimator.SetBool(P_WalkToRun, run); // ngăn RUN→WALK auto-exit khi false
        demonAnimator.SetBool(P_AtkToRun,  run); // ngăn RUN→ATTACK auto-exit khi false
    }
}
