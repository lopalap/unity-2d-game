using UnityEngine;
using System.Collections;

/// <summary>
/// FlyingEye 원거리 AI - 일정 거리 유지하며 투사체 발사
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class FlyingEyeController : MonoBehaviour
{
    [Header("플레이어")]
    public Transform player;

    [Header("이동")]
    public float moveSpeed    = 5f;
    public float detectRange  = 7f;    // 이 거리 안에 들어오면 활성화
    public float preferredDist = 3.5f; // 유지하려는 거리
    public float minDist      = 2.5f;  // 이보다 가까우면 뒤로 물러남

    [Header("공격")]
    public float      attackRange    = 5f;   // 이 거리 안에서 투사체 발사
    public float      attackCooldown = 2.0f;
    public float      windupTime     = 0.5f;
    public GameObject projectilePrefab;

    [Header("방 범위")]
    public Vector2 roomCenter;
    public Vector2 roomRange;

    private Rigidbody2D    rb;
    private SpriteRenderer sr;
    private EnemyHealth    health;
    private Animator       anim;

    private float attackTimer = 0f;
    private bool  isActing    = false;

    private static readonly int HashMoving = Animator.StringToHash("isMoving");
    private static readonly int HashAttack = Animator.StringToHash("Attack");

    void Awake()
    {
        rb     = GetComponent<Rigidbody2D>();
        sr     = GetComponent<SpriteRenderer>();
        health = GetComponent<EnemyHealth>();
        anim   = GetComponent<Animator>();
    }

    void Start()
    {
        if (player == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    void Update()
    {
        if (player == null) return;
        if (health != null && health.IsDead) return;

        attackTimer -= Time.deltaTime;

        if (isActing)
        {
            rb.linearVelocity = Vector2.zero;
            anim.SetBool(HashMoving, false);
            return;
        }

        float dist = Vector2.Distance(transform.position, player.position);
        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;

        if (dist > detectRange)
        {
            // 탐지 범위 밖: 정지
            rb.linearVelocity = Vector2.zero;
            anim.SetBool(HashMoving, false);
        }
        else if (dist < minDist)
        {
            // 너무 가까우면 뒤로 물러남
            rb.linearVelocity = -dir * moveSpeed;
            anim.SetBool(HashMoving, true);
            sr.flipX = dir.x < 0; // 플레이어 바라보기
        }
        else if (dist > preferredDist + 0.5f)
        {
            // 선호 거리보다 멀면 접근
            rb.linearVelocity = dir * moveSpeed;
            anim.SetBool(HashMoving, true);
            if (dir.x != 0) sr.flipX = dir.x < 0;
        }
        else
        {
            // 선호 거리 도달: 정지 후 공격
            rb.linearVelocity = Vector2.zero;
            anim.SetBool(HashMoving, false);
            if (dir.x != 0) sr.flipX = dir.x < 0;

            if (dist <= attackRange)
                TryFire();
        }

        if (roomRange.x > 0 && roomRange.y > 0)
            ClampToRoom();
    }

    void TryFire()
    {
        if (attackTimer > 0f || isActing) return;
        attackTimer = attackCooldown;
        StartCoroutine(WindupThenFire());
    }

    IEnumerator WindupThenFire()
    {
        isActing = true;

        // 전조: 흰색 깜빡임
        float elapsed = 0f;
        while (elapsed < windupTime)
        {
            elapsed += Time.deltaTime;
            float cycle = Mathf.PingPong(elapsed * 10f, 1f);
            sr.color = Color.Lerp(new Color(0.4f, 0.4f, 0.4f), Color.white, cycle);
            yield return null;
        }
        sr.color = Color.white;

        // 공격 애니메이션 트리거
        anim.SetTrigger(HashAttack);
        SoundManager.Instance?.PlaySFX(SoundManager.Instance.bossMagic);

        // 투사체 발사
        FireProjectile();
    }

    void FireProjectile()
    {
        if (projectilePrefab == null || player == null) return;
        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        var proj = Instantiate(projectilePrefab,
                               (Vector2)transform.position + dir * 0.5f,
                               Quaternion.identity);
        var ep = proj.GetComponent<EyeProjectile>();
        if (ep != null) { ep.sourceEnemy = gameObject; ep.Launch(dir); }
    }

    // Animation Event: Attack 애니메이션 마지막 프레임
    public void DeactivateHitbox()
    {
        isActing = false;
    }

    // 근접 공격 이벤트는 무시 (원거리이므로)
    public void ActivateHitbox() { }

    void ClampToRoom()
    {
        float cx = Mathf.Clamp(transform.position.x,
                               roomCenter.x - roomRange.x, roomCenter.x + roomRange.x);
        float cy = Mathf.Clamp(transform.position.y,
                               roomCenter.y - roomRange.y, roomCenter.y + roomRange.y);
        transform.position = new Vector3(cx, cy, transform.position.z);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, preferredDist);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, minDist);
    }
}
