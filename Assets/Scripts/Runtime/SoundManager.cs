using UnityEngine;

/// <summary>
/// 게임 전체 사운드 관리 싱글톤
/// 씬에 하나 배치하고 Inspector에서 AudioClip을 모두 할당합니다.
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("플레이어")]
    public AudioClip swordAttack;
    public AudioClip attackHit;          // 공격이 적에게 맞을 때
    public AudioClip playerHit;          // 플레이어가 피격될 때
    public AudioClip playerDeath;
    public AudioClip[] footsteps;        // Stone Chain Run 1~3

    [Header("상자 / 아이템")]
    public AudioClip chestOpen;
    public AudioClip keyCollect;
    public AudioClip glassBreaking;
    public AudioClip itemPickup;
    public AudioClip potionDrink;

    [Header("던전")]
    public AudioClip dungeonBGM;
    public AudioClip portalJump;

    [Header("몬스터")]
    public AudioClip goblinAttack;
    public AudioClip goblinDeath;
    public AudioClip skeletonAttack;
    public AudioClip skeletonDeath;
    public AudioClip slimeAttack;
    public AudioClip slimeDeath;
    public AudioClip smallMonsterAttack;
    public AudioClip smallMonsterDeath;

    [Header("보스")]
    public AudioClip bossBGM;
    public AudioClip bossMagic;
    public AudioClip bossDeath;
    public AudioClip necroAttack2;
    public AudioClip skeletonSummon;

    AudioSource _bgmSource;
    AudioSource _sfxSource;
    AudioSource _timedSfxSource;
    Coroutine   _timedSFXCoroutine;
    int         _footstepIndex;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // BGM용 AudioSource
        _bgmSource           = gameObject.AddComponent<AudioSource>();
        _bgmSource.loop      = true;
        _bgmSource.volume    = 0.5f;
        _bgmSource.playOnAwake = false;

        // SFX용 AudioSource
        _sfxSource           = gameObject.AddComponent<AudioSource>();
        _sfxSource.loop      = false;
        _sfxSource.playOnAwake = false;

        // 시간 제한 SFX용 AudioSource
        _timedSfxSource           = gameObject.AddComponent<AudioSource>();
        _timedSfxSource.loop      = false;
        _timedSfxSource.playOnAwake = false;
    }

    void Start()
    {
        if (dungeonBGM != null) PlayBGM(dungeonBGM);
    }

    // ── BGM ─────────────────────────────────────────────
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || (_bgmSource.clip == clip && _bgmSource.isPlaying)) return;
        _bgmSource.clip = clip;
        _bgmSource.Play();
    }

    public void StopBGM() => _bgmSource.Stop();

    // ── SFX ─────────────────────────────────────────────
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        _sfxSource.PlayOneShot(clip);
    }

    // 시간 제한 SFX: duration(초) 후 자동 정지
    public void PlaySFXTimed(AudioClip clip, float duration)
    {
        if (clip == null) return;
        if (_timedSFXCoroutine != null) StopCoroutine(_timedSFXCoroutine);
        _timedSFXCoroutine = StartCoroutine(TimedSFXCoroutine(clip, duration));
    }

    System.Collections.IEnumerator TimedSFXCoroutine(AudioClip clip, float duration)
    {
        _timedSfxSource.clip = clip;
        _timedSfxSource.Play();
        yield return new WaitForSeconds(duration);
        _timedSfxSource.Stop();
        _timedSFXCoroutine = null;
    }

    // 발걸음: 순서대로 순환
    public void PlayFootstep()
    {
        if (footsteps == null || footsteps.Length == 0) return;
        _footstepIndex = (_footstepIndex + 1) % footsteps.Length;
        PlaySFX(footsteps[_footstepIndex]);
    }
}
