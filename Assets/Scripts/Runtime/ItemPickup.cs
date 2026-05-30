using UnityEngine;

/// <summary>
/// 아이템 픽업 — 스폰 후 짧은 대기 뒤 플레이어 쪽으로 가속 이동하며 흡수됩니다.
/// </summary>
public class ItemPickup : MonoBehaviour
{
    public ItemData itemData;

    [Header("자석 이동")]
    [Tooltip("스폰 후 끌리기 시작까지 대기 시간(초)")]
    public float attractDelay    = 0.35f;
    [Tooltip("초기 이동 속도")]
    public float attractSpeed    = 2f;
    [Tooltip("최대 이동 속도")]
    public float maxSpeed        = 14f;
    [Tooltip("속도 가속도")]
    public float acceleration    = 10f;
    [Tooltip("이 거리 이하면 즉시 수집")]
    public float collectDistance = 0.25f;

    private Transform player;
    private float     delayTimer;
    private float     currentSpeed;
    private bool      attracting;

    void Start()
    {
        // 콜라이더는 트리거로 (다른 오브젝트와 물리 충돌 방지)
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        var go = GameObject.FindWithTag("Player");
        if (go != null) player = go.transform;

        delayTimer   = attractDelay;
        currentSpeed = attractSpeed;
    }

    void Update()
    {
        if (player == null) return;

        // 대기 타이머
        if (!attracting)
        {
            delayTimer -= Time.deltaTime;
            if (delayTimer <= 0f) attracting = true;
            return;
        }

        // 플레이어 방향으로 가속 이동
        Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
        float   dist     = toPlayer.magnitude;

        currentSpeed = Mathf.Min(currentSpeed + acceleration * Time.deltaTime, maxSpeed);
        transform.position += (Vector3)(toPlayer.normalized * currentSpeed * Time.deltaTime);

        // 수집
        if (dist < collectDistance)
            Collect();
    }

    void Collect()
    {
        SoundManager.Instance?.PlaySFX(SoundManager.Instance.itemPickup);
        var stats = player.GetComponent<PlayerStats>();
        if (stats != null)
            stats.ApplyItem(itemData);

        Destroy(gameObject);
    }
}
