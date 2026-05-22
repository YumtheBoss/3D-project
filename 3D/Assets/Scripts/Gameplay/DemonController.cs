using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// AI quỷ dùng cho Room 5.
/// • Idle: ẩn/hiện ngẫu nhiên, tối đa 2 con active cùng lúc.
/// • Chasing: đuổi player (NavMeshAgent), chỉ 1 con đuổi cùng lúc.
/// • Vanishing: tan biến khi bị chiếu đèn 3s liên tục → khóa đèn 5s.
/// FlashlightController (AnomalySystem) tự quét và gọi OnLightHit() mỗi frame.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class DemonController : MonoBehaviour
{
    public enum DemonState { Idle, Chasing, Vanishing }

    [Header("Di chuyển")]
    public float chaseSpeed = 3.5f;
    public float detectionRange = 10f;
    public float catchDistance = 1.5f;

    [Header("Ánh sáng")]
    [Tooltip("Giây chiếu đèn liên tục để quỷ tan biến")]
    public float lightVanishTime = 3f;

    [Header("Idle")]
    public float minIdleVisibleTime = 1f;
    public float maxIdleVisibleTime = 3f;
    public float minIdleHiddenTime = 2f;
    public float maxIdleHiddenTime = 6f;
    [Tooltip("Trì hoãn ban đầu ngẫu nhiên để tránh spawn cùng lúc")]
    public float startDelay = 0f;

    [Header("Animation")]
    public Animator animator;
    [Tooltip("Tên trigger để quỷ tan biến. Ví dụ: 'Death' nếu AnimatorController có state DEATH. Để trống → dùng hiệu ứng thu nhỏ về 0.")]
    public string vanishTriggerName = "";
    [Tooltip("Tên bool parameter để bật/tắt animation đi (ví dụ: 'isWalking'). Để trống nếu AnimatorController tự loop.")]
    public string walkBoolParam = "";

    // ─── Static: giới hạn demon active/chasing ─────────────────
    private static int activeCount = 0;
    private static int chasingCount = 0;
    private const int maxActive = 2;
    private const int maxChasing = 1;

    public static void ResetAll()
    {
        activeCount = 0;
        chasingCount = 0;
    }

    // ─── State ─────────────────────────────────────────────────
    public DemonState CurrentState { get; private set; } = DemonState.Idle;

    private NavMeshAgent agent;
    private Transform playerTransform;
    private Renderer[] renderers;
    private Coroutine idleCoroutine;

    // Light exposure tracking
    private float lightExposureAccum = 0f;
    private float lastLightHitTime = -100f;

    // ═══════════════════════════════════════════════════════════
    // UNITY LIFECYCLE
    // ═══════════════════════════════════════════════════════════

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = chaseSpeed;
        agent.enabled = false;
        renderers = GetComponentsInChildren<Renderer>();
        SetVisible(false);
    }

    private void OnEnable()
    {
        RoomManager.OnRoomEntered += HandleRoomEntered;
    }

    private void OnDisable()
    {
        RoomManager.OnRoomEntered -= HandleRoomEntered;
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;
    }

    private void OnDestroy()
    {
        if (CurrentState == DemonState.Chasing) chasingCount--;
        if (activeCount > 0) activeCount--;
        chasingCount = Mathf.Max(0, chasingCount);
    }

    // ═══════════════════════════════════════════════════════════
    // ROOM EVENT
    // ═══════════════════════════════════════════════════════════

    private void HandleRoomEntered(RoomManager.RoomState room)
    {
        if (room == RoomManager.RoomState.Room5)
        {
            if (activeCount >= maxActive) return;
            activeCount++;
            idleCoroutine = StartCoroutine(IdleCycle());
        }
    }

    // ═══════════════════════════════════════════════════════════
    // IDLE CYCLE
    // ═══════════════════════════════════════════════════════════

    private IEnumerator IdleCycle()
    {
        yield return new WaitForSeconds(startDelay + Random.Range(0f, 2f));

        while (CurrentState == DemonState.Idle)
        {
            SetVisible(true);
            float elapsed = 0f;
            float visibleDuration = Random.Range(minIdleVisibleTime, maxIdleVisibleTime);

            while (elapsed < visibleDuration && CurrentState == DemonState.Idle)
            {
                elapsed += Time.deltaTime;

                if (playerTransform != null && chasingCount < maxChasing)
                {
                    float dist = Vector3.Distance(transform.position, playerTransform.position);
                    if (dist <= detectionRange)
                    {
                        BeginChase();
                        yield break;
                    }
                }
                yield return null;
            }

            if (CurrentState != DemonState.Idle) yield break;

            SetVisible(false);
            yield return new WaitForSeconds(Random.Range(minIdleHiddenTime, maxIdleHiddenTime));
        }
    }

    private void BeginChase()
    {
        CurrentState = DemonState.Chasing;
        chasingCount++;
        SetVisible(true);
        agent.enabled = true;
        SetAnimWalk(true);
    }

    private void EndChase()
    {
        if (CurrentState == DemonState.Chasing) chasingCount--;
        agent.enabled = false;
        SetAnimWalk(false);
        CurrentState = DemonState.Idle;
        idleCoroutine = StartCoroutine(IdleCycle());
    }

    private void SetAnimWalk(bool walking)
    {
        if (animator != null && !string.IsNullOrEmpty(walkBoolParam))
            animator.SetBool(walkBoolParam, walking);
    }

    // ═══════════════════════════════════════════════════════════
    // UPDATE
    // ═══════════════════════════════════════════════════════════

    private void Update()
    {
        if (CurrentState == DemonState.Vanishing) return;
        if (playerTransform == null) return;

        if (CurrentState == DemonState.Chasing)
        {
            agent.SetDestination(playerTransform.position);

            if (Vector3.Distance(transform.position, playerTransform.position) <= catchDistance)
            {
                EndChase();
                RoomManager.Instance?.TriggerBadEnding("caught_room5");
                return;
            }
        }

        // Decay light exposure nếu không bị chiếu trong frame này
        if (Time.time - lastLightHitTime > Time.deltaTime * 1.5f)
        {
            lightExposureAccum -= Time.deltaTime;
            if (lightExposureAccum < 0f) lightExposureAccum = 0f;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // LIGHT API — gọi bởi FlashlightController.ScanForDemons()
    // ═══════════════════════════════════════════════════════════

    public void OnLightHit(float deltaTime)
    {
        if (CurrentState == DemonState.Vanishing) return;

        lastLightHitTime = Time.time;
        lightExposureAccum += deltaTime;

        if (lightExposureAccum >= lightVanishTime)
            StartCoroutine(Vanish());
    }

    // ═══════════════════════════════════════════════════════════
    // VANISH
    // ═══════════════════════════════════════════════════════════

    private IEnumerator Vanish()
    {
        if (CurrentState == DemonState.Vanishing) yield break;

        if (CurrentState == DemonState.Chasing) chasingCount--;
        CurrentState = DemonState.Vanishing;
        agent.enabled = false;

        bool useAnimator = animator != null && !string.IsNullOrEmpty(vanishTriggerName)
                           && HasAnimatorParameter(animator, vanishTriggerName, AnimatorControllerParameterType.Trigger);

        if (useAnimator)
        {
            SetAnimWalk(false);
            animator.SetTrigger(vanishTriggerName);
            yield return new WaitForSeconds(0.8f);
        }
        else
        {
            // Fallback: thu nhỏ về 0 (dùng khi AnimatorController không có trigger vanish)
            SetAnimWalk(false);
            float t = 0f;
            Vector3 orig = transform.localScale;
            while (t < 1f)
            {
                t += Time.deltaTime * 2.5f;
                transform.localScale = Vector3.Lerp(orig, Vector3.zero, t);
                yield return null;
            }
            transform.localScale = orig;
        }

        // Thông báo FlashlightController bắt đầu cooldown 5s
        FlashlightController fc = FindObjectOfType<FlashlightController>();
        fc?.StartCooldown();

        SetVisible(false);
        lightExposureAccum = 0f;
        lastLightHitTime = -100f;

        yield return new WaitForSeconds(Random.Range(minIdleHiddenTime, maxIdleHiddenTime));

        CurrentState = DemonState.Idle;
        idleCoroutine = StartCoroutine(IdleCycle());
    }

    // ═══════════════════════════════════════════════════════════
    // COLLISION — bắt player
    // ═══════════════════════════════════════════════════════════

    private void OnTriggerEnter(Collider other)
    {
        if (CurrentState == DemonState.Vanishing) return;
        if (!other.CompareTag("Player")) return;
        RoomManager.Instance?.TriggerBadEnding("caught_room5");
    }

    private void SetVisible(bool visible)
    {
        foreach (Renderer r in renderers)
            if (r != null) r.enabled = visible;
    }

    private static bool HasAnimatorParameter(Animator anim, string paramName, AnimatorControllerParameterType type)
    {
        foreach (AnimatorControllerParameter p in anim.parameters)
            if (p.name == paramName && p.type == type) return true;
        return false;
    }
}
