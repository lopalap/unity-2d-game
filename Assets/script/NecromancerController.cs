using System.Collections;
using UnityEngine;

public class NecromancerController : MonoBehaviour
{
    [Header("References")]
    public Transform   player;
    public GameObject  projectilePrefab;

    [Header("Movement")]
    public float moveSpeed     = 1.8f;
    public float detectRange   = 10f;
    public float preferredDist = 4f;   // 유지할 사거리
    public float minDist       = 2f;   // 이보다 가까우면 후퇴

    [Header("Attack")]
    public float attackRange    = 6f;   // 원거리 공격 사거리
    public float attackCooldown = 2.5f;
    public float windupTime     = 0.6f;

    [Header("Enrage (50% HP)")]
    public float enrageSpeedMult    = 1.5f;
    public float enrageCooldownMult = 0.6f;

    [Header("Room Bounds")]
    public Vector2 roomCenter;
    public Vector2 roomRange = new Vector2(8f, 8f);

    private static readonly int HashMoving     = Animator.StringToHash("isMoving");
    private static readonly int HashAttack     = Animator.StringToHash("Attack");
    private static readonly int HashAttackType = Animator.StringToHash("AttackType");

    private Animator       anim;
    private Rigidbody2D    rb;
    private SpriteRenderer sr;
    private float cooldownTimer;
    private bool  isActing;
    private bool  isDead;

    void Awake()
    {
        anim = GetComponent<Animator>();
        rb   = GetComponent<Rigidbody2D>();
        sr   = GetComponent<SpriteRenderer>();
        if (player == null) {
            var p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    void Update()
    {
        if (isDead || player == null) return;

        // isActing 중 매 프레임 정지 보장
        // 단, Hit 상태로 전환된 경우(공격 중 피격) isActing을 해제해 동결 방지
        if (isActing) {
            if (anim.GetCurrentAnimatorStateInfo(0).IsName("Hit"))
                isActing = false;
            else {
                rb.linearVelocity = Vector2.zero;
                anim.SetBool(HashMoving, false);
                return;
            }
        }

        cooldownTimer -= Time.deltaTime;
        float  dist = Vector2.Distance(transform.position, player.position);
        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;

        if (dist > detectRange) {
            rb.linearVelocity = Vector2.zero;
            anim.SetBool(HashMoving, false);
        } else if (dist < minDist) {
            // 너무 가까우면 후퇴
            rb.linearVelocity = -dir * moveSpeed;
            sr.flipX = dir.x < 0;
            anim.SetBool(HashMoving, true);
        } else if (dist > preferredDist + 0.5f) {
            // 선호 거리까지 접근
            rb.linearVelocity = dir * moveSpeed;
            sr.flipX = dir.x < 0;
            anim.SetBool(HashMoving, true);
        } else {
            // 선호 거리: 정지 후 공격 대기
            rb.linearVelocity = Vector2.zero;
            anim.SetBool(HashMoving, false);
            sr.flipX = dir.x < 0;
            if (dist <= attackRange && cooldownTimer <= 0f)
                StartCoroutine(WindupThenAttack());
        }
    }

    IEnumerator WindupThenAttack()
    {
        isActing = true;
        rb.linearVelocity = Vector2.zero;
        anim.SetBool(HashMoving, false);

        float elapsed  = 0f;
        Color original = sr.color;
        while (elapsed < windupTime) {
            elapsed += Time.deltaTime;
            sr.color = Color.Lerp(original, Color.white, Mathf.PingPong(elapsed * 8f, 1f));
            yield return null;
        }
        sr.color = original;

        anim.SetInteger(HashAttackType, UnityEngine.Random.Range(0, 3));
        anim.SetTrigger(HashAttack);
        cooldownTimer = attackCooldown;
    }

    // Animation Event: 투사체 발사 타이밍
    public void ActivateHitbox()
    {
        FireProjectile();
    }

    // Animation Event: 공격 종료
    public void DeactivateHitbox()
    {
        isActing = false;
        anim.SetBool(HashMoving, false);
    }

    void FireProjectile()
    {
        if (projectilePrefab == null || player == null) return;
        Vector2 dir  = ((Vector2)player.position - (Vector2)transform.position).normalized;
        var  proj = UnityEngine.Object.Instantiate(projectilePrefab,
                           (Vector2)transform.position + dir * 0.6f,
                           Quaternion.identity);
        var np = proj.GetComponent<NecromancerProjectile>();
        if (np != null) { np.sourceEnemy = gameObject; np.Launch(dir); }
    }

    public void OnHealthChanged(int currentHp, int maxHp)
    {
        if (currentHp <= maxHp / 2) {
            moveSpeed      *= enrageSpeedMult;
            attackCooldown *= enrageCooldownMult;
        }
    }

    public void Die()
    {
        isDead   = true;
        isActing = false;
        rb.linearVelocity = Vector2.zero;
        StopAllCoroutines();
    }

    void FixedUpdate()
    {
        if (isDead || isActing) return;
        ClampToRoom();
    }

    void ClampToRoom()
    {
        Vector2 pos = rb.position;
        pos.x = Mathf.Clamp(pos.x, roomCenter.x - roomRange.x, roomCenter.x + roomRange.x);
        pos.y = Mathf.Clamp(pos.y, roomCenter.y - roomRange.y, roomCenter.y + roomRange.y);
        rb.position = pos;
    }
}
