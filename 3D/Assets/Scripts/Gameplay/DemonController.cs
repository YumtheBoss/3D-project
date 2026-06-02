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
    private float stuckTimer          = 0f; // Cơ chế gỡ kẹt tự động

    // ═══════════════════════════════════════════════════════════
    // UNITY LIFECYCLE
    // ═══════════════════════════════════════════════════════════

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed   = chaseSpeed;
        agent.enabled = false;

        // Cải thiện bán kính lách qua các góc và cửa hẹp (Self-Healing)
        agent.radius = 0.2f;

        renderers     = GetComponentsInChildren<Renderer>();
        SetVisible(false);
    }

    private void OnEnable()  => RoomManager.OnRoomEntered += HandleRoomEntered;
    private void OnDisable() => RoomManager.OnRoomEntered -= HandleRoomEntered;

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;

        // Tự động kích hoạt quỷ nếu chạy test trực tiếp Scene trong Editor (khi không có RoomManager)
        if (RoomManager.Instance == null)
        {
            Debug.Log("[DemonController] RoomManager is missing (direct scene testing). Automatically activating idle cycle!");
            activeCount++;
            idleCoroutine = StartCoroutine(IdleCycle());
        }
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
        // Self-healing: Warp to closest NavMesh point if slightly off NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 10.0f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
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
    }

    // ═══════════════════════════════════════════════════════════
    // ROOM EVENT
    // ═══════════════════════════════════════════════════════════

    private void HandleRoomEntered(RoomManager.RoomState room)
    {
        if (room != RoomManager.RoomState.Room5) return;
        if (idleCoroutine != null) return; // Tránh kích hoạt đè khi sự kiện bị phát trùng lặp
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

        // Self-healing: Warp to closest NavMesh point if slightly off NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 10.0f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
        }

        agent.enabled = true;

        if (agent.enabled && !agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out hit, 15.0f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
        }

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
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                SafeSetDestination(playerTransform.position);
            }
            else
            {
                // FALLBACK DỰ PHÒNG TỐI TÂN (Physics-free translation chase):
                // Nếu không có NavMesh hoặc Agent bị lỗi, tự động di chuyển tịnh tiến thẳng về phía Player
                Vector3 moveDir = (playerTransform.position - transform.position);
                moveDir.y = 0; // Giữ ở cùng độ cao mặt đất
                if (moveDir.sqrMagnitude > 0.001f)
                {
                    transform.position += moveDir.normalized * chaseSpeed * Time.deltaTime;
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation, 
                        Quaternion.LookRotation(moveDir, Vector3.up), 
                        Time.deltaTime * 5f
                    );
                }
            }

            // Bộ gỡ kẹt tối tân (Stuck Resolver) & Tự động vượt cửa (NavMesh Bridge Warp) - Self-Healing
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                if (agent.velocity.sqrMagnitude < 0.04f)
                {
                    stuckTimer += Time.deltaTime;
                    if (stuckTimer > 0.6f) // Bị kẹt quá 0.6 giây
                    {
                        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
                        if (distToPlayer < 6f)
                        {
                            // Thử tìm đường đi qua khe cửa bị đứt đoạn NavMesh bằng cách dò điểm NavMesh ở phía đối diện
                            Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
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
                                        Debug.Log($"[DemonController] Đã tự động vượt qua khe cửa (NavMesh Bridge Warp) tới: {bridgeHit.position}");
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

            // Tăng khoảng cách bắt lên tối thiểu 2.2m để tránh CapsuleCollider cản trở va chạm
            float effectiveCatchDistance = Mathf.Max(catchDistance, 2.2f);
            if (Vector3.Distance(transform.position, playerTransform.position) <= effectiveCatchDistance)
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

        // Đóng băng người chơi hoàn toàn (Self-Healing)
        FirstPersonController.Instance?.FreezePlayer();

        SetAnimRun(false);
        if (animator != null) animator.CrossFade("ATTACK", 0.25f);

        // --- CAMERA ZOOM VÀO MẶT QUỶ KHI TẤN CÔNG (Jumpscare Camera) ---
        yield return StartCoroutine(ZoomCameraTowardSelf());

        yield return new WaitForSeconds(attackAnimDuration);
        RoomManager.Instance?.TriggerBadEnding("caught_room5");
    }

    private IEnumerator ZoomCameraTowardSelf()
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        // ── Bước 1: Tách camera khỏi rig người chơi ──
        cam.transform.SetParent(null, worldPositionStays: true);

        // ── Bước 2: Vị trí mặt quỷ ──
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

        cam.transform.position = targetPos;
        cam.transform.rotation = targetRot;
    }

    /// <summary>
    /// Raycast từ <paramref name="from"/> đến <paramref name="to"/>.
    /// Nếu có tường chắn, trả về vị trí ngay trước bề mặt tường (không bị xưỳng).
    /// </summary>
    private static Vector3 SafeTargetPos(Vector3 from, Vector3 to)
    {
        Vector3 dir      = to - from;
        float   maxDist  = dir.magnitude;
        // Ignore triggers; chỉ va chạm geometry thực
        if (Physics.Raycast(from, dir.normalized, out RaycastHit hit, maxDist,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            // Lui camera lại 0.12m từ bề mặt tường về phía quỷ để tránh clip
            return hit.point + hit.normal * 0.12f;
        }
        return to;
    }

    /// <summary>
    /// Tìm vị trí đầu / mặt quỷ mà không yêu cầu Humanoid rig.
    /// Ưu tiên: child tên Head → bounds đỉnh mesh → fallback +1.7m.
    /// </summary>
    private Vector3 FindHeadPosition()
    {
        // 1️⃣ Tìm child transform có tên chứa "head" (không phân biệt hoa/thường)
        Transform headBone = FindChildByName(transform, "head");
        if (headBone != null)
            return headBone.position;

        // 2️⃣ Dùng Renderer.bounds để ước tính đỉnh đầu mesh (không cần rig)
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds combined = renderers[0].bounds;
            foreach (Renderer r in renderers)
                combined.Encapsulate(r.bounds);
            // Lấy điểm phía trên (y max) − 10% chiều cao để ước lượng tầm mắt
            float headY = combined.max.y - combined.size.y * 0.1f;
            return new Vector3(transform.position.x, headY, transform.position.z);
        }

        // 3️⃣ Fallback cứng: 1.7m trên gốc quỷ
        return transform.position + Vector3.up * 1.7f;
    }

    /// <summary>Tìm đệ qui child có tên chứa từ khóa (không phân biệt hoa/thường).</summary>
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


    // ═══════════════════════════════════════════════════════════
    // LIGHT API — gọi bởi FlashlightController.ScanForDemons()
    // ═══════════════════════════════════════════════════════════

    public void OnLightHit(float deltaTime, bool forceFlinch = false)
    {
        if (CurrentState == DemonState.Vanishing) return;

        lastLightHitTime      = Time.time;

        // Xác định xem quái vật có thuộc Room 4 không (Scene Hospital)
        bool isRoom4 = false;
        if (RoomManager.Instance != null && RoomManager.Instance.CurrentRoom == RoomManager.RoomState.Room4)
        {
            isRoom4 = true;
        }
        else
        {
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (currentScene.Equals("Hospital", System.StringComparison.OrdinalIgnoreCase))
            {
                isRoom4 = true;
            }
        }

        // Flinch animation mỗi lần bị chiếu (nếu đang chase hoặc bị ép flinch khi zoom)
        if ((CurrentState == DemonState.Chasing || forceFlinch) && !isDamageFlinching)
            StartCoroutine(DamageFlinch(isRoom4));

        // Nếu ở Room 4, quái vật bất tử, không thể bị tiêu diệt (không gọi Vanish)
        if (isRoom4) return;

        lightExposureAccum   += deltaTime;
        if (lightExposureAccum >= lightVanishTime)
            StartCoroutine(Vanish());
    }

    private IEnumerator DamageFlinch(bool isRoom4)
    {
        isDamageFlinching = true;
        if (animator != null) animator.SetBool(AP.DamageFromRun, true);

        float originalSpeed = chaseSpeed;
        if (isRoom4 && agent != null && agent.enabled)
        {
            // Làm chậm quái vật xuống còn 30% tốc độ chạy ở Room 4 khi bị hào quang tâm linh chiếu
            agent.speed = originalSpeed * 0.3f;
        }

        yield return new WaitForSeconds(damageFlinchDuration);

        if (agent != null && agent.enabled)
        {
            agent.speed = originalSpeed;
        }

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
        FlashlightController fc = FindAnyObjectByType<FlashlightController>();
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
    // COLLISION — bắt player qua trigger hoặc va chạm vật lý
    // ═══════════════════════════════════════════════════════════
 
    private void OnTriggerEnter(Collider other)
    {
        if (CurrentState == DemonState.Vanishing) return;
        if (!other.CompareTag("Player")) return;
        StartCoroutine(CatchPlayer());
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (CurrentState == DemonState.Vanishing) return;
        if (!collision.gameObject.CompareTag("Player")) return;
        StartCoroutine(CatchPlayer());
    }

    // ═══════════════════════════════════════════════════════════
    // ANIMATION HELPERS
    // ═══════════════════════════════════════════════════════════

    /// <summary>Bật/tắt animation RUN và các guard params tránh auto-exit.</summary>
    private void SetAnimRun(bool run)
    {
        if (animator == null) return;

        // Anti-running-in-place guard: if the agent cannot move (e.g. NavMesh not baked or agent off NavMesh), force idle animation
        if (run && (agent == null || !agent.enabled || !agent.isOnNavMesh))
        {
            run = false;
        }

        animator.SetBool(AP.Run,         run);
        animator.SetBool(AP.WalkToRun,   run); // ngăn RUN→WALK auto-fire khi false
        animator.SetBool(AP.AttackToRun, run); // ngăn RUN→ATTACK auto-fire khi false
        if (!run) animator.SetBool(AP.Walk, false);
    }

    /// <summary>Bật/tắt animation WALK (patrol idle).</summary>
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

    private void SafeSetDestination(Vector3 target)
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.SetDestination(target);
        }
    }

    private static bool HasParam(Animator anim, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in anim.parameters)
            if (p.name == name && p.type == type) return true;
        return false;
    }
}
