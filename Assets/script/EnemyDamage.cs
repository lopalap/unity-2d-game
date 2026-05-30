using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    // 인스펙터 창에서 직접 값을 입력할 수 있습니다.
    // 근거리 적은 3, 원거리 적은 5를 입력하세요.
    public int damage = 3;

    [Header("데이터 (ScriptableObject)")]
    [SerializeField] EnemyData data;

    void Awake()
    {
        if (data != null) damage = data.damage;
    }
}