using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Collider2D))]
public class PortalController : MonoBehaviour
{
    [Header("전환 씬")]
    public string targetScene = "main_Scene";

    [Header("전환 딜레이 (초)")]
    public float transitionDelay = 0.5f;

    private bool isTransitioning = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isTransitioning) return;
        if (!other.CompareTag("Player")) return;

        StartCoroutine(TransitionCoroutine());
    }

    IEnumerator TransitionCoroutine()
    {
        isTransitioning = true;
        SoundManager.Instance?.PlaySFX(SoundManager.Instance.portalJump);
        yield return new WaitForSeconds(transitionDelay);
        SceneManager.LoadScene(targetScene);
    }
}
