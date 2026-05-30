using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D), typeof(NecromancerBossHealth), typeof(Animator))]
public class NecromancerBossController : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public GameObject projectilePrefab;
    public Vector2 roomCenter;
    public Vector2 roomRange;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float detectRange = 10f;

    [Header("Attack Ranges")]
    public float preferredDist = 4f;
    public float minDist = 2.5f;
    public float rangedRange = 5f;

    [Header("Timing")]
    public float globalAttackCooldown = 2f;
    public float attack1Cooldown = 3f;
    public float attack2Cooldown = 6f;
    public float windupTime = 0.6f;

    [Header("Phase 2 Enrage")]
    public float enrageSpeedMult = 1.5f;
    public float enrageCooldownMult = 0.6f;

    [Header("Attack1 - 부채꼴 투사체")]
    public GameObject multiShotCastEffectPrefab;
    public Vector2    multiShotCastOffset = new Vector2(0f, 1.5f);
    public float fanSpreadAngle = 25f; // 중앙 기준 좌우 퍼짐 각도

    [Header("Attack2 - 낙뢰")]
    public GameObject lightningWarningPrefab;
    public GameObject lightningEffectPrefab;
    public int   lightningCount  = 3;
    public float lightningRadius = 1.0f;
    public int   lightningDamage = 15;

    [Header("Attack3 - 스켈레톤 소환")]
    public GameObject summonEffectPrefab;
    public GameObject summonCastEffectPrefab;
    public Vector2    summonCastOffset = new Vector2(0f, 1.5f);
    public GameObject skeletonPrefab;
    public float summonRadius   = 3f;
    public float summonCooldown = 10f;
    public int   maxSummonCount = 3;

    static readonly int HashIsMoving   = Animator.StringToHash("isMoving");
    static readonly int HashAttack1    = Animator.StringToHash("Attack1");
    static readonly int HashAttack2    = Animator.StringToHash("Attack2");
    static readonly int HashAttack3    = Animator.StringToHash("Attack3");
    static readonly int HashTakeDamage = Animator.StringToHash("TakeDamage");
    static readonly int HashIsDead     = Animator.StringToHash("isDead");

    Animator anim;
    Rigidbody2D rb;
    NecromancerBossHealth health;
    SpriteRenderer sr;
    Transform attackHitbox;

    bool isActing = false;
    bool isDead = false;
    bool enraged = false;
    bool _bgmStarted = false;
    float globalAttackTimer = 0f;
    float attack1Timer = 0f;
    float attack2Timer = 0f;
    float summonTimer = 0f;
    readonly List<GameObject> summonedSkeletons = new List<GameObject>();

    void Awake()
    {
        anim   = GetComponent<Animator>();
        rb     = GetComponent<Rigidbody2D>();
        health = GetComponent<NecromancerBossHealth>();
        sr     = GetComponentInChildren<SpriteRenderer>();
        attackHitbox = transform.Find("AttackHitbox");
    }

    void Start()
    {
        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform;
        }
        attack1Timer = attack1Cooldown;
        attack2Timer = attack2Cooldown;
    }

void Update()
    {
        if (isDead || player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);

        // 플레이어 첫 감지 시 보스 BGM 시작
        if (!_bgmStarted && dist <= detectRange)
        {
            _bgmStarted = true;
            var sm = SoundManager.Instance;
            if (sm != null) sm.PlayBGM(sm.bossBGM);
        }

        if (!enraged && health.CurrentHp <= health.maxHp * 0.5f)
            Enrage();

        globalAttackTimer -= Time.deltaTime;
        attack1Timer -= Time.deltaTime;
        attack2Timer -= Time.deltaTime;
        summonTimer  -= Time.deltaTime;

        if (isActing)
        {
            rb.linearVelocity = Vector2.zero;
            anim.SetBool(HashIsMoving, false);
            return;
        }

        if (dist > detectRange)
        {
            StopMoving();
            return;
        }

        summonedSkeletons.RemoveAll(go => go == null);
        bool anyReady = globalAttackTimer <= 0f
                        && (attack1Timer <= 0f || attack2Timer <= 0f
                            || (summonTimer <= 0f && summonedSkeletons.Count < maxSummonCount));
        if (anyReady && dist <= rangedRange)
        {
            ChooseAttack(dist);
        }
        else if (dist < minDist)
        {
            MoveAway(player.position);
        }
        else if (dist > preferredDist)
        {
            MoveToward(player.position);
        }
        else
        {
            StopMoving();
        }
    }

    void MoveToward(Vector2 target)
    {
        Vector2 dir = (target - (Vector2)transform.position).normalized;
        float speed = moveSpeed * (enraged ? enrageSpeedMult : 1f);
        rb.linearVelocity = dir * speed;
        anim.SetBool(HashIsMoving, true);
        sr.flipX = dir.x < 0;
    }

    void MoveAway(Vector2 target)
    {
        Vector2 dir = ((Vector2)transform.position - target).normalized;
        float speed = moveSpeed * (enraged ? enrageSpeedMult : 1f);
        rb.linearVelocity = dir * speed;
        anim.SetBool(HashIsMoving, true);
        sr.flipX = (target - (Vector2)transform.position).x < 0; // 플레이어 방향을 바라봄
    }

    void StopMoving()
    {
        rb.linearVelocity = Vector2.zero;
        anim.SetBool(HashIsMoving, false);
    }

    // flipX 상태에 따라 X오프셋 반전
    Vector2 MirroredOffset(Vector2 offset)
    {
        return sr.flipX ? new Vector2(-offset.x, offset.y) : offset;
    }

    void ChooseAttack(float dist)
    {
        float mult = enraged ? enrageCooldownMult : 1f;

        bool canAtk1   = attack1Timer <= 0f;
        bool canAtk2   = attack2Timer <= 0f;
        bool canSummon = summonTimer  <= 0f && summonedSkeletons.Count < maxSummonCount;

        // 가능한 공격 목록
        var candidates = new List<int>();
        if (canAtk1)   candidates.Add(HashAttack1);
        if (canAtk2)   candidates.Add(HashAttack2);
        if (canSummon) candidates.Add(HashAttack3);
        if (candidates.Count == 0) return;

        int chosen = candidates[Random.Range(0, candidates.Count)];

        // 선택된 공격 쿨타임 리셋
        globalAttackTimer = globalAttackCooldown * mult;
        if      (chosen == HashAttack1) attack1Timer = attack1Cooldown * mult;
        else if (chosen == HashAttack2) attack2Timer = attack2Cooldown * mult;
        // Attack3 쿨타임은 DoSummon() 내부에서 설정

        StartCoroutine(DoAttack(chosen));
    }

    IEnumerator DoAttack(int attackHash)
    {
        isActing = true;
        StopMoving();

        // 공격 전 플레이어 방향으로 바라보기
        if (player != null)
            sr.flipX = player.position.x < transform.position.x;

        // Windup flash (red tint)
        float elapsed = 0f;
        while (elapsed < windupTime)
        {
            float t = Mathf.PingPong(elapsed * 8f, 1f);
            sr.color = Color.Lerp(Color.white, new Color(1f, 0.3f, 0.3f), t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        sr.color = enraged ? new Color(1f, 0.7f, 0.7f) : Color.white;

        anim.SetTrigger(attackHash);
        yield return new WaitForSeconds(0.4f); // 모션 시작 후 효과 발동 타이밍

        if      (attackHash == HashAttack1) yield return StartCoroutine(DoMultiShot());
        else if (attackHash == HashAttack2) yield return StartCoroutine(DoLightning());
        else if (attackHash == HashAttack3) yield return StartCoroutine(DoSummon());
        // isActing은 DeactivateHitbox() 애니메이션 이벤트에서 해제
    }

    // ── Attack1: 다방향 투사체 ──────────────────────────────
    IEnumerator DoMultiShot()
    {
        SoundManager.Instance?.PlaySFX(SoundManager.Instance.bossMagic);

        // 지팡이 쪽 캐스팅 이펙트
        if (multiShotCastEffectPrefab != null)
        {
            Vector2 spawnPos = (Vector2)sr.transform.position + MirroredOffset(multiShotCastOffset);
            PoolManager.Get(multiShotCastEffectPrefab, spawnPos, Quaternion.identity);
        }

        if (projectilePrefab == null) yield break;

        // 플레이어 방향 기준 부채꼴 3발
        float centerAngle = 0f;
        if (player != null)
        {
            Vector2 toPlayer = (player.position - transform.position).normalized;
            centerAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
        }

        float[] offsets = { -fanSpreadAngle, 0f, fanSpreadAngle };
        foreach (float offset in offsets)
        {
            float   angle = centerAngle + offset;
            Vector2 dir   = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            var     proj  = PoolManager.Get(projectilePrefab, (Vector2)transform.position + dir * 0.8f, Quaternion.Euler(0, 0, angle));
            var bp = proj.GetComponent<BossProjectile>();
            if (bp != null) { bp.sourceEnemy = gameObject; bp.Launch(dir); }
        }
    }

    // ── Attack2: 랜덤 낙뢰 ──────────────────────────────────
    IEnumerator DoLightning()
    {
        SoundManager.Instance?.PlaySFX(SoundManager.Instance.necroAttack2);
        if (player == null) yield break;

        // 낙뢰 위치 미리 결정
        var strikePositions = new Vector2[lightningCount];
        for (int i = 0; i < lightningCount; i++)
            strikePositions[i] = (Vector2)player.position + Random.insideUnitCircle * 2f;

        // 경고 이펙트 전부 동시에 표시
        if (lightningWarningPrefab != null)
            foreach (var pos in strikePositions)
                PoolManager.Get(lightningWarningPrefab, pos, Quaternion.identity);

        // 경고 애니메이션이 끝날 때까지 대기 (1.2s)
        yield return new WaitForSeconds(1.2f);

        // 낙뢰 투하
        var ph = player.GetComponent<PlayerHealth>();
        foreach (var pos in strikePositions)
        {
            if (lightningEffectPrefab != null)
                PoolManager.Get(lightningEffectPrefab, pos, Quaternion.identity);

            if (ph != null && Vector2.Distance(pos, player.position) <= lightningRadius)
                ph.TakeDamage(lightningDamage);

            yield return new WaitForSeconds(0.15f); // 살짝 시간차
        }
    }

    // ── Attack3: 스켈레톤 소환 ──────────────────────────────
    IEnumerator DoSummon()
    {
        SoundManager.Instance?.PlaySFX(SoundManager.Instance.skeletonSummon);

        // 네크로맨서 해골 위치에 소환 캐스팅 이펙트
        if (summonCastEffectPrefab != null)
        {
            Vector2 spawnPos = (Vector2)sr.transform.position + MirroredOffset(summonCastOffset);
            PoolManager.Get(summonCastEffectPrefab, spawnPos, Quaternion.identity);
        }

        for (int i = 0; i < 3; i++)
        {
            Vector2 spawnPos = (Vector2)transform.position
                               + Random.insideUnitCircle.normalized * summonRadius;

            if (summonEffectPrefab != null)
                PoolManager.Get(summonEffectPrefab, spawnPos, Quaternion.identity);

            yield return new WaitForSeconds(0.35f); // 이펙트 절반 진행 후 소환

            if (skeletonPrefab != null)
            {
                var sk   = Instantiate(skeletonPrefab, spawnPos, Quaternion.identity); // 스켈레톤은 상태가 있으므로 풀링 제외
                var ctrl = sk.GetComponent<EnemyController>();
                if (ctrl != null && player != null) ctrl.player = player;
                summonedSkeletons.Add(sk); // 목록에 등록
            }

            yield return new WaitForSeconds(0.15f); // 다음 소환 간격
        }

        // 소환 완료 후 쿨타임 시작 (중단 시 낭비 방지)
        summonTimer = summonCooldown;
    }

    // Called from Animation Events
    public void ActivateHitbox()
    {
        if (attackHitbox != null) attackHitbox.gameObject.SetActive(true);
    }

    public void DeactivateHitbox()
    {
        if (attackHitbox != null) attackHitbox.gameObject.SetActive(false);
        isActing = false;
        sr.color = enraged ? new Color(1f, 0.7f, 0.7f) : Color.white;
    }

    void Enrage()
    {
        enraged = true;
        // Persistent red tint in phase 2
        sr.color = new Color(1f, 0.7f, 0.7f);
    }

    public void OnTakeDamage()
    {
        if (isDead) return;
        // 공격 중 피격 시 isActing이 잠기는 버그 방지
        if (isActing)
        {
            StopAllCoroutines();
            isActing = false;
            if (attackHitbox != null) attackHitbox.gameObject.SetActive(false);
        }
        anim.SetTrigger(HashTakeDamage);
    }

    public void OnDead()
    {
        isDead = true;
        StopAllCoroutines();
        rb.linearVelocity = Vector2.zero;
        anim.SetBool(HashIsDead, true);
        // Disable all colliders
        foreach (var col in GetComponents<Collider2D>())
            col.enabled = false;
        if (attackHitbox != null) attackHitbox.gameObject.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        Vector3 center = transform.position + Vector3.up * 0.5f;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(center, detectRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, rangedRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(center, preferredDist);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, minDist);

        // 이펙트 스폰 위치 표시 (flipX 미러링 반영)
        var visSR = GetComponentInChildren<SpriteRenderer>();
        Vector3 visBase = visSR != null ? visSR.transform.position : transform.position;
        float xSign = (visSR != null && visSR.flipX) ? -1f : 1f;

        Vector3 summonPos = visBase + new Vector3(summonCastOffset.x * xSign, summonCastOffset.y, 0f);
        Gizmos.color = new Color(0.8f, 0.3f, 1f);
        Gizmos.DrawWireSphere(summonPos, 0.25f);
        UnityEditor.Handles.color = new Color(0.8f, 0.3f, 1f, 0.8f);
        UnityEditor.Handles.Label(summonPos + Vector3.up * 0.35f, "☠");

        // 1번 공격 캐스팅 위치 (주황색)
        Vector3 multiShotPos = visBase + new Vector3(multiShotCastOffset.x * xSign, multiShotCastOffset.y, 0f);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(multiShotPos, 0.25f);
        UnityEditor.Handles.color = new Color(1f, 0.5f, 0f, 0.8f);
        UnityEditor.Handles.Label(multiShotPos + Vector3.up * 0.35f, "✦");

        // 발밑 이펙트 위치 (하늘색)
        var footTf = transform.Find("FootEffect");
        if (footTf != null)
        {
            Gizmos.color = new Color(0f, 0.9f, 0.9f);
            Gizmos.DrawWireSphere(footTf.position, 0.25f);
            UnityEditor.Handles.color = new Color(0f, 0.9f, 0.9f, 0.8f);
            UnityEditor.Handles.Label(footTf.position + Vector3.up * 0.35f, "◎");
        }
    }
}
