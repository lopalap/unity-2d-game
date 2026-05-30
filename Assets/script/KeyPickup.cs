using UnityEngine;
using System.Collections;

/// <summary>
/// 열쇠 아이템 - 보스 처치 시 드롭, 플레이어가 획득
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class KeyPickup : MonoBehaviour
{
    [Header("자석 효과")]
    public float magnetRange = 2.0f;
    public float magnetSpeed = 4f;

    [Header("부유 효과")]
    public float bobHeight = 0.07f;
    public float bobSpeed  = 2.5f;

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

    IEnumerator DropArc()
    {
        Vector3 from = transform.position;
        Vector3 peak = from + new Vector3(Random.Range(-0.4f, 0.4f), 0.8f, 0f);
        Vector3 to   = from + new Vector3(Random.Range(-0.3f, 0.3f), 0f, 0f);

        float elapsed = 0f;
        float duration = 0.45f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
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
        if (isPickedUp || !isSettled) return;

        // 자석 없이 그냥 위아래 부유만
        float newY = settledPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(settledPos.x, newY, settledPos.z);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isPickedUp) return;
        if (!other.CompareTag("Player")) return;

        isPickedUp = true;
        SoundManager.Instance?.PlaySFX(SoundManager.Instance.keyCollect);

        // 인벤토리에 키 추가
        var inv = other.GetComponent<PlayerInventory>();
        if (inv != null) inv.AddKey();

        StartCoroutine(PickupEffect());
    }

    IEnumerator PickupEffect()
    {
        var sr      = GetComponent<SpriteRenderer>();
        float elapsed = 0f;
        Vector3 pos = transform.position;

        while (elapsed < 0.4f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.4f;
            transform.position = pos + Vector3.up * t * 0.5f;
            if (sr != null) sr.color = new Color(1f, 1f, 0.5f, 1f - t);
            yield return null;
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, magnetRange);
    }
}
