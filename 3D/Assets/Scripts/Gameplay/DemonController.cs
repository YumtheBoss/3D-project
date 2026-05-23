using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// AI quỷ dùng cho Room 5 (và làm prefab chung với Room 4).
/// • Idle  : ẩn/hiện ngẫu nhiên, tối đa 2 con active.
/// • Chasing: đuổi player (NavMeshAgent), tối đa 1 con đuổi.
/// • Vanishing: tan biến sau 3s bị chiếu đèn → khóa đèn 5s.
///
/// Animator parameters (khớp với mons_anim.controller):
///   is_idie_to_walk     (Bool) – IDLE → WALK
///   is_idie_to_running  (Bool) – IDLE → RUN
///   is_idie_to_attack   (Bool) – IDLE → ATTACK
///   is_walk_to_running  (Bool) – WALK → RUN  / giữ RUN khỏi tự thoát về WALK
///   is_attack_to_running(Bool) – ATTACK → RUN / giữ RUN khỏi tự thoát về ATTACK
///   is_damage_to_running(Bool) – RUN → DAMAGE (entry) + DAMAGE → RUN (exit)
///   isDead              (Bool) – AnyState → DEATH
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class DemonController : MonoBehaviour
{
    public enum DemonState { Idle, Chasing, Vanishing }

    // ─── Animator parameter names ───────────────────────────────
    private static class AP
    {
        public const string Walk          = "is_idie_to_walk";
        public const string Run           = "is_idie_to_running";
        public const string Attack        = "is_idie_to_attack";
        public const string WalkToRun     = "is_walk_to_running";
        public const string AttackToRun   = "is_attack_to_running";
        public const string DamageFromRun = "is_damage_to_running";
        public const string Dead          = "isDead";
    }

    // ─── Inspector ──────────────────────────────────────────────
    [Header("Di chuyển")]
    public float chaseSpeed    = 3.5f;
    public float detectionRange = 10f;
    public float catchDistance  = 1.5f;

    [Header("Ánh sáng")]
    [Tooltip("Giây chiếu đèn liên tục để quỷ tan biến")]
    public float lightVanishTime = 3f;

    [Header("Idle")]
    public float minIdleVisibleTime = 1f;
    public float maxIdleVisibleTime = 3f;
    public float minIdleHiddenTime  = 2f;
    public float maxIdleHiddenTime  = 6f;
    [Tooltip("Trì hoãn ban đầu (giây) để tránh spawn cùng lúc")]
    public float startDelay = 0f;

    [Header("Animation")]
    public Animator animator;
    [Tooltip("Giây chờ animation ATTACK trước khi hiện game over (Room 5)")]
    public float attackAnimDuration = 1.5f;
    [Tooltip("Giây flinch khi bị đèn pin chiếu (mỗi lần)")]
    public float damageFlinchDuration = 0.6f;

    // ─── Static: giới hạn demon ─────────────────────────────────
    private static int activeCount  = 0;
    private static int chasingCount = 0;
    private const  int maxActive    = 2;
    private const  int maxChasing   = 1;

    public static void ResetAll() { activeCount = 0; chasingCount = 0; }

    // ─── State ──────────────────────────────────────────────────
    public DemonState CurrentState { get; private set; } = DemonState.Idle;

    private NavMeshAgent  agent;
    private Transform     playerTransform;
    private Renderer[]    renderers;
    private Coroutine     idleCoroutine;

    private float lightExposureAccum = 0f;
    private float lastLightHitTime   = -100f;
    private bool  isDamageFlinching  = false;

    // ═══════════════════════════════════════════════════════════
    // UNITY LIFECYCLE
    // ═══════════════════════════════════════════════════════════

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed   = chaseSpeed;
        agent.enabled = false;
        renderers     = GetComponentsInChildren<Renderer>();
        SetVisible(false);
    }

    private void OnEnable()  => RoomManager.OnRoomEntered += HandleRoomEntered;
    private void OnDisable() => RoomManager.OnRoomEntered -= HandleRoomEntered;

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;
    }

    private void OnDestroy()
    {
        if (CurrentState == DemonState.Chasing) chasingCount--;
        if (activeCount > 0) activeCount--;
        chasingCount = Mathf.Max(0, chasingCount);
    }

    // ═══════════════════════════════════════════════════════════
    // PUBLIC API (dùng bởi Room4ChaseSequence)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Force-kích hoạt để Room4ChaseSequence dùng cùng prefab.
    /// Bật agent + hiển thị renderer (override trạng thái ẩn của Awake).
    /// </summary>
    public void ForceActivate()
    {
        agent.enabled = true;
        SetVisible(true);
    }

    // ═══════════════════════════════════════════════════════════
    // ROOM EVENT
    // ═══════════════════════════════════════════════════════════

    private void HandleRoomEntered(RoomManager.RoomState room)
    {
        if (room != RoomManager.RoomState.Room5) return;
        if (activeCount >= maxActive) return;
        activeCount++;
        idleCoroutine = StartCoroutine(IdleCycle());
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
            SetAnimWalk(true);

            float elapsed         = 0f;
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

            SetAnimWalk(false);
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
        SetAnimRun(true);
    }

    private void EndChase()
    {
        if (CurrentState == DemonState.Chasing) chasingCount--;
        agent.enabled = false;
        SetAnimRun(false);
        SetAnimWalk(false);
        CurrentState  = DemonState.Idle;
        idleCoroutine = StartCoroutine(IdleCycle());
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
                StartCoroutine(CatchPlayer());
                return;
            }
        }

        // Decay light exposure khi không bị chiếu frame này
        if (Time.time - lastLightHitTime > Time.deltaTime * 1.5f)
        {
            lightExposureAccum -= Time.deltaTime;
            if (lightExposureAccum < 0f) lightExposureAccum = 0f;
        }
    }

    // ─── Bắt player (Room 5) ────────────────────────────────────
    private IEnumerator CatchPlayer()
    {
        if (CurrentState == DemonState.Chasing) chasingCount--;
        CurrentState  = DemonState.Vanishing; // khóa trigger tiếp theo
        agent.enabled = false;

        SetAnimRun(false);
        if (animator != null) animator.CrossFade("ATTACK", 0.25f);

        yield return new WaitForSeconds(attackAnimDuration);
        RoomManager.Instance?.TriggerBadEnding("caught_room5");
    }

    // ═══════════════════════════════════════════════════════════
    // LIGHT API — gọi bởi FlashlightController.ScanForDemons()
    // ═══════════════════════════════════════════════════════════

    public void OnLightHit(float deltaTime)
    {
        if (CurrentState == DemonState.Vanishing) return;

        lastLightHitTime      = Time.time;
        lightExposureAccum   += deltaTime;

        // Flinch animation mỗi lần bị chiếu (chỉ khi đang chase, không flinch chồng)
        if (CurrentState == DemonState.Chasing && !isDamageFlinching)
            StartCoroutine(DamageFlinch());

        if (lightExposureAccum >= lightVanishTime)
            StartCoroutine(Vanish());
    }

    private IEnumerator DamageFlinch()
    {
        isDamageFlinching = true;
        if (animator != null) animator.SetBool(AP.DamageFromRun, true);
        yield return new WaitForSeconds(damageFlinchDuration);
        // Khôi phục RUN sau flinch (DAMAGE→RUN dùng cùng param)
        if (animator != null) animator.SetBool(AP.DamageFromRun, false);
        isDamageFlinching = false;
    }

    // ═══════════════════════════════════════════════════════════
    // VANISH
    // ═══════════════════════════════════════════════════════════

    private IEnumerator Vanish()
    {
        if (CurrentState == DemonState.Vanishing) yield break;
        if (CurrentState == DemonState.Chasing) chasingCount--;
        CurrentState     = DemonState.Vanishing;
        agent.enabled    = false;
        isDamageFlinching = false;

        SetAnimRun(false);
        SetAnimWalk(false);

        bool hasDead = animator != null
            && HasParam(animator, AP.Dead, AnimatorControllerParameterType.Bool);

        if (hasDead)
        {
            animator.SetBool(AP.Dead, true);
            yield return new WaitForSeconds(0.8f);
        }
        else
        {
            // Fallback: thu nhỏ về 0
            float   t    = 0f;
            Vector3 orig = transform.localScale;
            while (t < 1f)
            {
                t += Time.deltaTime * 2.5f;
                transform.localScale = Vector3.Lerp(orig, Vector3.zero, t);
                yield return null;
            }
            transform.localScale = orig;
        }

        // Thông báo FlashlightController cooldown 5s
        FlashlightController fc = FindObjectOfType<FlashlightController>();
        fc?.StartCooldown();

        SetVisible(false);
        lightExposureAccum = 0f;
        lastLightHitTime   = -100f;
        if (hasDead && animator != null) animator.SetBool(AP.Dead, false);

        yield return new WaitForSeconds(Random.Range(minIdleHiddenTime, maxIdleHiddenTime));

        CurrentState  = DemonState.Idle;
        idleCoroutine = StartCoroutine(IdleCycle());
    }

    // ═══════════════════════════════════════════════════════════
    // COLLISION — bắt player qua trigger
    // ═══════════════════════════════════════════════════════════

    private void OnTriggerEnter(Collider other)
    {
        if (CurrentState == DemonState.Vanishing) return;
        if (!other.CompareTag("Player")) return;
        StartCoroutine(CatchPlayer());
    }

    // ═══════════════════════════════════════════════════════════
    // ANIMATION HELPERS
    // ═══════════════════════════════════════════════════════════

    /// <summary>Bật/tắt animation RUN và các guard params tránh auto-exit.</summary>
    private void SetAnimRun(bool run)
    {
        if (animator == null) return;
        animator.SetBool(AP.Run,         run);
        animator.SetBool(AP.WalkToRun,   run); // ngăn RUN→WALK auto-fire khi false
        animator.SetBool(AP.AttackToRun, run); // ngăn RUN→ATTACK auto-fire khi false
        if (!run) animator.SetBool(AP.Walk, false);
    }

    /// <summary>Bật/tắt animation WALK (patrol idle).</summary>
    private void SetAnimWalk(bool walk)
    {
        if (animator == null) return;
        animator.SetBool(AP.Walk, walk);
        if (!walk) animator.SetBool(AP.Run, false);
    }

    private void SetVisible(bool visible)
    {
        foreach (Renderer r in renderers)
            if (r != null) r.enabled = visible;
    }

    private static bool HasParam(Animator anim, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in anim.parameters)
            if (p.name == name && p.type == type) return true;
        return false;
    }
}
