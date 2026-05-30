using UnityEngine;

public class EyeProjFrontTrigger : MonoBehaviour
{
    [HideInInspector] public EyeProjectile owner;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null) owner.OnFrontHit(other);
    }
}
