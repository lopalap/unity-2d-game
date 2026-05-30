using UnityEngine;
using System.Collections;

public class NecromancerBossHealth : MonoBehaviour
{
    public int maxHp = 300;
    public GameObject keyDropPrefab;
    public GameObject deathEffectPrefab;

    public int  CurrentHp { get; private set; }
    public bool IsDead    => isDead;

    NecromancerBossController controller;
    SpriteRenderer sr;
    bool isDead = false;

    void Awake()
    {
        controller = GetComponent<NecromancerBossController>();
        sr = GetComponentInChildren<SpriteRenderer>();
        CurrentHp = maxHp;
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;
        CurrentHp = Mathf.Max(0, CurrentHp - amount);
        controller?.OnTakeDamage();
        if (CurrentHp == 0) Die();
    }

    void Die()
    {
        isDead = true;
        var sm = SoundManager.Instance;
        if (sm != null) { sm.PlaySFX(sm.bossDeath); sm.PlayBGM(sm.dungeonBGM); }
        controller.OnDead();
        if (keyDropPrefab != null)
            Instantiate(keyDropPrefab, transform.position, Quaternion.identity);
        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        StartCoroutine(DestroyAfterDeath());
    }

    IEnumerator DestroyAfterDeath()
    {
        yield return new WaitForSeconds(1.2f);
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            sr.color = new Color(1f, 1f, 1f, 1f - t * 2f);
            yield return null;
        }
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("PlayerAttack")) return;
        // Try to read damage from AttackHitbox component on the same object or parent
        var hitbox = other.GetComponent<AttackHitbox>();
        TakeDamage(hitbox != null ? hitbox.damage : 10);
    }
}
