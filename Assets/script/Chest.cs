using UnityEngine;

public class Chest : MonoBehaviour
{
    public GameObject keyPrefab;    // 열쇠 프리팹 (총 1개)
    public GameObject potionPrefab; // 물약 프리팹

    [Range(0, 100)]
    public int potionDropChance = 50; // 물약 나올 확률(%)

    private bool isOpened = false; // 상자가 열렸는지 체크
    private static bool isKeyAlreadySpawned = false; // 열쇠는 전체 게임에서 1개만 나오게 제어

    void OnCollisionEnter2D(Collision2D collision)
    {
        // 플레이어와 닿았고 아직 열리지 않았다면
        if (collision.gameObject.CompareTag("Player") && !isOpened)
        {
            isOpened = true;
            OpenChest();
        }
    }

    void OpenChest()
    {
        SoundManager.Instance?.PlaySFX(SoundManager.Instance.chestOpen);
        // 1. 열쇠 처리: 아직 안 나왔다면 확률적으로 혹은 확정적으로 드랍
        if (!isKeyAlreadySpawned)
        {
            Instantiate(keyPrefab, transform.position + Vector3.up, Quaternion.identity);
            isKeyAlreadySpawned = true; // 열쇠는 한 번만 나오게 함
            Debug.Log("상자에서 열쇠가 나왔습니다!");
        }
        // 2. 물약 처리: 랜덤 확률로 드랍
        else if (Random.Range(0, 100) < potionDropChance)
        {
            Instantiate(potionPrefab, transform.position + Vector3.up, Quaternion.identity);
            Debug.Log("상자에서 물약이 나왔습니다!");
        }
    }
}