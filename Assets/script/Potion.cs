using UnityEngine;

public class Potion : MonoBehaviour
{
    public int healAmount = 20;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerHealth player = collision.GetComponent<PlayerHealth>();
            if (player != null)
            {
                player.Heal(healAmount);
                SoundManager.Instance?.PlaySFX(SoundManager.Instance.potionDrink);
                Debug.Log("물약을 먹었습니다! 현재 체력: " + player.CurrentHp);
                Destroy(gameObject);
            }
        }
    }
}