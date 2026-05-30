using UnityEngine;
using System.Collections;

/// <summary>
/// 플라스크 아이템 - 박스에서 나와 살짝 튀어오른 뒤 부유, 플레이어가 가까우면 빨려 들어와 회복
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class FlaskPickup : MonoBehaviour
{
    [Header("회복량")]
    public int healAmount = 30;

    [Header("자석 효과")]
    public float magnetRange = 1.5f;
    public float magnetSpeed = 5f;

    [Header("부유 효과")]
    public float bobHeight = 0.08f;
    public float bobSpeed  = 3f;

    private Transform playerTransform;
    private bool      isPickedUp;
    private bool      isSettled;
    private Vector3   settledPos;

    void Start()
    {
        var p = GameObject.FindWithTag("Player");
        if (p != null) playerTransform = p.transform;

        StartCoroutine(DropArc());
    }

    // 박스에서 튀어나오는 작은 아크 연출
    IEnumerator DropArc()
    {
        Vector3 from = transform.position;
        Vector3 peak = from + new Vector3(Random.Range(-0.3f, 0.3f), 0.5f, 0f);
        Vector3 to   = from + new Vector3(Random.Range(-0.2f, 0.2f), 0f, 0f);

        float elapsed = 0f;
        float duration = 0.35f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 포물선: Lerp(from→peak) + Lerp(peak→to)
            Vector3 a = Vector3.Lerp(from, peak, t);
            Vector3 b = Vector3.Lerp(peak, to,   t);
            transform.position = Vector3.Lerp(a, b, t);

            yield return null;
        }

        transform.position = to;
        settledPos = to;
        isSettled  = true;
    }

    void Update()
    {
        if (isPickedUp || !isSettled || playerTransform == null) return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);

        if (dist < magnetRange)
        {
            // 자석: 플레이어 쪽으로 끌려감
            Vector2 dir = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
            transform.position += (Vector3)(dir * magnetSpeed * Time.deltaTime);
        }
        else
        {
            // 부유: 위아래 bobbing
            float newY = settledPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(settledPos.x, newY, settledPos.z);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isPickedUp) return;
        if (!other.CompareTag("Player")) return;

        var ph = other.GetComponent<PlayerHealth>();
        if (ph == null) return;

        isPickedUp = true;
        ph.Heal(healAmount);
        SoundManager.Instance?.PlaySFX(SoundManager.Instance.potionDrink);
        StartCoroutine(PickupEffect());
    }

    IEnumerator PickupEffect()
    {
        var sr      = GetComponent<SpriteRenderer>();
        float elapsed = 0f;
        Vector3 pos = transform.position;

        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.3f;
            transform.position = pos + Vector3.up * t * 0.4f;
            if (sr != null) sr.color = new Color(1f, 1f, 1f, 1f - t);
            yield return null;
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, magnetRange);
    }
}
