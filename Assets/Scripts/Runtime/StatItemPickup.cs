using UnityEngine;

/// <summary>
/// 스탯 아이템 픽업 — SpriteRenderer + Collider2D(isTrigger) 와 함께 사용합니다.
/// itemData 에 ItemData SO 를 할당하면 플레이어가 닿을 때 PlayerStats 에 적용됩니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class StatItemPickup : MonoBehaviour
{
    public ItemData itemData;

    [Header("픽업 연출")]
    public GameObject pickupEffectPrefab;

    void Awake()
    {
        // 반드시 트리거여야 충돌 없이 감지
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var stats = other.GetComponent<PlayerStats>();
        if (stats == null) return;

        if (itemData != null)
            stats.ApplyItem(itemData);

        if (pickupEffectPrefab != null)
            PoolManager.Get(pickupEffectPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
