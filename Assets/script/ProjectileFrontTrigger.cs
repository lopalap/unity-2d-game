using UnityEngine;

/// <summary>
/// 투사체 앞쪽 트리거 - 충돌 이벤트를 BossProjectile로 전달
/// </summary>
public class ProjectileFrontTrigger : MonoBehaviour
{
    [HideInInspector]
    public BossProjectile owner;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null)
            owner.OnFrontHit(other);
    }
}
