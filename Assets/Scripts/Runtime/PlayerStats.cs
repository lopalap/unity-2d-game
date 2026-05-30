using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 플레이어 성장 스탯 — Player GameObject 에 추가합니다.
/// ItemData 를 ApplyItem() 으로 적용하면 스탯이 누적됩니다.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    [Header("기본 스탯")]
    public int   baseMaxHp     = 100;
    public int   baseDamage    = 10;
    public float baseMoveSpeed = 5f;

    // ── 런타임 보너스 ─────────────────────────────────────────
    int   _hpBonus;
    int   _dmgBonus;
    float _speedBonus;
    float _atkSpeedMult     = 1f;
    int   _defenseBonus;
    float _lifestealPercent;
    float _critChance;
    float _rollCooldownReduction;
    float _invincibilityBonus;
    int   _rangedUnlockCount;
    float _rangedRange;

    // 해금 카운트
    int _hpUnlockCount;
    int _dmgUnlockCount;
    int _spdUnlockCount;
    int _atkSpdUnlockCount;
    int _defUnlockCount;
    int _lifestealUnlockCount;
    int _critUnlockCount;
    int _rollPassUnlockCount;
    int _counterUnlockCount;

    // ── 외부에서 읽는 최종 스탯 ──────────────────────────────
    public int   MaxHp                => baseMaxHp     + _hpBonus;
    public int   Damage               => baseDamage    + _dmgBonus;
    public float MoveSpeed            => baseMoveSpeed + _speedBonus;
    public float AtkSpeedMult         => _atkSpeedMult;
    public int   Defense              => _defenseBonus;
    public float LifestealPercent     => _lifestealPercent;
    public float CritChance           => _critChance;
    public float RollCooldownReduction => _rollCooldownReduction;
    public float InvincibilityBonus   => _invincibilityBonus;
    public bool  RangedUnlocked       => _rangedUnlockCount >= 5;
    public int   RangedUnlockCount    => _rangedUnlockCount;
    public float RangedRange          => _rangedRange;

    // 해금 능력
    public bool FortitudeUnlocked    => _hpUnlockCount    >= 5;
    public bool ExecutionUnlocked    => _dmgUnlockCount   >= 5;
    public bool WindUnlocked         => _spdUnlockCount   >= 5;
    public bool DoubleStrikeUnlocked => _atkSpdUnlockCount >= 5;
    public bool ThornUnlocked        => _defUnlockCount   >= 5;
    public bool OverhealUnlocked     => _lifestealUnlockCount >= 5;
    public bool ChainUnlocked        => _critUnlockCount  >= 5;
    public bool RollPassUnlocked     => _rollPassUnlockCount >= 5;
    public bool CounterUnlocked      => _counterUnlockCount >= 5;

    readonly List<ItemData> _items = new();
    public IReadOnlyList<ItemData> Items => _items;

    // ── 아이템 적용 ───────────────────────────────────────────
    public void ApplyItem(ItemData item)
    {
        if (item == null) return;

        _items.Add(item);
        _hpBonus              += item.maxHpBonus;
        _dmgBonus             += item.damageBonus;
        _speedBonus           += item.speedBonus;
        _defenseBonus         += item.defenseBonus;
        _lifestealPercent     = Mathf.Min(0.8f, _lifestealPercent + item.lifestealPercent);
        _critChance           = Mathf.Min(0.9f, _critChance       + item.critChance);
        _rollCooldownReduction += item.rollCooldownReduction;
        _invincibilityBonus   += item.invincibilityBonus;

        if (item.attackSpeedMult > 0f && item.attackSpeedMult < 1f)
            _atkSpeedMult *= item.attackSpeedMult;

        _rangedRange += item.rangedRange;

        if (item.rangedUnlock)
        {
            _rangedUnlockCount++;
            if (RangedUnlocked)
                Debug.Log("[PlayerStats] 원거리 공격 해금!");
        }

        if (item.hpUnlock)         { _hpUnlockCount++;         if (FortitudeUnlocked)    Debug.Log("[PlayerStats] 불굴 해금!"); }
        if (item.dmgUnlock)        { _dmgUnlockCount++;        if (ExecutionUnlocked)    Debug.Log("[PlayerStats] 처형 해금!"); }
        if (item.spdUnlock)        { _spdUnlockCount++;        if (WindUnlocked)         Debug.Log("[PlayerStats] 질풍 해금!"); }
        if (item.atkSpdUnlock)     { _atkSpdUnlockCount++;     if (DoubleStrikeUnlocked) Debug.Log("[PlayerStats] 연타 해금!"); }
        if (item.defUnlock)        { _defUnlockCount++;        if (ThornUnlocked)        Debug.Log("[PlayerStats] 가시갑옷 해금!"); }
        if (item.lifestealUnlock)  { _lifestealUnlockCount++;  if (OverhealUnlocked)     Debug.Log("[PlayerStats] 과흡혈 해금!"); }
        if (item.critUnlock)       { _critUnlockCount++;       if (ChainUnlocked)        Debug.Log("[PlayerStats] 연쇄 해금!"); }
        if (item.rollPassUnlock)   { _rollPassUnlockCount++;   if (RollPassUnlocked)     Debug.Log("[PlayerStats] 구르기 관통 해금!"); }
        if (item.counterUnlock)    { _counterUnlockCount++;    if (CounterUnlocked)      Debug.Log("[PlayerStats] 반격 해금!"); }

        if (item.maxHpBonus > 0)
            GetComponent<PlayerHealth>()?.AddMaxHp(item.maxHpBonus);

        Debug.Log($"[PlayerStats] {item.itemName} 획득 → " +
                  $"HP {MaxHp}  DMG {Damage}  SPD {MoveSpeed:F1}  " +
                  $"DEF {Defense}  Crit {CritChance*100:F0}%  " +
                  $"Lifesteal {LifestealPercent*100:F0}%");

        SpawnPickupPopups(item);
    }

    void SpawnPickupPopups(ItemData item)
    {
        var pos = transform.position;
        int line = 0;

        // ── 스텟 수치 상승 ──────────────────────────────────────
        if (item.maxHpBonus > 0)
            StatPopup.SpawnStat($"HP +{item.maxHpBonus}", pos, line++);
        if (item.damageBonus > 0)
            StatPopup.SpawnStat($"공격력 +{item.damageBonus}", pos, line++);
        if (item.speedBonus > 0)
            StatPopup.SpawnStat($"이동속도 +{item.speedBonus:F1}", pos, line++);
        if (item.attackSpeedMult > 0f && item.attackSpeedMult < 1f)
            StatPopup.SpawnStat($"공격속도 ×{item.attackSpeedMult:F2}", pos, line++);
        if (item.defenseBonus > 0)
            StatPopup.SpawnStat($"방어력 +{item.defenseBonus}", pos, line++);
        if (item.lifestealPercent > 0)
            StatPopup.SpawnStat($"흡혈 +{item.lifestealPercent * 100:F0}%", pos, line++);
        if (item.critChance > 0)
            StatPopup.SpawnStat($"크리티컬 +{item.critChance * 100:F0}%", pos, line++);
        if (item.rollCooldownReduction > 0)
            StatPopup.SpawnStat($"구르기 -{item.rollCooldownReduction:F1}s", pos, line++);
        if (item.invincibilityBonus > 0)
            StatPopup.SpawnStat($"무적 +{item.invincibilityBonus:F1}s", pos, line++);
        if (item.rangedRange > 0)
            StatPopup.SpawnStat($"원거리 +{item.rangedRange:F1}", pos, line++);

        // ── 해금 진행/완성 ──────────────────────────────────────
        if (item.hpUnlock)
            StatPopup.SpawnUnlock(FortitudeUnlocked    ? "불굴 해금!"    : $"불굴 {_hpUnlockCount}/5",        pos, FortitudeUnlocked,    line++);
        if (item.dmgUnlock)
            StatPopup.SpawnUnlock(ExecutionUnlocked    ? "처형 해금!"    : $"처형 {_dmgUnlockCount}/5",       pos, ExecutionUnlocked,    line++);
        if (item.spdUnlock)
            StatPopup.SpawnUnlock(WindUnlocked         ? "질풍 해금!"    : $"질풍 {_spdUnlockCount}/5",       pos, WindUnlocked,         line++);
        if (item.atkSpdUnlock)
            StatPopup.SpawnUnlock(DoubleStrikeUnlocked ? "연타 해금!"    : $"연타 {_atkSpdUnlockCount}/5",   pos, DoubleStrikeUnlocked, line++);
        if (item.defUnlock)
            StatPopup.SpawnUnlock(ThornUnlocked        ? "가시 해금!"    : $"가시 {_defUnlockCount}/5",       pos, ThornUnlocked,        line++);
        if (item.lifestealUnlock)
            StatPopup.SpawnUnlock(OverhealUnlocked     ? "과흡혈 해금!"  : $"과흡혈 {_lifestealUnlockCount}/5", pos, OverhealUnlocked,  line++);
        if (item.critUnlock)
            StatPopup.SpawnUnlock(ChainUnlocked        ? "연쇄 해금!"    : $"연쇄 {_critUnlockCount}/5",      pos, ChainUnlocked,        line++);
        if (item.rollPassUnlock)
            StatPopup.SpawnUnlock(RollPassUnlocked     ? "관통 해금!"    : $"관통 {_rollPassUnlockCount}/5",  pos, RollPassUnlocked,     line++);
        if (item.counterUnlock)
            StatPopup.SpawnUnlock(CounterUnlocked      ? "반격 해금!"    : $"반격 {_counterUnlockCount}/5",   pos, CounterUnlocked,      line++);
        if (item.rangedUnlock)
            StatPopup.SpawnUnlock(RangedUnlocked       ? "원거리 해금!"  : $"원거리 {_rangedUnlockCount}/5",  pos, RangedUnlocked,       line++);
    }
}
