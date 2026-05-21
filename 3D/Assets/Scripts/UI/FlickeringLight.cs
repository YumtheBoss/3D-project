using System.Collections;
using UnityEngine;

namespace GameUI
{
    /// <summary>
    /// Script mô phỏng bóng đèn chập chờn (flicker) để tạo không khí kinh dị u ám.
    /// Gắn script này vào đèn Spotlight/Point Light phía trên cửa gỗ trong Menu.
    /// - Nhấp nháy nguồn sáng (Light component) ngẫu nhiên.
    /// - Tự động đồng bộ hóa bật/tắt vật liệu phát sáng (Emission Material) của bóng đèn.
    /// - Phát tiếng điện giật xèo xèo chập chờn đồng bộ (nếu gán AudioSource và AudioClip).
    /// </summary>
    public class FlickeringLight : MonoBehaviour
    {
        [Header("Light Components")]
        [Tooltip("Đèn cần làm nhấp nháy. Nếu trống, script tự quét lấy Light trên GameObject này")]
        public Light targetLight;

        [Header("Emission Material Sync (Optional)")]
        [Tooltip("Renderer chứa vật liệu bóng đèn để bật/tắt phát sáng (Emission)")]
        public Renderer bulbRenderer;
        [Tooltip("Chỉ số của vật liệu bóng đèn trong Renderer (mặc định là 0)")]
        public int materialIndex = 0;
        [Tooltip("Màu phát sáng (Emission) khi đèn BẬT")]
        [ColorUsage(true, true)]
        public Color emissionOnColor = Color.white;
        [Tooltip("Màu phát sáng khi đèn TẮT")]
        public Color emissionOffColor = Color.black;

        [Header("Flicker Timing")]
        [Tooltip("Cường độ ánh sáng nhỏ nhất")]
        public float minIntensity = 0.2f;
        [Tooltip("Cường độ ánh sáng lớn nhất")]
        public float maxIntensity = 4.0f;
        [Tooltip("Thời gian trễ tối thiểu giữa các lần đổi trạng thái (giây)")]
        public float minDelay = 0.01f;
        [Tooltip("Thời gian trễ tối đa giữa các lần đổi trạng thái (giây)")]
        public float maxDelay = 0.2f;

        [Header("Horror Flavor - Audio")]
        [Tooltip("Nguồn phát âm thanh điện chập chờn. Để trống sẽ tự động thêm nếu có âm thanh")]
        public AudioSource audioSource;
        [Tooltip("Tiếng rè điện chập chập, xèo xèo")]
        public AudioClip electricityBuzzClip;
        [Range(0f, 1f)]
        public float audioVolume = 0.5f;

        [Header("Scene Restriction")]
        [Tooltip("Chỉ cho phép nhấp nháy trong màn hình MainMenu (Không chạy ở Gameplay để tránh lag). Nếu tắt, đèn sẽ nhấp nháy ở mọi scene.")]
        public bool limitToMainMenuOnly = true;

        private float defaultIntensity;
        private Material bulbMaterial;
        private bool isFlickering = true;

        private void Awake()
        {
            // Kiểm tra scene ngay từ Awake để vô hiệu hóa script sớm nhất có thể ngoài MainMenu nếu bật giới hạn
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (limitToMainMenuOnly && !string.IsNullOrEmpty(sceneName) && !sceneName.ToLower().Contains("mainmenu"))
            {
                enabled = false;
            }
        }

        private void Start()
        {
            if (targetLight == null)
            {
                targetLight = GetComponent<Light>();
            }

            if (targetLight != null)
            {
                defaultIntensity = targetLight.intensity;
            }
            else
            {
                Debug.LogWarning("[FlickeringLight] Không tìm thấy Light component!");
            }

            // Chỉ chạy hiệu ứng chập chờn nếu không bị giới hạn hoặc tên scene chứa "mainmenu"
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (limitToMainMenuOnly && (string.IsNullOrEmpty(sceneName) || !sceneName.ToLower().Contains("mainmenu")))
            {
                if (targetLight != null)
                {
                    targetLight.intensity = defaultIntensity;
                }
                enabled = false;
                return;
            }

            // Thiết lập vật liệu phát sáng nếu có Renderer
            if (bulbRenderer != null && bulbRenderer.materials.Length > materialIndex)
            {
                // Sử dụng instances vật liệu riêng để tránh sửa đổi prefab gốc vĩnh viễn
                bulbMaterial = bulbRenderer.materials[materialIndex];
                bulbMaterial.EnableKeyword("_EMISSION");
            }

            // Tự động cấu hình AudioSource nếu có âm thanh mà chưa gán nguồn phát
            if (electricityBuzzClip != null && audioSource == null)
            {
                audioSource = gameObject.GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
                audioSource.playOnAwake = false;
                audioSource.loop = true;
                audioSource.clip = electricityBuzzClip;
                audioSource.volume = audioVolume;
                audioSource.spatialBlend = 1f; // 3D Sound
            }

            StartCoroutine(FlickerRoutine());
        }

        private IEnumerator FlickerRoutine()
        {
            while (isFlickering)
            {
                if (targetLight != null)
                {
                    // Random trạng thái đèn: Tắt hẳn hoặc để ở mức tối thiểu/tối đa ngẫu nhiên
                    bool turnOff = Random.value > 0.6f;
                    float randomIntensity = turnOff ? minIntensity : Random.Range(minIntensity * 2f, maxIntensity);
                    
                    targetLight.intensity = randomIntensity;

                    // Đồng bộ vật liệu phát sáng (Emission)
                    if (bulbMaterial != null)
                    {
                        Color currentEmission = (randomIntensity > minIntensity * 1.5f) ? emissionOnColor : emissionOffColor;
                        bulbMaterial.SetColor("_EmissionColor", currentEmission);
                    }

                    // Đồng bộ âm thanh
                    if (audioSource != null && electricityBuzzClip != null)
                    {
                        if (randomIntensity > minIntensity * 1.5f)
                        {
                            if (!audioSource.isPlaying)
                            {
                                audioSource.Play();
                            }
                            audioSource.volume = Random.Range(0.2f, 1f) * audioVolume;
                        }
                        else
                        {
                            audioSource.Stop();
                        }
                    }
                }

                // Chờ một khoảng thời gian ngẫu nhiên trước khi nháy tiếp
                yield return new WaitForSecondsRealtime(Random.Range(minDelay, maxDelay));
            }
        }

        private void OnDestroy()
        {
            // Trả vật liệu về bình thường khi bị huỷ
            if (bulbMaterial != null)
            {
                Destroy(bulbMaterial);
            }
        }
    }
}
