using UnityEngine;

// Đặt vào trigger collider ở cửa cuối hành lang Room 4.
// Khi player đến nơi, thông báo Room4ChaseSequence kết thúc chase và chuyển Room 5.
[RequireComponent(typeof(Collider))]
public class Room4ExitTrigger : MonoBehaviour
{
    [Tooltip("Script Room4ChaseSequence đang quản lý hành lang này")]
    public Room4ChaseSequence chaseSequence;

    private bool used = false;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (used) return;
        if (!other.CompareTag("Player")) return;

        used = true;
        chaseSequence?.OnPlayerReachedExit();
        gameObject.SetActive(false);
    }
}
