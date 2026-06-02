// CHANGE LOG
// 
// CHANGES || version VERSION
//
// "Enable/Disable Headbob, Changed look rotations - should result in reduced camera jitters" || version 1.0.1

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
    using UnityEditor;
    using System.Net;
#endif

public class FirstPersonController : MonoBehaviour
{
    public static FirstPersonController Instance { get; private set; }

    private Rigidbody rb;

    #region Camera Movement Variables

    public Camera playerCamera;

    public float fov = 60f;
    public bool invertCamera = false;
    public bool cameraCanMove = true;
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 50f;

    // Crosshair
    public bool lockCursor = true;
    public bool crosshair = true;
    public Sprite crosshairImage;
    public Color crosshairColor = Color.white;

    // Internal Variables
    private float yaw = 0.0f;
    private float pitch = 0.0f;
    private Image crosshairObject;

    /// <summary>
    /// Cờ tĩnh để các script UI bật/tắt khi mở/đóng canvas.
    /// Khi true, camera và di chuyển sẽ bị khóa hoàn toàn.
    /// </summary>
    public static bool IsUIOpen { get; set; } = false;

    #region Camera Zoom Variables

    public bool enableZoom = true;
    public bool holdToZoom = false;
    public KeyCode zoomKey = KeyCode.Mouse1;
    public float zoomFOV = 30f;
    public float zoomStepTime = 5f;

    // Internal Variables
    private bool isZoomed = false;
    public bool IsZoomed => isZoomed;

    #endregion
    #endregion

    #region Movement Variables

    public bool playerCanMove = true;
    public float walkSpeed = 5f;
    public float maxVelocityChange = 10f;

    // Internal Variables
    public bool isWalking = false;

    #region Sprint

    public bool enableSprint = true;
    public bool unlimitedSprint = false;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public float sprintSpeed = 7f;
    public float sprintDuration = 5f;
    public float sprintCooldown = .5f;
    public float sprintFOV = 80f;
    public float sprintFOVStepTime = 10f;

    // Sprint Bar
    public bool useSprintBar = true;
    public bool hideBarWhenFull = false;
    public Image sprintBarBG;
    public Image sprintBar;
    public float sprintBarWidthPercent = .3f;
    public float sprintBarHeightPercent = .015f;

    // Internal Variables
    private CanvasGroup sprintBarCG;
    public bool isSprinting = false;
    private float sprintRemaining;
    private float sprintBarWidth;
    private float sprintBarHeight;
    private bool isSprintCooldown = false;
    private float sprintCooldownReset;

    #endregion

    #region Jump

    public bool enableJump = true;
    public KeyCode jumpKey = KeyCode.Space;
    public float jumpPower = 5f;

    // Internal Variables
    private bool isGrounded = false;

    #endregion

    #region Crouch

    public bool enableCrouch = true;
    public bool holdToCrouch = true;
    public KeyCode crouchKey = KeyCode.LeftControl;
    public float crouchHeight = .75f;
    public float speedReduction = .5f;

    // Internal Variables
    private bool isCrouched = false;
    private Vector3 originalScale;

    #endregion
    #endregion

    #region Head Bob

    public bool enableHeadBob = true;
    public Transform joint;
    public float bobSpeed = 10f;
    public Vector3 bobAmount = new Vector3(.15f, .05f, 0f);

    // Internal Variables
    private Vector3 jointOriginalPos;
    private float timer = 0;

    #endregion

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.Log("[FirstPersonController] Duplicate player detected in Awake. Destroying the old persistent player to reset player GO in the new scene!");
            Destroy(Instance.gameObject);
            Instance = this;
        }
        else
        {
            Instance = this;
        }

        // Persist qua các scene tiếp theo
        DontDestroyOnLoad(gameObject);

        // --- ĐỌC CÀI ĐẶT (SETTINGS) ---
        mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 2f);
        AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        // -------------------------------

        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        rb.freezeRotation = true;
        rb.useGravity = true;
        rb.isKinematic = false;
        // Interpolate để render position mượt giữa các bước FixedUpdate — bắt buộc để tránh camera giật
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        crosshairObject = GetComponentInChildren<Image>();

        if (playerCamera != null) playerCamera.fieldOfView = fov;
        originalScale = transform.localScale;
        // Khởi tạo yaw từ rotation hiện tại để không bị "nhảy" lần đầu
        yaw = transform.eulerAngles.y;
        
        // Ngăn user tự gán chính GameObject này làm joint (sẽ gây lỗi dịch chuyển giật lùi về chỗ cũ)
        if (joint == transform)
        {
            joint = null;
        }

        if (joint != null)
        {
            jointOriginalPos = joint.localPosition;
        }
        else if (playerCamera != null)
        {
            // Tự động gán Camera làm joint nếu bị thiếu
            joint = playerCamera.transform;
            jointOriginalPos = joint.localPosition;
        }

        if (!unlimitedSprint)
        {
            sprintRemaining = sprintDuration;
            sprintCooldownReset = sprintCooldown;
        }
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            Debug.Log("[FirstPersonController] MainMenu loaded. Destroying persistent player!");
            if (Instance == this)
            {
                Instance = null;
            }
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Kích hoạt tự động HUD Hướng Dẫn Chơi (Tutorial HUD)
        var tutorial = TutorialHUDManager.Instance;
        // Tự động tạo và gán PhysicMaterial không ma sát tại runtime để ngăn người chơi bị kẹt tường/cầu thang khi đi thẳng (Friction Lockup)
        Collider playerCollider = GetComponent<Collider>();
        if (playerCollider == null) playerCollider = GetComponentInChildren<Collider>();
        if (playerCollider != null)
        {
            PhysicsMaterial frictionlessMat = new PhysicsMaterial("FrictionlessPlayerMaterial");
            frictionlessMat.staticFriction = 0f;
            frictionlessMat.dynamicFriction = 0f;
            frictionlessMat.frictionCombine = PhysicsMaterialCombine.Minimum;
            frictionlessMat.bounciness = 0f;
            frictionlessMat.bounceCombine = PhysicsMaterialCombine.Minimum;
            playerCollider.material = frictionlessMat;
            Debug.Log("[FirstPersonController] Đã tự động gán PhysicMaterial không ma sát để leo cầu thang trơn tru!");
        }

        // Chỉ khoá cursor trên PC (mobile không có cursor)
        if(lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        // Tự động gắn PlayerHandheldManager vào camera nếu chưa có (Self-Healing)
        Camera cam = playerCamera != null ? playerCamera : GetComponentInChildren<Camera>();
        if (cam == null) cam = Camera.main;
        if (cam != null && cam.GetComponent<PlayerHandheldManager>() == null)
        {
            cam.gameObject.AddComponent<PlayerHandheldManager>();
            Debug.Log("[FirstPersonController] Tự động gắn PlayerHandheldManager vào Camera!");
        }

        if(crosshair && crosshairObject != null)
        {
            crosshairObject.sprite = crosshairImage;
            crosshairObject.color = crosshairColor;
        }
        else if (crosshairObject != null)
        {
            crosshairObject.gameObject.SetActive(false);
        }

        #region Sprint Bar

        // Đảm bảo luôn sử dụng thanh thể lực theo yêu cầu game
        useSprintBar = true;

        // Tự động dựng UI Stamina Bar nếu thiếu kéo thả trong Inspector (Self-Healing UI)
        if (useSprintBar && (sprintBarBG == null || sprintBar == null))
        {
            BuildDynamicSprintBar();
        }

        if (sprintBarCG == null)
        {
            // Tìm CanvasGroup của chính SprintBar Canvas thay vì tìm bừa bãi trong các con
            var dynamicCanvas = transform.Find("_SprintBarCanvas_Auto");
            if (dynamicCanvas != null)
            {
                sprintBarCG = dynamicCanvas.GetComponent<CanvasGroup>();
            }
            else
            {
                sprintBarCG = GetComponentInChildren<CanvasGroup>();
            }
        }

        // Thiết lập thời gian hồi thể lực cố định từ 3 - 5 giây (chọn 4 giây)
        sprintCooldown = 4.0f;
        sprintCooldownReset = sprintCooldown;

        if(useSprintBar && sprintBarBG != null && sprintBar != null)
        {
            sprintBarBG.gameObject.SetActive(true);
            sprintBar.gameObject.SetActive(true);

            // Thiết lập kích thước cố định cho thanh đứng dọc: rộng 14px, cao 220px
            sprintBarWidth = 14f;
            sprintBarHeight = 220f;

            sprintBarBG.rectTransform.sizeDelta = new Vector3(sprintBarWidth, sprintBarHeight, 0f);
            sprintBar.rectTransform.sizeDelta = new Vector3(sprintBarWidth - 4, sprintBarHeight - 4, 0f);

            // Đưa thanh thể lực đứng dọc ở góc dưới bên trái màn hình (cách cạnh trái và đáy 50px)
            sprintBarBG.rectTransform.anchorMin = new Vector2(0f, 0f);
            sprintBarBG.rectTransform.anchorMax = new Vector2(0f, 0f);
            sprintBarBG.rectTransform.pivot = new Vector2(0f, 0f);
            sprintBarBG.rectTransform.anchoredPosition = new Vector2(50f, 50f);

            sprintBar.rectTransform.anchorMin = new Vector2(0f, 0f);
            sprintBar.rectTransform.anchorMax = new Vector2(1f, 1f);
            sprintBar.rectTransform.pivot = new Vector2(0.5f, 0f); // Pivot cạnh đáy để hồi thể lực tăng dần hướng lên trên
            sprintBar.rectTransform.anchoredPosition = Vector2.zero;
            sprintBar.rectTransform.offsetMin = new Vector2(2, 2);
            sprintBar.rectTransform.offsetMax = new Vector2(-2, -2);

            if(sprintBarCG != null)
            {
                sprintBarCG.alpha = hideBarWhenFull ? 0f : 1f;
            }
        }
        else
        {
            if (sprintBarBG != null) sprintBarBG.gameObject.SetActive(false);
            if (sprintBar != null) sprintBar.gameObject.SetActive(false);
        }

        #endregion
    }

    float camRotation;

    private void Update()
    {
        // Đọc mouse input mỗi frame để rotation mượt, lưu vào yaw/pitch
        // Rotation thực sự được apply ở FixedUpdate (body) và LateUpdate (camera)
        // Ba lớp bảo vệ: cameraCanMove + Cursor locked + timeScale > 0 + IsUIOpen == false
        if (cameraCanMove && !IsUIOpen && Cursor.lockState == CursorLockMode.Locked && Time.timeScale > 0f)
        {
            yaw   += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch += Input.GetAxis("Mouse Y") * mouseSensitivity * (invertCamera ? 1f : -1f);
            pitch  = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);
        }
        else
        {
            // Tiêu thụ mouse input buffer để tránh camera giật khi đóng canvas
            Input.GetAxis("Mouse X");
            Input.GetAxis("Mouse Y");
        }

        #region Zoom key detection (input only, no FOV lerp)

        if (enableZoom)
        {
            if(Input.GetKeyDown(zoomKey) && !holdToZoom && !isSprinting)
                isZoomed = !isZoomed;

            if(holdToZoom && !isSprinting)
            {
                if(Input.GetKeyDown(zoomKey))      isZoomed = true;
                else if(Input.GetKeyUp(zoomKey))   isZoomed = false;
            }
        }

        #endregion

        #region Sprint timer + bar

        if(enableSprint)
        {
            if(isSprinting)
            {
                isZoomed = false;
                if(!unlimitedSprint)
                {
                    sprintRemaining -= 1 * Time.deltaTime;
                    if (sprintRemaining <= 0f)
                    {
                        sprintRemaining = 0f;
                        isSprinting = false;
                        isSprintCooldown = true;
                    }
                }
            }
            else
            {
                // Hồi phục hoàn toàn thể lực trong 4 giây (giữa 3 và 5 giây)
                float recoverySpeed = sprintDuration / 4.0f;
                sprintRemaining = Mathf.Clamp(sprintRemaining + recoverySpeed * Time.deltaTime, 0, sprintDuration);
            }

            if(isSprintCooldown)
            {
                sprintCooldown -= 1 * Time.deltaTime;
                if (sprintCooldown <= 0) isSprintCooldown = false;
            }
            else
            {
                sprintCooldown = sprintCooldownReset;
            }

            if(useSprintBar && !unlimitedSprint && sprintBar != null)
            {
                float sprintRemainingPercent = sprintRemaining / sprintDuration;
                // Co giãn theo chiều dọc (trục Y) từ dưới lên trên
                sprintBar.transform.localScale = new Vector3(1f, sprintRemainingPercent, 1f);
            }
        }

        #endregion

        #region Jump

        if(enableJump && Input.GetKeyDown(jumpKey) && isGrounded)
            Jump();

        #endregion

        #region Crouch

        if (enableCrouch)
        {
            if(Input.GetKeyDown(crouchKey) && !holdToCrouch)
                Crouch();

            if(Input.GetKeyDown(crouchKey) && holdToCrouch)
            {
                isCrouched = false;
                Crouch();
            }
            else if(Input.GetKeyUp(crouchKey) && holdToCrouch)
            {
                isCrouched = true;
                Crouch();
            }
        }

        #endregion

        CheckGround();
    }

    private void LateUpdate()
    {
        // Chỉ apply pitch lên camera — body yaw được apply bởi rb.MoveRotation trong FixedUpdate
        if (cameraCanMove && playerCamera != null)
            playerCamera.transform.localEulerAngles = new Vector3(pitch, 0, 0);

        // FOV lerp (zoom + sprint)
        if (playerCamera != null)
        {
            if (enableSprint && isSprinting)
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, sprintFOV, sprintFOVStepTime * Time.deltaTime);
            else if (enableZoom && isZoomed)
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, zoomFOV, zoomStepTime * Time.deltaTime);
            else
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, fov, zoomStepTime * Time.deltaTime);
        }

        if(enableHeadBob)
            HeadBob();
    }

    void FixedUpdate()
    {
        // Set rotation trực tiếp qua rb.rotation — an toàn vì freezeRotation=true
        // (MoveRotation trên non-kinematic body có thể block AddForce)
        rb.rotation = Quaternion.Euler(0f, yaw, 0f);

        #region Movement

        if (playerCanMove)
        {
            // Đọc phím bấm trực tiếp thay vì qua GetAxis
            float moveH = 0f;
            float moveV = 0f;

            // PC Input
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) moveV += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) moveV -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveH += 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveH -= 1f;

            // Tính toán hướng di chuyển
            Vector3 targetVelocity = new Vector3(moveH, 0, moveV);
            if (targetVelocity.magnitude > 1f) targetVelocity.Normalize();

            // Checks if player is walking and isGrounded
            // Will allow head bob
            if ((targetVelocity.x != 0 || targetVelocity.z != 0) && isGrounded)
            {
                isWalking = true;
            }
            else
            {
                isWalking = false;
            }

            // Chỉ cho phép chạy nhanh khi người chơi thực sự nhấn phím di chuyển (W, A, S, D)
            // giúp tránh lỗi đứng yên bấm giữ Shift vẫn bị hao tổn thể lực.
            bool hasMovementInput = (moveH != 0f || moveV != 0f);
            bool isSprintInput = Input.GetKey(sprintKey) && hasMovementInput;
            if (enableSprint && isSprintInput && sprintRemaining > 0f && !isSprintCooldown)
            {
                targetVelocity = transform.TransformDirection(targetVelocity) * sprintSpeed;

                if (targetVelocity.magnitude < 0.1f && isGrounded)
                {
                    // Dừng lập tức khi không bấm phím và đang ở trên mặt đất (Bảo toàn Y để leo cầu thang trơn tru)
                    rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
                    isSprinting = false;
                }
                else
                {
                    // Apply a force that attempts to reach our target velocity
                    Vector3 velocity = rb.linearVelocity;
                    Vector3 velocityChange = (targetVelocity - velocity);
                    
                    velocityChange.x = Mathf.Clamp(velocityChange.x, -maxVelocityChange, maxVelocityChange);
                    velocityChange.z = Mathf.Clamp(velocityChange.z, -maxVelocityChange, maxVelocityChange);
                    velocityChange.y = 0;

                    // Player is only moving when there is input (targetVelocity > 0)
                    if (targetVelocity.magnitude > 0.1f)
                    {
                        isSprinting = true;

                        if (isCrouched)
                        {
                            Crouch();
                        }

                        if (useSprintBar && hideBarWhenFull && !unlimitedSprint && sprintBarCG != null)
                        {
                            sprintBarCG.alpha += 5 * Time.deltaTime;
                        }
                    }

                    rb.AddForce(velocityChange, ForceMode.VelocityChange);
                }
            }
            // All movement calculations while walking
            else
            {
                isSprinting = false;

                if (useSprintBar && hideBarWhenFull && sprintRemaining == sprintDuration && sprintBarCG != null)
                {
                    sprintBarCG.alpha -= 3 * Time.deltaTime;
                }

                targetVelocity = transform.TransformDirection(targetVelocity) * walkSpeed;

                if (targetVelocity.magnitude < 0.1f && isGrounded)
                {
                    // Dừng lập tức khi không bấm phím và đang ở trên mặt đất (Bảo toàn Y để leo cầu thang trơn tru)
                    rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
                }
                else
                {
                    // Apply a force that attempts to reach our target velocity
                    Vector3 velocity = rb.linearVelocity;
                    Vector3 velocityChange = (targetVelocity - velocity);
                    
                    velocityChange.x = Mathf.Clamp(velocityChange.x, -maxVelocityChange, maxVelocityChange);
                    velocityChange.z = Mathf.Clamp(velocityChange.z, -maxVelocityChange, maxVelocityChange);
                    velocityChange.y = 0;

                    rb.AddForce(velocityChange, ForceMode.VelocityChange);
                }
            }
        }

        #endregion

        // --- LOGIC BÁM DỐC & TRIỆT TIÊU GIA TỐC BAY (Anti-Airborne Stair Resolver) ---
        if (isGrounded && !Input.GetKey(jumpKey))
        {
            // 1. Áp dụng lực hút nhẹ xuống dưới để giữ nhân vật bám sát mặt dốc/bậc thang khi đi xuống
            rb.AddForce(Vector3.down * 12f, ForceMode.Acceleration);

            // 2. Nếu đi lên hết cầu thang và chạm đất phẳng, triệt tiêu nhanh vận tốc đứng (Y) dương để tránh bị bay lên
            if (rb.linearVelocity.y > 0.05f)
            {
                Vector3 vel = rb.linearVelocity;
                vel.y = Mathf.Lerp(vel.y, 0f, Time.fixedDeltaTime * 15f);
                rb.linearVelocity = vel;
            }
        }
        // -----------------------------------------------------------------------------

        // --- BỘ LEO BẬC THANG & VƯỢT CHƯỚNG NGẠI VẬT TỰ ĐỘNG (Rigidbody Step Climber) ---
        if (playerCanMove && isGrounded && !Input.GetKey(jumpKey))
        {
            float moveH = 0f;
            float moveV = 0f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) moveV += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) moveV -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveH += 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveH -= 1f;

            Vector3 moveDir = new Vector3(moveH, 0f, moveV);
            if (moveDir.sqrMagnitude > 0.01f)
            {
                moveDir = transform.TransformDirection(moveDir).normalized;
                Vector3 footPos = transform.position - new Vector3(0f, transform.localScale.y * 0.5f, 0f);
                Vector3 lowerOrigin = footPos + Vector3.up * 0.05f;
                Vector3 upperOrigin = footPos + Vector3.up * 0.32f; // Chiều cao bậc tối đa 32cm

                float checkDistance = 0.45f;
                // Bỏ qua các trigger ẩn để tránh va chạm nhầm
                if (Physics.Raycast(lowerOrigin, moveDir, out RaycastHit lowerHit, checkDistance, ~0, QueryTriggerInteraction.Ignore))
                {
                    // KIỂM TRA ĐỘ DỐC (Normal Check): Chỉ leo nếu va chạm là mặt đứng thẳng đứng (bậc thềm, gờ cửa...)
                    // Tránh việc nhận nhầm mặt dốc hoặc sàn lồi lõm của mô hình 3D dốc khiến người chơi bị phóng lên trời
                    if (lowerHit.normal.y < 0.5f)
                    {
                        // Phía trên bậc phải hoàn toàn trống trải
                        if (!Physics.Raycast(upperOrigin, moveDir, checkDistance, ~0, QueryTriggerInteraction.Ignore))
                        {
                            // Lướt nhẹ nhàng qua bậc thềm/ngưỡng cửa mà không bị khựng đột ngột
                            rb.position += new Vector3(0f, 5.0f * Time.fixedDeltaTime, 0f);
                            rb.linearVelocity = new Vector3(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, 1.8f), rb.linearVelocity.z);
                        }
                    }
                }
            }
        }
        // ---------------------------------------------------------------------------------
    }

    // Sets isGrounded based on a raycast sent straight down from the bottom of the player capsule (original stable logic)
    private void CheckGround()
    {
        Vector3 origin = new Vector3(transform.position.x, transform.position.y - (transform.localScale.y * .5f), transform.position.z);
        Vector3 direction = transform.TransformDirection(Vector3.down);
        float distance = 0.75f;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, distance))
        {
            Debug.DrawRay(origin, direction * distance, Color.green);
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }

    private void Jump()
    {
        // Adds force to the player rigidbody to jump
        if (isGrounded)
        {
            rb.AddForce(0f, jumpPower, 0f, ForceMode.Impulse);
            isGrounded = false;
        }

        // When crouched and using toggle system, will uncrouch for a jump
        if(isCrouched && !holdToCrouch)
        {
            Crouch();
        }
    }

    private void Crouch()
    {
        // Stands player up to full height
        // Brings walkSpeed back up to original speed
        if(isCrouched)
        {
            transform.localScale = new Vector3(originalScale.x, originalScale.y, originalScale.z);
            walkSpeed /= speedReduction;

            isCrouched = false;
        }
        // Crouches player down to set height
        // Reduces walkSpeed
        else
        {
            transform.localScale = new Vector3(originalScale.x, crouchHeight, originalScale.z);
            walkSpeed *= speedReduction;

            isCrouched = true;
        }
    }

    private void HeadBob()
    {
        if(isWalking)
        {
            // Calculates HeadBob speed during sprint
            if(isSprinting)
            {
                timer += Time.deltaTime * (bobSpeed + sprintSpeed);
            }
            // Calculates HeadBob speed during crouched movement
            else if (isCrouched)
            {
                timer += Time.deltaTime * (bobSpeed * speedReduction);
            }
            // Calculates HeadBob speed during walking
            else
            {
                timer += Time.deltaTime * bobSpeed;
            }
            // Applies HeadBob movement
            if (joint != null)
            {
                joint.localPosition = new Vector3(jointOriginalPos.x + Mathf.Sin(timer) * bobAmount.x, jointOriginalPos.y + Mathf.Sin(timer) * bobAmount.y, jointOriginalPos.z + Mathf.Sin(timer) * bobAmount.z);
            }
        }
        else
        {
            // Giảm timer về 0 từ từ thay vì reset lập tức để tránh giật hình đột ngột khi đổi trạng thái tiếp đất
            timer = Mathf.Lerp(timer, 0f, Time.deltaTime * bobSpeed);
            if (joint != null)
            {
                joint.localPosition = Vector3.Lerp(joint.localPosition, jointOriginalPos, Time.deltaTime * bobSpeed);
            }
        }
    }

    /// <summary>Khóa hoàn toàn mọi di chuyển, xoay camera và triệt tiêu lực vật lý của người chơi khi bị quỷ bắt.</summary>
    public void FreezePlayer()
    {
        playerCanMove = false;
        cameraCanMove = false;
        isWalking = false;
        isSprinting = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        Debug.Log("[FirstPersonController] Người chơi đã bị đóng băng hoàn toàn!");
    }

    private void BuildDynamicSprintBar()
    {
        // 1. Tạo Canvas cho Stamina Bar
        GameObject canvasObj = new GameObject("_SprintBarCanvas_Auto");
        canvasObj.transform.SetParent(transform, false); // Gắn làm con của Player để DontDestroyOnLoad theo player

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9997; // Dưới Tutorial HUD

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        sprintBarCG = canvasObj.AddComponent<CanvasGroup>();
        sprintBarCG.alpha = hideBarWhenFull ? 0f : 1f; // Tự động hiển thị nếu không bật ẩn khi đầy

        // 2. Tạo Ảnh Nền (Background Bar) - Màu đen mờ mỏng
        GameObject bgObj = new GameObject("SprintBarBG");
        bgObj.transform.SetParent(canvasObj.transform, false);

        sprintBarBG = bgObj.AddComponent<Image>();
        sprintBarBG.color = new Color(0.08f, 0.08f, 0.08f, 0.65f); // Đen mờ 65%

        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0f); // Dưới cùng bên trái
        bgRt.anchorMax = new Vector2(0f, 0f);
        bgRt.pivot = new Vector2(0f, 0f);
        bgRt.anchoredPosition = new Vector2(50f, 50f);
        
        sprintBarWidth = 14f;
        sprintBarHeight = 220f;
        bgRt.sizeDelta = new Vector2(sprintBarWidth, sprintBarHeight);

        // Tạo Viền Neon Mỏng cho Stamina Bar
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(bgObj.transform, false);
        Image borderImg = borderObj.AddComponent<Image>();
        borderImg.color = new Color(0.4f, 0.4f, 0.4f, 0.35f); // Viền xám mờ nhẹ
        RectTransform borderRt = borderObj.GetComponent<RectTransform>();
        borderRt.anchorMin = Vector2.zero;
        borderRt.anchorMax = Vector2.one;
        borderRt.offsetMin = new Vector2(-1, -1);
        borderRt.offsetMax = new Vector2(1, 1);
        borderObj.transform.SetAsFirstSibling();

        // 3. Tạo Ảnh Điền Thể Lực (Fill Bar) - Màu vàng kim neon rực rỡ
        GameObject fillObj = new GameObject("SprintBarFill");
        fillObj.transform.SetParent(bgObj.transform, false);

        sprintBar = fillObj.AddComponent<Image>();
        sprintBar.color = new Color(1f, 0.75f, 0.3f, 0.85f); // Vàng kim neon ấm áp

        RectTransform fillRt = fillObj.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(1f, 1f);
        fillRt.pivot = new Vector2(0.5f, 0f); // Pivot cạnh đáy
        fillRt.anchoredPosition = Vector2.zero;
        fillRt.offsetMin = new Vector2(2, 2);
        fillRt.offsetMax = new Vector2(-2, -2);

        Debug.Log("[FirstPersonController Self-Heal] Đã tự động dựng thành công UI Stamina Bar tuyệt đẹp tại runtime!");
    }

    /// <summary>Giải phóng trạng thái đóng băng di chuyển của người chơi khi chơi lại.</summary>
    public void UnfreezePlayer()
    {
        enabled = true; // Bật lại script nếu bị tắt trong lúc caught sequence
        playerCanMove = true;
        cameraCanMove = true;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        Debug.Log("[FirstPersonController] Đã giải phóng di chuyển cho người chơi.");
    }
}



// Custom Editor
#if UNITY_EDITOR
    [CustomEditor(typeof(FirstPersonController)), InitializeOnLoadAttribute]
    public class FirstPersonControllerEditor : Editor
    {
    FirstPersonController fpc;
    SerializedObject SerFPC;

    private void OnEnable()
    {
        fpc = (FirstPersonController)target;
        SerFPC = new SerializedObject(fpc);
    }

    public override void OnInspectorGUI()
    {
        SerFPC.Update();

        EditorGUILayout.Space();
        GUILayout.Label("Modular First Person Controller", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 16 });
        GUILayout.Label("By Jess Case", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Normal, fontSize = 12 });
        GUILayout.Label("version 1.0.1", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Normal, fontSize = 12 });
        EditorGUILayout.Space();

        #region Camera Setup

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("Camera Setup", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));
        EditorGUILayout.Space();

        fpc.playerCamera = (Camera)EditorGUILayout.ObjectField(new GUIContent("Camera", "Camera attached to the controller."), fpc.playerCamera, typeof(Camera), true);
        fpc.fov = EditorGUILayout.Slider(new GUIContent("Field of View", "The camera’s view angle. Changes the player camera directly."), fpc.fov, fpc.zoomFOV, 179f);
        fpc.cameraCanMove = EditorGUILayout.ToggleLeft(new GUIContent("Enable Camera Rotation", "Determines if the camera is allowed to move."), fpc.cameraCanMove);

        GUI.enabled = fpc.cameraCanMove;
        fpc.invertCamera = EditorGUILayout.ToggleLeft(new GUIContent("Invert Camera Rotation", "Inverts the up and down movement of the camera."), fpc.invertCamera);
        fpc.mouseSensitivity = EditorGUILayout.Slider(new GUIContent("Look Sensitivity", "Determines how sensitive the mouse movement is."), fpc.mouseSensitivity, .1f, 10f);
        fpc.maxLookAngle = EditorGUILayout.Slider(new GUIContent("Max Look Angle", "Determines the max and min angle the player camera is able to look."), fpc.maxLookAngle, 40, 90);
        GUI.enabled = true;

        fpc.lockCursor = EditorGUILayout.ToggleLeft(new GUIContent("Lock and Hide Cursor", "Turns off the cursor visibility and locks it to the middle of the screen."), fpc.lockCursor);

        fpc.crosshair = EditorGUILayout.ToggleLeft(new GUIContent("Auto Crosshair", "Determines if the basic crosshair will be turned on, and sets is to the center of the screen."), fpc.crosshair);

        // Only displays crosshair options if crosshair is enabled
        if(fpc.crosshair) 
        { 
            EditorGUI.indentLevel++; 
            EditorGUILayout.BeginHorizontal(); 
            EditorGUILayout.PrefixLabel(new GUIContent("Crosshair Image", "Sprite to use as the crosshair.")); 
            fpc.crosshairImage = (Sprite)EditorGUILayout.ObjectField(fpc.crosshairImage, typeof(Sprite), false);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            fpc.crosshairColor = EditorGUILayout.ColorField(new GUIContent("Crosshair Color", "Determines the color of the crosshair."), fpc.crosshairColor);
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--; 
        }

        EditorGUILayout.Space();

        #region Camera Zoom Setup

        GUILayout.Label("Zoom", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));

        fpc.enableZoom = EditorGUILayout.ToggleLeft(new GUIContent("Enable Zoom", "Determines if the player is able to zoom in while playing."), fpc.enableZoom);

        GUI.enabled = fpc.enableZoom;
        fpc.holdToZoom = EditorGUILayout.ToggleLeft(new GUIContent("Hold to Zoom", "Requires the player to hold the zoom key instead if pressing to zoom and unzoom."), fpc.holdToZoom);
        fpc.zoomKey = (KeyCode)EditorGUILayout.EnumPopup(new GUIContent("Zoom Key", "Determines what key is used to zoom."), fpc.zoomKey);
        fpc.zoomFOV = EditorGUILayout.Slider(new GUIContent("Zoom FOV", "Determines the field of view the camera zooms to."), fpc.zoomFOV, .1f, fpc.fov);
        fpc.zoomStepTime = EditorGUILayout.Slider(new GUIContent("Step Time", "Determines how fast the FOV transitions while zooming in."), fpc.zoomStepTime, .1f, 10f);
        GUI.enabled = true;

        #endregion

        #endregion

        #region Movement Setup

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("Movement Setup", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));
        EditorGUILayout.Space();

        fpc.playerCanMove = EditorGUILayout.ToggleLeft(new GUIContent("Enable Player Movement", "Determines if the player is allowed to move."), fpc.playerCanMove);

        GUI.enabled = fpc.playerCanMove;
        fpc.walkSpeed = EditorGUILayout.Slider(new GUIContent("Walk Speed", "Determines how fast the player will move while walking."), fpc.walkSpeed, .1f, fpc.sprintSpeed);
        GUI.enabled = true;

        EditorGUILayout.Space();

        #region Sprint

        GUILayout.Label("Sprint", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));

        fpc.enableSprint = EditorGUILayout.ToggleLeft(new GUIContent("Enable Sprint", "Determines if the player is allowed to sprint."), fpc.enableSprint);

        GUI.enabled = fpc.enableSprint;
        fpc.unlimitedSprint = EditorGUILayout.ToggleLeft(new GUIContent("Unlimited Sprint", "Determines if 'Sprint Duration' is enabled. Turning this on will allow for unlimited sprint."), fpc.unlimitedSprint);
        fpc.sprintKey = (KeyCode)EditorGUILayout.EnumPopup(new GUIContent("Sprint Key", "Determines what key is used to sprint."), fpc.sprintKey);
        fpc.sprintSpeed = EditorGUILayout.Slider(new GUIContent("Sprint Speed", "Determines how fast the player will move while sprinting."), fpc.sprintSpeed, fpc.walkSpeed, 20f);

        //GUI.enabled = !fpc.unlimitedSprint;
        fpc.sprintDuration = EditorGUILayout.Slider(new GUIContent("Sprint Duration", "Determines how long the player can sprint while unlimited sprint is disabled."), fpc.sprintDuration, 1f, 20f);
        fpc.sprintCooldown = EditorGUILayout.Slider(new GUIContent("Sprint Cooldown", "Determines how long the recovery time is when the player runs out of sprint."), fpc.sprintCooldown, .1f, fpc.sprintDuration);
        //GUI.enabled = true;

        fpc.sprintFOV = EditorGUILayout.Slider(new GUIContent("Sprint FOV", "Determines the field of view the camera changes to while sprinting."), fpc.sprintFOV, fpc.fov, 179f);
        fpc.sprintFOVStepTime = EditorGUILayout.Slider(new GUIContent("Step Time", "Determines how fast the FOV transitions while sprinting."), fpc.sprintFOVStepTime, .1f, 20f);

        fpc.useSprintBar = EditorGUILayout.ToggleLeft(new GUIContent("Use Sprint Bar", "Determines if the default sprint bar will appear on screen."), fpc.useSprintBar);

        // Only displays sprint bar options if sprint bar is enabled
        if(fpc.useSprintBar)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.BeginHorizontal();
            fpc.hideBarWhenFull = EditorGUILayout.ToggleLeft(new GUIContent("Hide Full Bar", "Hides the sprint bar when sprint duration is full, and fades the bar in when sprinting. Disabling this will leave the bar on screen at all times when the sprint bar is enabled."), fpc.hideBarWhenFull);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Bar BG", "Object to be used as sprint bar background."));
            fpc.sprintBarBG = (Image)EditorGUILayout.ObjectField(fpc.sprintBarBG, typeof(Image), true);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Bar", "Object to be used as sprint bar foreground."));
            fpc.sprintBar = (Image)EditorGUILayout.ObjectField(fpc.sprintBar, typeof(Image), true);
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginHorizontal();
            fpc.sprintBarWidthPercent = EditorGUILayout.Slider(new GUIContent("Bar Width", "Determines the width of the sprint bar."), fpc.sprintBarWidthPercent, .1f, .5f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            fpc.sprintBarHeightPercent = EditorGUILayout.Slider(new GUIContent("Bar Height", "Determines the height of the sprint bar."), fpc.sprintBarHeightPercent, .001f, .025f);
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }
        GUI.enabled = true;

        EditorGUILayout.Space();

        #endregion

        #region Jump

        GUILayout.Label("Jump", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));

        fpc.enableJump = EditorGUILayout.ToggleLeft(new GUIContent("Enable Jump", "Determines if the player is allowed to jump."), fpc.enableJump);

        GUI.enabled = fpc.enableJump;
        fpc.jumpKey = (KeyCode)EditorGUILayout.EnumPopup(new GUIContent("Jump Key", "Determines what key is used to jump."), fpc.jumpKey);
        fpc.jumpPower = EditorGUILayout.Slider(new GUIContent("Jump Power", "Determines how high the player will jump."), fpc.jumpPower, .1f, 20f);
        GUI.enabled = true;

        EditorGUILayout.Space();

        #endregion

        #region Crouch

        GUILayout.Label("Crouch", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));

        fpc.enableCrouch = EditorGUILayout.ToggleLeft(new GUIContent("Enable Crouch", "Determines if the player is allowed to crouch."), fpc.enableCrouch);

        GUI.enabled = fpc.enableCrouch;
        fpc.holdToCrouch = EditorGUILayout.ToggleLeft(new GUIContent("Hold To Crouch", "Requires the player to hold the crouch key instead if pressing to crouch and uncrouch."), fpc.holdToCrouch);
        fpc.crouchKey = (KeyCode)EditorGUILayout.EnumPopup(new GUIContent("Crouch Key", "Determines what key is used to crouch."), fpc.crouchKey);
        fpc.crouchHeight = EditorGUILayout.Slider(new GUIContent("Crouch Height", "Determines the y scale of the player object when crouched."), fpc.crouchHeight, .1f, 1);
        fpc.speedReduction = EditorGUILayout.Slider(new GUIContent("Speed Reduction", "Determines the percent 'Walk Speed' is reduced by. 1 being no reduction, and .5 being half."), fpc.speedReduction, .1f, 1);
        GUI.enabled = true;

        #endregion

        #endregion

        #region Head Bob

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("Head Bob Setup", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));
        EditorGUILayout.Space();

        fpc.enableHeadBob = EditorGUILayout.ToggleLeft(new GUIContent("Enable Head Bob", "Determines if the camera will bob while the player is walking."), fpc.enableHeadBob);
        

        GUI.enabled = fpc.enableHeadBob;
        fpc.joint = (Transform)EditorGUILayout.ObjectField(new GUIContent("Camera Joint", "Joint object position is moved while head bob is active."), fpc.joint, typeof(Transform), true);
        fpc.bobSpeed = EditorGUILayout.Slider(new GUIContent("Speed", "Determines how often a bob rotation is completed."), fpc.bobSpeed, 1, 20);
        fpc.bobAmount = EditorGUILayout.Vector3Field(new GUIContent("Bob Amount", "Determines the amount the joint moves in both directions on every axes."), fpc.bobAmount);
        GUI.enabled = true;

        #endregion

        //Sets any changes from the prefab
        if(GUI.changed)
        {
            EditorUtility.SetDirty(fpc);
            Undo.RecordObject(fpc, "FPC Change");
            SerFPC.ApplyModifiedProperties();
        }
    }

}

#endif