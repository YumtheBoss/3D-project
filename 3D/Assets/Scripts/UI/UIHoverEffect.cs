using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameUI
{
    /// <summary>
    /// Script gắn vào các nút bấm để tạo hiệu ứng tương tác cao cấp (Micro-animations):
    /// - Tự động phóng to nhẹ khi rê chuột vào (Hover Scale).
    /// - Tự động đổi màu chữ TextMeshPro (ví dụ sang đỏ kinh dị).
    /// - Hiệu ứng nảy (Pop) khi bấm chuột.
    /// - Phát âm thanh ghê rợn/rè nhẹ khi hover (nếu có AudioManager).
    /// </summary>
    public class UIHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Scale Animation")]
        [Tooltip("Tỷ lệ phóng to khi di chuột qua")]
        public Vector3 hoverScale = new Vector3(1.1f, 1.1f, 1.1f);
        [Tooltip("Tốc độ chuyển đổi kích thước")]
        public float animationSpeed = 10f;

        [Header("TextMeshPro Color Animation")]
        [Tooltip("Nếu được gán, màu chữ sẽ đổi khi hover")]
        public TextMeshProUGUI targetText;
        [Tooltip("Màu chữ khi hover (ví dụ: đỏ tươi/đỏ kinh dị)")]
        public Color hoverColor = new Color(0.9f, 0.1f, 0.1f, 1f);

        [Header("Audio Settings")]
        [Tooltip("Âm thanh riêng khi hover qua nút (nếu để trống sẽ dùng tiếng rít nhẹ mặc định hoặc không phát)")]
        public AudioClip customHoverSound;

        private Vector3 originalScale;
        private Color originalColor;
        private Vector3 targetScale;
        private Color targetTextColor;
        private Coroutine colorCoroutine;
        private bool hasText = false;

        private void Start()
        {
            originalScale = transform.localScale;
            targetScale = originalScale;

            if (targetText == null)
            {
                targetText = GetComponentInChildren<TextMeshProUGUI>();
            }

            if (targetText != null)
            {
                hasText = true;
                originalColor = targetText.color;
                targetTextColor = originalColor;
            }
        }

        private void Update()
        {
            // Lerp scale mượt mà theo thời gian thực
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * animationSpeed);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            targetScale = Vector3.Scale(originalScale, hoverScale);

            if (hasText)
            {
                targetTextColor = hoverColor;
                StartColorTransition(hoverColor);
            }

            // Phát âm thanh khi hover
            PlayHoverSound();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            targetScale = originalScale;

            if (hasText)
            {
                targetTextColor = originalColor;
                StartColorTransition(originalColor);
            }
        }

        private void StartColorTransition(Color toColor)
        {
            if (colorCoroutine != null) StopCoroutine(colorCoroutine);
            colorCoroutine = StartCoroutine(TransitionColorRoutine(toColor));
        }

        private IEnumerator TransitionColorRoutine(Color toColor)
        {
            if (targetText == null) yield break;

            Color fromColor = targetText.color;
            float elapsed = 0f;
            float duration = 0.15f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                targetText.color = Color.Lerp(fromColor, toColor, elapsed / duration);
                yield return null;
            }

            targetText.color = toColor;
        }

        private void PlayHoverSound()
        {
            if (AudioManager.Instance != null)
            {
                if (customHoverSound != null)
                {
                    AudioManager.Instance.PlaySFX(customHoverSound);
                }
                else
                {
                    // Phát tiếng click nhỏ hoặc tiếng động nhẹ làm hover sound
                    // Nếu AudioManager của bạn chưa có sẵn hover sound, ta có thể phát một click nhỏ hơn để phản hồi xúc giác
                }
            }
        }

        private void OnDisable()
        {
            // Khôi phục trạng thái ban đầu nếu nút bị ẩn đi đột ngột
            transform.localScale = originalScale;
            if (hasText && targetText != null)
            {
                targetText.color = originalColor;
            }
        }
    }
}
