using UnityEngine;
using System.Collections;

/// <summary>
/// 파괴 가능한 박스 - 여러 번 공격해야 부서지며 아이템 드롭
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class BreakableBox : MonoBehaviour
{
    [Header("내구도")]
    public int maxHits = 3;           // 몇 번 맞으면 부서지는지

    [Header("타격 사운드")]
    public AudioClip hitSound;         // 인스펙터에서 오브젝트별로 지정

    [Header("드롭 아이템")]
    public GameObject dropItemPrefab;

    private Animator       anim;
    private SpriteRenderer sr;
    private bool           isBroken;
    private int            hitCount;
    private bool           isFlashing;

    private static readonly int HashBreak = Animator.StringToHash("Break");

    void Awake()
    {
        anim = GetComponent<Animator>();
        sr   = GetComponent<SpriteRenderer>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isBroken || isFlashing) return;
        if (!other.CompareTag("PlayerAttack")) return;

        hitCount++;

        if (hitCount >= maxHits)
        {
            Break();
        }
        else
        {
            StartCoroutine(HitFlash());
        }
    }

    // 맞을 때마다 흰색으로 번쩍이는 피격 연출
    IEnumerator HitFlash()
    {
        isFlashing = true;
        SoundManager.Instance?.PlaySFX(hitSound);

        // 살짝 흔들기
        Vector3 origin = transform.localPosition;
        transform.localPosition = origin + new Vector3(0.05f, 0f, 0f);
        yield return new WaitForSeconds(0.04f);
        transform.localPosition = origin + new Vector3(-0.05f, 0f, 0f);
        yield return new WaitForSeconds(0.04f);
        transform.localPosition = origin;

        isFlashing = false;
    }

    void Break()
    {
        isBroken = true;
        SoundManager.Instance?.PlaySFX(hitSound);
        anim.SetTrigger(HashBreak);

        // 모든 콜라이더 비활성화
        foreach (var col in GetComponents<Collider2D>())
            col.enabled = false;
    }

    // Animation Event: Box_Break.anim 마지막 프레임에서 호출
    public void OnBreakComplete()
    {
        if (dropItemPrefab != null && !IsDropSuppressed())
            Instantiate(dropItemPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }

    bool IsDropSuppressed()
    {
        var pickup = dropItemPrefab.GetComponent<StatItemPickup>();
        if (pickup == null || pickup.itemData == null) return false;

        var stats = PlayerHealth.Instance?.GetComponent<PlayerStats>();
        return pickup.itemData.IsUnlockComplete(stats);
    }
}
