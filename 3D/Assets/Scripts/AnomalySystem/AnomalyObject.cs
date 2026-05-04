using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace AnomalySystem
{
    public class AnomalyObject : MonoBehaviour
    {
        [Header("Chọn loại biến đổi (Có thể tích nhiều cái)")]
        public bool useToggleObject;
        public bool useSwapObject;
        public bool useSwapMaterial;
        public bool useCustomEvent;
        public bool useWalkJumpscare;

        [Header("Toggle Object Settings")]
        [Tooltip("Chỉ Bật/Tắt duy nhất 1 vật thể (Ví dụ: cái đèn tự dưng tắt)")]
        public GameObject targetObject;
        public bool isActiveWhenNormal = true;

        [Header("Swap Object Settings")]
        [Tooltip("Biến đồ vật A thành đồ vật B (Ví dụ: chậu cây biến thành bức tượng)")]
        public GameObject normalObject;
        [Tooltip("Danh sách các đồ vật thay thế. Game sẽ chọn ngẫu nhiên 1 đồ vật từ đây.")]
        public GameObject[] anomalyObjects;

        [Header("Swap Material Settings")]
        [Tooltip("Renderer cần đổi Material (Ví dụ: bức tranh bị đổi màu)")]
        public MeshRenderer targetRenderer;
        [Tooltip("Danh sách các Material lỗi. Game sẽ chọn ngẫu nhiên 1 cái từ danh sách này.")]
        public Material[] anomalyMaterials;

        // Tự động lưu material gốc khi game bắt đầu
        private Material savedOriginalMaterial;

        [Header("Custom Event Settings")]
        [Tooltip("Dùng để gọi các hàm tự tạo, ví dụ như Play Animation, Play Sound...")]
        public UnityEvent OnSetNormal;
        public UnityEvent OnSetAnomaly;

        [Header("Walk Jumpscare Settings")]
        [Tooltip("Số giây giữ W liên tục trước khi bị hù")]
        public float holdDuration = 5f;
        [Tooltip("Kéo GameObject có script JumpscareController vào đây")]
        public JumpscareController jumpscareController;

        // ---- Biến nội bộ Walk Jumpscare ----
        private Coroutine walkJumpscareCoroutine;
        private bool jumpscareTriggered = false;

        private void Awake()
        {
            // Tự động lưu Material gốc của bức tranh ngay khi game bắt đầu
            if (useSwapMaterial && targetRenderer != null)
            {
                savedOriginalMaterial = targetRenderer.sharedMaterial;
                Debug.Log($"[AnomalyObject] Đã lưu Material gốc của {gameObject.name}: {savedOriginalMaterial.name}");
            }
        }

        public void SetNormal()
        {
            if (useToggleObject && targetObject != null)
            {
                targetObject.SetActive(isActiveWhenNormal);
            }

            if (useSwapObject)
            {
                if (normalObject != null) normalObject.SetActive(true);
                
                // Tắt tất cả các đồ vật lỗi
                if (anomalyObjects != null)
                {
                    foreach (var obj in anomalyObjects)
                    {
                        if (obj != null) obj.SetActive(false);
                    }
                }
            }

            if (useSwapMaterial && targetRenderer != null && savedOriginalMaterial != null)
            {
                targetRenderer.material = savedOriginalMaterial;
            }

            if (useCustomEvent)
            {
                OnSetNormal?.Invoke();
            }

            // Dừng coroutine Walk Jumpscare và reset trạng thái
            if (useWalkJumpscare)
            {
                if (walkJumpscareCoroutine != null)
                {
                    StopCoroutine(walkJumpscareCoroutine);
                    walkJumpscareCoroutine = null;
                }
                jumpscareTriggered = false;
                if (jumpscareController != null) jumpscareController.HideJumpscare();
            }
        }

        public void SetAnomaly()
        {
            if (useToggleObject && targetObject != null)
            {
                targetObject.SetActive(!isActiveWhenNormal);
            }

            if (useSwapObject)
            {
                if (normalObject != null) normalObject.SetActive(false);
                
                if (anomalyObjects != null && anomalyObjects.Length > 0)
                {
                    // Đảm bảo tắt hết các vật thể lỗi trước
                    foreach (var obj in anomalyObjects)
                    {
                        if (obj != null) obj.SetActive(false);
                    }
                    
                    // Bốc thăm ngẫu nhiên 1 vật thể lỗi để bật lên
                    int randomIndex = Random.Range(0, anomalyObjects.Length);
                    if (anomalyObjects[randomIndex] != null)
                    {
                        anomalyObjects[randomIndex].SetActive(true);
                    }
                }
            }

            if (useSwapMaterial && targetRenderer != null && anomalyMaterials != null && anomalyMaterials.Length > 0)
            {
                // Chọn ngẫu nhiên 1 material từ danh sách
                int randomIndex = Random.Range(0, anomalyMaterials.Length);
                if (anomalyMaterials[randomIndex] != null)
                {
                    targetRenderer.material = anomalyMaterials[randomIndex];
                }
            }

            if (useCustomEvent)
            {
                OnSetAnomaly?.Invoke();
            }

            // Bắt đầu đếm giờ chờ người chơi giữ W
            if (useWalkJumpscare)
            {
                jumpscareTriggered = false;
                if (jumpscareController != null) jumpscareController.HideJumpscare();
                if (walkJumpscareCoroutine != null) StopCoroutine(walkJumpscareCoroutine);
                walkJumpscareCoroutine = StartCoroutine(WalkJumpscareRoutine());
            }
        }

        private IEnumerator WalkJumpscareRoutine()
        {
            float holdTimer = 0f;
            Debug.Log($"[WalkJumpscare] Bắt đầu lắng nghe phím W. Controller: {(jumpscareController != null ? jumpscareController.name : "NULL - CHƯA GÁN!")}");

            while (!jumpscareTriggered)
            {
                if (Input.GetKey(KeyCode.W))
                {
                    holdTimer += Time.deltaTime;

                    if (holdTimer >= holdDuration)
                    {
                        jumpscareTriggered = true;
                        Debug.Log("[WalkJumpscare] Đủ 5 giây! Gọi TriggerJumpscare()!");

                        if (jumpscareController != null)
                            jumpscareController.TriggerJumpscare();
                        else
                            Debug.LogError("[WalkJumpscare] jumpscareController chưa được gán!");

                        yield break;
                    }
                }
                else
                {
                    holdTimer = 0f;
                }

                yield return null;
            }
        }
    }
}
