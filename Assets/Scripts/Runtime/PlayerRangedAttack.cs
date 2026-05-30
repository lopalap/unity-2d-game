using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 근접 공격키를 누를 때 추가로 투사체를 발사합니다.
/// RangedUnlocked(크리스탈 5개) 해금 시 활성화.
/// </summary>
public class PlayerRangedAttack : MonoBehaviour
{
    [Header("투사체")]
    public GameObject projectilePrefab;

    [Header("발사 설정")]
    public float cooldown    = 0.6f;
    public float spawnOffset = 0.4f;
    public float maxRange    = 8f;

    PlayerStats _stats;
    float       _cooldownTimer;

    void Awake()
    {
        _stats = GetComponent<PlayerStats>();
    }

    void Update()
    {
        if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;
        if (_stats == null || !_stats.RangedUnlocked) return;
        if (_cooldownTimer > 0f) return;

        var kb    = Keyboard.current;
        var mouse = Mouse.current;
        bool pressed = (kb    != null && kb.zKey.wasPressedThisFrame) ||
                       (mouse != null && mouse.leftButton.wasPressedThisFrame);
        if (!pressed) return;

        Fire();
    }

    void Fire()
    {
        Vector2 dir = GetNearestEnemyDir();
        if (dir == Vector2.zero)
        {
            // 적 없으면 플레이어가 바라보는 방향으로 발사
            var sr = GetComponent<SpriteRenderer>();
            dir = (sr != null && sr.flipX) ? Vector2.left : Vector2.right;
        }

        Vector3 spawnPos = transform.position + (Vector3)(dir * spawnOffset);
        var go   = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        var proj = go.GetComponent<PlayerProjectile>();
        if (proj != null)
            proj.Launch(dir, _stats.Damage);

        _cooldownTimer = cooldown;
    }

    Vector2 GetNearestEnemyDir()
    {
        float   best   = float.MaxValue;
        Vector2 target = Vector2.zero;

        // 일반 몬스터
        foreach (var eh in FindObjectsOfType<EnemyHealth>())
        {
            if (eh.IsDead) continue;
            float d = Vector2.Distance(transform.position, eh.transform.position);
            if (d < best) { best = d; target = eh.transform.position; }
        }
        // 보스
        foreach (var bh in FindObjectsOfType<BossHealth>())
        {
            if (bh.IsDead) continue;
            float d = Vector2.Distance(transform.position, bh.transform.position);
            if (d < best) { best = d; target = bh.transform.position; }
        }
        // 네크로맨서
        foreach (var nb in FindObjectsOfType<NecromancerBossHealth>())
        {
            if (nb.IsDead) continue;
            float d = Vector2.Distance(transform.position, nb.transform.position);
            if (d < best) { best = d; target = nb.transform.position; }
        }

        if (target == Vector2.zero || best > maxRange) return Vector2.zero;
        return ((Vector2)transform.position - target).normalized * -1f;
    }
}
