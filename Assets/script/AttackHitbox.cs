using UnityEngine;

/// <summary>
/// 공격 히트박스 — 적에게 데미지 전달 + 흡혈/연타/처형/연쇄 처리
/// </summary>
public class AttackHitbox : MonoBehaviour
{
    public int          damage;
    public bool         isCrit;
    public PlayerHealth playerHealth;    // 흡혈·반격용
    public float        lifestealPercent;
    public PlayerStats  stats;           // 특수 능력 판단용

    // 연타 해금
    public bool dealTwice;
    // 연쇄 해금 프록 (4배 데미지)
    public bool isChainProc;

    private System.Collections.Generic.HashSet<GameObject> hit
        = new System.Collections.Generic.HashSet<GameObject>();

    void OnEnable() => hit.Clear();

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hit.Contains(other.gameObject)) return;
        hit.Add(other.gameObject);

        // 파괴 가능 오브젝트
        var breakable = other.GetComponent<BreakableObject>();
        if (breakable != null)
        {
            breakable.TakeHit();
            return;
        }

        // 적 데미지
        var eh = other.GetComponent<EnemyHealth>();
        if (eh == null) eh = other.GetComponentInParent<EnemyHealth>();
        if (eh == null) return;

        // ── 처형: 공격 전 HP < 30% → 즉사 (BossHealth 없는 일반 적만) ──
        bool executionKill = false;
        if (stats != null && stats.ExecutionUnlocked && !eh.IsDead)
        {
            bool isBoss = other.GetComponent<BossHealth>() != null ||
                          other.GetComponentInParent<BossHealth>() != null;
            if (!isBoss && (float)eh.CurrentHp / eh.maxHp < 0.3f)
                executionKill = true;
        }

        if (executionKill)
        {
            SoundManager.Instance?.PlaySFX(SoundManager.Instance.attackHit);
            eh.TakeDamage(eh.maxHp + 1000);
            DamageNumber.Spawn(eh.CurrentHp, other.transform.position, false, true);
            return;
        }

        // ── 기본 타격 ──
        SoundManager.Instance?.PlaySFX(SoundManager.Instance.attackHit);
        int hitCount = (dealTwice) ? 2 : 1;
        for (int i = 0; i < hitCount; i++)
        {
            eh.TakeDamage(damage);
            Vector3 numPos = other.transform.position + (i == 1 ? Vector3.up * 0.4f : Vector3.zero);
            DamageNumber.Spawn(damage, numPos, isPlayerDamage: false, isCrit: isCrit && i == 0, isChain: isChainProc && i == 0);

            // 흡혈
            if (playerHealth != null && lifestealPercent > 0f)
                playerHealth.Heal(Mathf.Max(1, Mathf.RoundToInt(damage * lifestealPercent)));
        }

        // 과흡혈 해금: 10% 확률로 준 데미지 전량 흡혈
        if (playerHealth != null && stats != null && stats.OverhealUnlocked && Random.value < 0.1f)
            playerHealth.Heal(damage);
    }
}
