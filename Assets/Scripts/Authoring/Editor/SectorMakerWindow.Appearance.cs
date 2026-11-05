using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed partial class SectorMakerWindow
{
    internal const string NoGridThemePath = "Assets/ScriptableObject/GameScene/Stage/Sector/Themes/SectorTheme_Lab_NoGrid.asset";
    [SerializeField] private SectorThemeSO _appearanceThemeDraft;
    [NonSerialized] private int _appearanceRoomId;
    [SerializeField] private bool _showThemeFields;

    private void DrawAppearanceWorkbench()
    {
        if (!IsEditingRoom) return;
        int roomId = _activeSectorRuntime.GetInstanceID();
        var appearance = _activeSectorRuntime.GetComponent<SectorAppearance>();
        if (_appearanceRoomId != roomId)
        { _appearanceRoomId = roomId; _appearanceThemeDraft = appearance != null ? appearance.Theme : null; }
        EditorGUILayout.LabelField("바닥 / 벽 외형", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("전투 배치·문·포털·충돌·NavMesh는 유지합니다.\n외형 프리팹의 메시만 사용하며, 원래 물리 바닥 위에 격자 없는 표면을 맞춥니다.\nPrefab Mode Auto Save가 켜져 있으면 ‘적용’도 즉시 저장됩니다. Ctrl+Z로 되돌릴 수 있어요.", MessageType.Info);
        _appearanceThemeDraft = (SectorThemeSO)EditorGUILayout.ObjectField("방 테마", _appearanceThemeDraft, typeof(SectorThemeSO), false);
        Observe("AppearanceTheme");
        if (GUILayout.Button("무격자 연구소 바닥 선택 · 벽은 유지"))
            _appearanceThemeDraft = AssetDatabase.LoadAssetAtPath<SectorThemeSO>(NoGridThemePath);
        Observe("ChooseNoGridFloor");
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(_appearanceThemeDraft == null))
                if (GUILayout.Button("테마 복사…")) CopyAppearanceTheme();
            Observe("CopyAppearanceTheme");
            if (GUILayout.Button("새 테마…")) CreateAppearanceTheme();
        }
        _showThemeFields = EditorGUILayout.Foldout(_showThemeFields, "테마 설정 · 바닥 / 벽 프리팹·머티리얼", true);
        if (_showThemeFields && _appearanceThemeDraft != null)
        {
            EditorGUILayout.HelpBox("이 SO를 사용하는 방들이 설정을 공유합니다. 방 하나만 다르게 만들려면 먼저 ‘테마 복사’를 사용하세요.\n머티리얼에서 색·텍스처를 편집합니다. 문은 제외됩니다. 벽 프리팹은 기존 패널 크기에 맞춘 외형이며 충돌 형태는 바꾸지 않습니다.", MessageType.None);
            var data = new SerializedObject(_appearanceThemeDraft);
            data.Update();
            EditorGUILayout.PropertyField(data.FindProperty("_floorVisual"), new GUIContent("바닥 외형 프리팹")); Observe("FloorVisualField");
            EditorGUILayout.PropertyField(data.FindProperty("_floorMaterial"), new GUIContent("바닥 머티리얼")); Observe("FloorMaterialField");
            EditorGUILayout.PropertyField(data.FindProperty("_wallVisual"), new GUIContent("벽 패널 외형 프리팹")); Observe("WallVisualField");
            EditorGUILayout.PropertyField(data.FindProperty("_wallMaterial"), new GUIContent("벽 머티리얼")); Observe("WallMaterialField");
            data.ApplyModifiedProperties();
            if (GUILayout.Button("테마 SO 저장")) AssetDatabase.SaveAssetIfDirty(_appearanceThemeDraft);
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(_appearanceThemeDraft == null))
                if (GUILayout.Button("이 방에 적용", GUILayout.Height(30))) ApplyAppearanceTheme();
            Observe("ApplyAppearanceTheme");
            using (new EditorGUI.DisabledScope(appearance == null))
                if (GUILayout.Button("등록 당시 외형 복원", GUILayout.Height(30))) RestoreAppearanceTheme();
            Observe("RestoreAppearanceTheme");
        }
        if (appearance != null)
        {
            int floors = appearance.Surfaces.Count(s => s.renderer != null && s.kind == SectorAppearance.SurfaceKind.Floor);
            int walls = appearance.Surfaces.Count(s => s.renderer != null && s.kind == SectorAppearance.SurfaceKind.Wall);
            EditorGUILayout.LabelField($"등록된 바닥 {floors}개 / 벽 {walls}개 · 문/포털 제외", EditorStyles.wordWrappedMiniLabel);
            if (GUILayout.Button("외형 대상 확인 · Inspector")) Selection.activeObject = appearance;
        }
        EditorGUILayout.HelpBox("색·텍스처·외형만 바꾸면 NavMesh를 다시 구울 필요가 없습니다. 실제 벽·엄폐물·콜라이더를 바꿨다면 다시 검사해야 해요. 현재 스테이지 생성기는 원래 콜라이더로 NavMesh를 다시 만듭니다.", MessageType.None);
        DrawAppearanceStageDefaults();
    }

    private void EditAppearance(Action<SectorAppearance> action, string label)
    {
        SyncRoomStage();
        if (!IsEditingRoom) return;
        int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName(label);
        var appearance = _activeSectorRuntime.GetComponent<SectorAppearance>();
        if (appearance == null) appearance = Undo.AddComponent<SectorAppearance>(_activeSectorRuntime.gameObject);
        Undo.RecordObject(appearance, label);
        appearance.RegisterExistingSurfaces();
        if (appearance.Surfaces.Count == 0)
        { ShowNotification(new GUIContent("SectorVisual 아래의 Floor/Sector_Board 또는 Walls/Barriers&Portal을 찾을 수 없습니다.")); return; }
        foreach (var surface in appearance.Surfaces)
            if (surface.renderer != null) Undo.RecordObject(surface.renderer, label);
        action(appearance);
        foreach (var surface in appearance.Surfaces)
            if (surface.renderer != null) PrefabUtility.RecordPrefabInstancePropertyModifications(surface.renderer);
        PrefabUtility.RecordPrefabInstancePropertyModifications(appearance);
        EditorUtility.SetDirty(appearance);
        EditorSceneManager.MarkSceneDirty(_activeSectorRuntime.gameObject.scene);
        Undo.CollapseUndoOperations(group);
        SceneView.RepaintAll(); Repaint();
    }

    private void ApplyAppearanceTheme()
    {
        if (_appearanceThemeDraft == null) return;
        if (!SectorThemeSO.IsVisualValid(_appearanceThemeDraft.FloorVisual) || !SectorThemeSO.IsVisualValid(_appearanceThemeDraft.WallVisual))
        { ShowNotification(new GUIContent("외형 프리팹은 루트 MeshFilter + MeshRenderer를 가진 단일 메시여야 합니다.")); return; }
        EditAppearance(a =>
        {
            a.SetTheme(_appearanceThemeDraft);
            a.Apply(null, Undo.DestroyObjectImmediate, go => Undo.RegisterCreatedObjectUndo(go, "방 외형 생성"));
        }, "Sector Maker 테마 적용");
    }

    private void RestoreAppearanceTheme()
    {
        EditAppearance(a => { a.Restore(Undo.DestroyObjectImmediate); a.SetTheme(null); }, "Sector Maker 외형 복원");
        _appearanceThemeDraft = null;
    }

    private void CopyAppearanceTheme()
    {
        string path = EditorUtility.SaveFilePanelInProject("독립 테마 복사", _appearanceThemeDraft.name + "_Copy", "asset", "기존 테마는 변경하지 않습니다.");
        if (string.IsNullOrEmpty(path)) return;
        AssetAction(() => { SectorMakerAssets.RequireNewPath(path, ".asset");
            _appearanceThemeDraft = SectorThemeAuthoring.Copy(_appearanceThemeDraft, path); _showThemeFields = true; });
    }

    private void CreateAppearanceTheme()
    {
        string path = EditorUtility.SaveFilePanelInProject("새 테마", "SectorTheme_New", "asset", "비어 있는 항목은 스테이지 기본 테마 또는 원래 외형을 유지합니다.");
        if (string.IsNullOrEmpty(path)) return;
        AssetAction(() => { SectorMakerAssets.RequireNewPath(path, ".asset");
            var theme = CreateInstance<SectorThemeSO>(); AssetDatabase.CreateAsset(theme, path);
            _appearanceThemeDraft = theme; _showThemeFields = true; });
    }

    private void DrawAppearanceStageDefaults()
    {
        if (CurrentRoom == null || CurrentRoom.stages.Count == 0) return;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("연결된 스테이지 기본 테마", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("스테이지 전체에 적용됩니다. 방 테마의 비어 있지 않은 항목이 우선합니다.", EditorStyles.wordWrappedMiniLabel);
        foreach (var stage in CurrentRoom.stages)
        {
            var data = new SerializedObject(stage); data.Update();
            EditorGUILayout.PropertyField(data.FindProperty("_sectorTheme"), new GUIContent(stage.name));
            data.ApplyModifiedProperties();
            if (GUILayout.Button(stage.name + " 설정 저장")) AssetDatabase.SaveAssetIfDirty(stage);
        }
    }
}

/// <summary>Copy editable theme/material data; shared source meshes remain read-only.</summary>
internal static class SectorThemeAuthoring
{
    internal static SectorThemeSO Copy(SectorThemeSO source, string path)
    {
        SectorMakerAssets.RequireNewPath(path, ".asset");
        if (source == null) throw new ArgumentNullException(nameof(source));
        var created = new System.Collections.Generic.List<string>();
        var copy = UnityEngine.Object.Instantiate(source);
        copy.name = System.IO.Path.GetFileNameWithoutExtension(path);
        try
        {
            var data = new SerializedObject(copy);
            foreach (string field in new[] { "_floorMaterial", "_wallMaterial" })
            {
                var property = data.FindProperty(field);
                var material = property.objectReferenceValue as Material;
                if (material == null) continue;
                string materialPath = AssetDatabase.GenerateUniqueAssetPath(path.Substring(0, path.Length - 6) + field + ".mat");
                SectorMakerAssets.RequireNewPath(materialPath, ".mat");
                var independent = new Material(material) { name = System.IO.Path.GetFileNameWithoutExtension(materialPath) };
                AssetDatabase.CreateAsset(independent, materialPath); created.Add(materialPath);
                property.objectReferenceValue = independent;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(copy, path); created.Add(path);
            return copy;
        }
        catch
        {
            foreach (string asset in created) AssetDatabase.DeleteAsset(asset);
            if (copy != null && !EditorUtility.IsPersistent(copy)) UnityEngine.Object.DestroyImmediate(copy);
            throw;
        }
    }
}
