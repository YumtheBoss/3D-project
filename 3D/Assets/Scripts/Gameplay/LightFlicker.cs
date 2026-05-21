using System.Collections;
using UnityEngine;

/// <summary>
/// Utility nhấp nháy cho một Light đơn lẻ.
/// Room 3 nên dùng R3FlickerEvent.cs (trigger-based, có vignette + ambient).
/// Script này dùng cho các đèn riêng lẻ ở Room 4, hành lang, v.v.
/// Gọi StartFlicker() / StopFlicker() thủ công hoặc từ event.
/// </summary>
[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    [Header("Timing")]
    public float minOffTime = 0.05f;
    public float maxOffTime = 0.3f;
    public float minOnTime  = 0.1f;
    public float maxOnTime  = 0.8f;

    [Tooltip("Bắt đầu nhấp nháy ngay khi GameObject được enable")]
    public bool flickerOnEnable = false;

    private Light targetLight;
    private Coroutine flickerCoroutine;
    private float originalIntensity;

    private void Awake()
    {
        targetLight = GetComponent<Light>();
        originalIntensity = targetLight.intensity;
    }

    private void OnEnable()
    {
        if (flickerOnEnable) StartFlicker();
    }

    private void OnDisable()
    {
        StopFlicker();
    }

    public void StartFlicker()
    {
        if (flickerCoroutine != null) StopCoroutine(flickerCoroutine);
        flickerCoroutine = StartCoroutine(FlickerRoutine());
    }

    public void StopFlicker()
    {
        if (flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine);
            flickerCoroutine = null;
        }
        if (targetLight != null)
        {
            targetLight.intensity = originalIntensity;
            targetLight.enabled = true;
        }
    }

    private IEnumerator FlickerRoutine()
    {
        while (true)
        {
            targetLight.enabled = true;
            targetLight.intensity = originalIntensity;
            yield return new WaitForSeconds(Random.Range(minOnTime, maxOnTime));

            targetLight.enabled = false;
            yield return new WaitForSeconds(Random.Range(minOffTime, maxOffTime));
        }
    }
}
