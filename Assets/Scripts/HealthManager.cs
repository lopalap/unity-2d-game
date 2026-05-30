using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HP 비율에 따라 CriticalDamage-Sheet의 프레임을 스왑합니다.
/// 100% HP → 프레임 0 / 0% HP → 프레임 19
/// </summary>
public class HealthManager : MonoBehaviour
{
    [Header("스프라이트 시트 경로 (Assets 기준)")]
    public string sheetPath = "Assets/CrimsonFantasyGU_fixed/CrimsonFantasyGUI/CriticalDamage-Sheet.png";

    Sprite[]     frames;
    Image        hpImage;
    PlayerHealth playerHealth;
    int          lastFrameIndex = -1;

    void Awake()
    {
        // 자식 Image 가져오기
        var selfImg = GetComponent<Image>();
        foreach (var img in GetComponentsInChildren<Image>())
        {
            if (img != selfImg) { hpImage = img; break; }
        }

        // 스프라이트 시트 로드
        LoadSprites();
    }

    void Start()
    {
        var playerGo = GameObject.FindWithTag("Player");
        if (playerGo != null)
            playerHealth = playerGo.GetComponent<PlayerHealth>();

        Refresh();
    }

    void Update()
    {
        if (playerHealth == null) return;
        Refresh();
    }

    void LoadSprites()
    {
#if UNITY_EDITOR
        var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(sheetPath);
        var list = new System.Collections.Generic.List<Sprite>();
        foreach (var a in all)
            if (a is Sprite sp) list.Add(sp);

        // 이름 끝 숫자 기준 정렬 (_0 ~ _19)
        list.Sort((a, b) => {
            int ai = int.Parse(a.name.Split('_')[a.name.Split('_').Length - 1]);
            int bi = int.Parse(b.name.Split('_')[b.name.Split('_').Length - 1]);
            return ai.CompareTo(bi);
        });
        frames = list.ToArray();
#endif
    }

    void Refresh()
    {
        if (hpImage == null || frames == null || frames.Length == 0) return;

        int maxHp = playerHealth != null ? playerHealth.maxHp   : 1;
        int curHp = playerHealth != null ? playerHealth.CurrentHp : 1;

        // HP 비율 → 프레임 인덱스 (0 = 풀피, 끝 = 빈피)
        float ratio      = Mathf.Clamp01((float)curHp / maxHp);
        int   frameIndex = Mathf.RoundToInt((1f - ratio) * (frames.Length - 1));

        if (frameIndex == lastFrameIndex) return; // 변화 없으면 스킵
        lastFrameIndex   = frameIndex;
        hpImage.sprite   = frames[frameIndex];
    }
}