using UnityEditor;
using UnityEngine;
using System.Reflection;

/// <summary>
/// Unity 에디터 시작·컴파일 시 Game View 스케일을 0.33x로 자동 설정
/// </summary>
[InitializeOnLoad]
public static class GameViewScaleDefault
{
    const float TARGET_SCALE = 0.33f;

    static GameViewScaleDefault()
    {
        EditorApplication.delayCall += ApplyScale;
    }

    static void ApplyScale()
    {
        var gameViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        if (gameViewType == null) return;

        var gameView = EditorWindow.GetWindow(gameViewType, false, null, false);
        if (gameView == null) return;

        var zoomAreaField = gameViewType.GetField("m_ZoomArea",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var zoomArea = zoomAreaField?.GetValue(gameView);
        if (zoomArea == null) return;

        var scaleField = zoomArea.GetType().GetField("m_Scale",
            BindingFlags.Instance | BindingFlags.NonPublic);
        scaleField?.SetValue(zoomArea, new Vector2(TARGET_SCALE, TARGET_SCALE));

        gameView.Repaint();
    }
}
