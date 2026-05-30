using UnityEngine;
using System.Collections;

/// <summary>
/// 파괴 가능한 오브젝트 (박스: maxHits=3, 물병: maxHits=1)
/// 드롭 확률: 40% 없음 / 30% FlaskPickup / 30% 랜덤 보석
/// </summary>
public class BreakableObject : MonoBehaviour
{
    [Header("내구도")]
    public int maxHits = 1;

    [Header("드롭 확률 (합계 = 1)")]
    [Range(0f, 1f)] public float nothingChance = 0.4f;
    [Range(0f, 1f)] public float flaskChance   = 0.3f;
    // 나머지 확률(1 - nothing - flask)이 보석 확률

    [Header("드롭 프리팹")]
    public GameObject   flaskPrefab;
    public GameObject[] gemPrefabs;

    [Header("사운드")]
    public bool useGlassSound = false; // false = chestOpen, true = glassBreaking

    [Header("피격 진동")]
    public float shakeAmount   = 0.08f;
    public float shakeSpeed    = 25f;
    public float shakeDuration = 0.25f;

    private int     currentHits;
    private bool    isBroken;
    private bool    isShaking;
    private Vector3 originPos;

    void Awake()
    {
        originPos = transform.localPosition;
    }

    public void TakeHit()
    {
        if (isBroken) return;
        currentHits++;

        var sm = SoundManager.Instance;
        if (sm != null)
            sm.PlaySFX(useGlassSound ? sm.glassBreaking : sm.chestOpen);

        if (!isShaking)
            StartCoroutine(Shake());

        if (currentHits >= maxHits)
            Break();
    }

    IEnumerator Shake()
    {
        isShaking = true;
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            if (isBroken) yield break;
            elapsed += Time.deltaTime;
            float offsetX = Mathf.Sin(elapsed * shakeSpeed) * shakeAmount;
            transform.localPosition = originPos + new Vector3(offsetX, 0f, 0f);
            yield return null;
        }
        transform.localPosition = originPos;
        isShaking = false;
    }

    void Break()
    {
        isBroken = true;

        float roll = Random.value;

        if (roll < nothingChance)
        {
            // 40% — 아무것도 없음
        }
        else if (roll < nothingChance + flaskChance)
        {
            // 30% — FlaskPickup
            if (flaskPrefab != null)
                Instantiate(flaskPrefab, transform.position, Quaternion.identity);
        }
        else
        {
            // 30% — 랜덤 보석
            if (gemPrefabs != null && gemPrefabs.Length > 0)
            {
                var prefab = gemPrefabs[Random.Range(0, gemPrefabs.Length)];
                if (prefab != null)
                    Instantiate(prefab, transform.position, Quaternion.identity);
            }
        }

        Destroy(gameObject);
    }
}
