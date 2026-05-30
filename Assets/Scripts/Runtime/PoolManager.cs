using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

/// <summary>
/// 프리팹 기반 오브젝트 풀 매니저 (싱글톤)
/// 씬에 없어도 첫 호출 시 자동 생성됩니다.
///
/// 사용법:
///   스폰:  PoolManager.Get(prefab, position, rotation)
///   반환:  PoolManager.Return(instance)
/// </summary>
public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    readonly Dictionary<GameObject, ObjectPool<GameObject>> _pools         = new();
    readonly Dictionary<GameObject, GameObject>             _instanceToPrefab = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── 공개 API ───────────────────────────────────────────────

    /// <summary>풀에서 꺼내거나 새로 생성해 지정 위치에 배치합니다.</summary>
    public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;
        var inst = GetOrCreate().InternalGet(prefab);
        inst.transform.position = position;
        inst.transform.rotation = rotation;
        return inst;
    }

    /// <summary>인스턴스를 풀로 반환합니다. (SetActive false)</summary>
    public static void Return(GameObject instance)
    {
        if (instance == null) return;
        var mgr = GetOrCreate();
        if (mgr._instanceToPrefab.TryGetValue(instance, out var prefab))
            mgr.InternalGet_Pool(prefab).Release(instance);
        else
            Destroy(instance); // 풀 밖에서 생성된 객체는 그냥 파괴
    }

    // ── 내부 구현 ──────────────────────────────────────────────

    static PoolManager GetOrCreate()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("[PoolManager]");
        // Awake 가 즉시 실행돼 Instance 가 세팅됨
        go.AddComponent<PoolManager>();
        return Instance;
    }

    GameObject InternalGet(GameObject prefab)
    {
        var pool = InternalGet_Pool(prefab);
        var obj  = pool.Get();
        _instanceToPrefab[obj] = prefab;
        return obj;
    }

    ObjectPool<GameObject> InternalGet_Pool(GameObject prefab)
    {
        if (!_pools.TryGetValue(prefab, out var pool))
        {
            pool = new ObjectPool<GameObject>(
                createFunc:      () => Instantiate(prefab),
                actionOnGet:     obj => obj.SetActive(true),
                actionOnRelease: obj => obj.SetActive(false),
                actionOnDestroy: obj => Destroy(obj),
                maxSize: 20
            );
            _pools[prefab] = pool;
        }
        return pool;
    }
}
