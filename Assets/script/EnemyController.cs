using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour
{
    [Header("플레이어")]
    public Transform player;

    [Header("이동")]
    public float moveSpeed   = 2f;
    public float detectRange = 5f;

    [Header("공격")]
    public float attackRange    = 0.8f;
    public float attackCooldown = 1.5f;

    [Header("공격 전조 (Windup)")]
    [Tooltip("공격 애니메이션 전에 멈추고 경고하는 시간(초)")]
    public float windupTime  = 0.6f;

    [Header("방 범위 (Clamp)")]
    public Vector2 roomCenter;
    public Vector2 roomRange;

    [Header("사운드")]
    public AudioClip attackSFX;
    [Tooltip("0 = 전체 재생, 양수 = 해당 초 후 정지")]
    public float attackSFXDuration = 0f;

    [Header("데이터 (ScriptableObject)")]
    [Tooltip("할당하면 위 수치 대신 SO 값이 적용됩니다")]
    [SerializeField] EnemyData data;

    private Animator       anim;
    private SpriteRenderer sr;
    private EnemyHealth    health;
    private Rigidbody2D    rb;

    private float attackTimer = 0f;
    // 전조 + 공격 애니메이션 재생 중 모두 이동 차단
    private bool  isActing       = false;
    private float _isActingTimer = 0f;

    // 배회 상태
    private Vector2 wanderTarget;
    private float   wanderTimer  = 0f;   // > 0 : 목적지로 이동 중, <= 0 : 다음 포인트 선택 대기
    private bool    wanderIdle   = true; // true : 대기, false : 이동
    private Vector2 spawnPos;

    private static readonly int HashMoving = Animator.StringToHash("isMoving");
    private static readonly int HashAttack = Animator.StringToHash("Attack");

    void Awake()
    {
        anim   = GetComponent<Animator>();
        sr     = GetComponent<SpriteRenderer>();
        health = GetComponent<EnemyHealth>();
        rb     = GetComponent<Rigidbody2D>();

        if (data != null)
        {
            moveSpeed      = data.moveSpeed;
            detectRange    = data.detectRange;
            attackRange    = data.attackRange;
            attackCooldown = data.attackCooldown;
            windupTime     = data.windupTime;
        }
    }

    void Start()
    {
        if (player == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }
        spawnPos = transform.position;
    }

    void Update()
    {
        if (health != null && health.IsDead) return;

        attackTimer -= Time.deltaTime;

        // 전조 or 공격 중이면 무조건 정지
        if (isActing)
        {
            _isActingTimer += Time.deltaTime;
            if (_isActingTimer > windupTime + 1.5f)
            {
                isActing = false;
                _isActingTimer = 0f;
                sr.color = Color.white;
            }
            rb.linearVelocity = Vector2.zero;
            anim.SetBool(HashMoving, false);
            return;
        }
        _isActingTimer = 0f;

        bool chasing = false;
        if (player != null)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist <= attackRange)
            {
                rb.linearVelocity = Vector2.zero;
                anim.SetBool(HashMoving, false);
                TryAttack();
                chasing = true;
            }
            else if (dist <= detectRange)
            {
                MoveTowardPlayer();
                chasing = true;
            }
        }

        if (!chasing)
            Wander();

        if (roomRange.x > 0 && roomRange.y > 0)
            ClampToRoom();
    }

    // ── 배회 ─────────────────────────────────────────────────
    void Wander()
    {
        wanderTimer -= Time.deltaTime;

        if (wanderIdle)
        {
            // 대기 중 → 타이머 만료 시 새 목적지 선택
            rb.linearVelocity = Vector2.zero;
            anim.SetBool(HashMoving, false);
            if (wanderTimer <= 0f)
            {
                wanderTarget = PickWanderPoint();
                wanderTimer  = Random.Range(2f, 5f);
                wanderIdle   = false;
            }
        }
        else
        {
            // 이동 중
            float dist = Vector2.Distance(transform.position, wanderTarget);
            if (dist < 0.15f || wanderTimer <= 0f)
            {
                // 목적지 도착 or 타임아웃 → 대기로 전환
                rb.linearVelocity = Vector2.zero;
                anim.SetBool(HashMoving, false);
                wanderTimer = Random.Range(1f, 3f);
                wanderIdle  = true;
            }
            else
            {
                Vector2 dir = (wanderTarget - (Vector2)transform.position).normalized;
                rb.linearVelocity = dir * moveSpeed * 0.55f;
                anim.SetBool(HashMoving, true);
                if (dir.x != 0) sr.flipX = dir.x < 0;
            }
        }
    }

    Vector2 PickWanderPoint()
    {
        // roomRange가 설정된 경우 방 안에서, 아니면 스폰 위치 반경 3f 안에서 랜덤 선택
        Vector2 center = (roomRange.x > 0 && roomRange.y > 0) ? roomCenter : spawnPos;
        float   rx     = (roomRange.x > 0) ? roomRange.x * 0.8f : 3f;
        float   ry     = (roomRange.y > 0) ? roomRange.y * 0.8f : 3f;

        return center + new Vector2(
            Random.Range(-rx, rx),
            Random.Range(-ry, ry));
    }

    void MoveTowardPlayer()
    {
        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        rb.linearVelocity = dir * moveSpeed;
        anim.SetBool(HashMoving, true);
        if (dir.x != 0) sr.flipX = dir.x < 0;
    }

    void TryAttack()
    {
        if (attackTimer > 0f || isActing) return;
        attackTimer = attackCooldown;
        StartCoroutine(WindupThenAttack());
    }

    IEnumerator WindupThenAttack()
    {
        isActing = true;

        // ── 전조: 하얀색으로 깜빡이며 공격 예고 ──
        float elapsed = 0f;
        while (elapsed < windupTime)
        {
            elapsed += Time.deltaTime;
            float cycle = Mathf.PingPong(elapsed * 10f, 1f);
            sr.color = Color.Lerp(new Color(0.4f, 0.4f, 0.4f, 1f), Color.white, cycle);
            yield return null;
        }
        sr.color = Color.white;

        // ── 공격 애니메이션 발동 ──
        // isActing = true 유지 → DeactivateHitbox() 호출 시 해제
        anim.SetTrigger(HashAttack);
        if (attackSFXDuration > 0f)
            SoundManager.Instance?.PlaySFXTimed(attackSFX, attackSFXDuration);
        else
            SoundManager.Instance?.PlaySFX(attackSFX);
    }

    // ── Animation Event 콜백 ──────────────────────────────
    public void ActivateHitbox()
    {
        if (player == null) return;
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > attackRange * 1.5f) return;

        var ph = player.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            var ed = GetComponent<EnemyDamage>();
            ph.TakeDamage(ed != null ? ed.damage : 10);
        }
    }

    // 공격 애니메이션 마지막 프레임 → 이동 재개
    public void DeactivateHitbox()
    {
        isActing = false;
        _isActingTimer = 0f;
    }

    // EnemyHealth에서 피격 시 호출 — 애니메이션 끊겨도 isActing 리셋
    public void OnHit()
    {
        StopAllCoroutines();
        isActing = false;
        _isActingTimer = 0f;
        sr.color = Color.white;
    }

    void ClampToRoom()
    {
        float cx = Mathf.Clamp(transform.position.x,
                               roomCenter.x - roomRange.x,
                               roomCenter.x + roomRange.x);
        float cy = Mathf.Clamp(transform.position.y,
                               roomCenter.y - roomRange.y,
                               roomCenter.y + roomRange.y);
        transform.position = new Vector3(cx, cy, transform.position.z);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(roomCenter, new Vector3(roomRange.x * 2, roomRange.y * 2, 0));
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}
