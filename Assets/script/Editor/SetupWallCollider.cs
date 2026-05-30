using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using UnityEditor.SceneManagement;

public static class SetupWallCollider
{
    [MenuItem("Tools/Setup Wall Collider")]
    public static void Setup()
    {
        // Wall GameObject 찾기
        var wallGO = GameObject.Find("Wall");
        if (wallGO == null)
        {
            Debug.LogError("[SetupWall] 'Wall' 오브젝트를 찾을 수 없습니다.");
            return;
        }

        // 기존 BoxCollider2D 비활성화 (거대 박스 콜라이더)
        var oldBox = wallGO.GetComponent<BoxCollider2D>();
        if (oldBox != null) oldBox.enabled = false;

        // Rigidbody2D 추가 (Static)
        var rb = wallGO.GetComponent<Rigidbody2D>();
        if (rb == null) rb = Undo.AddComponent<Rigidbody2D>(wallGO);
        rb.bodyType = RigidbodyType2D.Static;

        // TilemapCollider2D 추가
        var tc = wallGO.GetComponent<TilemapCollider2D>();
        if (tc == null) tc = Undo.AddComponent<TilemapCollider2D>(wallGO);
        tc.compositeOperation = Collider2D.CompositeOperation.Merge; // Composite에 위임

        // CompositeCollider2D 추가
        var cc = wallGO.GetComponent<CompositeCollider2D>();
        if (cc == null) cc = Undo.AddComponent<CompositeCollider2D>(wallGO);
        cc.geometryType = CompositeCollider2D.GeometryType.Outlines;

        EditorUtility.SetDirty(wallGO);
        EditorSceneManager.MarkSceneDirty(wallGO.scene);

        Debug.Log("[SetupWall] Wall 콜라이더 설정 완료!");
    }
}
