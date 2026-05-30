using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public static class CreatePlayerObject
{
    // ── 기존 Player에 빠진 컴포넌트만 추가 ──────────────────────
    [MenuItem("Tools/Player/Setup Player Components")]
    public static void Setup()
    {
        var go = GameObject.Find("Player");
        if (go == null)
        {
            Debug.LogError("[PlayerSetup] 씬에 'Player' 오브젝트가 없습니다.");
            return;
        }

        // Animator Controller 로드 (없으면 자동 생성)
        const string CTRL = "Assets/animation/Player/PlayerAnimator.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CTRL);
        if (controller == null)
        {
            PlayerAnimatorSetup.Run();
            controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CTRL);
        }

        // Rigidbody2D 설정 보정 (중력 제거, 회전 고정)
        var rb = go.GetComponent<Rigidbody2D>();
        if (rb == null) rb = Undo.AddComponent<Rigidbody2D>(go);
        rb.gravityScale = 0f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Animator 추가
        var anim = go.GetComponent<Animator>();
        if (anim == null) anim = Undo.AddComponent<Animator>(go);
        anim.runtimeAnimatorController = controller;

        // PlayerController 추가
        if (go.GetComponent<PlayerController>() == null)
            Undo.AddComponent<PlayerController>(go);

        // PlayerHealth 추가
        if (go.GetComponent<PlayerHealth>() == null)
            Undo.AddComponent<PlayerHealth>(go);

        go.tag = "Player";

        EditorUtility.SetDirty(go);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);

        Debug.Log("[PlayerSetup] 완료! Animator / PlayerController / PlayerHealth 추가됨.");
    }

    // ── 새 Player 오브젝트 생성 (기존 것 없을 때) ───────────────
    [MenuItem("Tools/Player/Create Player in Scene")]
    public static void Create()
    {
        var existing = GameObject.Find("Player");
        if (existing != null)
        {
            Debug.LogWarning("[CreatePlayer] 이미 Player가 있습니다. 'Setup Player Components'를 사용하세요.");
            Selection.activeGameObject = existing;
            EditorGUIUtility.PingObject(existing);
            return;
        }

        const string SHEET = "Assets/FreeKnight_v1/Colour1/NoOutline/120x80_PNGSheets/_Idle.png";
        Sprite idleSprite = null;
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(SHEET))
        {
            if (obj is Sprite s && s.name == "_Idle_0") { idleSprite = s; break; }
        }

        const string CTRL = "Assets/animation/Player/PlayerAnimator.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CTRL);
        if (controller == null) { PlayerAnimatorSetup.Run(); controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CTRL); }

        var go = new GameObject("Player");
        Undo.RegisterCreatedObjectUndo(go, "Create Player");
        go.transform.position   = Vector3.zero;
        go.transform.localScale = new Vector3(2f, 2f, 1f);
        go.tag = "Player";

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = idleSprite;
        sr.sortingOrder = 5;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var col = go.AddComponent<BoxCollider2D>();
        col.size   = new Vector2(0.6f, 0.9f);
        col.offset = new Vector2(0f, 0.1f);

        var anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;

        go.AddComponent<PlayerController>();
        go.AddComponent<PlayerHealth>();

        Selection.activeGameObject = go;
        SceneView.FrameLastActiveSceneView();
        EditorGUIUtility.PingObject(go);
        Debug.Log("[CreatePlayer] Player 생성 완료!");
    }
}
