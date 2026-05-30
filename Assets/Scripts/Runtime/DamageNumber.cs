using UnityEngine;
using TMPro;

/// <summary>
/// 피격 시 위로 떠오르며 사라지는 데미지 숫자.
/// DamageNumber.Spawn() 으로 생성합니다.
/// </summary>
[RequireComponent(typeof(TextMeshPro))]
public class DamageNumber : MonoBehaviour
{
    [Header("이동/연출")]
    public float floatSpeed  = 1.8f;
    public float lifetime    = 0.9f;
    public float fadeStart   = 0.4f;  // lifetime 의 몇 초 남았을 때 페이드 시작

    private TextMeshPro tmp;
    private float       elapsed;
    private Color       baseColor;

    // ── 정적 생성 헬퍼 ───────────────────────────────────────────
    private static GameObject prefab;

    /// <summary>
    /// 데미지 숫자를 월드 좌표에 스폰합니다.
    /// </summary>
    /// <param name="damage">표시할 수치</param>
    /// <param name="worldPos">스폰 위치</param>
    /// <param name="isPlayerDamage">true = 플레이어 피격(빨강)</param>
    /// <param name="isCrit">크리티컬(2배) — 노랑</param>
    /// <param name="isChain">연쇄 프록(4배) — 빨강</param>
    public static void Spawn(int damage, Vector3 worldPos,
                             bool isPlayerDamage = false, bool isCrit = false, bool isChain = false)
    {
        if (prefab == null)
            prefab = Resources.Load<GameObject>("DamageNumber");
        if (prefab == null) return;

        // 살짝 랜덤 수평 오프셋으로 겹침 방지
        Vector3 offset = new Vector3(Random.Range(-0.15f, 0.15f), 0.3f, -0.1f);
        var go = Instantiate(prefab, worldPos + offset, Quaternion.identity);
        go.GetComponent<DamageNumber>().Init(damage, isPlayerDamage, isCrit, isChain);
    }

    void Awake()
    {
        tmp = GetComponent<TextMeshPro>();
    }

    public void Init(int damage, bool isPlayerDamage, bool isCrit, bool isChain = false)
    {
        if (isPlayerDamage)
        {
            baseColor    = Color.white;                    // 흰색 (플레이어 피격)
            tmp.fontSize = 3.5f;
        }
        else if (isChain)
        {
            baseColor    = new Color(1f, 0.1f, 0.1f);     // 빨강 (연쇄 4배)
            tmp.fontSize = 6f;
        }
        else if (isCrit)
        {
            baseColor    = new Color(1f, 0.95f, 0.2f);    // 노랑 (크리티컬 2배)
            tmp.fontSize = 5f;
        }
        else
        {
            baseColor    = Color.white;                    // 흰색 (일반)
            tmp.fontSize = 3.5f;
        }

        tmp.text  = damage.ToString();
        tmp.color = baseColor;
    }

    void Update()
    {
        elapsed += Time.deltaTime;

        // 위로 이동
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        // 페이드아웃
        float remaining = lifetime - elapsed;
        if (remaining < fadeStart)
        {
            float alpha = Mathf.Clamp01(remaining / fadeStart);
            tmp.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        }

        if (elapsed >= lifetime)
            Destroy(gameObject);
    }
}
