using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>장비 테스트와 부위 탭은 같은 임시 장비 데이터를 편집한다. 장착점을 임의로 변경하지 않는다.</summary>
public sealed partial class ShooterCustomizeStationWindow
{
    private static readonly ShooterVisualEquipmentKind[] FittingKinds = { ShooterVisualEquipmentKind.Weapon,
        ShooterVisualEquipmentKind.Armor, ShooterVisualEquipmentKind.GasMask, ShooterVisualEquipmentKind.Backpack };
    [SerializeField] private ShooterVisualEquipmentKind _selectedFittingGear;
    [SerializeField] private bool _focusFittingAnchor = true;

    // 부위는 편집 바로가기다. 실제 무기/방탄복 소켓을 Body/LeftHand 등에 다시 부모 지정하지 않는다.
    public static bool EquipmentAppearsInPart(ShooterVisualEquipmentKind kind, ShooterVisualSocketId part,
        ShooterVisualSocketId maskSocket = ShooterVisualSocketId.Face) => kind switch
    {
        ShooterVisualEquipmentKind.Armor => part == ShooterVisualSocketId.Body || part == ShooterVisualSocketId.Armor,
        ShooterVisualEquipmentKind.GasMask => part == ShooterVisualSocketId.Mouth || part == ShooterVisualSocketId.Face || part == maskSocket,
        ShooterVisualEquipmentKind.Backpack => part == ShooterVisualSocketId.Back,
        ShooterVisualEquipmentKind.Weapon => part == ShooterVisualSocketId.LeftHand || part == ShooterVisualSocketId.RightHand,
        _ => false
    };

    private bool HasEquipmentAtPart(ShooterVisualSocketId part) => FittingKinds.Any(k => FittingItem(k) != null &&
        EquipmentAppearsInPart(k, part, _draft.equipmentSocket));
    private ItemSO FittingItem(ShooterVisualEquipmentKind kind) => kind switch
    {
        ShooterVisualEquipmentKind.Weapon => _draft.previewWeapon,
        ShooterVisualEquipmentKind.Armor => _draft.previewArmor,
        ShooterVisualEquipmentKind.GasMask => _draft.equipment,
        ShooterVisualEquipmentKind.Backpack => _draft.previewBackpack,
        _ => null
    };
    private static string FittingLabel(ShooterVisualEquipmentKind kind) => kind switch
    {
        ShooterVisualEquipmentKind.Weapon => "무기",
        ShooterVisualEquipmentKind.Armor => "방탄복",
        ShooterVisualEquipmentKind.GasMask => "가스마스크",
        ShooterVisualEquipmentKind.Backpack => "백팩",
        _ => "장비"
    };
    private bool FittingDirty(ShooterVisualEquipmentKind kind) => kind switch
    {
        ShooterVisualEquipmentKind.Weapon => _draft.weaponDirty,
        ShooterVisualEquipmentKind.Armor => _draft.armorDirty,
        ShooterVisualEquipmentKind.GasMask => _draft.equipmentDirty,
        ShooterVisualEquipmentKind.Backpack => _draft.backpackDirty,
        _ => false
    };
    private bool FittingVisible(ShooterVisualEquipmentKind kind) => kind switch
    {
        ShooterVisualEquipmentKind.Weapon => _draft.showPreviewWeapon,
        ShooterVisualEquipmentKind.Armor => _draft.showPreviewArmor,
        ShooterVisualEquipmentKind.GasMask => _draft.showPreviewMask,
        ShooterVisualEquipmentKind.Backpack => _draft.showPreviewBackpack,
        _ => false
    };
    private void SetFittingVisible(ShooterVisualEquipmentKind kind, bool visible)
    {
        switch (kind)
        {
            case ShooterVisualEquipmentKind.Weapon: _draft.showPreviewWeapon = visible; break;
            case ShooterVisualEquipmentKind.Armor: _draft.showPreviewArmor = visible; break;
            case ShooterVisualEquipmentKind.GasMask: _draft.showPreviewMask = visible; break;
            case ShooterVisualEquipmentKind.Backpack: _draft.showPreviewBackpack = visible; break;
        }
    }
    private void ReloadFitting(ShooterVisualEquipmentKind kind, bool remove = false)
    {
        switch (kind)
        {
            case ShooterVisualEquipmentKind.Weapon: _draft.LoadWeapon(remove ? null : _draft.previewWeapon); break;
            case ShooterVisualEquipmentKind.Armor: _draft.LoadArmor(remove ? null : _draft.previewArmor); break;
            case ShooterVisualEquipmentKind.GasMask: _draft.LoadEquipment(remove ? null : _draft.equipment); break;
            case ShooterVisualEquipmentKind.Backpack: _draft.LoadBackpack(remove ? null : _draft.previewBackpack); break;
        }
        SyncDirty();
    }
    private void SaveFitting(ShooterVisualEquipmentKind kind)
    {
        switch (kind)
        {
            case ShooterVisualEquipmentKind.Weapon: _draft.SaveWeapon(); break;
            case ShooterVisualEquipmentKind.Armor: _draft.SaveArmor(); break;
            case ShooterVisualEquipmentKind.GasMask: _draft.SaveEquipment(); break;
            case ShooterVisualEquipmentKind.Backpack: _draft.SaveBackpack(); break;
        }
        SyncDirty();
    }
    private bool ConfirmFittingSwitch(ShooterVisualEquipmentKind kind)
    {
        if (!FittingDirty(kind)) return true;
        string label = FittingLabel(kind);
        int choice = EditorUtility.DisplayDialogComplex(label + " 배치가 아직 저장되지 않았어요",
            label + " Item SO에 현재 배치를 저장할까요? 같은 아이템을 쓰는 모든 캐릭터에 적용됩니다.", "SO에 저장", "취소", "변경 버리기");
        if (choice == 1) return false;
        if (choice == 0) SaveFitting(kind); else ReloadFitting(kind);
        return true;
    }
    private void EditFitting(ShooterVisualEquipmentKind kind, Action change)
    {
        _focusFittingAnchor = true;
        Undo.RecordObject(_draft, "Adjust " + kind + " Equipment Placement"); change();
        switch (kind)
        {
            case ShooterVisualEquipmentKind.Weapon: _draft.weaponDirty = true; break;
            case ShooterVisualEquipmentKind.Armor: _draft.armorDirty = true; break;
            case ShooterVisualEquipmentKind.GasMask: _draft.equipmentDirty = true; break;
            case ShooterVisualEquipmentKind.Backpack: _draft.backpackDirty = true; break;
        }
        _applyNeeded = true; SyncDirty(); Repaint();
    }

    private void DrawPartEquipment()
    {
        if (!HasEquipmentAtPart(Part)) return;
        GUILayout.Label("이 부위의 테스트 장비 — 선택해서 배치 조절", EditorStyles.boldLabel);
        DrawFittingCards(true);
        GUILayout.Space(8);
        GUILayout.Label("아래 기준점·장식은 캐릭터 외형에 저장", EditorStyles.miniBoldLabel);
    }

    private void DrawFittingCards(bool onlyPart)
    {
        _draft.EnsureEquipmentDrafts();
        var kinds = FittingKinds.Where(k => FittingItem(k) != null &&
            (!onlyPart || EquipmentAppearsInPart(k, Part, _draft.equipmentSocket))).ToArray();
        if (kinds.Length == 0) return;
        if (!kinds.Contains(_selectedFittingGear)) _selectedFittingGear = !onlyPart && kinds.Contains(ShooterVisualEquipmentKind.Backpack)
            ? ShooterVisualEquipmentKind.Backpack : kinds[0];
        foreach (var kind in kinds)
        {
            var style = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft, wordWrap = true };
            string label = FittingLabel(kind) + (FittingDirty(kind) ? " · 미저장 *" : "") + "\n" + FittingItem(kind).name;
            float height = Mathf.Max(42, style.CalcHeight(new GUIContent(label), Mathf.Max(90, _workbenchWidth - 155)) + 6);
            using (new EditorGUILayout.HorizontalScope())
            {
                Color color = GUI.backgroundColor;
                if (_selectedFittingGear == kind) GUI.backgroundColor = new Color(.25f, .72f, .86f);
                if (GUILayout.Button(new GUIContent(label, AssetPreview.GetMiniThumbnail(FittingItem(kind))), style, GUILayout.Height(height)))
                { _selectedFittingGear = kind; _attachment = -1; _focusFittingAnchor = true; }
                Observe("fitting:select:" + kind); GUI.backgroundColor = color;
                bool shown = _showEquipment && FittingVisible(kind);
                bool visible = GUILayout.Toggle(shown, "보기", EditorStyles.miniButton, GUILayout.Width(42), GUILayout.Height(height));
                if (visible != shown) ChangePreview(() => SetFittingVisible(kind, visible));
                Observe("fitting:visible:" + kind);
                if (GUILayout.Button(new GUIContent("벗기", "미리보기에서만 벗깁니다. 인벤토리·외형·원본 Item SO는 바뀌지 않습니다."), GUILayout.Width(42), GUILayout.Height(height)))
                    Queue(() => { if (ConfirmFittingSwitch(kind)) ChangePreview(() => ReloadFitting(kind, true)); });
                Observe("fitting:remove:" + kind);
            }
        }
        DrawFittingPlacement(_selectedFittingGear);
    }

    private void DrawFittingPlacement(ShooterVisualEquipmentKind kind)
    {
        if (FittingItem(kind) == null) return;
        MakerRegistrationUi.ItemSummary(FittingItem(kind));
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            string label = FittingLabel(kind);
            GUILayout.Label(label + " 위치·회전·크기 — Item SO에 저장", EditorStyles.boldLabel);
            GUILayout.Label("저장 전에는 미리보기만 변경됩니다. SO에 저장하면 같은 아이템을 사용하는 모든 캐릭터에 적용됩니다. 아이템 수치·소지품·스폰 장비는 바꾸지 않습니다.", EditorStyles.wordWrappedMiniLabel);
            Transform mount = _preview.EquipmentMount(kind);
            GUILayout.Label("실제 장착점: " + (mount != null ? AnimationUtility.CalculateTransformPath(mount, _preview.Root.transform) : "장착 모델 / 연결 확인 필요"), EditorStyles.wordWrappedMiniLabel);
            if (kind == ShooterVisualEquipmentKind.Backpack) { DrawBackpackFitting(); return; }
            if (kind == ShooterVisualEquipmentKind.GasMask) { DrawMaskItemAuthoring(); return; }
            GameObject model = kind == ShooterVisualEquipmentKind.Weapon ? _draft.previewWeapon.WeaponViewPrefab?.gameObject : _draft.previewArmor.EquippedVisualPrefab;
            using (new EditorGUI.DisabledScope(true)) EditorGUILayout.ObjectField("착용 모델 (원본 유지)", model, typeof(GameObject), false);
            if (model == null || mount == null) EditorGUILayout.HelpBox("Item SO의 착용 모델과 캐릭터 장착점 연결을 확인하세요.", MessageType.Warning);
            var pose = kind == ShooterVisualEquipmentKind.Weapon ? _draft.weaponPose : _draft.armorPose;
            DrawPose(label + " 배치", pose, value => EditFitting(kind, () =>
            { if (kind == ShooterVisualEquipmentKind.Weapon) _draft.weaponPose = value; else _draft.armorPose = value; }), false);
            Observe("fitting:pose:" + kind);
            if (GUILayout.Button(label + " Item SO에 저장" + (FittingDirty(kind) ? " *" : "")))
                Queue(() => { SaveFitting(kind); ShowNotification(new GUIContent(label + " 배치를 Item SO에 저장했습니다.")); });
            Observe("fitting:save:" + kind);
            if (GUILayout.Button(label + " SO 저장값 불러오기"))
                Queue(() => { if (ConfirmFittingSwitch(kind)) ChangePreview(() => ReloadFitting(kind)); });
            Observe("fitting:reload:" + kind);
        }
    }

    private void OpenFittingPart(ShooterVisualEquipmentKind kind)
    {
        ShooterVisualSocketId part = kind switch
        {
            ShooterVisualEquipmentKind.Armor => ShooterVisualSocketId.Body,
            ShooterVisualEquipmentKind.GasMask => ShooterVisualSocketId.Mouth,
            ShooterVisualEquipmentKind.Backpack => ShooterVisualSocketId.Back,
            _ => ShooterVisualSocketId.RightHand
        };
        _part = Array.IndexOf(Parts, part); _equipmentMode = false; _selectedFittingGear = kind;
        _focusFittingAnchor = true;
        _attachment = -1; _editScroll = Vector2.zero;
        if (kind == ShooterVisualEquipmentKind.Backpack) SetPreviewView(new Vector2(0, 8));
        Repaint();
    }
}
