using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class PlayerAnimatorSetup
{
    const string SHEET = "Assets/FreeKnight_v1/Colour1/NoOutline/120x80_PNGSheets/";
    const string OUT   = "Assets/animation/Player/";
    const string CTRL  = "Assets/animation/Player/PlayerAnimator.controller";

    [MenuItem("Tools/Player/Setup Player Animator")]
    public static void Run()
    {
        Directory.CreateDirectory(Application.dataPath + "/animation/Player");
        AssetDatabase.Refresh();

        var idle   = MakeClip("_Idle.png",   "_Idle",   10, "Player_Idle.anim",   10f, loop: true);
        var run    = MakeClip("_Run.png",    "_Run",    10, "Player_Run.anim",    12f, loop: true);
        var attack = MakeClip("_Attack.png", "_Attack",  4, "Player_Attack.anim", 10f, loop: false);
        var roll   = MakeClip("_Roll.png",   "_Roll",   12, "Player_Roll.anim",   15f, loop: false);
        var hit    = MakeClip("_Hit.png",    "_Hit",     1, "Player_Hit.anim",     5f, loop: false);
        var death  = MakeClip("_Death.png",  "_Death",  10, "Player_Death.anim",  10f, loop: false);

        BuildController(idle, run, attack, roll, hit, death);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PlayerAnimatorSetup] 완료 → " + CTRL);
    }

    // ── 애니메이션 클립 생성 ─────────────────────────────────────
    static AnimationClip MakeClip(string png, string prefix, int frameCount,
                                  string clipFile, float fps, bool loop)
    {
        string clipPath = OUT + clipFile;

        // 기존 파일 제거
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath) != null)
            AssetDatabase.DeleteAsset(clipPath);

        // 스프라이트 로드 및 인덱스 순 정렬
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(SHEET + png)
            .OfType<Sprite>()
            .OrderBy(s => {
                var num = s.name.Substring(prefix.Length + 1);
                return int.TryParse(num, out int n) ? n : 0;
            })
            .ToArray();

        var clip = new AnimationClip { frameRate = fps };

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");

        int count = Mathf.Min(frameCount, sprites.Length);
        var keys  = new ObjectReferenceKeyframe[count];
        for (int i = 0; i < count; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = sprites[i] };

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        AssetDatabase.CreateAsset(clip, clipPath);
        return clip;
    }

    // ── Animator Controller 생성 ─────────────────────────────────
    static void BuildController(AnimationClip idle, AnimationClip run,
                                 AnimationClip attack, AnimationClip roll,
                                 AnimationClip hit, AnimationClip death)
    {
        if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), CTRL)))
            AssetDatabase.DeleteAsset(CTRL);

        var ac = AnimatorController.CreateAnimatorControllerAtPath(CTRL);
        ac.AddParameter("isMoving", AnimatorControllerParameterType.Bool);
        ac.AddParameter("Attack",   AnimatorControllerParameterType.Trigger);
        ac.AddParameter("Roll",     AnimatorControllerParameterType.Trigger);
        ac.AddParameter("Hit",      AnimatorControllerParameterType.Trigger);
        ac.AddParameter("isDead",   AnimatorControllerParameterType.Bool);

        var sm = ac.layers[0].stateMachine;

        var stIdle   = AddState(sm, "Idle",   idle);
        var stRun    = AddState(sm, "Run",    run);
        var stAttack = AddState(sm, "Attack", attack);
        var stRoll   = AddState(sm, "Roll",   roll);
        var stHit    = AddState(sm, "Hit",    hit);
        var stDeath  = AddState(sm, "Death",  death);
        sm.defaultState = stIdle;

        // Idle <-> Run
        BoolTransition(stIdle, stRun,  "isMoving", true);
        BoolTransition(stRun,  stIdle, "isMoving", false);

        // AnyState → Attack / Roll / Hit (트리거)
        AnyTrigger(sm, stAttack, "Attack");
        AnyTrigger(sm, stRoll,   "Roll");
        AnyTrigger(sm, stHit,    "Hit");

        // AnyState → Death (bool)
        var toDeath = sm.AddAnyStateTransition(stDeath);
        toDeath.AddCondition(AnimatorConditionMode.If, 0, "isDead");
        toDeath.hasExitTime = false; toDeath.duration = 0;
        toDeath.canTransitionToSelf = false;

        // Attack / Roll / Hit → Idle (애니메이션 종료 후)
        ExitToIdle(stAttack, stIdle);
        ExitToIdle(stRoll,   stIdle);
        ExitToIdle(stHit,    stIdle);
    }

    // ── 헬퍼 ──────────────────────────────────────────────────────
    static AnimatorState AddState(AnimatorStateMachine sm, string name, Motion clip)
    {
        var s = sm.AddState(name);
        s.motion = clip;
        return s;
    }

    static void BoolTransition(AnimatorState from, AnimatorState to, string param, bool val)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = false; t.duration = 0;
        t.AddCondition(val ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, param);
    }

    static void AnyTrigger(AnimatorStateMachine sm, AnimatorState to, string trigger)
    {
        var t = sm.AddAnyStateTransition(to);
        t.AddCondition(AnimatorConditionMode.If, 0, trigger);
        t.hasExitTime = false; t.duration = 0;
        t.canTransitionToSelf = false;
    }

    static void ExitToIdle(AnimatorState from, AnimatorState idle)
    {
        var t = from.AddTransition(idle);
        t.hasExitTime = true; t.exitTime = 1f; t.duration = 0;
    }
}
