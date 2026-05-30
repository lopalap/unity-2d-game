using UnityEngine;

/// <summary>
/// 플레이어 인벤토리 — 열쇠 보유 여부 관리
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [Header("열쇠로 열릴 문 오브젝트 (Inspector에서 지정 or 자동 검색)")]
    public GameObject bossDoor;

    public bool HasKey { get; private set; }

    public void AddKey()
    {
        HasKey = true;

        // Inspector에서 지정 안 했으면 이름으로 검색
        if (bossDoor == null)
            bossDoor = GameObject.Find("door");

        if (bossDoor != null)
            bossDoor.SetActive(false);
    }
}
