using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    [Header("이동")]
    public float moveSpeed = 5f;

    [Header("회피")]
    public float rollSpeed = 10f;
    public float rollDuration = 0.45f;

    public bool IsRolling => isRolling;
    public bool IsDead    => isDead;

    private Rigidbody2D  rb;
    private Animator     anim;
    private SpriteRenderer sr;

    private PlayerStats stats;

    private bool    isAttacking;
    private bool    isRolling;
    private bool    isDead;
    private float   rollTimer;
    private Vector2 rollDir;

    // 콤보
    private bool    comboQueued;   // 현재 공격 중 다음 공격 예약
    private bool    nextIsAttack2; // 다음에 쓸 공격: false=Attack, true=Attack2

    // 구르기 쿨타임
    [Header("구르기 쿨타임")]
    public float rollCooldown = 1.0f;
    private float rollCooldownTimer;

    // 질풍 속도 부스트 타이머 (이동속도 해금)
    private float rollSpeedBoostTimer;

    [Header("스프린트")]
    public float sprintDelay        = 3f;    // 이동 후 스프린트 전환까지 시간
    public float sprintSpeedMult    = 1.5f;  // 스프린트 속도 배율
    public float sprintAnimSpeedMult = 1.5f; // 스프린트 시 애니메이션 배속
    private float _moveTimer;             // 연속 이동 시간
    private bool  _isSprinting;
    public  bool  IsSprinting => _isSprinting;

    private float _footstepTimer;

    private static readonly int HashMoving   = Animator.StringToHash("isMoving");
    private static readonly int HashAttack   = Animator.StringToHash("Attack");
    private static readonly int HashAttack2  = Animator.StringToHash("Attack2");
    private static readonly int HashRoll     = Animator.StringToHash("Roll");
    private static readonly int HashHit      = Animator.StringToHash("Hit");
    private static readonly int HashDead     = Animator.StringToHash("isDead");
    private static readonly int HashDashing  = Animator.StringToHash("isDashing");

    void Awake()
    {
        rb    = GetComponent<Rigidbody2D>();
        anim  = GetComponent<Animator>();
        sr    = GetComponent<SpriteRenderer>();
        stats = GetComponent<PlayerStats>();

        rb.gravityScale = 0f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
    }

    void Update()
    {
        if (isDead) return;

        if (rollSpeedBoostTimer > 0f) rollSpeedBoostTimer -= Time.deltaTime;

        HandleMovement();
        HandleAttack();
        HandleRoll();
        SyncAttackState();
    }

    void FixedUpdate()
    {
        if (rollCooldownTimer > 0f)
            rollCooldownTimer -= Time.fixedDeltaTime;

        if (!isRolling) return;

        rollTimer -= Time.fixedDeltaTime;
        rb.linearVelocity = rollDir * rollSpeed;

        if (rollTimer <= 0f)
        {
            isRolling = false;
            rb.linearVelocity = Vector2.zero;
            SetEnemyPassThrough(false);
            // 질풍 해금: 구르기 후 2초간 이동속도 20% 증가
            if (stats != null && stats.WindUnlocked)
                rollSpeedBoostTimer = 2f;
        }
    }

    // ── 이동 (WASD / 방향키) ──────────────────────────
    void HandleMovement()
    {
        if (isRolling || isAttacking) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        float h = 0f, v = 0f;
        if      (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  h = -1f;
        else if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)  h =  1f;
        if      (kb.sKey.isPressed || kb.downArrowKey.isPressed)   v = -1f;
        else if (kb.wKey.isPressed || kb.upArrowKey.isPressed)     v =  1f;

        var dir = new Vector2(h, v).normalized;
        bool isMoving = dir.sqrMagnitude > 0f;

        // ── 스프린트 타이머 ──
        if (isMoving)
        {
            _moveTimer += Time.deltaTime;
        }
        else
        {
            _moveTimer = 0f;
            ExitSprint();
        }

        if (_moveTimer >= sprintDelay && !_isSprinting)
            EnterSprint();

        // ── 속도 계산 ──
        float speed = stats != null ? stats.MoveSpeed : moveSpeed;
        if (rollSpeedBoostTimer > 0f && stats != null && stats.WindUnlocked)
            speed *= 1.2f;
        if (_isSprinting)
            speed *= sprintSpeedMult;

        rb.linearVelocity = dir * speed;
        anim.SetBool(HashMoving, isMoving);

        if (h != 0f) sr.flipX = h < 0f;

        // 발걸음 사운드
        if (isMoving)
        {
            _footstepTimer -= Time.deltaTime;
            if (_footstepTimer <= 0f)
            {
                bool windBoost = rollSpeedBoostTimer > 0f && stats != null && stats.WindUnlocked;
                _footstepTimer = _isSprinting ? 0.35f / sprintAnimSpeedMult
                               : windBoost    ? 0.35f / 1.2f
                               :                0.35f;
                SoundManager.Instance?.PlayFootstep();
            }
        }
        else
        {
            _footstepTimer = 0f;
        }
    }

    void EnterSprint()
    {
        _isSprinting = true;
        anim.SetBool(HashDashing, true);
    }

    // 전투 행동(공격·피격·구르기) 시 스프린트 해제용 public 메서드
    public void ExitSprint()
    {
        if (!_isSprinting) return;
        _isSprinting = false;
        _moveTimer   = 0f;
        anim.SetBool(HashDashing, false);
    }

    // ── 공격 (Z키 또는 마우스 좌클릭) ────────────────
    void HandleAttack()
    {
        if (isRolling) return;

        var kb    = Keyboard.current;
        var mouse = Mouse.current;

        bool pressed = (kb    != null && kb.zKey.wasPressedThisFrame) ||
                       (mouse != null && mouse.leftButton.wasPressedThisFrame);
        if (!pressed) return;

        if (!isAttacking)
        {
            // 공격 시작 — 항상 Attack1부터
            ExitSprint();
            isAttacking   = true;
            comboQueued   = false;
            nextIsAttack2 = true; // 다음 콤보는 Attack2
            rb.linearVelocity = Vector2.zero;
            anim.SetTrigger(HashAttack);
            SoundManager.Instance?.PlaySFX(SoundManager.Instance.swordAttack);
        }
        else if (!comboQueued)
        {
            // 공격 중 한 번만 예약
            comboQueued = true;
        }
    }

    // 공격 상태 종료 감지 + 사이클 콤보 발동
    void SyncAttackState()
    {
        if (isAttacking)
        {
            var info = anim.GetCurrentAnimatorStateInfo(0);
            if (info.IsName("Idle") || info.IsName("Run"))
            {
                if (comboQueued)
                {
                    // 예약된 다음 공격 발동
                    comboQueued = false;
                    rb.linearVelocity = Vector2.zero;
                    if (nextIsAttack2)
                    {
                        nextIsAttack2 = false; // 그 다음은 Attack1
                        anim.SetTrigger(HashAttack2);
                    }
                    else
                    {
                        nextIsAttack2 = true;  // 그 다음은 Attack2
                        anim.SetTrigger(HashAttack);
                    }
                }
                else
                {
                    isAttacking = false;
                }
            }
        }

        // anim.speed: 공격 중 → AtkSpeedMult, 스프린트 중 → sprintAnimSpeedMult, 질풍 중 → 1.2x, 그 외 → 1
        float targetSpeed;
        bool windActive = rollSpeedBoostTimer > 0f && stats != null && stats.WindUnlocked;
        if (isAttacking)
        {
            float mult = (stats != null && stats.AtkSpeedMult > 0f) ? stats.AtkSpeedMult : 1f;
            targetSpeed = 1f / mult;
        }
        else if (_isSprinting)
        {
            targetSpeed = sprintAnimSpeedMult;
        }
        else if (windActive)
        {
            targetSpeed = 1.2f;
        }
        else
        {
            targetSpeed = 1f;
        }
        if (!Mathf.Approximately(anim.speed, targetSpeed))
            anim.speed = targetSpeed;
    }

    // ── 회피 (Left Shift 또는 X키) ────────────────────
    void HandleRoll()
    {
        if (isRolling || isAttacking) return;
        if (rollCooldownTimer > 0f) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        bool rolled = kb.leftShiftKey.wasPressedThisFrame ||
                      kb.xKey.wasPressedThisFrame;
        if (!rolled) return;

        float h = 0f, v = 0f;
        if      (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  h = -1f;
        else if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)  h =  1f;
        if      (kb.sKey.isPressed || kb.downArrowKey.isPressed)   v = -1f;
        else if (kb.wKey.isPressed || kb.upArrowKey.isPressed)     v =  1f;

        rollDir = new Vector2(h, v).normalized;
        if (rollDir == Vector2.zero)
            rollDir = sr.flipX ? Vector2.left : Vector2.right;

        ExitSprint();
        isRolling = true;
        rollTimer = rollDuration;

        // 쿨타임 설정 (스탯 감소 반영)
        float reduction = stats != null ? stats.RollCooldownReduction : 0f;
        rollCooldownTimer = Mathf.Max(0.1f, rollCooldown - reduction);

        anim.SetTrigger(HashRoll);
        // 구르기 관통은 h 해금(RollPassUnlocked)이 있을 때만 활성화
        if (stats != null && stats.RollPassUnlocked)
            SetEnemyPassThrough(true);
    }

    // ── 구르기 중 몬스터 관통 ──────────────────────────────────
    void SetEnemyPassThrough(bool ignore)
    {
        int playerLayer = gameObject.layer;
        int enemyLayer  = LayerMask.NameToLayer("Enemy");
        if (enemyLayer < 0) return;
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, ignore);
    }

    // ── 외부 호출용 ───────────────────────────────────
    public void TriggerHit()
    {
        ExitSprint();
        anim.SetTrigger(HashHit);
    }

    public void TriggerDeath()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        anim.SetBool(HashDead, true);
    }
}
