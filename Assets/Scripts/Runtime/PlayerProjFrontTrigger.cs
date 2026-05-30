using UnityEngine;

public class PlayerProjFrontTrigger : MonoBehaviour
{
    [HideInInspector] public PlayerProjectile owner;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null) owner.OnFrontHit(other);
    }
}
