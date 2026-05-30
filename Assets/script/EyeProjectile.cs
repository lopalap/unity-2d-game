using UnityEngine;

/// <summary>
/// FlyingEye 투사체 - BossProjectile과 동일한 앞쪽 트리거 구조
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class EyeProjectile : MonoBehaviour
{
    [Header("투사체")]
    public float speed    = 5f;
    public int   damage   = 8;
    public float lifetime = 5f;

    [Header("앞쪽 트리거")]
    public float frontOffset = 1.5f;
    public float frontRadius = 0.2f;

    [Header("충돌 연출")]
    public float impactScale        = 1.4f;
    public int   impactSortingOrder = 20;

    [HideInInspector] public GameObject sourceEnemy;

    private Rigidbody2D    rb;
    private Animator       anim;
    private SpriteRenderer sr;
    private GameObject     frontTriggerObj;
    private bool           hasHit      = false;
    private bool           isReflected = false;

    private const float CounterChance = 0.3f;
    private static readonly int HashHit = Animator.StringToHash("Hit");

    void Awake()
    {
        rb   = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr   = GetComponent<SpriteRenderer>();
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void Start()
    {
        Destroy(gameObject, lifetime);
        BuildFrontTrigger();
    }

    void BuildFrontTrigger()
    {
        frontTriggerObj = new GameObject("FrontTrigger");
        frontTriggerObj.transform.SetParent(transform, false);
        frontTriggerObj.transform.localPosition = new Vector3(frontOffset, 0f, 0f);

        var col = frontTriggerObj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = frontRadius;
        col.enabled   = false; // 발사 직후 비활성 (자기 자신 충돌 방지)

        var bridge = frontTriggerObj.AddComponent<EyeProjFrontTrigger>();
        bridge.owner = this;

        // 0.1초 후 활성화
        Invoke(nameof(EnableFrontTrigger), 0.1f);
    }

    void EnableFrontTrigger()
    {
        if (hasHit || frontTriggerObj == null) return;
        var col = frontTriggerObj.GetComponent<CircleCollider2D>();
        if (col != null) col.enabled = true;
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
        if (other.GetComponent<EyeProjectile>() != null) return;
        if (other.transform.IsChildOf(transform)) return;

        if (isReflected)
        {
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

        if (frontTriggerObj != null)
        {
            transform.position = frontTriggerObj.transform.position;
            frontTriggerObj.SetActive(false);
        }

        transform.localScale = Vector3.one * impactScale;
        transform.rotation   = Quaternion.identity;
        if (sr != null) sr.sortingOrder = impactSortingOrder;

        anim.SetTrigger(HashHit);

        // Impact 애니메이션(0.75s) + 여유시간
        Destroy(gameObject, 1.5f);
    }

    // Animation Event: Impact 마지막 프레임에서 호출 → 즉시 소멸
    public void OnImpactEnd()
    {
        Destroy(gameObject);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, 0.15f);
        Gizmos.color = Color.red;
        Vector3 front = transform.position + transform.right * frontOffset;
        Gizmos.DrawWireSphere(front, frontRadius);
        Gizmos.DrawLine(transform.position, front);
    }
}
