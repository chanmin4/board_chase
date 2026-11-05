using UnityEditor;
using UnityEngine;

public sealed partial class StageMakerWindow
{
    [SerializeField] private bool _compositionOptionsOpen, _specialOptionsOpen;
    private static readonly (string id, string path)[] SpecialCatalog =
    {
        ("office", "Office/SectorPreset_Office_01.asset"),
        ("storage", "Warehouse/SectorPreset_Warehouse_01.asset"),
        ("security", "Armory/SectorPreset_Armory_01.asset"),
        ("medical", "Medical/SectorPreset_Medical_01.asset"),
        ("vault", "Treasure/SectorPreset_Treasure_01.asset"),
        ("server", "Server/SectorPreset_Server_01.asset")
    };

    private void DrawCompositionControls()
    {
        bool ratio = Prop("_specialSectors.useRatio").boolValue;
        if (ratio)
        {
            Heading("기본 등장 비율");
            var themeEnabled = Prop("_roomComposition.enabled");
            var themePercent = Prop("_roomComposition.themePercent");
            var enabled = Prop("_specialSectors.enabled");
            var percent = Prop("_roomComposition.specialPercent");
            EditorGUI.BeginChangeCheck();
            themeEnabled.boolValue = EditorGUILayout.Toggle("스테이지 전용맵 사용", themeEnabled.boolValue);
            using (new EditorGUI.DisabledScope(!themeEnabled.boolValue))
                themePercent.intValue = EditorGUILayout.IntSlider("스테이지 전용맵 비율 (%)", themePercent.intValue, 0, 100);
            if (EditorGUI.EndChangeCheck()) UpdateCommonShare();
            EditorGUI.BeginChangeCheck();
            enabled.boolValue = EditorGUILayout.Toggle("특별맵 사용", enabled.boolValue);
            if (EditorGUI.EndChangeCheck())
            {
                if (!enabled.boolValue) percent.intValue = 0;
                else if (percent.intValue == 0) percent.intValue = 30;
                UpdateCommonShare();
            }
            using (new EditorGUI.DisabledScope(!enabled.boolValue))
            {
                EditorGUI.BeginChangeCheck();
                percent.intValue = EditorGUILayout.IntSlider("특별맵 비율 (%)", percent.intValue, 0, 100);
                if (EditorGUI.EndChangeCheck()) UpdateCommonShare();
            }
            int special = enabled.boolValue ? percent.intValue : 0;
            int theme = themeEnabled.boolValue ? themePercent.intValue : 0;
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.IntField("공용맵 비율 (%) · 나머지", 100 - special - theme);
            GUILayout.Label($"공용 {100 - special - theme}%  /  스테이지 전용 {theme}%  /  특별 {special}%", EditorStyles.boldLabel);
            if (special + theme > 100)
                EditorGUILayout.HelpBox($"전용 {theme}% + 특별 {special}% = {theme + special}%입니다. 합계를 100% 이하로 줄여주세요.", MessageType.Error);
            GUILayout.Label("발전실·해치·보스·탈출구를 제외한 배정 대상 방 기준입니다. 상인은 비율 추첨 후 공용방에 별도 추가합니다. 종류/자리가 부족하면 가능한 만큼만 배치하고 일반방으로 채웁니다.", EditorStyles.wordWrappedMiniLabel);
            if (_preview != null && !_previewStale && _preview.usesSpecialRatio)
                GUILayout.Label($"최근 생성: 목표 {_preview.requestedSpecialRooms}개 / 실제 {_preview.actualSpecialRooms}개 / 일반 대체 {Mathf.Max(0, _preview.requestedSpecialRooms - _preview.actualSpecialRooms)}개", EditorStyles.wordWrappedMiniLabel);
        }
        else EditorGUILayout.HelpBox("고급 개수 모드를 사용 중입니다. 아래 고급 생성 제한에서 비율 모드로 전환할 수 있습니다.", MessageType.Info);

        if (!ratio)
        {
            EditorGUI.BeginChangeCheck();
            P("_roomComposition.enabled", "스테이지 전용맵 사용");
            P("_roomComposition.themePercent", "스테이지 전용맵 비율 (%)");
            if (EditorGUI.EndChangeCheck()) UpdateCommonShare();
        }
        DrawCandidates(Prop("_roomComposition.themePresets"), "스테이지 전용맵 풀", false);
        if (Prop("_roomComposition.enabled").boolValue && Prop("_roomComposition.themePercent").intValue > 0 &&
            Prop("_roomComposition.themePresets").arraySize == 0)
            EditorGUILayout.HelpBox("전용맵 비율이 있지만 등록된 전용맵이 없습니다. 바로 위 [스테이지 전용맵 풀]에 SectorPreset SO를 넣어주세요.", MessageType.Warning);
        _compositionOptionsOpen = EditorGUILayout.Foldout(_compositionOptionsOpen, "공용·기존 일반맵 풀 설정", true);
        if (!_compositionOptionsOpen) return;
        DrawCandidates(Prop("_roomComposition.commonPresets"), "공용맵 풀 (비어 있으면 기존 일반 풀)", false);
        DrawCandidates(Prop("_normalBattle.sectorPresets"), "기존 일반맵 풀", false);
    }

    private void UpdateCommonShare()
    {
        int theme = Prop("_roomComposition.enabled").boolValue ? Prop("_roomComposition.themePercent").intValue : 0;
        Prop("_roomComposition.commonPercent").intValue = 100 - theme - Prop("_roomComposition.specialPercent").intValue;
    }

    private void DrawSpecialControls()
    {
        GUILayout.Label("체크한 종류만 이 스테이지에 등장합니다. 다음 스테이지에서는 체크 종류를 늘리면 됩니다.", EditorStyles.wordWrappedMiniLabel);
        var pools = Prop("_specialSectors.pools");
        bool ratio = Prop("_specialSectors.useRatio").boolValue;
        bool limits = !ratio || Prop("_specialSectors.useAdvancedCountLimits").boolValue;
        int allowed = 0;
        for (int i = 0; i < pools.arraySize; i++)
        {
            var pool = pools.GetArrayElementAtIndex(i);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var disabled = pool.FindPropertyRelative("disabled");
                    disabled.boolValue = !EditorGUILayout.Toggle(!disabled.boolValue, GUILayout.Width(18));
                    if (!disabled.boolValue) allowed++;
                    pool.isExpanded = EditorGUILayout.Foldout(pool.isExpanded,
                        SpecialPoolLabel(pool.FindPropertyRelative("id").stringValue) +
                        $" · 변형 {pool.FindPropertyRelative("variants").arraySize}개", true);
                    GUILayout.Label(new GUIContent("비중", "허용 종류끼리의 상대 비중입니다. 절대 출현 확률은 아닙니다."), GUILayout.Width(30));
                    var weight = pool.FindPropertyRelative("weight");
                    weight.intValue = Mathf.Max(0, EditorGUILayout.IntField(weight.intValue, GUILayout.Width(42)));
                    if (GUILayout.Button("↑", GUILayout.Width(22)) && i > 0) { pools.MoveArrayElement(i, i - 1); break; }
                    if (GUILayout.Button("↓", GUILayout.Width(22)) && i + 1 < pools.arraySize) { pools.MoveArrayElement(i, i + 1); break; }
                    if (GUILayout.Button("×", GUILayout.Width(22))) { pools.DeleteArrayElementAtIndex(i); break; }
                }
                if (!pool.isExpanded) continue;
                DrawCandidates(pool.FindPropertyRelative("variants"), "등장할 맵 프리셋", true);
                string key = pool.propertyPath;
                bool advanced = EditorGUILayout.Foldout(_poolAdvanced.Contains(key), "고급 · 개수 / 배치 조건 / 잠금 / 퀘스트", true);
                if (advanced) _poolAdvanced.Add(key); else _poolAdvanced.Remove(key);
                if (!advanced) continue;
                using (new EditorGUI.DisabledScope(!limits))
                {
                    EditorGUILayout.PropertyField(pool.FindPropertyRelative("minimumCount"), new GUIContent("이 종류 최소 개수"));
                    EditorGUILayout.PropertyField(pool.FindPropertyRelative("maximumCount"), new GUIContent("이 종류 최대 개수"));
                }
                if (!limits) GUILayout.Label("개수 제한은 OFF입니다. 아래 고급 생성 제한에서 켤 수 있습니다.", EditorStyles.wordWrappedMiniLabel);
                foreach (var pair in new[] { ("minimumStartDistance", "최소 시작 거리"), ("maximumStartDistance", "최대 시작 거리 (0: 무제한)"),
                    ("direction", "시작점 기준 방향"), ("lockedCombatArea", "내부 잠금 전투 구역"), ("keyTier", "잠금 열쇠 등급"),
                    ("requiredByQuests", "진행 중이면 1개 보장할 퀘스트"), ("id", "저장 ID · 변경 주의") })
                    EditorGUILayout.PropertyField(pool.FindPropertyRelative(pair.Item1), new GUIContent(pair.Item2), true);
            }
        }
        if (allowed == 0) EditorGUILayout.HelpBox("허용된 종류가 없습니다. 기본 비율 모드에서는 특별맵 없이 일반방으로 채웁니다. 필수 퀘스트/강제 최소 조건은 별도입니다.", MessageType.Info);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("기존 특별맵 등록 · 빠진 종류만")) RegisterSpecialCatalog(pools);
            if (GUILayout.Button("+ 직접 종류 추가", GUILayout.Width(130))) AddSpecialPool(pools, "type_" + System.Guid.NewGuid().ToString("N").Substring(0, 6), null);
        }
        _specialOptionsOpen = EditorGUILayout.Foldout(_specialOptionsOpen, "고급 생성 제한 · 중복 / 최소·최대 / 예전 방식", true);
        if (!_specialOptionsOpen) return;
        P("_specialSectors.useRatio", "비율 기준 사용 (기본)");
        P("_specialSectors.enabled", "특별맵 사용");
        P("_specialSectors.allowRepeatedTypes", "같은 종류 반복 허용");
        P("_specialSectors.avoidRepeatedVariants", "변형 프리팹 우선 중복 회피");
        P("_specialSectors.useAdvancedCountLimits", "비율에도 개수 제한 강제 적용");
        using (new EditorGUI.DisabledScope(!limits))
        { P("_specialSectors.minimumCount", "전체 최소 개수"); P("_specialSectors.maximumCount", "전체 최대 개수"); }
        P("_raidMap.vaultNearCandidateChance", "별도 금고 추가 확률 (0~1)");
        GUILayout.Label("기본은 종류당 1개, 부족분은 일반방입니다. 반복을 허용하면 비율 목표까지 같은 종류를 다시 뽑습니다. 퀘스트 보장과 강제 최소는 비율보다 우선하며, 불가능하면 오류를 표시합니다. 기존 저장 ID는 변경하지 마세요.", EditorStyles.wordWrappedMiniLabel);
    }

    private static SerializedProperty AddSpecialPool(SerializedProperty pools, string id, SectorPresetSO preset)
    {
        pools.InsertArrayElementAtIndex(pools.arraySize);
        var pool = pools.GetArrayElementAtIndex(pools.arraySize - 1);
        pool.FindPropertyRelative("id").stringValue = id;
        pool.FindPropertyRelative("disabled").boolValue = preset != null;
        pool.FindPropertyRelative("weight").intValue = 1;
        pool.FindPropertyRelative("minimumCount").intValue = 0; pool.FindPropertyRelative("maximumCount").intValue = 1;
        pool.FindPropertyRelative("minimumStartDistance").intValue = 0; pool.FindPropertyRelative("maximumStartDistance").intValue = 0;
        pool.FindPropertyRelative("direction").intValue = 0; pool.FindPropertyRelative("lockedCombatArea").boolValue = id == "vault";
        pool.FindPropertyRelative("keyTier").intValue = 0;
        var variants = pool.FindPropertyRelative("variants"); variants.ClearArray();
        if (preset != null)
        {
            AppendCandidate(variants, preset, true);
            variants.GetArrayElementAtIndex(0).FindPropertyRelative("id").stringValue = id + "_01";
        }
        pool.FindPropertyRelative("requiredByQuests").ClearArray(); pool.isExpanded = preset == null;
        return pool;
    }

    private void RegisterSpecialCatalog(SerializedProperty pools)
    {
        int added = 0, missing = 0;
        foreach (var entry in SpecialCatalog)
        {
            bool exists = false;
            for (int i = 0; i < pools.arraySize; i++)
                if (pools.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue == entry.id) { exists = true; break; }
            if (exists) continue;
            var preset = AssetDatabase.LoadAssetAtPath<SectorPresetSO>("Assets/ScriptableObject/GameScene/Stage/Sector/Preset/RaidRooms/" + entry.path);
            if (preset == null) { missing++; continue; }
            AddSpecialPool(pools, entry.id, preset); added++;
        }
        ShowNotification(new GUIContent($"{added}종 추가 · 기존 항목 유지 · 새 종류는 체크 OFF" + (missing > 0 ? $" / 에셋 누락 {missing}종" : "")));
    }
}
