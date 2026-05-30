using UnityEngine;

/// <summary>
/// lifetime 초 후 오브젝트를 풀로 반환(또는 파괴)합니다.
/// PoolManager.Get() 으로 스폰하면 자동으로 풀 반환됩니다.
/// </summary>
public class EffectAutoDestroy : MonoBehaviour
{
    public float lifetime = 1.5f;

    void OnEnable()
    {
        CancelInvoke();
        Invoke(nameof(ReturnToPool), lifetime);
    }

    void OnDisable()
    {
        CancelInvoke();
    }

    void ReturnToPool()
    {
        PoolManager.Return(gameObject);
    }
}
