using UnityEngine;

/// <summary>
/// 플레이어 달리기 발 이펙트 — 스프린트 중에만 표시
/// </summary>
public class PlayerRunEffect : MonoBehaviour
{
    [Header("위치 / 크기 조절")]
    public Vector2 offset = new Vector2(0f, -0.3f);
    public Vector3 effectScale = new Vector3(1f, 1f, 1f);

    PlayerController _player;
    SpriteRenderer   _sr;
    SpriteRenderer   _playerSr;
    Animator         _anim;

    void Awake()
    {
        _player   = GetComponentInParent<PlayerController>();
        _sr       = GetComponent<SpriteRenderer>();
        _playerSr = _player != null ? _player.GetComponent<SpriteRenderer>() : null;
        _anim     = GetComponent<Animator>();
    }

    void Update()
    {
        bool sprinting  = _player != null && _player.IsSprinting;
        bool facingLeft = _playerSr != null && _playerSr.flipX;

        // enabled 토글 시 Animator가 sprite를 쓰지 않아 null 유지되는 문제 방지
        // → color alpha로 투명/불투명 전환 (SpriteRenderer는 항상 enabled)
        if (_sr != null)
        {
            _sr.color = sprinting ? Color.white : new Color(1f, 1f, 1f, 0f);
            _sr.flipX = facingLeft; // 플레이어 방향에 맞게 스프라이트 반전
        }
        if (_anim != null) _anim.speed = sprinting ? 1f : 0f;

        // X offset을 플레이어 방향에 따라 반전 (뒤에 위치하도록)
        float xPos = facingLeft ? -offset.x : offset.x;
        transform.localPosition = new Vector3(xPos, offset.y, 0f);
        transform.localScale    = effectScale;
    }

    void OnDrawGizmos()
    {
        // 씬 뷰에 항상 원 표시 — offset 위치 확인용
        Gizmos.color = new Color(0f, 1f, 0f, 0.8f); // 초록 원
        Vector3 worldPos = transform.parent != null
            ? transform.parent.TransformPoint(new Vector3(offset.x, offset.y, 0f))
            : transform.position;
        Gizmos.DrawWireSphere(worldPos, 0.15f);

        // 중심점 표시
        Gizmos.color = new Color(1f, 1f, 0f, 1f); // 노란 점
        Gizmos.DrawSphere(worldPos, 0.04f);
    }
}
