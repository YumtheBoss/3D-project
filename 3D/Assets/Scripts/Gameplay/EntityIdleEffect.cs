using System.Collections;
using UnityEngine;

// Gắn script này vào các thực thể ở Room 4.
// Chúng xuất hiện và biến mất ngẫu nhiên để tạo hiệu ứng tâm lý đáng sợ.
// Không đuổi player - chỉ là hiệu ứng thị giác.
public class EntityIdleEffect : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("Thời gian hiện tối thiểu (giây)")]
    public float minVisibleTime = 0.5f;
    [Tooltip("Thời gian hiện tối đa (giây)")]
    public float maxVisibleTime = 2.5f;
    [Tooltip("Thời gian ẩn tối thiểu (giây)")]
    public float minHiddenTime = 1f;
    [Tooltip("Thời gian ẩn tối đa (giây)")]
    public float maxHiddenTime = 4f;

    [Header("Appearance")]
    [Tooltip("Danh sách Renderer để bật/tắt (nếu để trống, dùng tất cả Renderer trong children)")]
    public Renderer[] renderers;

    private Coroutine idleCoroutine;

    private void Awake()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>();
    }

    private void OnEnable()
    {
        idleCoroutine = StartCoroutine(IdleRoutine());
    }

    private void OnDisable()
    {
        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
            idleCoroutine = null;
        }
    }

    private IEnumerator IdleRoutine()
    {
        // Bắt đầu ẩn
        SetVisible(false);
        yield return new WaitForSeconds(Random.Range(0f, maxHiddenTime));

        while (true)
        {
            // Hiện
            SetVisible(true);
            yield return new WaitForSeconds(Random.Range(minVisibleTime, maxVisibleTime));

            // Ẩn
            SetVisible(false);
            yield return new WaitForSeconds(Random.Range(minHiddenTime, maxHiddenTime));
        }
    }

    private void SetVisible(bool visible)
    {
        foreach (Renderer r in renderers)
            if (r != null) r.enabled = visible;
    }
}
