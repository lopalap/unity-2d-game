using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class EnemyHealth : MonoBehaviour
{
    public enum MonsterType { Goblin, Skeleton, Slime, Small, Generic }

    [Header("체력")]
    public int maxHp = 30;

    [Header("사운드 타입")]
    public MonsterType monsterType = MonsterType.Generic;

    [Header("데이터 (ScriptableObject)")]
    [SerializeField] EnemyData data;

    public int  CurrentHp { get; private set; }
    public bool IsDead    { get; private set; }

    private Animator        anim;
    private SpriteRenderer  sr;
    private EnemyController controller;

    private static readonly int HashDead = Animator.StringToHash("isDead");
    private static readonly int HashHit  = Animator.StringToHash("TakeDamage");

    void Awake()
    {
        anim       = GetComponent<Animator>();
        sr         = GetComponent<SpriteRenderer>();
        controller = GetComponent<EnemyController>();
        if (data != null) maxHp = data.maxHp;
        CurrentHp  = maxHp;
    }

    public void TakeDamage(int damage)
    {
        if (IsDead) return;

        CurrentHp = Mathf.Max(CurrentHp - damage, 0);

        if (CurrentHp <= 0) Die();
        else
        {
            if (controller != null) controller.OnHit();
            anim.SetTrigger(HashHit);
            StartCoroutine(FlashRed());
        }
    }

    void Die()
    {
        IsDead = true;

        var sm = SoundManager.Instance;
        if (sm != null)
        {
            switch (monsterType)
            {
                case MonsterType.Goblin:   sm.PlaySFX(sm.goblinDeath);       break;
                case MonsterType.Skeleton: sm.PlaySFX(sm.skeletonDeath);     break;
                case MonsterType.Slime:    sm.PlaySFX(sm.slimeDeath);        break;
                case MonsterType.Small:    sm.PlaySFX(sm.smallMonsterDeath); break;
            }
        }

        // TakeDamage 트리거 잔여분 제거 → Hurt 트랜지션 간섭 방지
        anim.ResetTrigger(HashHit);
        anim.SetBool(HashDead, true);

        if (controller != null) controller.enabled = false;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        StartCoroutine(DestroyAfterAnim());
    }

    IEnumerator FlashRed()
    {
        sr.color = Color.red;
        yield return new WaitForSeconds(0.15f);
        sr.color = Color.white;
    }

    IEnumerator DestroyAfterAnim()
    {
        yield return new WaitForSeconds(2f);
        Destroy(gameObject);
    }
}
