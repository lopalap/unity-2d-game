using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class BossHealth : MonoBehaviour
{
    [Header("체력")]
    public int maxHp = 150;

    [Header("드롭")]
    public GameObject keyDropPrefab;

    public int  CurrentHp  { get; private set; }
    public bool IsDead     { get; private set; }

    private Animator       anim;
    private SpriteRenderer sr;
    private BossController controller;

    private static readonly int HashDead = Animator.StringToHash("isDead");
    private static readonly int HashHit  = Animator.StringToHash("TakeDamage");

    void Awake()
    {
        anim       = GetComponent<Animator>();
        sr         = GetComponent<SpriteRenderer>();
        controller = GetComponent<BossController>();
        CurrentHp  = maxHp;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (IsDead) return;
        if (!other.CompareTag("PlayerAttack")) return;
        TakeDamage(10);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsDead) return;
        if (!collision.gameObject.CompareTag("PlayerAttack")) return;
        TakeDamage(10);
    }

    public void TakeDamage(int damage)
    {
        if (IsDead) return;

        CurrentHp = Mathf.Max(CurrentHp - damage, 0);

        // 50% 이하 = 분노 상태 알림
        if (controller != null)
            controller.OnHealthChanged(CurrentHp, maxHp);

        if (CurrentHp <= 0)
            Die();
        else
        {
            // 공격 중 피격 시 isActing이 잠기는 버그 방지
            controller?.ResetIsActing();
            anim.SetTrigger(HashHit);
            StartCoroutine(FlashWhite());
        }
    }

    void Die()
    {
        IsDead = true;

        var sm = SoundManager.Instance;
        if (sm != null)
        {
            sm.PlaySFX(sm.bossDeath);
            sm.PlayBGM(sm.dungeonBGM);
        }

        // TakeDamage 트리거 잔여분 제거 → Hurt 트랜지션 간섭 방지
        anim.ResetTrigger(HashHit);
        anim.SetBool(HashDead, true);

        if (controller != null) controller.enabled = false;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        foreach (var col in GetComponents<Collider2D>())
            col.enabled = false;

        // 키 즉시 드롭 (사망 위치)
        if (keyDropPrefab != null)
            Instantiate(keyDropPrefab, transform.position, Quaternion.identity);

        // 애니메이션 이벤트가 누락될 경우 대비 안전장치
        StartCoroutine(FallbackDestroy());
    }

    IEnumerator FallbackDestroy()
    {
        // 사망 애니메이션 길이(22프레임 / 10fps = 2.2s) + 페이드(0.8s) + 여유
        yield return new WaitForSeconds(4f);
        if (gameObject != null) Destroy(gameObject);
    }

    // Animation Event: NightBorne_Death 마지막 프레임에서 호출
    public void OnDeathComplete()
    {
        StartCoroutine(FadeAndDestroy());
    }

    IEnumerator FlashWhite()
    {
        sr.color = Color.white * 2f; // 밝게
        yield return new WaitForSeconds(0.1f);
        sr.color = Color.white;
    }

    IEnumerator FadeAndDestroy()
    {
        float elapsed = 0f;
        Color c = sr.color;
        while (elapsed < 0.8f)
        {
            elapsed += Time.deltaTime;
            sr.color = new Color(c.r, c.g, c.b, 1f - elapsed / 0.8f);
            yield return null;
        }
        Destroy(gameObject);
    }
}
