using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// AI quỷ chuyên dụng cho Room 5 — map 2 tầng.
///
/// Mỗi instance gán 1 tầng (floorYMin / floorYMax).
/// Quỷ chỉ đuổi player khi player ở CÙNG tầng với nó.
/// Khi player chạy sang tầng khác, quỷ tìm kiếm ngắn rồi quay về tuần tra.
/// Hai con sẽ không bao giờ gặp nhau vì mỗi con bị khóa Y-range riêng.
///
/// States:
///   Inactive   → chờ Room5 event
///   Patrolling → đi tuần tra theo waypoints (hoặc lang thang ngẫu nhiên)
///   Chasing    → đuổi player trên cùng tầng
///   Searching  → player vừa thoát sang tầng khác, tìm ngắn rồi quay về
///   Vanishing  → bị đèn pin tiêu diệt
///
/// Gắn script này lên prefab quỷ Room5 (thay thế hoặc song song với DemonController).
/// FlashlightController sẽ gọi OnLightHit(dt) như cũ.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class FloorDemonAI : MonoBehaviour
{
    public enum State { Inactive, Patrolling, Chasing, Searching, Vanishing }

    // ─── Tầng ──────────────────────────────────────────────────────
    [Header("Tầng (Floor Bounds)")]
    [Tooltip("Y tối thiểu của tầng — quỷ chỉ đuổi khi player.y >= giá trị này")]
    public float floorYMin = 0f;
    [Tooltip("Y tối đa của tầng — quỷ chỉ đuổi khi player.y <= giá trị này")]
    public float floorYMax = 5f;

    // ─── Tuần tra ─────────────────────────────────────────────────
    [Header("Tuần tra")]
    [Tooltip("Các điểm tuần tra trên tầng này. Để trống → lang thang ngẫu nhiên.")]
    public Transform[] patrolWaypoints;
    [Tooltip("Tốc độ tuần tra (m/s)")]
    public float patrolSpeed = 1.4f;
    [Tooltip("Giây đứng tại mỗi waypoint trước khi đi tiếp")]
    public float waypointWaitTime = 1.5f;
    [Tooltip("Bán kính lang thang ngẫu nhiên khi không có waypoint")]
    public float wanderRadius = 8f;

    // ─── Đuổi ────────────────────────────────────────────────────
    [Header("Đuổi")]
    [Tooltip("Tốc độ chạy khi đuổi player (m/s)")]
    public float chaseSpeed = 3.8f;
    [Tooltip("Khoảng cách phát hiện player (mét)")]
    public float detectionRange = 12f;
    [Tooltip("Khoảng cách bắt được player (mét)")]
    public float catchDistance = 1.5f;
    [Tooltip("Giây player ở tầng khác → quỷ vào trạng thái Searching")]
    public float loseFloorTime = 2.5f;
    [Tooltip("Giây tìm kiếm tại vị trí cuối thấy player trước khi quay về tuần tra")]
    public float searchDuration = 4f;

    // ─── Animation ────────────────────────────────────────────────
    [Header("Animation")]
    public Animator animator;
    [Tooltip("Trì hoãn trước khi kích hoạt (giây) — đặt khác nhau cho 2 con")]
    public float startDelay = 0f;
    [Tooltip("Giây chờ animation ATTACK trước khi game over")]
    public float attackAnimDuration = 1.5f;
    [Tooltip("Giây flinch khi bị đèn pin chiếu")]
    public float damageFlinchDuration = 0.6f;

    // ─── Đèn pin ──────────────────────────────────────────────────
    [Header("Đèn pin")]
    [Tooltip("Giây chiếu liên tục để tiêu diệt quỷ")]
    public float lightVanishTime = 3f;

    // ─── Animator params (khớp mons_anim.controller) ──────────────
    private static class AP
    {
        public const string Walk          = "is_idie_to_walk";
        public const string Run           = "is_idie_to_running";
        public const string WalkToRun     = "is_walk_to_running";
        public const string AttackToRun   = "is_attack_to_running";
        public const string DamageFromRun = "is_damage_to_running";
        public const string Dead          = "isDead";
    }

    // ─── Private ──────────────────────────────────────────────────
    public State CurrentState { get; private set; } = State.Inactive;

    private NavMeshAgent agent;
    private Transform    player;
    private Renderer[]   renderers;
    private Vector3      spawnPosition;

    // Patrol
    private int  waypointIndex    = 0;
    private bool isWaitingAtPoint = false;

    // Chasing / Searching
    private float   loseFloorTimer   = 0f;
    private float   searchTimer      = 0f;
    private Vector3 lastKnownPos;

    // Light
    private float lightExposure    = 0f;
    private float lastLightHitTime = -100f;
    private bool  isDamageFlinching = false;

    // ═══════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ═══════════════════════════════════════════════════════════════

    private void Awake()
    {
        agent         = GetComponent<NavMeshAgent>();
        renderers     = GetComponentsInChildren<Renderer>();
        spawnPosition = transform.position;

        agent.speed   = patrolSpeed;
        agent.enabled = false;
        SetVisible(false);
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    private void OnEnable()  => RoomManager.OnRoomEntered += HandleRoomEntered;
    private void OnDisable() => RoomManager.OnRoomEntered -= HandleRoomEntered;

    private void HandleRoomEntered(RoomManager.RoomState room)
    {
        if (room == RoomManager.RoomState.Room5)
            StartCoroutine(ActivateAfterDelay());
    }

    private IEnumerator ActivateAfterDelay()
    {
        yield return new WaitForSeconds(startDelay);
        agent.enabled = true;
        SetVisible(true);
        EnterPatrolling();
    }

    // ═══════════════════════════════════════════════════════════════
    // UPDATE
    // ═══════════════════════════════════════════════════════════════

    private void Update()
    {
        if (CurrentState == State.Inactive || CurrentState == State.Vanishing) return;
        if (player == null) return;

        // Decay light exposure khi không bị chiếu frame này
        if (Time.time - lastLightHitTime > Time.deltaTime * 1.5f)
            lightExposure = Mathf.Max(0f, lightExposure - Time.deltaTime * 0.5f);

        switch (CurrentState)
        {
            case State.Patrolling:
                UpdatePatrolling();
                TryDetectPlayer();
                break;

            case State.Chasing:
                UpdateChasing();
                break;

            case State.Searching:
                UpdateSearching();
                break;
        }
    }

    // ─── Patrolling ───────────────────────────────────────────────

    private void UpdatePatrolling()
    {
        if (isWaitingAtPoint) return;
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
            StartCoroutine(WaitThenAdvanceWaypoint());
    }

    private void TryDetectPlayer()
    {
        if (!IsPlayerOnMyFloor()) return;
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= detectionRange)
            EnterChasing();
    }

    // ─── Chasing ─────────────────────────────────────────────────

    private void UpdateChasing()
    {
        if (IsPlayerOnMyFloor())
        {
            loseFloorTimer   = 0f;
            lastKnownPos     = player.position;
            agent.SetDestination(player.position);

            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= catchDistance)
                StartCoroutine(CatchPlayer());
        }
        else
        {
            // Player đã chạy sang tầng khác
            loseFloorTimer += Time.deltaTime;
            if (loseFloorTimer >= loseFloorTime)
                EnterSearching();
        }
    }

    // ─── Searching ────────────────────────────────────────────────

    private void UpdateSearching()
    {
        searchTimer += Time.deltaTime;

        // Đến được vị trí cuối thấy player → đứng ngó nghiêng
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
            agent.isStopped = true;

        // Hết thời gian tìm kiếm → quay về tuần tra
        if (searchTimer >= searchDuration)
            EnterPatrolling();

        // Nếu player quay lại tầng này trong lúc đang tìm → đuổi ngay
        if (IsPlayerOnMyFloor())
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= detectionRange)
                EnterChasing();
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // STATE TRANSITIONS
    // ═══════════════════════════════════════════════════════════════

    private void EnterPatrolling()
    {
        CurrentState      = State.Patrolling;
        loseFloorTimer    = 0f;
        searchTimer       = 0f;
        isWaitingAtPoint  = false;
        agent.isStopped   = false;
        agent.speed       = patrolSpeed;
        SetAnimRun(false);
        SetAnimWalk(true);
        AdvanceWaypoint();
    }

    private void EnterChasing()
    {
        CurrentState      = State.Chasing;
        loseFloorTimer    = 0f;
        isWaitingAtPoint  = false;
        agent.isStopped   = false;
        agent.speed       = chaseSpeed;
        SetAnimRun(true);
    }

    private void EnterSearching()
    {
        CurrentState    = State.Searching;
        searchTimer     = 0f;
        agent.isStopped = false;
        agent.speed     = patrolSpeed;
        SetAnimRun(false);
        SetAnimWalk(true);
        agent.SetDestination(lastKnownPos);
    }

    // ═══════════════════════════════════════════════════════════════
    // PATROL — waypoints / wander
    // ═══════════════════════════════════════════════════════════════

    private IEnumerator WaitThenAdvanceWaypoint()
    {
        isWaitingAtPoint = true;
        SetAnimWalk(false);

        yield return new WaitForSeconds(waypointWaitTime);

        if (CurrentState != State.Patrolling) { isWaitingAtPoint = false; yield break; }

        SetAnimWalk(true);
        AdvanceWaypoint();
        isWaitingAtPoint = false;
    }

    private void AdvanceWaypoint()
    {
        if (patrolWaypoints != null && patrolWaypoints.Length > 0)
        {
            // Tuần tra theo thứ tự vòng lặp
            waypointIndex = (waypointIndex + 1) % patrolWaypoints.Length;
            agent.SetDestination(patrolWaypoints[waypointIndex].position);
        }
        else
        {
            // Lang thang ngẫu nhiên quanh spawn, giữ nguyên Y
            Vector3 randomDir = Random.insideUnitSphere * wanderRadius;
            randomDir    += spawnPosition;
            randomDir.y   = spawnPosition.y;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDir, out hit, wanderRadius, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // CATCH PLAYER
    // ═══════════════════════════════════════════════════════════════

    private IEnumerator CatchPlayer()
    {
        if (CurrentState == State.Vanishing) yield break;
        CurrentState    = State.Vanishing;
        agent.isStopped = true;
        agent.enabled   = false;

        SetAnimRun(false);
        if (animator != null) animator.CrossFade("ATTACK", 0.25f);

        yield return new WaitForSeconds(attackAnimDuration);
        RoomManager.Instance?.TriggerBadEnding("caught_room5_floor");
    }

    // Fallback qua trigger collider
    private void OnTriggerEnter(Collider other)
    {
        if (CurrentState == State.Vanishing) return;
        if (!other.CompareTag("Player")) return;
        StartCoroutine(CatchPlayer());
    }

    // ═══════════════════════════════════════════════════════════════
    // LIGHT HIT — gọi bởi FlashlightController.ScanForDemons()
    // ═══════════════════════════════════════════════════════════════

    public void OnLightHit(float deltaTime)
    {
        if (CurrentState == State.Vanishing) return;

        lastLightHitTime  = Time.time;
        lightExposure    += deltaTime;

        if (CurrentState == State.Chasing && !isDamageFlinching)
            StartCoroutine(DamageFlinch());

        if (lightExposure >= lightVanishTime)
            StartCoroutine(Vanish());
    }

    private IEnumerator DamageFlinch()
    {
        isDamageFlinching = true;
        if (animator != null) animator.SetBool(AP.DamageFromRun, true);
        yield return new WaitForSeconds(damageFlinchDuration);
        if (animator != null) animator.SetBool(AP.DamageFromRun, false);
        isDamageFlinching = false;
    }

    // ═══════════════════════════════════════════════════════════════
    // VANISH — bị đèn pin tiêu diệt
    // ═══════════════════════════════════════════════════════════════

    private IEnumerator Vanish()
    {
        if (CurrentState == State.Vanishing) yield break;
        CurrentState      = State.Vanishing;
        agent.enabled     = false;
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
            // Fallback: thu nhỏ về 0 rồi phục hồi lại scale
            float t = 0f; Vector3 orig = transform.localScale;
            while (t < 1f)
            {
                t += Time.deltaTime * 2.5f;
                transform.localScale = Vector3.Lerp(orig, Vector3.zero, t);
                yield return null;
            }
            transform.localScale = orig;
        }

        // Kích hoạt cooldown đèn pin 5s
        FlashlightController fc = FindObjectOfType<FlashlightController>();
        fc?.StartCooldown();

        SetVisible(false);
        lightExposure    = 0f;
        lastLightHitTime = -100f;
        if (hasDead && animator != null) animator.SetBool(AP.Dead, false);

        // Hồi sinh ở spawn sau khoảng delay dài
        yield return new WaitForSeconds(Random.Range(10f, 18f));

        transform.position = spawnPosition;
        agent.enabled      = true;
        agent.Warp(spawnPosition);
        SetVisible(true);
        EnterPatrolling();
    }

    // ═══════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════

    private bool IsPlayerOnMyFloor()
    {
        if (player == null) return false;
        float py = player.position.y;
        return py >= floorYMin && py <= floorYMax;
    }

    private void SetAnimRun(bool run)
    {
        if (animator == null) return;
        animator.SetBool(AP.Run,         run);
        animator.SetBool(AP.WalkToRun,   run);
        animator.SetBool(AP.AttackToRun, run);
        if (!run) animator.SetBool(AP.Walk, false);
    }

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

    // Vẽ floor bounds và detection range trong Scene view để dễ chỉnh
    private void OnDrawGizmosSelected()
    {
        // Floor zone
        Vector3 center = new Vector3(transform.position.x, (floorYMin + floorYMax) * 0.5f, transform.position.z);
        Vector3 size   = new Vector3(wanderRadius * 2f, floorYMax - floorYMin, wanderRadius * 2f);
        Gizmos.color   = new Color(1f, 0.3f, 0.1f, 0.12f);
        Gizmos.DrawCube(center, size);
        Gizmos.color   = new Color(1f, 0.3f, 0.1f, 0.7f);
        Gizmos.DrawWireCube(center, size);

        // Detection range
        Gizmos.color = new Color(1f, 0.9f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Catch distance
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, catchDistance);
    }
}
