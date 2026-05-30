using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;

public static class CreateMonsters
{
    // ── 경로 상수 ─────────────────────────────────────────────────
    const string CHAR_BASE   = "Assets/2D Pixel Dungeon Asset Pack v2.0/2D Pixel Dungeon Asset Pack/Character_animation/monsters_idle/";
    const string ANIM_BASE   = "Assets/Enemy_Animations_Set/Enemy_Animations_Set/";
    const string ANIM_OUT    = "Assets/animation/Monsters/";
    const string PREFAB_OUT  = "Assets/Prefabs/Monsters/";

    // ── 몬스터 정의 ───────────────────────────────────────────────
    static readonly MonsterDef[] Defs = {
        new MonsterDef("Skull_v2",
            idleSprites:   new SpriteSheet(CHAR_BASE + "skull/v2/skull_v2_{0}.png", 4),
            moveSprites:   null,
            attackSprites: null,
            deathSprites:  null,
            hitSprites:    null),

        new MonsterDef("Vampire_v2",
            idleSprites:   new SpriteSheet(ANIM_BASE + "enemies-vampire_idle.png",        6, false),
            moveSprites:   new SpriteSheet(ANIM_BASE + "enemies-vampire_movement.png",    8, false),
            attackSprites: new SpriteSheet(ANIM_BASE + "enemies-vampire_attack.png",     16, false),
            deathSprites:  new SpriteSheet(ANIM_BASE + "enemies-vampire_death.png",      14, false),
            hitSprites:    new SpriteSheet(ANIM_BASE + "enemies-vampire_take_damage.png", 5, false)),
    };

    // ── 프리팹 생성 ───────────────────────────────────────────────
    [MenuItem("Tools/Monsters/Create Monster Prefabs")]
    public static void CreatePrefabs()
    {
        Directory.CreateDirectory(Application.dataPath + "/animation/Monsters");
        Directory.CreateDirectory(Application.dataPath + "/Prefabs/Monsters");
        AssetDatabase.Refresh();

        foreach (var def in Defs)
        {
            var ctrl   = BuildController(def);
            var prefab = BuildPrefab(def, ctrl);
            Debug.Log($"[Monsters] 프리팹 생성: {prefab.name}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Monsters] 전체 프리팹 생성 완료 → Assets/Prefabs/Monsters/");
    }

    // ── 씬에 배치 (프리팹 인스턴스로) ────────────────────────────
    [MenuItem("Tools/Monsters/Place Monsters in Scene")]
    public static void PlaceInScene()
    {
        // 프리팹이 없으면 먼저 생성
        string firstPath = PREFAB_OUT + Defs[0].name + ".prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(firstPath) == null)
            CreatePrefabs();

        var playerGO  = GameObject.Find("Player");
        Transform playerTr = playerGO?.transform;

        Vector2[][] positions = {
            new[]{ new Vector2(-17f,  8f), new Vector2(-14f, 10f) },
            new[]{ new Vector2(-10f, 12f), new Vector2( -8f, 10f) },
            new[]{ new Vector2(  2f, 12f), new Vector2(  4f, 10f) },
            new[]{ new Vector2( 10f,  8f), new Vector2( 12f, 10f) },
        };

        for (int d = 0; d < Defs.Length; d++)
        {
            var def    = Defs[d];
            string path = PREFAB_OUT + def.name + ".prefab";
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabAsset == null) { Debug.LogWarning("프리팹 없음: " + path); continue; }

            for (int i = 0; i < positions[d].Length; i++)
            {
                var pos  = positions[d][i];
                string name = i == 0 ? def.name : def.name + "_" + i;

                // 기존 인스턴스 제거
                var existing = GameObject.Find(name);
                if (existing != null) Undo.DestroyObjectImmediate(existing);

                // 프리팹 인스턴스 생성
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
                go.name = name;
                go.transform.position = new Vector3(pos.x, pos.y, 0f);
                Undo.RegisterCreatedObjectUndo(go, "Place " + name);

                // EnemyController 설정
                var ec = go.GetComponent<EnemyController>();
                if (ec != null)
                {
                    ec.roomCenter = pos;
                    ec.roomRange  = new Vector2(4f, 4f);
                    if (playerTr != null) ec.player = playerTr;
                }
            }
        }

        EditorSceneManager.MarkAllScenesDirty();
        Debug.Log("[Monsters] 씬 배치 완료!");
    }

    // ── Animator Controller 생성 ─────────────────────────────────
    static AnimatorController BuildController(MonsterDef def)
    {
        string path = ANIM_OUT + def.name + "_Animator.controller";
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
            AssetDatabase.DeleteAsset(path);

        var idle   = MakeClip(def.name + "_Idle",   def.idleSprites,   loop: true,  fps: 8f);
        var move   = def.moveSprites   != null ? MakeClip(def.name + "_Move",   def.moveSprites,   loop: true,  fps: 10f) : idle;
        var attack = def.attackSprites != null ? MakeClip(def.name + "_Attack", def.attackSprites, loop: false, fps: 12f) : idle;
        var death  = def.deathSprites  != null ? MakeClip(def.name + "_Death",  def.deathSprites,  loop: false, fps: 10f) : idle;
        var hit    = def.hitSprites    != null ? MakeClip(def.name + "_Hit",    def.hitSprites,    loop: false, fps: 10f) : idle;

        var ac = AnimatorController.CreateAnimatorControllerAtPath(path);
        ac.AddParameter("isMoving",   AnimatorControllerParameterType.Bool);
        ac.AddParameter("Attack",     AnimatorControllerParameterType.Trigger);
        ac.AddParameter("TakeDamage", AnimatorControllerParameterType.Trigger);
        ac.AddParameter("isDead",     AnimatorControllerParameterType.Bool);

        var sm = ac.layers[0].stateMachine;
        var stIdle   = AddState(sm, "Idle",   idle);
        var stMove   = AddState(sm, "Move",   move);
        var stAttack = AddState(sm, "Attack", attack);
        var stHit    = AddState(sm, "Hit",    hit);
        var stDeath  = AddState(sm, "Death",  death);
        sm.defaultState = stIdle;

        BoolTrans(stIdle, stMove, "isMoving", true);
        BoolTrans(stMove, stIdle, "isMoving", false);
        AnyTrigger(sm, stAttack, "Attack");
        AnyTrigger(sm, stHit,    "TakeDamage");

        var tDeath = sm.AddAnyStateTransition(stDeath);
        tDeath.AddCondition(AnimatorConditionMode.If, 0, "isDead");
        tDeath.hasExitTime = false; tDeath.duration = 0;
        tDeath.canTransitionToSelf = false;

        ExitToIdle(stAttack, stIdle);
        ExitToIdle(stHit,    stIdle);

        AssetDatabase.SaveAssets();
        return ac;
    }

    // ── 프리팹 GameObject 생성 ────────────────────────────────────
    static GameObject BuildPrefab(MonsterDef def, AnimatorController ctrl)
    {
        string prefabPath = PREFAB_OUT + def.name + ".prefab";

        // 첫 번째 스프라이트
        var firstSprite = def.idleSprites.isIndividual
            ? AssetDatabase.LoadAssetAtPath<Sprite>(string.Format(def.idleSprites.pathPattern, 1))
            : AssetDatabase.LoadAllAssetsAtPath(def.idleSprites.sheetPath).OfType<Sprite>().FirstOrDefault();

        // 임시 GameObject 구성
        var go = new GameObject(def.name);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = firstSprite;
        sr.sortingOrder = 5;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = go.AddComponent<BoxCollider2D>();
        col.size   = new Vector2(0.7f, 0.7f);
        col.offset = Vector2.zero;

        var anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;

        var ec = go.AddComponent<EnemyController>();
        ec.moveSpeed     = 2f;
        ec.detectRange   = 5f;
        ec.attackRange   = 0.8f;
        ec.attackCooldown = 1.5f;

        var ed = go.AddComponent<EnemyDamage>();
        ed.damage = 10;

        var eh = go.AddComponent<EnemyHealth>();
        eh.maxHp = 30;

        // 프리팹 저장 (기존 덮어쓰기)
        bool isNew = !File.Exists(Path.Combine(Directory.GetCurrentDirectory(), prefabPath));
        var prefab = isNew
            ? PrefabUtility.SaveAsPrefabAsset(go, prefabPath)
            : PrefabUtility.SaveAsPrefabAsset(go, prefabPath);

        Object.DestroyImmediate(go);
        return prefab;
    }

    // ── 애니메이션 클립 생성 ──────────────────────────────────────
    static AnimationClip MakeClip(string clipName, SpriteSheet sheet, bool loop, float fps)
    {
        string clipPath = ANIM_OUT + clipName + ".anim";
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath) != null)
            AssetDatabase.DeleteAsset(clipPath);

        var clip = new AnimationClip { frameRate = fps };
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");

        Sprite[] sprites;
        if (sheet.isIndividual)
        {
            sprites = Enumerable.Range(1, sheet.frameCount)
                .Select(i => AssetDatabase.LoadAssetAtPath<Sprite>(string.Format(sheet.pathPattern, i)))
                .Where(s => s != null).ToArray();
        }
        else
        {
            sprites = AssetDatabase.LoadAllAssetsAtPath(sheet.sheetPath)
                .OfType<Sprite>()
                .OrderBy(s => {
                    var p = s.name.Split('_');
                    return int.TryParse(p[p.Length - 1], out int n) ? n : 0;
                }).ToArray();
        }

        if (sprites.Length == 0)
            Debug.LogWarning($"[Monsters] 스프라이트 없음: {clipName}");

        var keys = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = sprites[i] };

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        AssetDatabase.CreateAsset(clip, clipPath);
        return clip;
    }

    // ── 헬퍼 ──────────────────────────────────────────────────────
    static AnimatorState AddState(AnimatorStateMachine sm, string n, Motion m)
    { var s = sm.AddState(n); s.motion = m; return s; }

    static void BoolTrans(AnimatorState f, AnimatorState t, string p, bool v)
    { var tr = f.AddTransition(t); tr.hasExitTime=false; tr.duration=0;
      tr.AddCondition(v?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,p); }

    static void AnyTrigger(AnimatorStateMachine sm, AnimatorState to, string trigger)
    { var t = sm.AddAnyStateTransition(to); t.AddCondition(AnimatorConditionMode.If,0,trigger);
      t.hasExitTime=false; t.duration=0; t.canTransitionToSelf=false; }

    static void ExitToIdle(AnimatorState from, AnimatorState idle)
    { var t = from.AddTransition(idle); t.hasExitTime=true; t.exitTime=1f; t.duration=0; }

    // ── 데이터 구조 ───────────────────────────────────────────────
    class MonsterDef
    {
        public string      name;
        public SpriteSheet idleSprites, moveSprites, attackSprites, deathSprites, hitSprites;

        public MonsterDef(string name,
            SpriteSheet idleSprites, SpriteSheet moveSprites,
            SpriteSheet attackSprites, SpriteSheet deathSprites, SpriteSheet hitSprites)
        {
            this.name = name;
            this.idleSprites   = idleSprites;
            this.moveSprites   = moveSprites;
            this.attackSprites = attackSprites;
            this.deathSprites  = deathSprites;
            this.hitSprites    = hitSprites;
        }
    }

    class SpriteSheet
    {
        public bool   isIndividual;
        public string pathPattern;
        public string sheetPath;
        public int    frameCount;

        // 개별 PNG (Character_animation)  — 파라미터: (경로패턴, 프레임수)
        public SpriteSheet(string pathPattern, int frameCount)
        { isIndividual=true; this.pathPattern=pathPattern; this.frameCount=frameCount; }

        // 스프라이트 시트 (Enemy_Animations_Set) — 파라미터: (시트경로, 프레임수, isSheet=false)
        public SpriteSheet(string sheetPath, int frameCount, bool isSheet)
        { isIndividual=false; this.sheetPath=sheetPath; this.frameCount=frameCount; }
    }
}
