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
    private float stuckTimer        = 0f; // Cơ chế gỡ kẹt tự động

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

        // Cải thiện bán kính lách qua các góc và cửa hẹp (Self-Healing)
        agent.radius = 0.2f;

        SetVisible(false);
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        // Tự động kích hoạt quỷ nếu chạy test trực tiếp Scene trong Editor (khi không có RoomManager)
        if (RoomManager.Instance == null)
        {
            Debug.Log("[FloorDemonAI] RoomManager is missing (direct scene testing). Automatically activating!");
            StartCoroutine(ActivateAfterDelay());
        }
    }

    private void OnEnable()  => RoomManager.OnRoomEntered += HandleRoomEntered;
    private void OnDisable() => RoomManager.OnRoomEntered -= HandleRoomEntered;

    private void HandleRoomEntered(RoomManager.RoomState room)
    {
        if (room == RoomManager.RoomState.Room5)
        {
            if (CurrentState != State.Inactive) return; // Tránh kích hoạt đè khi sự kiện bị phát trùng lặp
            StartCoroutine(ActivateAfterDelay());
        }
    }

    private IEnumerator ActivateAfterDelay()
    {
        yield return new WaitForSeconds(startDelay);

        // Self-healing: Warp to closest NavMesh point if not perfectly on NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 10.0f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
        }
        else
        {
            Debug.LogError($"[FloorDemonAI] {gameObject.name} is spawn-stuck off NavMesh! Please bake the NavMesh in LevelTst or adjust the spawn coordinates.");
        }

        agent.enabled = true;

        if (agent.enabled && !agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out hit, 15.0f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
        }

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
        if (agent != null && agent.enabled && agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance < 0.5f)
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

            // Đuổi theo vị trí trễ 1 giây (-1s) từ PlayerTracker nếu có
            Vector3 targetPos = PlayerTracker.Instance != null
                ? PlayerTracker.Instance.GetDelayedPlayerPosition(1.0f)
                : player.position;
            SafeSetDestination(targetPos);

            // Bộ gỡ kẹt tối tân (Stuck Resolver) & Tự động vượt cửa (NavMesh Bridge Warp) - Self-Healing
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                if (agent.velocity.sqrMagnitude < 0.04f)
                {
                    stuckTimer += Time.deltaTime;
                    if (stuckTimer > 0.6f) // Bị kẹt quá 0.6 giây
                    {
                        float distToPlayer = Vector3.Distance(transform.position, player.position);
                        if (distToPlayer < 6f)
                        {
                            // Thử tìm đường đi qua khe cửa bị đứt đoạn NavMesh bằng cách dò điểm NavMesh ở phía đối diện
                            Vector3 dirToPlayer = (player.position - transform.position).normalized;
                            dirToPlayer.y = 0; // Chỉ tính toán trên mặt phẳng ngang
                            dirToPlayer.Normalize();

                            // Dò tìm điểm NavMesh hợp lệ ở phía trước (khoảng 1.5m đến 3.0m)
                            bool bridged = false;
                            for (float checkDist = 1.5f; checkDist <= 3.0f; checkDist += 0.5f)
                            {
                                Vector3 testPos = transform.position + dirToPlayer * checkDist;
                                NavMeshHit bridgeHit;
                                // Tìm điểm NavMesh trong bán kính 1.5m quanh điểm test
                                if (NavMesh.SamplePosition(testPos, out bridgeHit, 1.5f, NavMesh.AllAreas))
                                {
                                    // Đảm bảo điểm mới không quá sát điểm hiện tại
                                    if (Vector3.Distance(bridgeHit.position, transform.position) > 0.8f)
                                    {
                                        agent.Warp(bridgeHit.position);
                                        stuckTimer = 0f;
                                        bridged = true;
                                        Debug.Log($"[FloorDemonAI] Đã tự động vượt qua khe cửa (NavMesh Bridge Warp) tới: {bridgeHit.position}");
                                        break;
                                    }
                                }
                            }

                            if (bridged) return;
                        }

                        // Fallback: Đẩy nhẹ dọc theo hướng đi tối ưu của đường dẫn NavMesh (desiredVelocity)
                        Vector3 nudgeDir = agent.desiredVelocity.normalized;
                        if (nudgeDir.sqrMagnitude > 0.001f)
                        {
                            transform.position += nudgeDir * Time.deltaTime * chaseSpeed * 0.5f;
                        }
                    }
                }
                else
                {
                    stuckTimer = 0f;
                }
            }

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
        if (agent != null && agent.enabled && agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance < 0.5f)
            SafeSetStopped(true);

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
        SafeSetStopped(false);
        if (agent != null && agent.enabled) agent.speed = patrolSpeed;
        SetAnimRun(false);
        SetAnimWalk(true);
        AdvanceWaypoint();
    }

    public void AlertDemon(Vector3 targetPos)
    {
        if (CurrentState == State.Inactive || CurrentState == State.Vanishing) return;
        lastKnownPos = targetPos;
        EnterChasing();
    }

    private void EnterChasing()
    {
        CurrentState      = State.Chasing;
        loseFloorTimer    = 0f;
        isWaitingAtPoint  = false;
        SafeSetStopped(false);
        if (agent != null && agent.enabled) agent.speed = chaseSpeed;
        SetAnimRun(true);
    }

    private void EnterSearching()
    {
        CurrentState    = State.Searching;
        searchTimer     = 0f;
        SafeSetStopped(false);
        if (agent != null && agent.enabled) agent.speed = patrolSpeed;
        SetAnimRun(false);
        SetAnimWalk(true);
        SafeSetDestination(lastKnownPos);
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
            SafeSetDestination(patrolWaypoints[waypointIndex].position);
        }
        else
        {
            // Nếu không gán waypoint cố định, quỷ sẽ tự chọn điểm hay ghé thăm của người chơi trên tầng đó để tuần tra
            Vector3 learnedTarget;
            if (PlayerTracker.Instance != null && PlayerTracker.Instance.TryGetFrequentPatrolTarget(out learnedTarget))
            {
                // Chỉ đi tuần tra điểm nóng nếu điểm đó thuộc về tầng (Floor) của quỷ
                if (learnedTarget.y >= floorYMin && learnedTarget.y <= floorYMax)
                {
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(learnedTarget, out hit, 4f, NavMesh.AllAreas))
                    {
                        SafeSetDestination(hit.position);
                        Debug.Log($"[FloorDemonAI] Dynamic patrol targeting player's learned hotspot: {hit.position}");
                        return;
                    }
                }
            }

            // Fallback: Lang thang ngẫu nhiên quanh spawn, giữ nguyên Y
            Vector3 randomDir = Random.insideUnitSphere * wanderRadius;
            randomDir    += spawnPosition;
            randomDir.y   = spawnPosition.y;

            NavMeshHit hit2;
            if (NavMesh.SamplePosition(randomDir, out hit2, wanderRadius, NavMesh.AllAreas))
                SafeSetDestination(hit2.position);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // CATCH PLAYER
    // ═══════════════════════════════════════════════════════════════

    private IEnumerator CatchPlayer()
    {
        if (CurrentState == State.Vanishing) yield break;
        CurrentState    = State.Vanishing;
        SafeSetStopped(true);
        agent.enabled   = false;

        // Đóng băng người chơi hoàn toàn (Self-Healing)
        FirstPersonController.Instance?.FreezePlayer();

        SetAnimRun(false);
        if (animator != null) animator.CrossFade("ATTACK", 0.25f);

        // --- CAMERA ZOOM VÀO MẶT QUỶ KHI TẤN CÔNG (Jumpscare Camera) ---
        yield return StartCoroutine(ZoomCameraTowardSelf());

        yield return new WaitForSeconds(attackAnimDuration);
        RoomManager.Instance?.TriggerBadEnding("caught_room5_floor");
    }

    private IEnumerator ZoomCameraTowardSelf()
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        // ── Bước 1: Tách camera khỏi rig người chơi để parent không ghi đè animation ──
        cam.transform.SetParent(null, worldPositionStays: true);

        // ── Bước 2: Tìm vị trí mặt quỷ (không cần Humanoid rig) ──
        Vector3 facePosition = FindHeadPosition();

        // ── Bước 3: Camera đứng trước mặt quỷ, nhìn ngược vào quỷ ──
        Vector3 demonForward = transform.forward;
        demonForward.y = 0f;
        if (demonForward.sqrMagnitude < 0.001f) demonForward = Vector3.forward;
        demonForward.Normalize();

        float zoomDistance = 1.2f;
        float zoomDuration = 0.45f;

        Vector3 rawTarget = facePosition + demonForward * zoomDistance;
        // ── Bước 4: Raycast chống xưỳng tường ──
        Vector3    targetPos = SafeTargetPos(facePosition, rawTarget);
        Quaternion targetRot = Quaternion.LookRotation(facePosition - targetPos, Vector3.up);

        Vector3    startPos = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;
        float elapsed = 0f;

        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / zoomDuration);
            cam.transform.position = Vector3.Lerp(startPos, targetPos, t);
            cam.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        // Snap chính xác vào đích
        cam.transform.position = targetPos;
        cam.transform.rotation = targetRot;
    }

    /// <summary>
    /// Tìm vị trí đầu / mặt quỷ trên bất kỳ rig nào (Generic, Legacy, không rig).
    /// 1) Child tên "head" → 2) Renderer.bounds đỉnh mesh → 3) +1.7m fallback.
    /// </summary>
    private Vector3 FindHeadPosition()
    {
        // 1️⃣ Tìm child có tên chứa "head"
        Transform headBone = FindChildByName(transform, "head");
        if (headBone != null)
            return headBone.position;

        // 2️⃣ Renderer.bounds đỉnh mesh
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds combined = renderers[0].bounds;
            foreach (Renderer r in renderers)
                combined.Encapsulate(r.bounds);
            float headY = combined.max.y - combined.size.y * 0.1f;
            return new Vector3(transform.position.x, headY, transform.position.z);
        }

        // 3️⃣ Fallback cứng
        return transform.position + Vector3.up * 1.7f;
    }

    private static Transform FindChildByName(Transform parent, string keyword)
    {
        foreach (Transform child in parent)
        {
            if (child.name.ToLower().Contains(keyword))
                return child;
            Transform found = FindChildByName(child, keyword);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>
    /// Raycast từ <paramref name="from"/> đến <paramref name="to"/>.
    /// Nếu có tường chắn, trả về vị trí ngay trước bề mặt tường (không bị xưỳng).
    /// </summary>
    private static Vector3 SafeTargetPos(Vector3 from, Vector3 to)
    {
        Vector3 dir     = to - from;
        float   maxDist = dir.magnitude;
        if (Physics.Raycast(from, dir.normalized, out RaycastHit hit, maxDist,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return hit.point + hit.normal * 0.12f;
        }
        return to;
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

    public void OnLightHit(float deltaTime, bool forceFlinch = false)
    {
        if (CurrentState == State.Vanishing) return;

        lastLightHitTime  = Time.time;
        lightExposure    += deltaTime;

        // Flinch animation mỗi lần bị chiếu (nếu đang chase hoặc bị ép flinch khi zoom)
        if ((CurrentState == State.Chasing || forceFlinch) && !isDamageFlinching)
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
        FlashlightController fc = FindAnyObjectByType<FlashlightController>();
        fc?.StartCooldown();

        SetVisible(false);
        lightExposure    = 0f;
        lastLightHitTime = -100f;
        if (hasDead && animator != null) animator.SetBool(AP.Dead, false);

        // Hồi sinh ở spawn sau khoảng delay dài
        yield return new WaitForSeconds(Random.Range(10f, 18f));

        // Self-healing: Find closest NavMesh point to original spawnPosition
        Vector3 targetSpawn = spawnPosition;
        NavMeshHit spawnHit;
        if (NavMesh.SamplePosition(spawnPosition, out spawnHit, 10.0f, NavMesh.AllAreas))
        {
            targetSpawn = spawnHit.position;
        }

        transform.position = targetSpawn;
        agent.enabled      = true;
        
        if (agent.isOnNavMesh)
        {
            agent.Warp(targetSpawn);
        }
        else
        {
            Debug.LogWarning($"[FloorDemonAI] Spawn position {spawnPosition} is still not on NavMesh. Check if scene NavMesh is baked!");
            agent.enabled = false;
        }

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

        // Anti-running-in-place guard: if the agent cannot move (e.g. NavMesh not baked or agent off NavMesh), force idle animation
        if (run && (agent == null || !agent.enabled || !agent.isOnNavMesh))
        {
            run = false;
        }

        animator.SetBool(AP.Run,         run);
        animator.SetBool(AP.WalkToRun,   run);
        animator.SetBool(AP.AttackToRun, run);
        
        // Tắt hoạt ảnh đi bộ khi đang chạy
        animator.SetBool(AP.Walk, !run);
    }

    private void SetAnimWalk(bool walk)
    {
        if (animator == null) return;

        // Anti-running-in-place guard: if the agent cannot move (e.g. NavMesh not baked or agent off NavMesh), force idle animation
        if (walk && (agent == null || !agent.enabled || !agent.isOnNavMesh))
        {
            walk = false;
        }

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

    private void SafeSetDestination(Vector3 target)
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.SetDestination(target);
        }
    }

    private void SafeSetStopped(bool stopped)
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = stopped;
        }
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
