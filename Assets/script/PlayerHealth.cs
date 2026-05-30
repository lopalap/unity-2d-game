using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }

    [Header("체력")]
    public int maxHp = 100;

    public int CurrentHp  { get; private set; }
    public int ShieldHp   { get; private set; }   // 과흡혈 보호막

    private bool             isInvincible;
    private PlayerController controller;
    private PlayerStats      stats;
    private float            _regenTimer;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (transform.parent != null)
            transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        controller = GetComponent<PlayerController>();
        stats      = GetComponent<PlayerStats>();
        if (stats != null) maxHp = stats.MaxHp;

        if (CurrentHp == 0) CurrentHp = maxHp;
    }

    void Update()
    {
        if (controller != null && controller.IsDead) return;
        if (stats == null || !stats.FortitudeUnlocked) return;
        if (CurrentHp >= maxHp) return;

        _regenTimer += Time.deltaTime;
        if (_regenTimer >= 1f)
        {
            _regenTimer = 0f;
            Heal(2);
        }
    }

    // 밀림 방지: 적 몸통에 닿아 있는 동안 플레이어가 밀리지 않도록 속도 보정
    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<EnemyDamage>() == null) return;
        var rb = GetComponent<Rigidbody2D>();
        if (rb == null || collision.contactCount == 0) return;

        Vector2 normal = collision.contacts[0].normal;
        float push = Vector2.Dot(rb.linearVelocity, -normal);
        if (push > 0f)
            rb.linearVelocity += normal * push;
    }

    public void TakeDamage(int damage, GameObject attacker = null)
    {
        if (controller != null && controller.IsDead) return;
        if (isInvincible) return;

        // 방어력 적용 (최소 1 데미지)
        int reduced = (stats != null && stats.Defense > 0)
            ? Mathf.Max(1, damage - stats.Defense)
            : damage;

        // 불굴 해금: HP ≤ 30% 시 방어+10, 피해 80%
        if (stats != null && stats.FortitudeUnlocked &&
            (float)CurrentHp / Mathf.Max(1, maxHp) <= 0.3f)
        {
            reduced = Mathf.Max(1, Mathf.RoundToInt(reduced * 0.8f) - 10);
        }

        // 보호막 먼저 소모
        if (ShieldHp > 0)
        {
            int absorbed = Mathf.Min(ShieldHp, reduced);
            ShieldHp -= absorbed;
            reduced  -= absorbed;
            if (reduced <= 0)
            {
                DamageNumber.Spawn(absorbed, transform.position, isPlayerDamage: true);
                return;
            }
        }

        CurrentHp = Mathf.Max(CurrentHp - reduced, 0);

        DamageNumber.Spawn(reduced, transform.position, isPlayerDamage: true);

        // 가시 해금: 받은 피해의 30% 반사
        if (attacker != null && stats != null && stats.ThornUnlocked)
        {
            int thornDmg = Mathf.Max(1, Mathf.RoundToInt(reduced * 0.3f));
            ApplyDamageToEnemy(attacker, thornDmg);
        }

        if (CurrentHp <= 0)
        {
            SoundManager.Instance?.PlaySFX(SoundManager.Instance.playerDeath);
            controller?.TriggerDeath();
        }
        else
        {
            controller?.TriggerHit();
            StartCoroutine(InvincibilityCoroutine());
        }
    }

    public void Heal(int amount)
    {
        CurrentHp = Mathf.Min(CurrentHp + amount, maxHp);
    }

    public void AddMaxHp(int amount)
    {
        maxHp     += amount;
        CurrentHp  = Mathf.Min(CurrentHp + amount, maxHp);
    }

    // ── 반격 / 가시 공용 헬퍼 ─────────────────────────
    void ReflectDamage(int dmg, GameObject target)
    {
        ApplyDamageToEnemy(target, dmg);
        DamageNumber.Spawn(dmg, target.transform.position, false, false);
    }

    void ApplyDamageToEnemy(GameObject target, int dmg)
    {
        var eh = target.GetComponent<EnemyHealth>();
        if (eh != null) { eh.TakeDamage(dmg); return; }
        var bh = target.GetComponent<BossHealth>();
        if (bh != null) { bh.TakeDamage(dmg); return; }
        var nbh = target.GetComponent<NecromancerBossHealth>();
        if (nbh != null) nbh.TakeDamage(dmg);
    }

    IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;
        float duration = 1.0f + (stats != null ? stats.InvincibilityBonus : 0f);
        yield return new WaitForSeconds(duration);
        isInvincible = false;
    }
}
