using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 공격 히트박스 - Animation Event 기반
/// 공격 애니메이션의 hit 프레임에서만 히트박스 활성화
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerAttack : MonoBehaviour
{
    [Header("공격 설정")]
    public int   attackDamage = 20;
    public float hitboxWidth  = 1.4f;
    public float hitboxHeight = 0.6f;
    public float hitboxOffset = 0.9f;

    private SpriteRenderer sr;
    private PlayerStats    stats;
    private PlayerHealth   playerHealth;
    private GameObject     hitboxObj;

    void Awake()
    {
        sr           = GetComponent<SpriteRenderer>();
        stats        = GetComponent<PlayerStats>();
        playerHealth = GetComponent<PlayerHealth>();
        BuildHitbox();
    }

    void BuildHitbox()
    {
        hitboxObj = new GameObject("PlayerAttackHitbox");
        hitboxObj.transform.SetParent(transform, false);
        hitboxObj.tag = "PlayerAttack";

        var col       = hitboxObj.AddComponent<BoxCollider2D>();
        col.size      = new Vector2(hitboxWidth, hitboxHeight);
        col.isTrigger = true;

        var hb    = hitboxObj.AddComponent<AttackHitbox>();
        hb.damage = attackDamage;

        hitboxObj.SetActive(false);
    }

    // ── Animation Event 콜백 ─────────────────────────────────
    /// Player_Attack.anim 의 hit 프레임에서 자동 호출
    public void ActivateHitbox()
    {
        float dir = (sr != null && sr.flipX) ? -1f : 1f;
        hitboxObj.transform.localPosition = new Vector3(hitboxOffset * dir, 0f, 0f);

        // 크리티컬 판정
        int baseDmg  = stats != null ? stats.Damage : attackDamage;
        bool isCrit  = stats != null && Random.value < stats.CritChance;
        var hb       = hitboxObj.GetComponent<AttackHitbox>();
        hb.damage           = isCrit ? baseDmg * 2 : baseDmg;
        hb.isCrit           = isCrit;
        hb.playerHealth     = playerHealth;
        hb.lifestealPercent = stats != null ? stats.LifestealPercent : 0f;

        if (isCrit) Debug.Log("[PlayerAttack] 크리티컬!");

        // 연타 해금: 25% 확률로 두 번 타격
        hb.dealTwice = stats != null && stats.DoubleStrikeUnlocked && Random.value < 0.25f;

        // 연쇄 해금: 25% 확률로 4배 데미지 (크리티컬과 색상 분리)
        bool chainProc = stats != null && stats.ChainUnlocked && Random.value < 0.25f;
        if (chainProc)
        {
            hb.damage      = baseDmg * 4;
            hb.isCrit      = false;
            hb.isChainProc = true;
        }
        else
        {
            hb.isChainProc = false;
        }

        // stats 전달 (처형 등에 사용)
        hb.stats = stats;

        hitboxObj.SetActive(true);
    }

    /// Player_Attack.anim 의 마지막 프레임에서 자동 호출
    public void DeactivateHitbox()
    {
        hitboxObj.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        if (sr == null) return;
        float dir = sr.flipX ? -1f : 1f;
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.4f);
        Gizmos.DrawWireCube(
            transform.position + new Vector3(hitboxOffset * dir, 0f, 0f),
            new Vector3(hitboxWidth, hitboxHeight, 0f));
    }
}
