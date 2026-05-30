using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class DemonSlimeHealth : MonoBehaviour
{
    [Header("체력")]
    public int maxHp = 200;

    [Header("드롭")]
    public GameObject keyDropPrefab;

    public int  CurrentHp { get; private set; }
    public bool IsDead    { get; private set; }

    private Animator              anim;
    private SpriteRenderer        sr;
    private DemonSlimeController  controller;

    private static readonly int HashDead = Animator.StringToHash("isDead");
    private static readonly int HashHit  = Animator.StringToHash("TakeDamage");

    void Awake()
    {
        anim       = GetComponent<Animator>();
        sr         = GetComponent<SpriteRenderer>();
        controller = GetComponent<DemonSlimeController>();
        CurrentHp  = maxHp;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (IsDead) return;
        if (!other.CompareTag("PlayerAttack")) return;
        var hb = other.GetComponent<AttackHitbox>();
        TakeDamage(hb != null ? hb.damage : 10);
    }

    public void TakeDamage(int damage)
    {
        if (IsDead) return;
        CurrentHp = Mathf.Max(CurrentHp - damage, 0);

        if (controller != null)
            controller.OnHealthChanged(CurrentHp, maxHp);

        if (CurrentHp <= 0)
            Die();
        else
        {
            anim.SetTrigger(HashHit);
            StartCoroutine(FlashWhite());
        }
    }

    void Die()
    {
        IsDead = true;
        anim.ResetTrigger(HashHit);
        anim.SetBool(HashDead, true);

        if (controller != null) controller.enabled = false;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        foreach (var col in GetComponents<Collider2D>())
            col.enabled = false;

        if (keyDropPrefab != null)
            Instantiate(keyDropPrefab, transform.position, Quaternion.identity);

        StartCoroutine(FallbackDestroy());
    }

    IEnumerator FallbackDestroy()
    {
        yield return new WaitForSeconds(4f);
        if (gameObject != null) Destroy(gameObject);
    }

    // Animation Event: death 마지막 프레임
    public void OnDeathComplete()
    {
        StartCoroutine(FadeAndDestroy());
    }

    IEnumerator FlashWhite()
    {
        sr.color = Color.white * 2f;
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
