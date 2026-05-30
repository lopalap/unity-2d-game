using UnityEngine;

/// <summary>
/// Necromancer 보스 투사체 - 플레이어 방향으로 직선 이동, 충돌 시 데미지
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class NecromancerProjectile : MonoBehaviour
{
    public float speed    = 6f;
    public int   damage   = 20;
    public float lifetime = 4f;

    [HideInInspector] public GameObject sourceEnemy;

    private Rigidbody2D rb;
    private bool hasHit      = false;
    private bool isReflected = false;

    private const float CounterChance = 0.3f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    public void Launch(Vector2 direction)
    {
        rb.linearVelocity = direction.normalized * speed;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

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
                Hit();
            }
            else if (!other.isTrigger && !other.CompareTag("Player")) Hit();
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
            Hit();
        }
        else if (!other.isTrigger
                 && !other.CompareTag("Enemy")
                 && !other.CompareTag("Boss")
                 && !other.CompareTag("Untagged"))
        {
            Hit();
        }
    }

    void Hit()
    {
        hasHit = true;
        Destroy(gameObject);
    }
}
