using UnityEngine;

public class EnemyHealthBar : MonoBehaviour
{
    public Transform fill;
    public Transform bg;

    [Header("스프라이트 프레임 (00=빈, 17=가득)")]
    public Sprite[] healthFrames;

    const float ShowDuration = 3f;
    const float FadeDuration = 0.5f;

    EnemyHealth           _eh;
    BossHealth            _bh;
    NecromancerBossHealth _nbh;

    SpriteRenderer _fillSR;
    SpriteRenderer _bgSR;

    int   _prevHp;
    float _visTimer;   // > FadeDuration: 완전 표시 / 0~FadeDuration: 페이드아웃 / <=0: 투명

    void Start()
    {
        _eh  = GetComponentInParent<EnemyHealth>();
        _bh  = GetComponentInParent<BossHealth>();
        _nbh = GetComponentInParent<NecromancerBossHealth>();

        if (fill != null)
            _fillSR = fill.GetComponent<SpriteRenderer>();
        if (bg != null)
            _bgSR = bg.GetComponent<SpriteRenderer>();

        _prevHp = GetCurrentHp();
        SetAlpha(0f);
    }

    void Update()
    {
        if (fill == null) return;

        bool dead = (_eh  != null && _eh.IsDead)  ||
                    (_bh  != null && _bh.IsDead)   ||
                    (_nbh != null && _nbh.IsDead);
        if (dead) { gameObject.SetActive(false); return; }

        // 피격 감지 → 타이머 리셋
        int curHp = GetCurrentHp();
        if (curHp < _prevHp)
            _visTimer = ShowDuration + FadeDuration;
        _prevHp = curHp;

        // 알파 계산 및 적용
        if (_visTimer > 0f)
        {
            _visTimer -= Time.deltaTime;
            float alpha = (_visTimer <= FadeDuration)
                ? Mathf.Clamp01(_visTimer / FadeDuration)   // 페이드아웃 구간
                : 1f;                                        // 완전 표시 구간
            SetAlpha(alpha);
        }

        // 체력 비율로 스프라이트 프레임 선택 (00=빈, 17=가득)
        if (_fillSR != null && healthFrames != null && healthFrames.Length > 0)
        {
            float ratio = GetRatio();
            int idx = Mathf.Clamp(Mathf.RoundToInt(ratio * (healthFrames.Length - 1)), 0, healthFrames.Length - 1);
            _fillSR.sprite = healthFrames[idx];
        }
    }

    void SetAlpha(float a)
    {
        if (_fillSR != null) { var c = _fillSR.color; _fillSR.color = new Color(c.r, c.g, c.b, a); }
        if (_bgSR   != null) { var c = _bgSR.color;   _bgSR.color   = new Color(c.r, c.g, c.b, a); }
    }

    int GetCurrentHp()

    {
        if (_eh  != null) return _eh.CurrentHp;
        if (_bh  != null) return _bh.CurrentHp;
        if (_nbh != null) return _nbh.CurrentHp;
        return 0;
    }

    float GetRatio()
    {
        if (_eh  != null && _eh.maxHp  > 0) return Mathf.Clamp01((float)_eh.CurrentHp  / _eh.maxHp);
        if (_bh  != null && _bh.maxHp  > 0) return Mathf.Clamp01((float)_bh.CurrentHp  / _bh.maxHp);
        if (_nbh != null && _nbh.maxHp > 0) return Mathf.Clamp01((float)_nbh.CurrentHp / _nbh.maxHp);
        return 1f;
    }
}
