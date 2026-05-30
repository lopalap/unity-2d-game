using UnityEngine;
using System.Collections;

/// <summary>
/// 중간보스 AI - EnemyController 기반, 분노 단계 + 드롭 키 추가
/// </summary>
public class BossController : MonoBehaviour
{
    [Header("플레이어")]
    public Transform player;

    [Header("이동")]
    public float moveSpeed   = 2.5f;
    public float detectRange = 8f;

    [Header("공격")]
    public float attackRange    = 1.0f;
    public float attackCooldown = 2.0f;

    [Header("공격 전조 (Windup)")]
    [Tooltip("공격 전 경고 시간(초)")]
    public float windupTime = 0.8f;

    [Header("분노 단계 (HP 50% 이하)")]
    public float enrageSpeedMult   = 1.6f;   // 이동속도 배율
    public float enrageCooldownMult = 0.65f; // 공격쿨다운 배율

    [Header("투사체")]
    public GameObject projectilePrefab;   // BossProjectile 프리팹

    [Header("방 범위 (Clamp)")]
    public Vector2 roomCenter;
    public Vector2 roomRange;

    private Animator       anim;
    private SpriteRenderer sr;
    private BossHealth     health;
    private Rigidbody2D    rb;

    private float attackTimer = 0f;
    private bool  isActing    = false;
    private float _isActingTimer = 0f;  // 안전장치: isActing 타임아웃  // 전조 + 공격 애니메이션 중 이동 차단
    private bool  isEnraged   = false;

    private float currentMoveSpeed;
    private float currentCooldown;

    private static readonly int HashMoving = Animator.StringToHash("isMoving");
    private static readonly int HashAttack = Animator.StringToHash("Attack");

    void Awake()
    {
        anim   = GetComponent<Animator>();
        sr     = GetComponent<SpriteRenderer>();
        health = GetComponent<BossHealth>();
        rb     = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        if (player == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }

        currentMoveSpeed = moveSpeed;
        currentCooldown  = attackCooldown;

        // 보스 이동 시 플레이어를 물리적으로 밀지 않도록
        if (player != null)
        {
            var playerCols = player.GetComponents<Collider2D>();
            foreach (var bc in GetComponents<Collider2D>())
            {
                if (bc.isTrigger) continue;
                foreach (var pc in playerCols)
                    Physics2D.IgnoreCollision(bc, pc, true);
            }
        }
    }

    // BossHealth에서 체력 변화 시 호출
    public void OnHealthChanged(int current, int max)
    {
        if (!isEnraged && current <= max / 2)
        {
            isEnraged        = true;
            currentMoveSpeed = moveSpeed   * enrageSpeedMult;
            currentCooldown  = attackCooldown * enrageCooldownMult;
            StartCoroutine(EnrageEffect());
        }
    }

    IEnumerator EnrageEffect()
    {
        // 분노 진입 시 빠르게 깜빡임
        for (int i = 0; i < 6; i++)
        {
            sr.color = new Color(1f, 0.3f, 0.3f);
            yield return new WaitForSeconds(0.07f);
            sr.color = Color.white;
            yield return new WaitForSeconds(0.07f);
        }
    }

void Update()
    {
        if (player == null) return;
        if (health != null && health.IsDead) return;

        attackTimer -= Time.deltaTime;

        // isActing 안전장치: windupTime + 공격 애니 + 여유시간 초과 시 강제 해제
        if (isActing)
        {
            _isActingTimer += Time.deltaTime;
            if (_isActingTimer > windupTime + 1.5f)
            {
                isActing = false;
                _isActingTimer = 0f;
                sr.color = isEnraged ? new Color(1f, 0.5f, 0.5f) : Color.white;
            }
            rb.linearVelocity = Vector2.zero;
            anim.SetBool(HashMoving, false);
            return;
        }
        _isActingTimer = 0f;

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= attackRange)
        {
            rb.linearVelocity = Vector2.zero;
            anim.SetBool(HashMoving, false);
            TryAttack();
        }
        else if (dist <= detectRange)
        {
            MoveTowardPlayer();
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            anim.SetBool(HashMoving, false);
        }

        if (roomRange.x > 0 && roomRange.y > 0)
            ClampToRoom();
    }

    void MoveTowardPlayer()
    {
        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        rb.linearVelocity = dir * currentMoveSpeed;
        anim.SetBool(HashMoving, true);
        if (dir.x != 0) sr.flipX = dir.x < 0;
    }

    void TryAttack()
    {
        if (attackTimer > 0f || isActing) return;
        attackTimer = currentCooldown;
        StartCoroutine(WindupThenAttack());
    }

    IEnumerator WindupThenAttack()
    {
        isActing = true;

        // 전조: 하얗게 깜빡임
        float elapsed = 0f;
        while (elapsed < windupTime)
        {
            elapsed += Time.deltaTime;
            float cycle = Mathf.PingPong(elapsed * 10f, 1f);
            sr.color = Color.Lerp(new Color(0.4f, 0.4f, 0.4f), Color.white, cycle);
            yield return null;
        }
        sr.color = Color.white;

        // isActing = true 유지 → DeactivateHitbox() 호출 시 해제
        anim.SetTrigger(HashAttack);
        SoundManager.Instance?.PlaySFX(SoundManager.Instance.bossMagic);
    }

    // ── Animation Event 콜백 ──────────────────────────────

    /// 공격 애니메이션 hit 프레임 → 근접 데미지 + 투사체 발사
    public void ActivateHitbox()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);

        // 근접 범위 내에 있으면 직접 피격
        if (dist <= attackRange * 1.8f)
        {
            var ph = player.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                var ed = GetComponent<EnemyDamage>();
                ph.TakeDamage(ed != null ? ed.damage : 20);
            }
        }

        // 투사체 발사 (플레이어 방향)
        FireProjectile();
    }

    void FireProjectile()
    {
        if (projectilePrefab == null || player == null) return;

        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        var proj = Instantiate(projectilePrefab,
                               (Vector2)transform.position + dir * 0.6f,
                               Quaternion.identity);

        var bp = proj.GetComponent<BossProjectile>();
        if (bp != null) { bp.sourceEnemy = gameObject; bp.Launch(dir); }
    }

    /// 공격 애니메이션 마지막 프레임 → 이동 재개
public void DeactivateHitbox()
    {
        isActing = false;
        _isActingTimer = 0f;
    }

    // BossHealth에서 피격 시 호출 — 공격 애니메이션이 끊겨도 isActing 리셋
public void ResetIsActing()
    {
        StopAllCoroutines();
        isActing = false;
        _isActingTimer = 0f;
        sr.color = isEnraged ? new Color(1f, 0.5f, 0.5f) : Color.white;
    }

    // BossHealth에서 사망 애니메이션 종료 후 호출
    public void OnDeathComplete() { }

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
