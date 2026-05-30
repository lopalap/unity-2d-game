using UnityEngine;
using System.Collections;

/// <summary>
/// 보스방 문 — 열쇠 획득 시 플레이어가 닿으면 즉시 열림
/// </summary>
public class BossDoor : MonoBehaviour
{
    [Header("사운드")]
    public AudioClip openSound;
    public AudioClip lockedSound;

    private bool _isOpen;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (_isOpen) return;
        if (!collision.gameObject.CompareTag("Player")) return;

        var inv = collision.gameObject.GetComponent<PlayerInventory>();
        if (inv != null && inv.HasKey)
            Open();
        else
            SoundManager.Instance?.PlaySFX(lockedSound);
    }

    // PlayerInventory.AddKey()에서 호출 — 키 획득 시 문 반짝임
    public void OnKeyObtained()
    {
        StartCoroutine(FlashDoor());
    }

    IEnumerator FlashDoor()
    {
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == null) yield break;

        for (int i = 0; i < 3; i++)
        {
            sr.color = Color.yellow;
            yield return new WaitForSeconds(0.15f);
            sr.color = Color.white;
            yield return new WaitForSeconds(0.15f);
        }
    }

    void Open()
    {
        _isOpen = true;
        SoundManager.Instance?.PlaySFX(openSound);
        gameObject.SetActive(false);
    }
}