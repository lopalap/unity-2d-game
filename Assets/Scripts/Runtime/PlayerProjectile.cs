using UnityEngine;

/// <summary>
/// 플레이어 원거리 투사체 — Transform 직접 이동 방식
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerProjectile : MonoBehaviour
{
    [Header("투사체")]
    public float speed    = 8f;
    public int   damage   = 15;
    public float lifetime = 4f;

    [Header("앞쪽 트리거")]
    public float frontOffset = 0.5f;
    public float frontRadius = 0.2f;

    [Header("충돌 연출")]
    public float impactScale        = 1.5f;
    public int   impactSortingOrder = 20;

    Animator       _anim;
    SpriteRenderer _sr;
    GameObject     _frontTriggerObj;
    bool           _hasHit;
    Vector2        _dir;

    static readonly int HashHit = Animator.StringToHash("Hit");

    void Awake()
    {
        _anim = GetComponent<Animator>();
        _sr   = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        Destroy(gameObject, lifetime);
        BuildFrontTrigger();
    }

    void Update()
    {
        if (_hasHit || _dir == Vector2.zero) return;
        transform.Translate(_dir * speed * Time.deltaTime, Space.World);
    }

    void BuildFrontTrigger()
    {
        _frontTriggerObj = new GameObject("FrontTrigger");
        _frontTriggerObj.transform.SetParent(transform, false);
        _frontTriggerObj.transform.localPosition = new Vector3(frontOffset, 0f, 0f);

        var col       = _frontTriggerObj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = frontRadius;
        col.enabled   = false;

        var bridge   = _frontTriggerObj.AddComponent<PlayerProjFrontTrigger>();
        bridge.owner = this;

        Invoke(nameof(EnableFrontTrigger), 0.1f);
    }

    void EnableFrontTrigger()
    {
        if (_hasHit || _frontTriggerObj == null) return;
        var col = _frontTriggerObj.GetComponent<CircleCollider2D>();
        if (col != null) col.enabled = true;
    }

    public void Launch(Vector2 direction, int dmg)
    {
        damage = dmg;
        _dir   = direction.normalized;
        // 스프라이트 기본 방향이 왼쪽이므로 180도 보정
        float angle = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg + 180f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void OnFrontHit(Collider2D other)
    {
        if (_hasHit) return;
        if (other.GetComponent<PlayerProjectile>() != null) return;
        if (other.transform.IsChildOf(transform)) return;

        var eh  = other.GetComponent<EnemyHealth>()              ?? other.GetComponentInParent<EnemyHealth>();
        var bh  = other.GetComponent<BossHealth>()               ?? other.GetComponentInParent<BossHealth>();
        var nbh = other.GetComponent<NecromancerBossHealth>()    ?? other.GetComponentInParent<NecromancerBossHealth>();

        if      (eh  != null) { eh.TakeDamage(damage);  DamageNumber.Spawn(damage, other.transform.position); }
        else if (bh  != null) { bh.TakeDamage(damage);  DamageNumber.Spawn(damage, other.transform.position); }
        else if (nbh != null) { nbh.TakeDamage(damage); DamageNumber.Spawn(damage, other.transform.position); }
        else
        {
            // 플레이어는 무시
            if (other.GetComponent<PlayerHealth>() != null) return;
            if (other.GetComponentInParent<PlayerHealth>() != null) return;
            // 그 외(벽 등) → 폭발
        }

        OnHit();
    }

    void OnHit()
    {
        _hasHit = true;
        _dir    = Vector2.zero;

        if (_frontTriggerObj != null)
        {
            transform.position = _frontTriggerObj.transform.position;
            _frontTriggerObj.SetActive(false);
        }

        transform.localScale = new Vector3(3f, 3f, 1f);
        transform.rotation   = Quaternion.identity;
        if (_sr != null) _sr.sortingOrder = impactSortingOrder;

        _anim.SetTrigger(HashHit);
        Destroy(gameObject, 0.3f);
    }

    public void OnImpactEnd() => Destroy(gameObject);

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.9f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, 0.15f);
        Gizmos.color = Color.red;
        Vector3 front = transform.position + transform.right * frontOffset;
        Gizmos.DrawWireSphere(front, frontRadius);
        Gizmos.DrawLine(transform.position, front);
    }
}
