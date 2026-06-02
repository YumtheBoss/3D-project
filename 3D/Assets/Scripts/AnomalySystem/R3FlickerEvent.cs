using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace AnomalySystem
{
    /// <summary>
    /// Gắn script này vào một GameObject có BoxCollider (Is Trigger = true) bao quanh khu vực R3.
    /// Khi người chơi bước vào, tất cả đèn trong R3 sẽ nhấp nháy và môi trường tối lại.
    /// Khi người chơi rời đi, mọi thứ trở về bình thường.
    /// </summary>
    public class R3FlickerEvent : MonoBehaviour
    {
        [Header("Đèn trong R3")]
        [Tooltip("Kéo tất cả các Light (Point Light, Spot Light...) trong R3 vào đây")]
        public Light[] r3Lights;

        [Header("Cài đặt nhấp nháy")]
        [Tooltip("Thời gian tối thiểu giữa mỗi lần nháy (giây)")]
        public float flickerMinInterval = 0.05f;
        [Tooltip("Thời gian tối đa giữa mỗi lần nháy (giây)")]
        public float flickerMaxInterval = 0.35f;
        [Tooltip("Xác suất đèn tắt hoàn toàn trong một lần nháy (0-1)")]
        [Range(0f, 1f)]
        public float offChance = 0.3f;
        [Tooltip("Cường độ đèn tối thiểu khi nháy (0 = tắt hoàn toàn)")]
        [Range(0f, 1f)]
        public float minIntensityPercent = 0.1f;

        [Header("Cài đặt môi trường tối")]
        [Tooltip("Màu Ambient khi ở trong R3 (nên để rất tối)")]
        public Color r3AmbientColor = new Color(0.02f, 0.01f, 0.02f);
        [Tooltip("Tốc độ chuyển màu khi vào/thoát R3")]
        public float ambientTransitionSpeed = 2f;

        [Header("Vignette / Màn hình tối (URP Post Processing)")]
        [Tooltip("Kéo Volume Profile URP có Vignette vào đây (nếu dùng PP). Để trống nếu không dùng.")]
        public Volume postProcessVolume;
        [Tooltip("Cường độ vignette khi ở trong R3 (0-1)")]
        [Range(0f, 1f)]
        public float vignetteIntensityInR3 = 0.55f;

        [Header("Âm thanh")]
        [Tooltip("Tiếng ù/vo ve điện khi đèn nhấp nháy. Để trống nếu không cần.")]
        public AudioClip electricHumSound;
        [Range(0f, 1f)]
        public float humVolume = 0.4f;

        // ---- Trạng thái nội bộ ----
        private bool playerInR3 = false;
        private float[] originalIntensities;
        private Color originalAmbientColor;
        private Coroutine flickerCoroutine;
        private AudioSource audioSource;
        private bool isTransitioning = false;

        // Vignette (URP)
        private UnityEngine.Rendering.Universal.Vignette vignetteEffect;
        private float originalVignetteIntensity = 0f;

        private void Awake()
        {
            // Lưu cường độ gốc của từng đèn
            if (r3Lights != null && r3Lights.Length > 0)
            {
                originalIntensities = new float[r3Lights.Length];
                for (int i = 0; i < r3Lights.Length; i++)
                {
                    if (r3Lights[i] != null)
                        originalIntensities[i] = r3Lights[i].intensity;
                }
            }

            // Lưu màu Ambient gốc
            originalAmbientColor = RenderSettings.ambientLight;

            // Tạo AudioSource cho tiếng vo ve
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = electricHumSound;
            audioSource.loop = true;
            audioSource.spatialBlend = 0f; // 2D sound
            audioSource.volume = 0f;
            audioSource.playOnAwake = false;

            // Lấy Vignette từ Post Processing Volume (nếu có)
            if (postProcessVolume != null)
            {
                postProcessVolume.profile.TryGet(out vignetteEffect);
                if (vignetteEffect != null)
                    originalVignetteIntensity = vignetteEffect.intensity.value;
            }

            // Đảm bảo Collider là Trigger
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnEnable()
        {
            RoomManager.OnRoomEntered += HandleRoomEntered;
        }

        private void OnDisable()
        {
            RoomManager.OnRoomEntered -= HandleRoomEntered;
            if (flickerCoroutine != null)
            {
                StopCoroutine(flickerCoroutine);
                flickerCoroutine = null;
            }
        }

        private void Start()
        {
            // Kiểm tra xem player đã ở sẵn trong trigger khi bắt đầu scene không
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                Collider col = GetComponent<Collider>();
                if (col != null && col.bounds.Contains(p.transform.position))
                {
                    StartFlickerEvent();
                }
            }
        }

        private void HandleRoomEntered(RoomManager.RoomState room)
        {
            if (room == RoomManager.RoomState.Room3)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null)
                {
                    Collider col = GetComponent<Collider>();
                    if (col != null && col.bounds.Contains(p.transform.position))
                    {
                        StartFlickerEvent();
                    }
                }
            }
            else
            {
                StopFlickerEvent();
            }
        }

        public void StartFlickerEvent()
        {
            if (playerInR3) return;
            Debug.Log("[R3FlickerEvent] Bắt đầu sự kiện đèn nhấp nháy!");
            playerInR3 = true;

            // Bắt đầu nhấp nháy
            if (flickerCoroutine != null) StopCoroutine(flickerCoroutine);
            flickerCoroutine = StartCoroutine(FlickerRoutine());

            // Chuyển màu tối + âm thanh
            StartCoroutine(TransitionEnvironment(true));
        }

        public void StopFlickerEvent()
        {
            if (!playerInR3) return;
            Debug.Log("[R3FlickerEvent] Khôi phục đèn về bình thường.");
            playerInR3 = false;

            // Dừng nhấp nháy
            if (flickerCoroutine != null)
            {
                StopCoroutine(flickerCoroutine);
                flickerCoroutine = null;
            }

            // Trả đèn về cường độ gốc
            RestoreLights();

            // Khôi phục môi trường
            StartCoroutine(TransitionEnvironment(false));
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            StartFlickerEvent();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            StopFlickerEvent();
        }

        // ────── Coroutine nhấp nháy ──────
        private IEnumerator FlickerRoutine()
        {
            while (playerInR3)
            {
                // Chờ một khoảng thời gian ngẫu nhiên
                float waitTime = Random.Range(flickerMinInterval, flickerMaxInterval);
                yield return new WaitForSeconds(waitTime);

                // Quyết định tắt hoàn toàn hay chỉ giảm cường độ
                bool turnOff = Random.value < offChance;

                SetLightsIntensity(turnOff ? 0f : Random.Range(minIntensityPercent, 1f));

                // Giữ trạng thái đó một chút rồi bật lại
                yield return new WaitForSeconds(Random.Range(0.04f, 0.15f));

                // Bật lại cường độ gần đủ (không hoàn toàn 100% để tạo cảm giác run rẩy)
                SetLightsIntensity(Random.Range(0.7f, 1f));
            }
        }

        // ────── Chuyển môi trường tối/sáng ──────
        private IEnumerator TransitionEnvironment(bool goingDark)
        {
            Color targetAmbient = goingDark ? r3AmbientColor : originalAmbientColor;
            float targetVignette = goingDark ? vignetteIntensityInR3 : originalVignetteIntensity;
            float targetHum = goingDark ? humVolume : 0f;

            // Bắt đầu phát âm thanh (nếu đang vào tối)
            if (goingDark && electricHumSound != null && !audioSource.isPlaying)
                audioSource.Play();

            float elapsed = 0f;
            Color startAmbient = RenderSettings.ambientLight;
            float startVignette = vignetteEffect != null ? vignetteEffect.intensity.value : 0f;
            float startHum = audioSource.volume;

            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime * ambientTransitionSpeed;
                float t = Mathf.Clamp01(elapsed);

                RenderSettings.ambientLight = Color.Lerp(startAmbient, targetAmbient, t);

                if (vignetteEffect != null)
                    vignetteEffect.intensity.value = Mathf.Lerp(startVignette, targetVignette, t);

                audioSource.volume = Mathf.Lerp(startHum, targetHum, t);

                yield return null;
            }

            // Dừng âm thanh nếu đã thoát R3
            if (!goingDark && audioSource.isPlaying)
                audioSource.Stop();
        }

        // ────── Hàm tiện ích ──────
        private void SetLightsIntensity(float intensityPercent)
        {
            if (r3Lights == null) return;
            for (int i = 0; i < r3Lights.Length; i++)
            {
                if (r3Lights[i] != null)
                    r3Lights[i].intensity = originalIntensities[i] * intensityPercent;
            }
        }

        private void RestoreLights()
        {
            if (r3Lights == null) return;
            for (int i = 0; i < r3Lights.Length; i++)
            {
                if (r3Lights[i] != null)
                    r3Lights[i].intensity = originalIntensities[i];
            }
        }

        // Khôi phục khi script bị destroy (tránh môi trường bị kẹt tối)
        private void OnDestroy()
        {
            RenderSettings.ambientLight = originalAmbientColor;
            if (vignetteEffect != null)
                vignetteEffect.intensity.value = originalVignetteIntensity;
            RestoreLights();
        }

        // Vẽ vùng trigger trong Editor để dễ chỉnh
        private void OnDrawGizmosSelected()
        {
            Collider col = GetComponent<Collider>();
            if (col == null) return;
            Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.3f);
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
            Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.9f);
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}
