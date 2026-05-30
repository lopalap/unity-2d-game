using UnityEngine;

/// <summary>
/// 보스 투사체 - 앞쪽 트리거로 충돌 감지, 관통 없음
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class BossProjectile : MonoBehaviour
{
    [Header("투사체")]
    public float speed    = 6f;
    public int   damage   = 15;
    public float lifetime = 4f;

    [Header("앞쪽 트리거")]
    public float frontOffset = 1.9f;    // 진행 방향 앞쪽 거리
    public float frontRadius = 0.18f;   // 트리거 반지름

    [Header("폭발 연출")]
    public float impactScale        = 1.7f;
    public int   impactSortingOrder = 20;

    [HideInInspector] public GameObject sourceEnemy;   // 발사한 적 (가시갑옷·반격용)

    private Rigidbody2D    rb;
    private Animator       anim;
    private SpriteRenderer sr;
    private bool           hasHit     = false;
    private bool           isReflected = false;
    private int            _origSortingOrder;

    private const float CounterChance = 0.3f;

    // 앞쪽에 생성되는 자식 트리거 오브젝트
    private GameObject frontTriggerObj;

    private static readonly int HashHit = Animator.StringToHash("Hit");

    void Awake()
    {
        rb   = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr   = GetComponent<SpriteRenderer>();
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _origSortingOrder = sr != null ? sr.sortingOrder : 0;
        BuildFrontTrigger();
    }

    void OnEnable()
    {
        // 풀에서 꺼낼 때마다 상태 초기화
        hasHit      = false;
        isReflected = false;
        rb.bodyType       = RigidbodyType2D.Dynamic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.linearVelocity = Vector2.zero;
        transform.localScale = Vector3.one;
        if (sr != null) sr.sortingOrder = _origSortingOrder;
        if (frontTriggerObj != null) frontTriggerObj.SetActive(true);
        CancelInvoke();
        Invoke(nameof(ReturnToPool), lifetime);
    }

    void OnDisable()
    {
        CancelInvoke();
    }

    void BuildFrontTrigger()
    {
        frontTriggerObj = new GameObject("FrontTrigger");
        frontTriggerObj.transform.SetParent(transform, false);

        // 로컬 X+ 방향이 진행 방향 → 앞쪽에 배치
        frontTriggerObj.transform.localPosition = new Vector3(frontOffset, 0f, 0f);

        var col = frontTriggerObj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = frontRadius;

        // 충돌 이벤트를 부모로 전달하는 브리지 컴포넌트 추가
        var bridge = frontTriggerObj.AddComponent<ProjectileFrontTrigger>();
        bridge.owner = this;
    }

    void OnDrawGizmos()
    {
        // 투사체 본체 (흰색)
        Gizmos.color = new Color(1f, 1f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, 0.15f);

        // 앞쪽 트리거 위치 (빨간색)
        Gizmos.color = Color.red;
        Vector3 front = transform.position + transform.right * frontOffset;
        Gizmos.DrawWireSphere(front, frontRadius);
        Gizmos.DrawLine(transform.position, front);
    }

    public void Launch(Vector2 direction)
    {
        rb.linearVelocity = direction.normalized * speed;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void OnFrontHit(Collider2D other)
    {
        if (hasHit) return;
        if (other.GetComponent<BossProjectile>() != null) return;
        if (other.transform.IsChildOf(transform)) return;

        if (isReflected)
        {
            // 반사된 투사체 → 적에게 피해
            var eh  = other.GetComponentInParent<EnemyHealth>();
            var bh  = other.GetComponentInParent<BossHealth>();
            var nbh = other.GetComponentInParent<NecromancerBossHealth>();
            if (eh != null || bh != null || nbh != null)
            {
                if (eh  != null) eh.TakeDamage(damage);
                if (bh  != null) bh.TakeDamage(damage);
                if (nbh != null) nbh.TakeDamage(damage);
                OnHit();
            }
            else if (!other.isTrigger) OnHit();
            return;
        }

        if (other.CompareTag("Player"))
        {
            var ph = other.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                // 반격 체크 (30% 확률)
                var ps = ph.GetComponent<PlayerStats>();
                if (ps != null && ps.CounterUnlocked && sourceEnemy != null && Random.value < CounterChance)
                {
                    isReflected = true;
                    Vector2 reflectDir = ((Vector2)sourceEnemy.transform.position - (Vector2)transform.position).normalized;
                    Launch(reflectDir);
                    return;
                }
                ph.TakeDamage(damage, sourceEnemy);
            }
        }

        OnHit();
    }

    void OnHit()
    {
        hasHit = true;

        rb.linearVelocity = Vector2.zero;
        rb.bodyType       = RigidbodyType2D.Static;

        // 앞쪽 트리거 위치로 이동 → 폭발이 트리거 위치에서 발생
        if (frontTriggerObj != null)
            transform.position = frontTriggerObj.transform.position;

        // 앞쪽 트리거 비활성화
        if (frontTriggerObj != null)
            frontTriggerObj.SetActive(false);

        // 폭발: 크기 확대 + 회전 초기화 + 앞에 표시
        transform.localScale = Vector3.one * impactScale;
        transform.rotation   = Quaternion.identity;
        if (sr != null) sr.sortingOrder = impactSortingOrder;

        anim.SetTrigger(HashHit);

        CancelInvoke();
        Invoke(nameof(ReturnToPool), 1.2f);
    }

    void ReturnToPool()
    {
        PoolManager.Return(gameObject);
    }
}
