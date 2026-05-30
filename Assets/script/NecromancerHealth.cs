using System.Collections;
using UnityEngine;

public class NecromancerHealth : MonoBehaviour
{
    [Header("Stats")]
    public int maxHp = 300;

    [Header("Drop")]
    public GameObject keyDropPrefab;

    private static readonly int HashDead = Animator.StringToHash("isDead");
    private static readonly int HashHit  = Animator.StringToHash("TakeDamage");

    private Animator             anim;
    private NecromancerController controller;
    private SpriteRenderer        sr;

    private int  currentHp;
    private bool isDead;
    private bool enrageTriggered;

    void Awake()
    {
        anim       = GetComponent<Animator>();
        controller = GetComponent<NecromancerController>();
        sr         = GetComponent<SpriteRenderer>();
        currentHp  = maxHp;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;
        if (!other.CompareTag("PlayerAttack")) return;

        var hitbox = other.GetComponent<AttackHitbox>();
        int dmg = hitbox != null ? hitbox.damage : 10;
        TakeDamage(dmg);
    }

    void TakeDamage(int dmg)
    {
        currentHp -= dmg;
        currentHp  = Mathf.Max(currentHp, 0);

        if (!enrageTriggered && currentHp <= maxHp / 2)
        {
            enrageTriggered = true;
            controller.OnHealthChanged(currentHp, maxHp);
        }

        if (currentHp <= 0) { Die(); return; }

        anim.SetTrigger(HashHit);
    }

    void Die()
    {
        isDead = true;
        controller.Die();
        anim.SetBool(HashDead, true);

        foreach (var col in GetComponents<Collider2D>())
            col.enabled = false;

        if (keyDropPrefab != null)
            Instantiate(keyDropPrefab, transform.position, Quaternion.identity);

        Invoke(nameof(FallbackDestroy), 4f);
    }

    // Animation Event (Death 마지막 프레임)
    public void OnDeathComplete()
    {
        StartCoroutine(FadeAndDestroy());
    }

    IEnumerator FadeAndDestroy()
    {
        float t = 0f;
        Color c = sr.color;
        while (t < 1f)
        {
            t += Time.deltaTime * 1.5f;
            sr.color = new Color(c.r, c.g, c.b, 1f - t);
            yield return null;
        }
        Destroy(gameObject);
    }

    void FallbackDestroy() => Destroy(gameObject);
}
