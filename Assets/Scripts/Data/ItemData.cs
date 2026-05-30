using UnityEngine;

/// <summary>
/// 플레이어 성장 아이템 데이터 — Assets > Create > Game > Item Data 로 생성
/// StatItemPickup 프리팹의 itemData 슬롯에 할당하면 습득 시 PlayerStats 에 적용됩니다.
/// </summary>
[CreateAssetMenu(fileName = "ItemData", menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("기본 정보")]
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("스탯 보너스")]
    [Tooltip("최대 HP 증가량")]
    public int   maxHpBonus;
    [Tooltip("공격력 증가량")]
    public int   damageBonus;
    [Tooltip("이동속도 증가량")]
    public float speedBonus;
    [Tooltip("공격속도 배율 (0.92 = 8% 빠르게, 1 = 변화없음)")]
    [Range(0.5f, 1f)]
    public float attackSpeedMult = 1f;

    [Header("추가 스탯")]
    [Tooltip("방어력 — 피격 시 데미지 감소량")]
    public int   defenseBonus;
    [Tooltip("흡혈 — 공격 데미지의 몇 %를 HP로 회복 (0~1)")]
    [Range(0f, 1f)]
    public float lifestealPercent;
    [Tooltip("크리티컬 확률 — 2배 데미지 발동 확률 (0~1)")]
    [Range(0f, 1f)]
    public float critChance;
    [Tooltip("구르기 쿨타임 감소량 (초)")]
    public float rollCooldownReduction;
    [Tooltip("피격 무적 시간 추가량 (초)")]
    public float invincibilityBonus;
    [Tooltip("원거리 공격 해금용 — 5개 모으면 해금")]
    public bool  rangedUnlock;
    [Tooltip("원거리 사거리 보너스")]
    public float rangedRange;

    [Header("특수 능력 해금 (5개 수집 시 해금)")]
    [Tooltip("체력 불굴 — HP≤30% 시 방어력+10, 피해 20% 감소")]
    public bool hpUnlock;
    [Tooltip("공격력 처형 — HP<30% 적 즉사 (보스 제외)")]
    public bool dmgUnlock;
    [Tooltip("이동속도 질풍 — 구르기 후 2초간 이동속도 20% 증가")]
    public bool spdUnlock;
    [Tooltip("공격속도 연타 — 25% 확률로 공격 2회")]
    public bool atkSpdUnlock;
    [Tooltip("방어력 가시갑옷 — 피격 시 받은 피해의 30% 반사")]
    public bool defUnlock;
    [Tooltip("흡혈 과흡혈 — 10% 확률로 준 데미지 전량 흡혈")]
    public bool lifestealUnlock;
    [Tooltip("크리티컬 연쇄 — 25% 확률로 데미지 4배")]
    public bool critUnlock;
    [Tooltip("구르기 관통 — 구르기 중 적 관통")]
    public bool rollPassUnlock;
    [Tooltip("무적시간 반격 — 구르기 중 피격 시 100% 데미지 반사")]
    public bool counterUnlock;

    /// <summary>이 아이템의 해금 능력이 이미 완료됐으면 true (드롭 필터링용)</summary>
    public bool IsUnlockComplete(PlayerStats stats)
    {
        if (stats == null) return false;
        if (hpUnlock        && stats.FortitudeUnlocked)    return true;
        if (dmgUnlock       && stats.ExecutionUnlocked)    return true;
        if (spdUnlock       && stats.WindUnlocked)         return true;
        if (atkSpdUnlock    && stats.DoubleStrikeUnlocked) return true;
        if (defUnlock       && stats.ThornUnlocked)        return true;
        if (lifestealUnlock && stats.OverhealUnlocked)     return true;
        if (critUnlock      && stats.ChainUnlocked)        return true;
        if (rollPassUnlock  && stats.RollPassUnlocked)     return true;
        if (counterUnlock   && stats.CounterUnlocked)      return true;
        if (rangedUnlock    && stats.RangedUnlocked)       return true;
        return false;
    }
}
