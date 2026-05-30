using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 특정 조건(슬롯 상태, 가방 확장 등)에 따라 Inventory 스프라이트 시트의 프레임을 스왑합니다.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    [Header("스프라이트 시트 경로 (Assets 기준)")]
    public string sheetPath = "Assets/CrimsonFantasyGU_fixed/CrimsonFantasyGUI/Inventory.png"; // 실제 경로에 맞게 확인해 주세요!

    Sprite[] frames;
    Image targetImage;
    int lastFrameIndex = -1;

    void Awake()
    {
        // 내 자신 컴포넌트나 자식 오브젝트에서 Image 가져오기
        targetImage = GetComponent<Image>();
        if (targetImage == null)
        {
            targetImage = GetComponentInChildren<Image>();
        }

        // 스프라이트 시트 로드
        LoadSprites();
    }

    void LoadSprites()
    {
#if UNITY_EDITOR
        var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(sheetPath);
        if (all == null || all.Length == 0)
        {
            Debug.LogError($"[InventoryManager] 경로를 찾을 수 없습니다: {sheetPath}");
            return;
        }

        var list = new System.Collections.Generic.List<Sprite>();
        foreach (var a in all)
        {
            if (a is Sprite sp) list.Add(sp);
        }

        // 이름 끝 숫자 기준 정렬 (Inventory_0 ~ Inventory_7)
        list.Sort((a, b) => {
            try
            {
                int ai = int.Parse(a.name.Split('_')[a.name.Split('_').Length - 1]);
                int bi = int.Parse(b.name.Split('_')[b.name.Split('_').Length - 1]);
                return ai.CompareTo(bi);
            }
            catch
            {
                return a.name.CompareTo(b.name);
            }
        });

        frames = list.ToArray();
#endif
    }

    /// <summary>
    /// 원하는 인벤토리 슬롯 번호(0~7)를 입력하여 이미지를 변경합니다.
    /// </summary>
    public void ChangeInventorySprite(int index)
    {
        if (targetImage == null || frames == null || frames.Length == 0) return;

        // 인덱스 범위 예외 방지
        index = Mathf.Clamp(index, 0, frames.Length - 1);

        if (index == lastFrameIndex) return; // 변화 없으면 스킵
        lastFrameIndex = index;

        targetImage.sprite = frames[index];
    }
}