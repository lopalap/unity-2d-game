using UnityEngine;

/// <summary>
/// 적 공통 스탯 데이터 — Assets > Create > Game > Enemy Data 로 생성
/// EnemyController / EnemyHealth / EnemyDamage 의 [SerializeField] data 슬롯에 할당하면
/// Inspector 기본값 대신 이 SO 값이 적용됩니다.
/// </summary>
[CreateAssetMenu(fileName = "EnemyData", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("체력")]
    public int maxHp = 30;

    [Header("이동")]
    public float moveSpeed   = 2f;
    public float detectRange = 5f;

    [Header("공격")]
    public float attackRange    = 0.8f;
    public float attackCooldown = 1.5f;
    public float windupTime     = 0.6f;
    public int   damage         = 10;
}
