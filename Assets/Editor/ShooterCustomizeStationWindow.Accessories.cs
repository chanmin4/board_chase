using UnityEditor;
using UnityEngine;

/// <summary>장식 기본 SO와 캐릭터별 복사본을 분리한다. 기본값 저장은 명시적인 별도 버튼으로만 수행한다.</summary>
public sealed partial class ShooterCustomizeStationWindow
{
    private Object _addAccessoryAsset;

    private void DrawAccessoryRegistration()
    {
        GUILayout.Label("악세서리 추가 — SO 또는 프리팹", EditorStyles.boldLabel);
        GUILayout.Label("추가하면 SO에 저장된 배치를 가져옵니다. 무기·방탄복·가스마스크·백팩은 '장비 테스트'에서 Item SO로 선택하세요.", EditorStyles.wordWrappedMiniLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            _addAccessoryAsset = EditorGUILayout.ObjectField(_addAccessoryAsset, typeof(Object), false);
            using (new EditorGUI.DisabledScope(!CanAddAccessoryAsset(_addAccessoryAsset)))
                if (GUILayout.Button("+ 추가", GUILayout.Width(56)))
                { var value = _addAccessoryAsset; Queue(() => AddAccessoryAsset(value)); }
        }
        Observe("accessory:add-asset");
        if (GUILayout.Button("현재 폴더의 장식 SO 등록 (WIP·기능 장비 제외)")) Queue(() =>
        { ShooterCustomizeAccessoryCatalog.RegisterCurrentFolder(); ShowNotification(new GUIContent("장식 기본 SO를 등록했습니다. 기존 외형은 그대로입니다.")); });
        Observe("accessory:register-folder");
    }

    private static bool CanAddAccessoryAsset(Object asset) => asset is ShooterVisualAttachmentSO definition && ShooterCustomizeAccessoryCatalog.IsEligible(definition.VisualPrefab) ||
        asset is GameObject prefab && ShooterCustomizeAccessoryCatalog.IsEligible(prefab);

    private void AddAccessoryAsset(Object asset)
    {
        if (!CanAddAccessoryAsset(asset)) return;
        var definition = asset as ShooterVisualAttachmentSO ?? ShooterCustomizeAccessoryCatalog.Register((GameObject)asset);
        var entry = ShooterCustomizeAccessoryCatalog.CopyDefault(definition);
        if (_preview?.Rig.ResolveSocket(entry.socket) == null)
        { _error = "이 캐릭터에는 " + entry.socket + " 기준점이 없습니다."; return; }
        Edit("Add Accessory Definition", () => _draft.attachments.Add(entry));
        _attachment = _draft.attachments.Count - 1;
        int part = System.Array.IndexOf(Parts, entry.socket); if (part >= 0) _part = part;
        _equipmentMode = false; _addAccessoryAsset = null;
    }

    private void DrawAccessoryDefinition(int index)
    {
        var row = _draft.attachments[index];
        var definition = row.definition != null && row.definition.VisualPrefab == row.visualPrefab ? row.definition : ShooterCustomizeAccessoryCatalog.Find(row.visualPrefab);
        GUILayout.Label("현재 캐릭터의 악세서리 조정", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(true)) EditorGUILayout.ObjectField("악세서리 SO", definition, typeof(ShooterVisualAttachmentSO), false);
            using (new EditorGUI.DisabledScope(definition == null))
                if (GUILayout.Button(new GUIContent("SO 찾기", "Project 창에서 이 악세서리 SO를 선택합니다."), GUILayout.Width(60))) { Selection.activeObject = definition; EditorGUIUtility.PingObject(definition); }
        }
        Observe("accessory:definition");
        int currentPart = System.Array.IndexOf(Parts, row.socket);
        if (currentPart >= 0)
        {
            int selectedPart = EditorGUILayout.Popup("장착 부위", currentPart, System.Array.ConvertAll(Parts, p => p.ToString()));
            if (selectedPart != currentPart)
            {
                var socket = Parts[selectedPart];
                if (_preview.Rig.ResolveSocket(socket) == null) _error = "이 캐릭터에는 " + socket + " 기준점이 없습니다.";
                else { Edit("Change Cosmetic Socket", () => { var copy = _draft.attachments[index]; copy.socket = socket; _draft.attachments[index] = copy; }); _part = selectedPart; }
            }
        }
        GUILayout.Label("아래 조정은 현재 캐릭터에서만 바뀝니다. 다음에 추가할 때도 쓰려면 '악세서리 SO에 저장'을 누르세요.", EditorStyles.wordWrappedMiniLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(!ShooterCustomizeAccessoryCatalog.IsEligible(row.visualPrefab)))
                if (GUILayout.Button(new GUIContent("악세서리 SO에 저장", "현재 장착 부위·위치·회전·크기·숨김 조건을 이 악세서리 SO에 저장합니다. 다음에 같은 SO나 프리팹을 추가하면 저장값을 사용합니다."))) Queue(() => SaveAccessoryDefault(index));
            Observe("accessory:save-default");
            using (new EditorGUI.DisabledScope(definition == null))
                if (GUILayout.Button(new GUIContent("SO 저장값 불러오기", "SO에 저장된 배치를 현재 선택한 악세서리에 적용합니다. 현재 조정값을 대체하며 Ctrl+Z로 되돌릴 수 있습니다."))) Queue(() => RestoreAccessoryDefault(index));
            Observe("accessory:restore-default");
        }
        GUILayout.Label("SO 저장은 다음 추가용입니다. 이미 저장된 다른 캐릭터는 그대로입니다.\n현재 캐릭터 외형 저장: 위쪽 '저장·스폰에 적용'.", EditorStyles.wordWrappedMiniLabel);
    }

    private void SaveAccessoryDefault(int index)
    {
        if (index < 0 || index >= _draft.attachments.Count) return;
        var row = _draft.attachments[index];
        if (!ShooterCustomizeAccessoryCatalog.IsEligible(row.visualPrefab)) { _error = "등록 가능한 순수 장식 프리팹이 아닙니다."; return; }
        if (!EditorUtility.DisplayDialog("악세서리 SO에 저장", "저장 대상: " + row.visualPrefab.name + " 악세서리 SO\n" +
            "저장 내용: 장착 부위·위치·회전·크기·장비 착용 시 숨김 조건\n\n" +
            "다음에 같은 SO나 프리팹을 추가하면 이 배치를 사용합니다. 이미 저장된 다른 캐릭터는 바뀌지 않습니다.\n\n" +
            "현재 캐릭터 외형은 위쪽 '저장·스폰에 적용'으로 별도 저장하세요.", "SO에 저장", "취소")) return;
        var definition = ShooterCustomizeAccessoryCatalog.Register(row.visualPrefab);
        ShooterCustomizeAccessoryCatalog.SaveDefault(definition, row);
        if (row.definition != definition) Edit("Link Accessory Definition", () =>
        { var copy = _draft.attachments[index]; copy.definition = definition; _draft.attachments[index] = copy; });
        ShowNotification(new GUIContent("악세서리 SO 저장 완료 — 다음 추가부터 이 배치를 사용합니다."));
    }

    private void RestoreAccessoryDefault(int index)
    {
        if (index < 0 || index >= _draft.attachments.Count) return;
        var row = _draft.attachments[index];
        var definition = row.definition != null ? row.definition : ShooterCustomizeAccessoryCatalog.Find(row.visualPrefab);
        if (definition == null) return;
        var copy = ShooterCustomizeAccessoryCatalog.CopyDefault(definition);
        if (_preview?.Rig.ResolveSocket(copy.socket) == null) { _error = "이 캐릭터에는 SO에 저장된 장착 부위의 기준점(Anchor)이 없습니다."; return; }
        Edit("Restore Accessory Default", () => _draft.attachments[index] = copy);
        int part = System.Array.IndexOf(Parts, copy.socket); if (part >= 0) _part = part;
    }
}
