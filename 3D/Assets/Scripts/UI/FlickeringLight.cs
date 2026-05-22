using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameUI
{
    /// <summary>
    /// Bóng đèn chập chờn tạo không khí kinh dị.
    /// Gắn script này vào Light ở MainMenu.
    /// Tự động tắt hoàn toàn khi load sang scene gameplay.
    /// </summary>
    public class FlickeringLight : MonoBehaviour
    {
        [Header("Light")]
        [Tooltip("Để trống để tự tìm Light trên GameObject này")]
        public Light targetLight;

        [Header("Emission Sync (tuỳ chọn)")]
        public Renderer bulbRenderer;
        public int materialIndex = 0;
        [ColorUsage(true, true)]
        public Color emissionOnColor = Color.white;
        public Color emissionOffColor = Color.black;

        [Header("Flicker Timing")]
        public float minIntensity = 0.2f;
        public float maxIntensity = 4.0f;
        public float minDelay = 0.01f;
        public float maxDelay = 0.2f;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip electricityBuzzClip;
        [Range(0f, 1f)]
        public float audioVolume = 0.5f;

        [Header("Scene Restriction")]
        [Tooltip("Tên CHÍNH XÁC của scene Main Menu (phân biệt HOA/thường)")]
        public string mainMenuSceneName = "MainMenu";

        // ── nội bộ ──
        private float defaultIntensity;
        private Material bulbMaterial;
        private Coroutine flickerCoroutine;

        private void Awake()
        {
            if (targetLight == null)
                targetLight = GetComponent<Light>();

            if (targetLight != null)
                defaultIntensity = targetLight.intensity;

            // Đăng ký lắng nghe mỗi lần scene mới được load
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            SetupMaterial();
            SetupAudio();
            ApplyForCurrentScene();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            RestoreLight();
            if (bulbMaterial != null) Destroy(bulbMaterial);
        }

        // Gọi mỗi khi bất kỳ scene nào được load xong
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyForCurrentScene();
        }

        /// <summary>Bật flicker nếu đang ở MainMenu, tắt hoàn toàn nếu không phải.</summary>
        private void ApplyForCurrentScene()
        {
            bool isMainMenu = SceneManager.GetActiveScene().name == mainMenuSceneName;

            if (isMainMenu)
            {
                StartFlicker();
            }
            else
            {
                StopFlicker();
            }
        }

        private void StartFlicker()
        {
            if (flickerCoroutine != null) return; // đã chạy rồi
            if (targetLight == null) return;

            targetLight.enabled = true;
            flickerCoroutine = StartCoroutine(FlickerRoutine());
        }

        private void StopFlicker()
        {
            if (flickerCoroutine != null)
            {
                StopCoroutine(flickerCoroutine);
                flickerCoroutine = null;
            }

            RestoreLight();

            if (audioSource != null && audioSource.isPlaying)
                audioSource.Stop();
        }

        private void RestoreLight()
        {
            if (targetLight != null)
            {
                targetLight.intensity = defaultIntensity;
                targetLight.enabled = true;
            }

            if (bulbMaterial != null)
                bulbMaterial.SetColor("_EmissionColor", emissionOnColor);
        }

        // ─────────────────────────────────────────────────────────
        //  Coroutine nhấp nháy
        // ─────────────────────────────────────────────────────────

        private IEnumerator FlickerRoutine()
        {
            while (true)
            {
                bool turnOff = Random.value > 0.6f;
                float intensity = turnOff
                    ? minIntensity
                    : Random.Range(minIntensity * 2f, maxIntensity);

                targetLight.intensity = intensity;

                if (bulbMaterial != null)
                {
                    Color emission = intensity > minIntensity * 1.5f ? emissionOnColor : emissionOffColor;
                    bulbMaterial.SetColor("_EmissionColor", emission);
                }

                if (audioSource != null && electricityBuzzClip != null)
                {
                    if (intensity > minIntensity * 1.5f)
                    {
                        if (!audioSource.isPlaying) audioSource.Play();
                        audioSource.volume = Random.Range(0.2f, 1f) * audioVolume;
                    }
                    else
                    {
                        audioSource.Stop();
                    }
                }

                yield return new WaitForSecondsRealtime(Random.Range(minDelay, maxDelay));
            }
        }

        // ─────────────────────────────────────────────────────────
        //  Setup lúc khởi động
        // ─────────────────────────────────────────────────────────

        private void SetupMaterial()
        {
            if (bulbRenderer != null && bulbRenderer.materials.Length > materialIndex)
            {
                bulbMaterial = bulbRenderer.materials[materialIndex];
                bulbMaterial.EnableKeyword("_EMISSION");
            }
        }

        private void SetupAudio()
        {
            if (electricityBuzzClip != null && audioSource == null)
            {
                audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.loop = true;
                audioSource.clip = electricityBuzzClip;
                audioSource.volume = audioVolume;
                audioSource.spatialBlend = 1f;
            }
        }
    }
}
