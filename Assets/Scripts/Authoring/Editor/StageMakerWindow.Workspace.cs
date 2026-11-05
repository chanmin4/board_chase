using System;
using UnityEditor;
using UnityEngine;

public readonly struct StageMakerWorkspaceRects
{
    public readonly Rect library, settings, preview;
    public StageMakerWorkspaceRects(Rect library, Rect settings, Rect preview)
    { this.library = library; this.settings = settings; this.preview = preview; }
}

/// <summary>Responsive editor-only geometry. Does not read or modify any Stage assets.</summary>
public static class StageMakerWorkspaceLayout
{
    public static StageMakerWorkspaceRects Calculate(Vector2 size, bool showLibrary, bool showPreview,
        float libraryWidth, float settingsShare)
    {
        float width = Mathf.Max(0, size.x), height = Mathf.Max(0, size.y - 24);
        float list = showLibrary ? Mathf.Min(Mathf.Clamp(libraryWidth, 175, 280), width * .3f) : 0;
        float left = showLibrary ? list + 5 : 0;
        float available = Mathf.Max(0, width - left - (showPreview ? 5 : 0));
        float mapMinimum = Mathf.Min(300, available * .4f);
        float settingsMinimum = Mathf.Min(470, available - mapMinimum);
        float settings = showPreview
            ? Mathf.Clamp(available * Mathf.Clamp(settingsShare, .25f, .8f), settingsMinimum, available - mapMinimum)
            : available;
        return new StageMakerWorkspaceRects(new Rect(0, 24, list, height),
            new Rect(left, 24, settings, height), showPreview
                ? new Rect(left + settings + 5, 24, available - settings, height) : Rect.zero);
    }
}

/// <summary>Fit occupied rooms, including room labels and extraction badges, not the empty grid.</summary>
public static class StageMakerMapView
{
    public static void Fit(Vector2 viewport, Vector2 minimum, Vector2 maximum, Vector2 pitch,
        out float zoom, out Vector2 pan)
    {
        var content = new Rect(minimum.x * pitch.x - 80, -maximum.y * pitch.y - 60,
            (maximum.x - minimum.x) * pitch.x + 160, (maximum.y - minimum.y) * pitch.y + 135);
        zoom = Mathf.Clamp(Mathf.Min(Mathf.Max(1, viewport.x - 48) / content.width,
            Mathf.Max(1, viewport.y - 48) / content.height), .01f, 2.5f);
        pan = -content.center * zoom;
    }
}

public static class StageMakerGenerationDiagnostics
{
    public static string Summary(string error) => error;
}

public sealed partial class StageMakerWindow
{
    private bool _showErrorDetails;

    private void DrawGenerationFeedback()
    {
        if (!_hasPreviewAttempt) return;
        if (_previewSourceJson != EditorJsonUtility.ToJson(_selected)) _previewStale = true;
        if (_previewStale)
        {
            EditorGUILayout.HelpBox("설정이 변경됐습니다. 아래 [현재 시드로 생성]을 눌러 결과를 갱신하세요.", MessageType.Warning);
            return;
        }
        if (string.IsNullOrEmpty(_generationError)) return;
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.HelpBox(StageMakerGenerationDiagnostics.Summary(_generationError), MessageType.Error);
            if (_generationError.StartsWith("맵 생성 설정 오류", StringComparison.Ordinal) && GUILayout.Button("문제 설정 펼치기"))
            {
                _generationOpen = true; _mapsOpen = false;
                if (_generationError.Contains("고급 생성 규칙 오류")) _advanced = true;
            }
            _showErrorDetails = EditorGUILayout.Foldout(_showErrorDetails, "원본 오류 로그", true);
            if (_showErrorDetails) GUILayout.Label(_generationError, EditorStyles.wordWrappedMiniLabel);
        }
    }

    private void DrawPreviewOptions()
    {
        using (new EditorGUI.DisabledScope(EditorApplication.isCompiling))
        {
            EditorGUI.BeginChangeCheck();
            _seed = EditorGUILayout.IntField("미리보기 시드", _seed);
            if (_selected.MapPolicy != StageMapPolicy.RadialBraidedRaid)
                _legacyStart = EditorGUILayout.Vector2IntField("구버전 시작 좌표", _legacyStart);
            _requiredPreviewIds = EditorGUILayout.TextField(new GUIContent("보장할 특별맵 ID", "예: office,medical. 퀘스트나 Stage SO를 수정하지 않는 테스트 옵션입니다."), _requiredPreviewIds);
            if (EditorGUI.EndChangeCheck()) Changed();
            _batchCount = Mathf.Clamp(EditorGUILayout.IntField("연속 검증 횟수", _batchCount), 1, 1000);
            GUILayout.Label("생성·검증은 씬이나 저장 데이터를 바꾸지 않습니다. 아래 버튼으로 실행합니다.", EditorStyles.wordWrappedMiniLabel);
        }
    }
}
