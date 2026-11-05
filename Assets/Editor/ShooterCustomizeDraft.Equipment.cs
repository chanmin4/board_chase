using UnityEditor;
using UnityEngine;

/// <summary>무기·방탄복의 임시 배치. 런타임 SO 필드와 장착점은 바꾸지 않고 명시적으로 저장할 때만 기록한다.</summary>
public sealed partial class ShooterCustomizeDraft
{
    public ShooterVisualLocalTransform weaponPose = ShooterVisualLocalTransform.Identity;
    public ShooterVisualLocalTransform armorPose = ShooterVisualLocalTransform.Identity;
    public bool weaponDirty, armorDirty;
    // 기존 창 복구 JSON / 직접 선택으로 들어온 SO도 첫 미리보기에서 기존 배치를 읽는다.
    public WeaponSO weaponPoseSource;
    public ArmorItemSO armorPoseSource;
    public bool HasEquipmentEdits => weaponDirty || armorDirty || equipmentDirty || backpackDirty;

    public void LoadWeapon(WeaponSO item)
    {
        previewWeapon = weaponPoseSource = item;
        weaponPose = ReadPlacement(item, "_weaponView");
        weaponDirty = false;
    }

    public void LoadArmor(ArmorItemSO item)
    {
        previewArmor = armorPoseSource = item;
        armorPose = ReadPlacement(item, "_equippedVisual");
        armorDirty = false;
    }

    public void EnsureEquipmentDrafts()
    {
        if (weaponPoseSource != previewWeapon) LoadWeapon(previewWeapon);
        if (armorPoseSource != previewArmor) LoadArmor(previewArmor);
    }

    public void SaveWeapon()
    {
        if (previewWeapon == null) return;
        WritePlacement(previewWeapon, "_weaponView", weaponPose, "Save Weapon Visual Placement");
        weaponDirty = false;
    }

    public void SaveArmor()
    {
        if (previewArmor == null) return;
        WritePlacement(previewArmor, "_equippedVisual", armorPose, "Save Armor Visual Placement");
        armorDirty = false;
    }

    public void SaveEquipmentEdits()
    {
        if (weaponDirty) SaveWeapon();
        if (armorDirty) SaveArmor();
        if (equipmentDirty) SaveEquipment();
        if (backpackDirty) SaveBackpack();
    }

    public void ReloadEquipmentEdits()
    {
        LoadWeapon(previewWeapon); LoadArmor(previewArmor);
        LoadEquipment(equipment); LoadBackpack(previewBackpack);
    }

    // 기존 직렬화된 배치 필드만 기록한다. GUID·공격력·내구도·모델 연결은 유지한다.
    private static ShooterVisualLocalTransform ReadPlacement(Object item, string prefix)
    {
        if (item == null) return ShooterVisualLocalTransform.Identity;
        var data = new SerializedObject(item);
        return new ShooterVisualLocalTransform(data.FindProperty(prefix + "LocalPosition").vector3Value,
            data.FindProperty(prefix + "LocalEulerAngles").vector3Value,
            data.FindProperty(prefix + "LocalScale").vector3Value);
    }

    private static void WritePlacement(Object item, string prefix, ShooterVisualLocalTransform pose, string undoLabel)
    {
        Undo.RecordObject(item, undoLabel);
        var data = new SerializedObject(item);
        data.FindProperty(prefix + "LocalPosition").vector3Value = pose.localPosition;
        data.FindProperty(prefix + "LocalEulerAngles").vector3Value = pose.localEulerAngles;
        data.FindProperty(prefix + "LocalScale").vector3Value = pose.localScale;
        data.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(item); AssetDatabase.SaveAssetIfDirty(item);
    }
}
