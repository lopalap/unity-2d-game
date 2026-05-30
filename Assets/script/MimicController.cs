using UnityEngine;
using System.Collections;

/// <summary>
/// 미믹 전용 컨트롤러
/// - 평소: 상자처럼 가만히 있음 (EnemyController 비활성)
/// - 플레이어에게 공격받으면: 각성 → 일반 몬스터처럼 추적/공격
/// </summary>
[RequireComponent(typeof(EnemyController))]
[RequireComponent(typeof(EnemyHealth))]
public class MimicController : MonoBehaviour
{
    [Header("각성 연출")]
    public float wakeUpFlashCount = 4f;   // 깨어날 때 번쩍이는 횟수

    private EnemyController controller;
    private EnemyHealth     health;
    private SpriteRenderer  sr;
    private bool            isAwakened = false;

    void Awake()
    {
        controller = GetComponent<EnemyController>();
        health     = GetComponent<EnemyHealth>();
        sr         = GetComponent<SpriteRenderer>();

        // 처음엔 AI 비활성화 → 상자처럼 가만히 있음
        controller.enabled = false;
    }

    void Update()
    {
        if (isAwakened) return;
        if (health == null) return;

        // 체력이 처음으로 줄어들면 → 각성!
        if (health.CurrentHp < health.maxHp)
            StartCoroutine(WakeUp());
    }

    IEnumerator WakeUp()
    {
        isAwakened = true;

        // 번쩍이는 각성 연출
        for (int i = 0; i < wakeUpFlashCount; i++)
        {
            if (sr != null) sr.color = Color.red;
            yield return new WaitForSeconds(0.08f);
            if (sr != null) sr.color = Color.white;
            yield return new WaitForSeconds(0.08f);
        }

        // AI 활성화 → 추적/공격 시작
        if (!health.IsDead)
            controller.enabled = true;
    }
}
