using UnityEngine;

/// <summary>
/// 아이템 픽업 시 스텟 상승/해금 정보를 월드에 떠오르며 표시.
/// 프리팹: Assets/Resources/StatPopup.prefab — Inspector에서 폰트/크기 설정.
/// StatPopup.SpawnStat() / SpawnUnlock() 으로 호출.
/// </summary>
[RequireComponent(typeof(TextMesh))]
public class StatPopup : MonoBehaviour
{
    public float floatSpeed = 1.5f;
    public float lifetime   = 1.4f;
    public float fadeStart  = 0.55f;

    TextMesh _tm;
    Color    _baseColor;
    float    _elapsed;

    static readonly Color C_STAT     = new Color(0.45f, 1f,    0.55f);  // 연두
    static readonly Color C_PROGRESS = new Color(1f,    0.88f, 0.15f);  // 금색
    static readonly Color C_UNLOCKED = new Color(1f,    0.50f, 0.10f);  // 주황

    // ── 프리팹 로드 ───────────────────────────────────────────────
    static GameObject _prefab;
    static GameObject GetPrefab()
    {
        if (!_prefab) _prefab = Resources.Load<GameObject>("StatPopup");
        return _prefab;
    }

    // ── 공개 스폰 메서드 ──────────────────────────────────────────
    public static void SpawnStat(string text, Vector3 worldPos, int lineIndex = 0)
        => Spawn(text, C_STAT, worldPos, lineIndex, 36);

    public static void SpawnUnlock(string text, Vector3 worldPos, bool completed, int lineIndex = 0)
        => Spawn(text, completed ? C_UNLOCKED : C_PROGRESS, worldPos, lineIndex, completed ? 50 : 40);

    // ── 내부 생성 ─────────────────────────────────────────────────
    static void Spawn(string text, Color color, Vector3 worldPos, int lineIndex, int fontSize)
    {
        var prefab = GetPrefab();
        if (prefab == null)
        {
            Debug.LogWarning("[StatPopup] Assets/Resources/StatPopup.prefab 을 찾을 수 없습니다.");
            return;
        }

        float yOff = 1.0f + lineIndex * 0.55f;
        var go = Object.Instantiate(
            prefab,
            worldPos + new Vector3(Random.Range(-0.2f, 0.2f), yOff, -0.1f),
            Quaternion.identity);

        var tm = go.GetComponent<TextMesh>();
        tm.text     = text;
        tm.color    = color;
        tm.fontSize = fontSize;

        go.GetComponent<StatPopup>()._baseColor = color;
    }

    // ─────────────────────────────────────────────────────────────
    void Awake() => _tm = GetComponent<TextMesh>();

    void Update()
    {
        _elapsed += Time.deltaTime;
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        float remaining = lifetime - _elapsed;
        if (remaining < fadeStart)
        {
            float a = Mathf.Clamp01(remaining / fadeStart);
            _tm.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, a);
        }

        if (_elapsed >= lifetime) Destroy(gameObject);
    }
}
