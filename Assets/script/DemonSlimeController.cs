using UnityEngine;
using System.Collections;

/// <summary>
/// 두 번째 보스 - Demon Slime AI
/// BossController 구조 동일: windup + cleave 근접 공격, 분노 단계
/// </summary>
public class DemonSlimeController : MonoBehaviour
{
    [Header("플레이어")]
    public Transform player;

    [Header("이동")]
    public float moveSpeed   = 2.0f;
    public float detectRange = 9f;

    [Header("공격")]
    public float attackRange    = 1.2f;
    public float attackCooldown = 2.0f;
    public float windupTime     = 0.7f;

    [Header("분노 단계 (HP 50% 이하)")]
    public float enrageSpeedMult    = 1.5f;
    public float enrageCooldownMult = 0.6f;

    [Header("방 범위 (Clamp)")]
    public Vector2 roomCenter;
    public Vector2 roomRange;

    private Animator          anim;
    private SpriteRenderer    sr;
    private DemonSlimeHealth  health;
    private Rigidbody2D       rb;

    private float attackTimer  = 0f;
    private bool  isActing     = false;
    private bool  isEnraged    = false;

    private float currentMoveSpeed;
    private float currentCooldown;

    private static readonly int HashMoving = Animator.StringToHash("isMoving");
    private static readonly int HashAttack = Animator.StringToHash("Attack");

    void Awake()
    {
        anim   = GetComponent<Animator>();
        sr     = GetComponent<SpriteRenderer>();
        health = GetComponent<DemonSlimeHealth>();
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
    }

    public void OnHealthChanged(int current, int max)
    {
        if (!isEnraged && current <= max / 2)
        {
            isEnraged        = true;
            currentMoveSpeed = moveSpeed      * enrageSpeedMult;
            currentCooldown  = attackCooldown * enrageCooldownMult;
            StartCoroutine(EnrageEffect());
        }
    }

    IEnumerator EnrageEffect()
    {
        for (int i = 0; i < 6; i++)
        {
            sr.color = new Color(1f, 0.2f, 0.2f);
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

        if (isActing)
        {
            rb.linearVelocity = Vector2.zero;
            anim.SetBool(HashMoving, false);
            return;
        }

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

        float elapsed = 0f;
        while (elapsed < windupTime)
        {
            elapsed += Time.deltaTime;
            float cycle = Mathf.PingPong(elapsed * 10f, 1f);
            sr.color = Color.Lerp(new Color(0.4f, 0.4f, 0.4f), Color.white, cycle);
            yield return null;
        }
        sr.color = Color.white;
        anim.SetTrigger(HashAttack);
    }

    // Animation Event: cleave hit 프레임
    public void ActivateHitbox()
    {
        if (player == null) return;
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > attackRange * 1.8f) return;

        var ph = player.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            var ed = GetComponent<EnemyDamage>();
            ph.TakeDamage(ed != null ? ed.damage : 25);
        }
    }

    // Animation Event: cleave 마지막 프레임
    public void DeactivateHitbox()
    {
        isActing = false;
    }

    void ClampToRoom()
    {
        float cx = Mathf.Clamp(transform.position.x, roomCenter.x - roomRange.x, roomCenter.x + roomRange.x);
        float cy = Mathf.Clamp(transform.position.y, roomCenter.y - roomRange.y, roomCenter.y + roomRange.y);
        transform.position = new Vector3(cx, cy, transform.position.z);
    }

    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(roomCenter, new Vector3(roomRange.x * 2, roomRange.y * 2, 0));
        Gizmos.color = new Color(1f, 0.3f, 0f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}
