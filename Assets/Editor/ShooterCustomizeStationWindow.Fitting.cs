using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>기준점과 장식은 외형에 저장하고, 입혀보기 선택은 미리보기에만 유지한다.</summary>
public sealed partial class ShooterCustomizeStationWindow
{
    private void DrawAccessoryAlignment(int index)
    {
        GUILayout.Label("빠르게 위치 맞추기 — 현재 악세서리만 이동", EditorStyles.miniBoldLabel);
        GUILayout.Label("캡슐을 감싸는 띠·마스크 → 몸통 중앙 / 눈·입 표정 → 얼굴 앞면. 기준점(Anchor)과 원본 프리팹은 바뀌지 않습니다.", EditorStyles.wordWrappedMiniLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(new GUIContent("몸통 중앙에 맞추기", "좌우·앞뒤 위치를 몸통 중심축에 맞춥니다. 높이·회전·크기는 유지합니다. 캡슐을 둘러싸는 마스크·띠 등에 사용하세요."))) FitSelectedAccessory(index, false);
            Observe("accessory:fit-axis");
            using (new EditorGUI.DisabledScope(Part != ShooterVisualSocketId.Eyes && Part != ShooterVisualSocketId.Mouth && Part != ShooterVisualSocketId.Face))
                if (GUILayout.Button(new GUIContent("얼굴 앞면에 맞추기", "메시 경계 기준의 근사 배치. 눈·입 표정용이며 높이·회전·크기는 유지합니다. 필요하면 이동값으로 미세 조정하세요."))) FitSelectedAccessory(index, true);
            Observe("accessory:fit-front");
        }
        GUILayout.Label("맞춘 뒤 우클릭으로 확인하고 미세 조정하세요. 다음에도 이 배치를 쓰려면 '악세서리 SO에 저장'. 현재 캐릭터에 반영하려면 위쪽 '저장·스폰에 적용'. Ctrl+Z로 되돌릴 수 있습니다.", EditorStyles.wordWrappedMiniLabel);
    }

    private void FitSelectedAccessory(int index, bool front)
    {
        if (index < 0 || index >= _draft.attachments.Count) return;
        if (!ShooterCustomizeAccessoryPlacement.TryFit(_preview, _draft.attachments[index], front, out var pose))
        { _error = "몸통 또는 장식의 메시 경계를 찾지 못했습니다. 위치 입력으로 조정해 주세요."; return; }
        Edit("Align Accessory Pivot", () => { var entry = _draft.attachments[index]; entry.localTransform = pose; _draft.attachments[index] = entry; });
    }

    [SerializeField] private bool _showAnchorEditor = true, _showAllAttachments = true;
    [SerializeField] private bool _showBodyShape;

    private void ChangePreview(Action change)
    {
        Undo.RecordObject(_draft, "Change fitting preview");
        change(); _showEquipment = true; _applyNeeded = true; Repaint();
        // Merely trying on an item must not mark the look or the Item SO dirty.
    }

    private void DrawAnchorEditor()
    {
        _showAnchorEditor = EditorGUILayout.Foldout(_showAnchorEditor, PartLabel(Part) + " 기준점 (Anchor) — 위치 / 회전 / 크기", true);
        Observe("anchor:fold");
        if (!_showAnchorEditor) return;
        if (_preview.Rig.ResolveSocket(Part) == null)
        { EditorGUILayout.HelpBox("이 프리팹에는 " + Part + " 기준점이 없습니다.", MessageType.Warning); return; }
        GUILayout.Label(Part == ShooterVisualSocketId.Body ?
            "Body 기준점은 몸통 장식용입니다. 방탄복 자체는 위쪽 장비 카드에서 조절합니다. 실제 방탄복 기준점은 Armor입니다." :
            Part == ShooterVisualSocketId.Mouth ?
            "Mouth 기준점은 입 장식용입니다. 가스마스크 자체는 위쪽 장비 카드에서 조절합니다. 실제 장착점은 Item SO의 부위와 프리팹 연결을 따릅니다." :
            Part == ShooterVisualSocketId.Back ?
            "등 장식과 착용 백팩이 함께 움직입니다. 이 캐릭터의 등 Anchor는 위쪽 '저장·스폰에 적용'으로 저장합니다. 백팩 자체 보정은 장비 테스트의 '백팩 Item SO에 저장'을 사용하세요." :
            "이 부위의 장식이 함께 움직입니다. Armor / RightHand는 인게임 장비에도 영향을 줍니다.", EditorStyles.wordWrappedMiniLabel);
        using (new EditorGUI.DisabledScope(!_preview.CanMoveSocket(Part)))
        {
            var pose = _draft.SocketPose(Part);
            EditorGUI.BeginChangeCheck();
            Vector3 pos = EditorGUILayout.Vector3Field("위치", pose.localPosition); Observe("anchor:position");
            Vector3 rotation = EditorGUILayout.Vector3Field("회전", pose.localEulerAngles);
            Vector3 scale = EditorGUILayout.Vector3Field("크기", pose.localScale == Vector3.zero ? Vector3.one : pose.localScale);
            if (EditorGUI.EndChangeCheck()) Edit("Move Visual Anchor", () => _draft.SetSocketPose(Part, new ShooterVisualLocalTransform(pos, rotation, scale)));
            if (GUILayout.Button("기준점 원래 위치로"))
                Edit("Reset Visual Anchor", () => _draft.SetSocketPose(Part, _preview.DefaultSocketPose(Part)));
            Observe("anchor:reset");
        }
        if (!_preview.CanMoveSocket(Part)) EditorGUILayout.HelpBox("게임플레이 충돌체가 포함된 기준점은 이동할 수 없습니다.", MessageType.Warning);
        GUILayout.Space(6);
    }

    private void DrawEquipmentHideRules(int index)
    {
        GUILayout.Label("장비를 착용하면 이 악세서리 숨기기", EditorStyles.boldLabel);
        GUILayout.Label("체크한 장비를 착용하면 숨기고, 벗으면 다시 표시합니다. 악세서리를 삭제하는 기능이 아닙니다.", EditorStyles.wordWrappedMiniLabel);
        var kinds = _draft.attachments[index].hideWhenEquipped;
        EditorGUI.BeginChangeCheck();
        using (new EditorGUILayout.HorizontalScope())
        {
            kinds = KindToggle("무기", kinds, ShooterVisualEquipmentKind.Weapon);
            kinds = KindToggle("방탄복", kinds, ShooterVisualEquipmentKind.Armor);
            kinds = KindToggle("가스마스크", kinds, ShooterVisualEquipmentKind.GasMask);
            kinds = KindToggle("백팩", kinds, ShooterVisualEquipmentKind.Backpack);
        }
        if (EditorGUI.EndChangeCheck()) Edit("Set equipment hiding rule", () =>
        { var row = _draft.attachments[index]; row.hideWhenEquipped = kinds; _draft.attachments[index] = row; });
        Observe("accessory:hide-rules");
        if (_preview.IsAttachmentHidden(_draft.attachments[index]))
            EditorGUILayout.HelpBox("현재 숨김 상태입니다. 장비 테스트에서 '숨김 규칙 무시'를 켜면 겹치는 모습을 비교할 수 있어요.", MessageType.Info);
    }

    private static ShooterVisualEquipmentKind KindToggle(string label, ShooterVisualEquipmentKind value, ShooterVisualEquipmentKind flag)
        => GUILayout.Toggle((value & flag) != 0, label, EditorStyles.miniButton) ? value | flag : value & ~flag;

    private void DrawEquipment()
    {
        EditorGUILayout.HelpBox("Item SO를 넣으면 무기·방탄복·가스마스크·백팩을 함께 입혀 봅니다. 선택만으로 실제 인벤토리·스폰 장비·아이템 수치는 바뀌지 않아요.", MessageType.Info);
        GUILayout.Label("1. 함께 착용할 아이템", EditorStyles.boldLabel);
        var weapon = (WeaponSO)EditorGUILayout.ObjectField("무기 Item SO", _draft.previewWeapon, typeof(WeaponSO), false); Observe("gear:weapon");
        if (weapon != _draft.previewWeapon && ConfirmFittingSwitch(ShooterVisualEquipmentKind.Weapon)) ChangePreview(() =>
        { _draft.LoadWeapon(weapon); _draft.showPreviewWeapon = true; _selectedFittingGear = ShooterVisualEquipmentKind.Weapon; SyncDirty(); });
        var armor = (ArmorItemSO)EditorGUILayout.ObjectField("방탄복 Item SO", _draft.previewArmor, typeof(ArmorItemSO), false); Observe("gear:armor");
        if (armor != _draft.previewArmor && ConfirmFittingSwitch(ShooterVisualEquipmentKind.Armor)) ChangePreview(() =>
        { _draft.LoadArmor(armor); _draft.showPreviewArmor = true; _selectedFittingGear = ShooterVisualEquipmentKind.Armor; SyncDirty(); });
        var mask = (GasMaskItemSO)EditorGUILayout.ObjectField("가스마스크 SO", _draft.equipment, typeof(GasMaskItemSO), false); Observe("gear:mask");
        if (mask != _draft.equipment && ConfirmEquipmentSwitch()) ChangePreview(() => { _draft.LoadEquipment(mask); _draft.showPreviewMask = true; _selectedFittingGear = ShooterVisualEquipmentKind.GasMask; SyncDirty(); });
        var backpack = (BackpackItemSO)EditorGUILayout.ObjectField("백팩 Item SO", _draft.previewBackpack, typeof(BackpackItemSO), false); Observe("gear:backpack");
        if (backpack != _draft.previewBackpack && ConfirmBackpackSwitch()) ChangePreview(() => { _draft.LoadBackpack(backpack); _draft.showPreviewBackpack = true; _selectedFittingGear = ShooterVisualEquipmentKind.Backpack; SyncDirty(); });
        for (int row = 0; row < 2; row++)
            using (new EditorGUILayout.HorizontalScope())
                for (int column = 0; column < 2; column++)
                {
                    var kind = FittingKinds[row * 2 + column];
                    using (new EditorGUI.DisabledScope(FittingItem(kind) == null))
                    {
                        if (GUILayout.Button(FittingLabel(kind) + " 부위에서 편집", GUILayout.Height(26))) Queue(() => OpenFittingPart(kind));
                        Observe("fitting:open-part:" + kind);
                    }
                }
        if (_draft.previewBackpack != null)
        {
            if (GUILayout.Button("등 Anchor 조정 — 이 캐릭터의 등 기준점")) Queue(OpenBackAnchor);
            Observe("gear:backpack-anchor");
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUI.BeginChangeCheck();
            bool w = GUILayout.Toggle(_draft.showPreviewWeapon, "무기 보기", EditorStyles.miniButton);
            bool a = GUILayout.Toggle(_draft.showPreviewArmor, "방탄복 보기", EditorStyles.miniButton);
            bool m = GUILayout.Toggle(_draft.showPreviewMask, "마스크 보기", EditorStyles.miniButton);
            bool b = GUILayout.Toggle(_draft.showPreviewBackpack, "백팩 보기", EditorStyles.miniButton);
            if (EditorGUI.EndChangeCheck()) ChangePreview(() =>
            { _draft.showPreviewWeapon = w; _draft.showPreviewArmor = a; _draft.showPreviewMask = m; _draft.showPreviewBackpack = b; });
        }
        if (GUILayout.Button("테스트 장비 모두 벗기") && FittingKinds.All(ConfirmFittingSwitch))
            ChangePreview(() => { foreach (var kind in FittingKinds) ReloadFitting(kind, true); });
        Observe("gear:clear");
        GUILayout.Space(10);
        GUILayout.Label("2. 겹침 확인 / 장식 숨김", EditorStyles.boldLabel);
        bool ignore = EditorGUILayout.ToggleLeft("숨김 규칙 무시 — 겹치는 원래 모습 비교", _draft.ignoreEquipmentHiding);
        if (ignore != _draft.ignoreEquipmentHiding) ChangePreview(() => _draft.ignoreEquipmentHiding = ignore);
        Observe("gear:ignore");
        if (ignore) EditorGUILayout.HelpBox("비교 모드: 게임에 저장되지 않습니다. 최종 확인은 체크를 끄고 하세요.", MessageType.Warning);
        GUILayout.Label("위 부위 버튼 → 악세서리 선택 → '장비를 착용하면 이 악세서리 숨기기'에서 장비를 체크하거나 위치를 조정하세요. 장비를 켠 채 다른 부위로 이동할 수 있어요.", EditorStyles.wordWrappedLabel);
        foreach (string warning in _preview.EquipmentWarnings) EditorGUILayout.HelpBox(warning, MessageType.Warning);
        GUILayout.Space(8);
        GUILayout.Label("3. 선택한 장비 배치 조절", EditorStyles.boldLabel);
        DrawFittingCards(false);
        GUILayout.Label("무기는 Weapon View, 방탄복·가스마스크·백팩은 Item SO의 착용 모델을 사용합니다. 정지 자세의 외형 검사이며 공격 애니메이션·물리 충돌 검사는 아닙니다.", EditorStyles.wordWrappedMiniLabel);
    }

    [SerializeField] private bool _showBackpackFitting = true;
    private void DrawBackpackFitting()
    {
        if (_draft.previewBackpack == null) return;
        GUILayout.Space(8);
        _showBackpackFitting = EditorGUILayout.Foldout(_showBackpackFitting, "백팩 위치·크기 조정 — Item SO에 저장", true);
        if (!_showBackpackFitting) return;
        GUILayout.Label("등 기준점(Back)에 장착합니다. 조정 후 아래 버튼으로 저장하면 이 백팩을 착용할 때 같은 배치를 사용합니다. 용량·이동 속도는 바뀌지 않습니다.", EditorStyles.wordWrappedMiniLabel);
        var prefab = (GameObject)EditorGUILayout.ObjectField("착용 모델", _draft.backpackPrefab, typeof(GameObject), false);
        if (prefab != _draft.backpackPrefab) EditBackpack(() => _draft.backpackPrefab = prefab);
        using (new EditorGUI.DisabledScope(_preview.BackpackVisual == null))
            if (GUILayout.Button("몸통 뒤에 맞추기 — 백팩 위치만 이동")) Queue(FitBackpackBehindBody);
        Observe("gear:backpack-fit");
        GUILayout.Label("메시 중심을 기준으로 몸통 뒤에 맞춥니다. 회전·크기는 유지하며, 자동 맞춤 후 필요하면 아래 위치값으로 미세 조정하세요.", EditorStyles.wordWrappedMiniLabel);
        DrawPose("백팩 위치·크기·회전", _draft.backpackPose, pose => EditBackpack(() => _draft.backpackPose = pose), false);
        if (GUILayout.Button("백팩 Item SO에 저장" + (_draft.backpackDirty ? " *" : "")))
            Queue(() => { _draft.SaveBackpack(); SyncDirty(); ShowNotification(new GUIContent("백팩 Item SO에 착용 모델·배치를 저장했습니다.")); });
        Observe("gear:backpack-save");
        if (GUILayout.Button("백팩 SO 저장값 불러오기") && ConfirmBackpackSwitch())
            ChangePreview(() => { _draft.LoadBackpack(_draft.previewBackpack); SyncDirty(); });
    }

    private void EditBackpack(Action change)
    {
        Undo.RecordObject(_draft, "Adjust Backpack Equipment Visual"); change();
        _draft.backpackDirty = true; _applyNeeded = true; SyncDirty(); Repaint();
    }

    private void OpenBackAnchor()
    {
        _part = Array.IndexOf(Parts, ShooterVisualSocketId.Back); _equipmentMode = false;
        _attachment = -1; _editScroll = Vector2.zero; _showAnchorEditor = _showAnchors = true;
        SetPreviewView(new Vector2(0, 8));
        // Selection/view only: keep the equipment preview and both dirty flags intact.
    }

    private void FitBackpackBehindBody()
    {
        if (!ShooterCustomizeAccessoryPlacement.TryFitBackpack(_preview, _draft.backpackPose, out var pose))
        { _error = "몸통·백팩 메시 또는 등 장착점을 찾지 못했습니다. 모델과 등 Anchor 연결을 확인하세요."; return; }
        EditBackpack(() => _draft.backpackPose = pose);
    }

    private bool ConfirmBackpackSwitch()
    {
        return ConfirmFittingSwitch(ShooterVisualEquipmentKind.Backpack);
    }
}
