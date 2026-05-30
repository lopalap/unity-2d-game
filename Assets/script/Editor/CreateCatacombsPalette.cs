using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using UnityEditor.Tilemaps;

public static class CreateCatacombsPalette
{
    const string PNG_PATH     = "Assets/RF_Catacombs_v1.0/PSD/mainlevbuild.psd";
    const string TILE_OUT     = "Assets/RF_Catacombs_v1.0/Tiles";
    const string PALETTE_PATH = "Assets/RF_Catacombs_v1.0/RF_Catacombs_Palette.prefab";

    [MenuItem("Tools/Create RF_Catacombs Tile Palette")]
    public static void Create()
    {
        // ── 1. 현재 슬라이싱된 스프라이트 그대로 로드 ──────────
        // (사용자가 이미 16x16으로 슬라이싱 완료)
        AssetDatabase.Refresh();

        var sprites = AssetDatabase.LoadAllAssetsAtPath(PNG_PATH)
            .OfType<Sprite>()
            .OrderBy(s => {
                var p = s.name.Split('_');
                return int.TryParse(p[p.Length - 1], out int n) ? n : 0;
            })
            .ToArray();

        if (sprites.Length == 0)
        {
            Debug.LogError("[Catacombs] 스프라이트를 찾을 수 없습니다. mainlevbuild.png 슬라이싱을 확인하세요.");
            return;
        }
        Debug.Log($"[Catacombs] 스프라이트 {sprites.Length}개 로드됨");

        // ── 2. 기존 Tiles 폴더 초기화 후 새 타일 생성 ──────────
        // 기존 타일 에셋 삭제
        if (AssetDatabase.IsValidFolder(TILE_OUT))
        {
            var oldTiles = AssetDatabase.FindAssets("t:Tile", new[] { TILE_OUT });
            foreach (var guid in oldTiles)
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));
        }
        else
        {
            Directory.CreateDirectory(Application.dataPath + "/RF_Catacombs_v1.0/Tiles");
        }
        AssetDatabase.Refresh();

        // 새 타일 에셋 생성
        var tiles = new Tile[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            string tilePath = $"{TILE_OUT}/{sprites[i].name}.asset";
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprites[i];
            tile.color  = Color.white;
            AssetDatabase.CreateAsset(tile, tilePath);
            tiles[i] = tile;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[Catacombs] 타일 에셋 {tiles.Length}개 생성 완료");

        // ── 3. 기존 팔렛트 삭제 후 새로 생성 ───────────────────
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PALETTE_PATH) != null)
            AssetDatabase.DeleteAsset(PALETTE_PATH);

        var palette = GridPaletteUtility.CreateNewPalette(
            Path.GetDirectoryName(PALETTE_PATH),
            Path.GetFileNameWithoutExtension(PALETTE_PATH),
            GridLayout.CellLayout.Rectangle,
            GridPalette.CellSizing.Automatic,
            Vector3.one,
            GridLayout.CellSwizzle.XYZ
        );

        // 팔렛트에 타일 배치 (PNG 원본 가로 기준 열 수 계산)
        var tilemap = palette.GetComponentInChildren<Tilemap>();

        // mainlevbuild.png 가로 1024px / 16px = 64열
        int cols = 64;
        for (int i = 0; i < tiles.Length; i++)
        {
            int x = i % cols;
            int y = -(i / cols);
            tilemap.SetTile(new Vector3Int(x, y, 0), tiles[i]);
        }

        PrefabUtility.SavePrefabAsset(palette);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Catacombs] 타일 팔렛트 생성 완료! ({tiles.Length}개 타일, 64열 배치)");
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(PALETTE_PATH));
    }
}
