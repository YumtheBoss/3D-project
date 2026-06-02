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

    [Header("Monster Sounds")]
    [Tooltip("Danh sách tiếng kêu/gầm của quái vật")]
    public AudioClip[] demonGrowlSounds;
    [Tooltip("Thời gian tối thiểu giữa mỗi lần kêu (giây)")]
    public float growlMinInterval = 3f;
    [Tooltip("Thời gian tối đa giữa mỗi lần kêu (giây)")]
    public float growlMaxInterval = 7f;
    [Tooltip("AudioSource trên quái vật (sẽ tự động tạo nếu thiếu)")]
    public AudioSource demonAudioSource;

    [Header("Jump Scare Sound")]
    [Tooltip("Tiếng thét khi quái vật bắt được người chơi")]
    public AudioClip catchJumpScareSound;

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

        #if UNITY_EDITOR
        AutoAssignAssets();
        #endif
    }

    private void Start()
    {
        if (playerController == null)
        {
            playerController = FindAnyObjectByType<FirstPersonController>();
            if (playerController != null)
            {
                Debug.Log("[Room4ChaseSequence] Automatically assigned playerController dynamically!");
            }
        }

        // Tự động tìm hoặc gắn AudioSource cho nhạc nền chase
        if (chaseAudioSource == null)
        {
            chaseAudioSource = GetComponent<AudioSource>();
            if (chaseAudioSource == null)
            {
                chaseAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        if (chaseAudioSource != null)
        {
            chaseAudioSource.playOnAwake = false;
            chaseAudioSource.spatialBlend = 0f; // 2D sound for BGM
        }

        // Tự động tìm hoặc gắn AudioSource cho quái vật
        if (corridorDemon != null && demonAudioSource == null)
        {
            demonAudioSource = corridorDemon.GetComponent<AudioSource>();
            if (demonAudioSource == null)
            {
                demonAudioSource = corridorDemon.AddComponent<AudioSource>();
            }
        }
        if (demonAudioSource != null)
        {
            demonAudioSource.playOnAwake = false;
            demonAudioSource.spatialBlend = 1.0f; // 3D sound (positional)
            demonAudioSource.minDistance = 2f;
            demonAudioSource.maxDistance = 25f;
            demonAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoAssignAssets();
    }

    private void AutoAssignAssets()
    {
        // 1. Tìm nhạc chase
        if (chaseMusic == null)
        {
            string[] musicGuids = UnityEditor.AssetDatabase.FindAssets("01 Before Dark FULL LOOP t:AudioClip");
            if (musicGuids.Length == 0) musicGuids = UnityEditor.AssetDatabase.FindAssets("Before Dark t:AudioClip");
            if (musicGuids.Length == 0) musicGuids = UnityEditor.AssetDatabase.FindAssets("LOOP t:AudioClip");
            if (musicGuids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(musicGuids[0]);
                chaseMusic = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                Debug.Log($"[Room4ChaseSequence Editor-Helper] Tự động gán nhạc chase: {path}");
            }
        }

        // 2. Tìm tiếng gầm của quái vật (Backroom Entities)
        if (demonGrowlSounds == null || demonGrowlSounds.Length == 0)
        {
            string[] soundGuids = UnityEditor.AssetDatabase.FindAssets("Backroom Entities t:AudioClip");
            if (soundGuids.Length > 0)
            {
                var clips = new System.Collections.Generic.List<AudioClip>();
                foreach (string guid in soundGuids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    AudioClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    if (clip != null) clips.Add(clip);
                }
                demonGrowlSounds = clips.ToArray();
                Debug.Log($"[Room4ChaseSequence Editor-Helper] Tự động gán {demonGrowlSounds.Length} âm thanh quái vật từ Backroom Entities!");
            }
        }

        // 3. Tìm tiếng jumpscare khi bị bắt
        if (catchJumpScareSound == null)
        {
            string[] scareGuids = UnityEditor.AssetDatabase.FindAssets("JumpScare_Large t:AudioClip");
            if (scareGuids.Length == 0) scareGuids = UnityEditor.AssetDatabase.FindAssets("JumpScare t:AudioClip");
            if (scareGuids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(scareGuids[0]);
                catchJumpScareSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                Debug.Log($"[Room4ChaseSequence Editor-Helper] Tự động gán tiếng jumpscare: {path}");
            }
        }
    }
#endif

    // ═══════════════════════════════════════════════════════════
    // TRIGGER — player bước vào hành lang
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Kích hoạt chuỗi đuổi bắt chủ động (dùng khi dịch chuyển trực tiếp vào hành lang).
    /// </summary>
    public void ForceStartChase(Transform player)
    {
        if (triggered) return;
        triggered       = true;
        playerTransform = player;
        StartCoroutine(BeginChase());
    }

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

            // Tự động gắn thành phần bắt va chạm với Player (Self-Healing)
            // Tìm chính xác GameObject có chứa Collider (gồm cả con) để gắn bộ phát hiện va chạm
            Collider demonCollider = corridorDemon.GetComponentInChildren<Collider>();
            if (demonCollider != null)
            {
                var contactDetector = demonCollider.gameObject.GetComponent<Room4DemonContact>();
                if (contactDetector == null)
                {
                    contactDetector = demonCollider.gameObject.AddComponent<Room4DemonContact>();
                }
                contactDetector.chaseSequence = this;
            }
            else
            {
                var contactDetector = corridorDemon.GetComponent<Room4DemonContact>();
                if (contactDetector == null)
                {
                    contactDetector = corridorDemon.AddComponent<Room4DemonContact>();
                }
                contactDetector.chaseSequence = this;
            }

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

        // Bắt đầu phát âm thanh của quái vật
        StartCoroutine(PlayDemonSoundsRoutine());

        // Hiển thị HUD hướng dẫn chạy nhanh (Sprint) bằng phím Shift
        TutorialHUDManager.Instance?.ShowTutorial(TutorialHUDManager.TutorialType.Sprint);
    }

    // ═══════════════════════════════════════════════════════════
    // UPDATE — kiểm tra bắt kịp
    // ═══════════════════════════════════════════════════════════

    private void Update()
    {
        if (!chaseActive || playerTransform == null || corridorDemon == null) return;

        // Đuổi theo vị trí trễ 1 giây (-1s) từ PlayerTracker nếu có, ngược lại đuổi theo vị trí tức thời
        Vector3 targetPos = PlayerTracker.Instance != null
            ? PlayerTracker.Instance.GetDelayedPlayerPosition(1.0f)
            : playerTransform.position;

        // Ưu tiên sử dụng NavMeshAgent để di chuyển thông minh
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.SetDestination(targetPos);
        }
        else
        {
            // FALLBACK DỰ PHÒNG TỐI TÂN (Physics-free translation chase):
            // Nếu không có NavMesh hoặc Agent bị lỗi, tự động di chuyển tịnh tiến thẳng về phía Player
            Vector3 moveDir = (targetPos - corridorDemon.transform.position);
            moveDir.y = 0; // Giữ ở cùng độ cao mặt đất
            if (moveDir.sqrMagnitude > 0.001f)
            {
                corridorDemon.transform.position += moveDir.normalized * demonSpeed * Time.deltaTime;
                corridorDemon.transform.rotation = Quaternion.Slerp(
                    corridorDemon.transform.rotation, 
                    Quaternion.LookRotation(moveDir, Vector3.up), 
                    Time.deltaTime * 5f
                );
            }
        }

        // Tăng khoảng cách bắt lên tối thiểu 2.2m để ngăn cản việc 2 CapsuleCollider đẩy nhau ra xa cản trở dist <= 1.5f
        float effectiveCatchDistance = Mathf.Max(catchDistance, 2.2f);
        float dist = Vector3.Distance(corridorDemon.transform.position, playerTransform.position);
        if (dist <= effectiveCatchDistance)
        {
            chaseActive = false;
            StartCoroutine(AttackAndBadEnding());
        }
    }

    // ═══════════════════════════════════════════════════════════
    // ATTACK SEQUENCE — bắt được player
    // ═══════════════════════════════════════════════════════════

    private IEnumerator PlayDemonSoundsRoutine()
    {
        // Thêm tiếng gầm đọa dẫm ngay khi xuất hiện
        if (demonGrowlSounds != null && demonGrowlSounds.Length > 0)
        {
            AudioClip startClip = demonGrowlSounds[Random.Range(0, demonGrowlSounds.Length)];
            if (startClip != null && demonAudioSource != null)
            {
                demonAudioSource.PlayOneShot(startClip);
            }
        }

        while (chaseActive && corridorDemon != null && corridorDemon.activeSelf)
        {
            float waitTime = Random.Range(growlMinInterval, growlMaxInterval);
            yield return new WaitForSeconds(waitTime);

            if (!chaseActive || corridorDemon == null || !corridorDemon.activeSelf) break;

            if (demonGrowlSounds != null && demonGrowlSounds.Length > 0)
            {
                AudioClip clip = demonGrowlSounds[Random.Range(0, demonGrowlSounds.Length)];
                if (clip != null && demonAudioSource != null)
                {
                    demonAudioSource.PlayOneShot(clip);
                }
            }
        }
    }

    private IEnumerator AttackAndBadEnding()
    {
        // Dừng quỷ di chuyển
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled   = false;
        }

        // Đóng băng người chơi hoàn toàn (Self-Healing) và tắt script di chuyển để tránh HeadBob di chuyển camera khi unparented
        if (FirstPersonController.Instance != null)
        {
            FirstPersonController.Instance.FreezePlayer();
            FirstPersonController.Instance.enabled = false;
        }
        else if (playerController != null)
        {
            playerController.enabled = false;
        }

        // Dừng nhạc chase nền để thét jumpscare rõ hơn
        if (chaseAudioSource != null)
        {
            chaseAudioSource.Stop();
        }

        // Phát tiếng thét jumpscare từ quái vật
        if (demonAudioSource != null && catchJumpScareSound != null)
        {
            demonAudioSource.PlayOneShot(catchJumpScareSound, 1.0f);
        }

        // Chuyển từ RUN → ATTACK
        SetDemonRun(false);
        if (demonAnimator != null) demonAnimator.CrossFade("ATTACK", 0.25f);

        // Lưu thông tin gốc của camera để khôi phục tránh lỗi camera bay ra ngoài bản đồ và bị xoá khi chuyển scene
        Camera cam = Camera.main;
        Transform originalParent = null;
        Vector3 originalLocalPos = Vector3.zero;
        Quaternion originalLocalRot = Quaternion.identity;

        if (cam != null)
        {
            originalParent = cam.transform.parent;
            originalLocalPos = cam.transform.localPosition;
            originalLocalRot = cam.transform.localRotation;
        }

        // Camera zoom vào quỷ
        yield return StartCoroutine(ZoomCameraTowardDemon());

        // Chờ animation ATTACK chạy xong
        yield return new WaitForSeconds(attackAnimDuration);

        // Gắn lại camera về người chơi trước khi màn hình Game Over hiển thị
        if (cam != null && originalParent != null)
        {
            cam.transform.SetParent(originalParent, false);
            cam.transform.localPosition = originalLocalPos;
            cam.transform.localRotation = originalLocalRot;
        }

        if (chaseAudioSource != null) chaseAudioSource.Stop();
        RoomManager.Instance?.TriggerBadEnding("caught_corridor");
    }

    private IEnumerator ZoomCameraTowardDemon()
    {
        Camera cam = Camera.main;
        if (cam == null || corridorDemon == null) yield break;

        // ── Bước 1: Tách camera khỏi rig người chơi ──
        cam.transform.SetParent(null, worldPositionStays: true);

        Transform demonT = corridorDemon.transform;

        // ── Bước 2: Tìm vị trí mặt quỷ (không cần Humanoid rig) ──
        Vector3 facePosition = FindHeadPositionOnDemon(demonT);

        // ── Bước 3: Camera đứng trước mặt quỷ, nhìn ngược vào quỷ ──
        Vector3 demonForward = demonT.forward;
        demonForward.y = 0f;
        if (demonForward.sqrMagnitude < 0.001f) demonForward = Vector3.forward;
        demonForward.Normalize();

        Vector3 rawTarget = facePosition + demonForward * cameraZoomDistance;
        // ── Bước 4: Raycast chống xưỳng tường ──
        Vector3    targetPos = SafeTargetPos(facePosition, rawTarget);
        Quaternion targetRot = Quaternion.LookRotation(facePosition - targetPos, Vector3.up);

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

        cam.transform.position = targetPos;
        cam.transform.rotation = targetRot;
    }

    /// <summary>
    /// Tìm vị trí đầu quỷ trên bất kỳ rig nào (Generic, Legacy, không rig).
    /// 1) Child tên "head" → 2) Renderer.bounds đỉnh mesh → 3) +1.7m fallback.
    /// </summary>
    private static Vector3 FindHeadPositionOnDemon(Transform demonRoot)
    {
        // 1️⃣ Child có tên chứa "head"
        Transform headBone = FindChildByNameOnDemon(demonRoot, "head");
        if (headBone != null)
            return headBone.position;

        // 2️⃣ Renderer bounds đỉnh mesh
        Renderer[] renderers = demonRoot.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds combined = renderers[0].bounds;
            foreach (Renderer r in renderers)
                combined.Encapsulate(r.bounds);
            float headY = combined.max.y - combined.size.y * 0.1f;
            return new Vector3(demonRoot.position.x, headY, demonRoot.position.z);
        }

        // 3️⃣ Fallback cứng
        return demonRoot.position + Vector3.up * 1.7f;
    }

    private static Transform FindChildByNameOnDemon(Transform parent, string keyword)
    {
        foreach (Transform child in parent)
        {
            if (child.name.ToLower().Contains(keyword))
                return child;
            Transform found = FindChildByNameOnDemon(child, keyword);
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
    // ═══════════════════════════════════════════════════════════
    // EXIT — player chạy thoát thành công
    // ═══════════════════════════════════════════════════════════

    /// <summary>Gọi từ Room4DemonContact khi quỷ chạm trực tiếp vào người chơi.</summary>
    public void OnDemonTouchedPlayer()
    {
        if (!chaseActive) return;
        chaseActive = false;
        StartCoroutine(AttackAndBadEnding());
    }

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

        // Anti-running-in-place guard: if the agent cannot move (e.g. NavMesh not baked or agent off NavMesh), force idle animation
        if (run && (agent == null || !agent.enabled || !agent.isOnNavMesh))
        {
            run = false;
        }

        demonAnimator.SetBool(P_Run,       run);
        demonAnimator.SetBool(P_WalkToRun, run); // ngăn RUN→WALK auto-exit khi false
        demonAnimator.SetBool(P_AtkToRun,  run); // ngăn RUN→ATTACK auto-exit khi false
        
        // Tắt hoạt ảnh đi bộ khi chuyển sang hoạt ảnh chạy
        demonAnimator.SetBool("is_idie_to_walk", !run);
    }
}

/// <summary>
/// Script phụ gắn động vào quỷ hành lang Room 4 để bắt va chạm trực tiếp với người chơi.
/// </summary>
public class Room4DemonContact : MonoBehaviour
{
    public Room4ChaseSequence chaseSequence;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            chaseSequence?.OnDemonTouchedPlayer();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            chaseSequence?.OnDemonTouchedPlayer();
        }
    }
}
