using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>격리된 미리보기에만 장비를 장착한다. 게임의 Item SO 배치 함수와 실제 프리팹 소켓을 사용한다.</summary>
public sealed partial class ShooterCustomizePreview
{
    private GameObject _weaponVisual, _armorVisual, _backpackVisual;
    private Transform _weaponMount, _armorMount, _maskOverride, _backpackMount;
    private bool _maskMountInvalid, _armorEnabled, _maskEnabled, _maskIsPlayerOnly, _backpackEnabled, _backpackIsPlayerOnly;
    private readonly Dictionary<ShooterVisualSocketId, ShooterVisualLocalTransform> _socketDefaults = new();
    private readonly HashSet<ShooterVisualSocketId> _unsafeSockets = new();
    private readonly List<string> _mountWarnings = new(), _equipmentWarnings = new();
    public IReadOnlyList<string> EquipmentWarnings => _equipmentWarnings;
    public GameObject WeaponVisual => _weaponVisual;
    public GameObject ArmorVisual => _armorVisual;
    public GameObject MaskVisual => _equipment;
    public GameObject BackpackVisual => _backpackVisual;
    public Transform BackpackMount => _backpackMount;
    public Transform EquipmentMount(ShooterVisualEquipmentKind kind) => kind switch
    {
        ShooterVisualEquipmentKind.Weapon => _weaponMount,
        ShooterVisualEquipmentKind.Armor => _armorMount,
        ShooterVisualEquipmentKind.GasMask => _equipment != null ? _equipment.transform.parent : _maskOverride,
        ShooterVisualEquipmentKind.Backpack => _backpackMount,
        _ => null
    };
    public ShooterVisualEquipmentKind EquippedKinds { get; private set; }
    public ShooterVisualOccupancy EquippedRegions { get; private set; }
    public bool EquipmentRulesIgnored { get; private set; }

    public bool CanMoveSocket(ShooterVisualSocketId id) => _socketDefaults.ContainsKey(id) && !_unsafeSockets.Contains(id);
    public ShooterVisualLocalTransform DefaultSocketPose(ShooterVisualSocketId id) =>
        _socketDefaults.TryGetValue(id, out var pose) ? pose : ShooterVisualLocalTransform.Identity;

    private void CaptureFittingBaseline(GameObject source, GameObject character)
    {
        _socketDefaults.Clear(); _unsafeSockets.Clear(); _mountWarnings.Clear();
        foreach (ShooterVisualSocketId id in System.Enum.GetValues(typeof(ShooterVisualSocketId)))
        {
            var socket = Rig.ResolveSocket(id);
            if (socket == null) continue;
            _socketDefaults[id] = ShooterVisualLocalTransform.FromTransform(socket);
            if (socket.GetComponentInChildren<Collider>(true) != null) _unsafeSockets.Add(id);
        }
        _weaponMount = Rig.ResolveSocket(ShooterVisualSocketId.RightHand);
        _armorMount = Rig.ResolveSocket(ShooterVisualSocketId.Armor);
        _backpackMount = Rig.ResolveSocket(ShooterVisualSocketId.Back);
        _maskOverride = null; _maskMountInvalid = false;
        _armorEnabled = _maskEnabled = _backpackEnabled = true; _maskIsPlayerOnly = _backpackIsPlayerOnly = false;
        var controller = character != null ? character.GetComponentInChildren<ShooterEquipmentController>(true) : null;
        if (controller == null) return;
        var data = new SerializedObject(controller);
        var weapon = data.FindProperty("_weaponRoot").objectReferenceValue as Transform;
        var armor = data.FindProperty("_armorVisualRoot").objectReferenceValue as Transform;
        var inventory = data.FindProperty("_equipmentRuntime").objectReferenceValue as EntityEquipmentRuntime;
        armor ??= inventory != null ? inventory.ArmorVisualRoot : null;
        // Match explicit gameplay roots; do not pretend a different socket is the same mount.
        if (weapon != null) _weaponMount = MapMount(source, weapon, "무기");
        _armorMount = MapMount(source, armor != null ? armor : controller.transform, "방탄복");
        var mask = data.FindProperty("_maskEquipmentVisualRoot").objectReferenceValue as Transform;
        if (mask != null) { _maskOverride = MapMount(source, mask, "가스마스크"); _maskMountInvalid = _maskOverride == null; }
        _armorEnabled = data.FindProperty("_renderArmorVisual").boolValue;
        _maskEnabled = data.FindProperty("_renderGasMaskVisual").boolValue;
        _maskIsPlayerOnly = character.GetComponentInChildren<PlayerInventoryRuntime>(true) == null;
        _backpackIsPlayerOnly = _maskIsPlayerOnly;
        var backpack = data.FindProperty("_backpackVisualRoot").objectReferenceValue as Transform;
        if (backpack != null) _backpackMount = MapMount(source, backpack, "백팩");
        _backpackEnabled = data.FindProperty("_renderBackpackVisual").boolValue;
    }

    private Transform MapMount(GameObject source, Transform original, string label)
    {
        if (original == source.transform) return Root.transform;
        if (!original.IsChildOf(source.transform))
        {
            _mountWarnings.Add(label + " 장착점이 Visual 밖에 있어 재현할 수 없습니다. 프리팹 연결을 확인하세요.");
            return null;
        }
        return Root.transform.Find(AnimationUtility.CalculateTransformPath(original, source.transform));
    }

    private void ClearFittingEquipment()
    {
        if (_weaponVisual != null) Object.DestroyImmediate(_weaponVisual);
        if (_armorVisual != null) Object.DestroyImmediate(_armorVisual);
        if (_equipment != null) Object.DestroyImmediate(_equipment);
        if (_backpackVisual != null) Object.DestroyImmediate(_backpackVisual);
        _weaponVisual = _armorVisual = _equipment = _backpackVisual = null;
        Rig?.ClearEquipmentOccupancy("CustomizePreview");
        EquippedKinds = ShooterVisualEquipmentKind.None; EquippedRegions = ShooterVisualOccupancy.None;
    }

    private GameObject CreateFittingVisual(GameObject prefab, Transform mount, string label)
    {
        if (prefab == null || mount == null)
        {
            _equipmentWarnings.Add(label + (prefab == null ? ": Item SO에 장착용 Visual Prefab이 없습니다." : ": 장착 기준점이 없습니다."));
            return null;
        }
        // Item visuals may reference an FBX child, not an asset root. Clone directly below
        // the isolated mount, as gameplay does; the parent overload also keeps it out of the user scene.
        var instance = Object.Instantiate(prefab, mount, false);
        instance.name = "TryOn_" + label + "_" + prefab.name;
        return instance;
    }

    private void ApplyFittingEquipment(ShooterCustomizeDraft draft, bool show)
    {
        draft.EnsureEquipmentDrafts();
        _equipmentWarnings.Clear(); _equipmentWarnings.AddRange(_mountWarnings);
        EquipmentRulesIgnored = draft.ignoreEquipmentHiding;
        if (!show) return;
        if (draft.previewWeapon != null && draft.showPreviewWeapon)
        {
            _weaponVisual = CreateFittingVisual(draft.previewWeapon.WeaponViewPrefab != null ? draft.previewWeapon.WeaponViewPrefab.gameObject : null, _weaponMount, "무기");
            if (_weaponVisual != null)
            {
                draft.weaponPose.ApplyTo(_weaponVisual.transform);
                EquippedKinds |= ShooterVisualEquipmentKind.Weapon;
            }
        }
        if (draft.previewArmor != null && draft.showPreviewArmor)
        {
            if (!_armorEnabled) _equipmentWarnings.Add("이 캐릭터의 방탄복 시각화가 꺼져 있습니다.");
            else _armorVisual = CreateFittingVisual(draft.previewArmor.EquippedVisualPrefab, _armorMount, "방탄복");
            if (_armorVisual != null)
            {
                draft.armorPose.ApplyTo(_armorVisual.transform);
                EquippedKinds |= ShooterVisualEquipmentKind.Armor; EquippedRegions |= draft.previewArmor.VisualCoverage;
            }
        }
        if (draft.equipment != null && draft.showPreviewMask)
        {
            if (_maskIsPlayerOnly) _equipmentWarnings.Add("가스마스크는 현재 Player 전용 장비입니다. 이 대상에서는 외형 조합만 시험합니다.");
            if (!_maskEnabled) _equipmentWarnings.Add("이 캐릭터의 가스마스크 시각화가 꺼져 있습니다.");
            else
            {
                var mount = _maskMountInvalid ? null : _maskOverride ?? Rig.ResolveSocket(draft.equipmentSocket) ??
                    Rig.ResolveSocket(ShooterVisualSocketId.Face) ?? Rig.ResolveSocket(ShooterVisualSocketId.Hair);
                _equipment = CreateFittingVisual(draft.equipmentPrefab, mount, "가스마스크");
            }
            if (_equipment != null)
            {
                draft.equipmentPose.ApplyTo(_equipment.transform);
                EquippedKinds |= ShooterVisualEquipmentKind.GasMask; EquippedRegions |= draft.ResolvedEquipmentCoverage;
            }
        }
        if (draft.previewBackpack != null && draft.showPreviewBackpack)
        {
            if (_backpackIsPlayerOnly) _equipmentWarnings.Add("백팩 실제 착용은 현재 Player 인벤토리에 연결됩니다. 이 대상에서는 외형 조합만 시험합니다.");
            if (!_backpackEnabled) _equipmentWarnings.Add("이 캐릭터의 백팩 시각화가 꺼져 있습니다.");
            else _backpackVisual = CreateFittingVisual(draft.backpackPrefab, _backpackMount, "백팩");
            if (_backpackVisual != null)
            {
                draft.backpackPose.ApplyTo(_backpackVisual.transform);
                EquippedKinds |= ShooterVisualEquipmentKind.Backpack; EquippedRegions |= draft.backpackCoverage;
            }
        }
        if (!EquipmentRulesIgnored)
        {
            Rig.SetEquipmentOccupancy("CustomizePreview", EquippedRegions);
            Rig.SetEquipmentKinds("CustomizePreview", EquippedKinds);
            Rig.SetEquipmentFaceSlotOccupied("CustomizePreview", _equipment != null && draft.equipmentSocket == ShooterVisualSocketId.Face);
        }
    }

    public bool IsAttachmentHidden(ShooterCustomizeAttachment entry)
    {
        var socket = Rig != null ? Rig.ResolveSocket(entry.socket) : null;
        return socket != null && (!socket.gameObject.activeInHierarchy || !EquipmentRulesIgnored &&
            ((entry.occupiedRegions & EquippedRegions) != 0 || (entry.hideWhenEquipped & EquippedKinds) != 0));
    }
}
