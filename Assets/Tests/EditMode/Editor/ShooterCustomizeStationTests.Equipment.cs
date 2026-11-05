using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed partial class ShooterCustomizeStationTests
{
    [Test, Category("GearPartEditing")]
    public void EquipmentPartShortcutsReflectLogicalPartsWithoutChangingActualSocket()
    {
        Assert.That(ShooterCustomizeStationWindow.EquipmentAppearsInPart(ShooterVisualEquipmentKind.Armor, ShooterVisualSocketId.Body), Is.True);
        Assert.That(ShooterCustomizeStationWindow.EquipmentAppearsInPart(ShooterVisualEquipmentKind.Armor, ShooterVisualSocketId.Armor), Is.True);
        Assert.That(ShooterCustomizeStationWindow.EquipmentAppearsInPart(ShooterVisualEquipmentKind.GasMask, ShooterVisualSocketId.Mouth), Is.True);
        Assert.That(ShooterCustomizeStationWindow.EquipmentAppearsInPart(ShooterVisualEquipmentKind.GasMask, ShooterVisualSocketId.Face), Is.True);
        Assert.That(ShooterCustomizeStationWindow.EquipmentAppearsInPart(ShooterVisualEquipmentKind.Backpack, ShooterVisualSocketId.Back), Is.True);
        foreach (var hand in new[] { ShooterVisualSocketId.LeftHand, ShooterVisualSocketId.RightHand })
            Assert.That(ShooterCustomizeStationWindow.EquipmentAppearsInPart(ShooterVisualEquipmentKind.Weapon, hand), Is.True);
        Assert.That(ShooterCustomizeStationWindow.EquipmentAppearsInPart(ShooterVisualEquipmentKind.Backpack, ShooterVisualSocketId.Body), Is.False);
        FitAllEquipment(); _preview.Apply(_draft, true);
        Assert.That(_preview.EquipmentMount(ShooterVisualEquipmentKind.Weapon), Is.SameAs(_preview.Rig.ResolveSocket(ShooterVisualSocketId.RightHand)));
        Assert.That(_preview.EquipmentMount(ShooterVisualEquipmentKind.Armor), Is.SameAs(_preview.Rig.ResolveSocket(ShooterVisualSocketId.Armor)));
    }

    [Test, Category("GearPartEditing")]
    public void WeaponAndArmorDraftEditsAffectPreviewButNotItemsLookOrCollider()
    {
        FitAllEquipment(); _draft.EnsureEquipmentDrafts();
        var snapshots = new UnityEngine.Object[] { _draft.previewWeapon, _draft.previewArmor, _source }
            .ToDictionary(o => o, EditorJsonUtility.ToJson);
        var collider = _preview.Root.GetComponentInChildren<Collider>(true);
        var colliderMatrix = collider.transform.localToWorldMatrix;
        var weapon = new ShooterVisualLocalTransform(new Vector3(.21f, .35f, -.42f), new Vector3(18, 74, 12), new Vector3(.6f, .7f, .8f));
        var armor = new ShooterVisualLocalTransform(new Vector3(-.12f, .24f, .39f), new Vector3(3, 44, 8), new Vector3(1.2f, .8f, 1.4f));
        _draft.weaponPose = weapon; _draft.weaponDirty = true;
        _draft.armorPose = armor; _draft.armorDirty = true;
        _preview.Apply(_draft, true);
        AssertPlacement(_preview.WeaponVisual.transform, weapon);
        AssertPlacement(_preview.ArmorVisual.transform, armor);
        Assert.That(_draft.HasEquipmentEdits, Is.True); Assert.That(_draft.lookDirty, Is.False);
        Assert.That(collider.transform.localToWorldMatrix, Is.EqualTo(colliderMatrix));
        var look = Asset<ShooterCustomizeSO>(); _draft.CaptureTo(look);
        Assert.That(look.Attachments, Is.Empty);
        foreach (var pair in snapshots) Assert.That(EditorJsonUtility.ToJson(pair.Key), Is.EqualTo(pair.Value));
    }

    [Test, Category("GearPartEditing")]
    public void WeaponAndArmorSaveOnlyTheirExistingPlacementFieldsAndReloadExactly()
    {
        FitAllEquipment(); _draft.EnsureEquipmentDrafts();
        AssetDatabase.CreateAsset(_draft.previewWeapon, _folder + "/Weapon.asset");
        AssetDatabase.CreateAsset(_draft.previewArmor, _folder + "/Armor.asset");
        var weaponBefore = EditorJsonUtility.ToJson(_draft.previewWeapon);
        var armorBefore = EditorJsonUtility.ToJson(_draft.previewArmor);
        var oldWeapon = _draft.weaponPose; var oldArmor = _draft.armorPose;
        _draft.weaponPose = new ShooterVisualLocalTransform(new Vector3(.3f, .6f, .9f), Vector3.up * 35, Vector3.one * .7f);
        _draft.armorPose = new ShooterVisualLocalTransform(new Vector3(-.2f, .4f, -.6f), Vector3.up * 12, Vector3.one * 1.1f);
        var weapon = _draft.weaponPose; var armor = _draft.armorPose;
        _draft.weaponDirty = _draft.armorDirty = true;
        _draft.SaveWeapon();
        Assert.That(_draft.weaponDirty, Is.False); Assert.That(_draft.armorDirty, Is.True);
        Assert.That(EditorJsonUtility.ToJson(_draft.previewArmor), Is.EqualTo(armorBefore));
        _draft.SaveArmor(); _draft.ReloadEquipmentEdits();
        Assert.That(_draft.weaponPose, Is.EqualTo(weapon)); Assert.That(_draft.armorPose, Is.EqualTo(armor));
        Assert.That(_draft.HasEquipmentEdits, Is.False);
        ResetPlacement(_draft.previewWeapon, "_weaponView", oldWeapon);
        ResetPlacement(_draft.previewArmor, "_equippedVisual", oldArmor);
        Assert.That(EditorJsonUtility.ToJson(_draft.previewWeapon), Is.EqualTo(weaponBefore));
        Assert.That(EditorJsonUtility.ToJson(_draft.previewArmor), Is.EqualTo(armorBefore));
    }

    [Test, Category("GearPartEditing")]
    public void LegacyDraftRecoveryLoadsExistingItemPosesInsteadOfIdentity()
    {
        FitAllEquipment();
        ResetPlacement(_draft.previewWeapon, "_weaponView", new ShooterVisualLocalTransform(Vector3.up * .8f, Vector3.up * 61, Vector3.one * .45f));
        ResetPlacement(_draft.previewArmor, "_equippedVisual", new ShooterVisualLocalTransform(Vector3.right * .2f, Vector3.up * 23, Vector3.one * 1.3f));
        Assert.That(_draft.weaponPoseSource, Is.Null); Assert.That(_draft.armorPoseSource, Is.Null);
        _preview.Apply(_draft, true);
        Assert.That(_preview.WeaponVisual.transform.localPosition, Is.EqualTo(Vector3.up * .8f));
        Assert.That(_preview.ArmorVisual.transform.localScale, Is.EqualTo(Vector3.one * 1.3f));
        Assert.That(_draft.HasEquipmentEdits, Is.False); Assert.That(_draft.lookDirty, Is.False);
    }

    [Test, Category("GearPartEditing")]
    public void PreviewEquipmentDraftScaleMatchesLegacyPerAxisSanitization()
    {
        FitAllEquipment();
        ResetPlacement(_draft.previewWeapon, "_weaponView", new ShooterVisualLocalTransform(Vector3.zero, Vector3.zero, new Vector3(0, -.4f, .000001f)));
        _preview.Apply(_draft, true);
        Assert.That(_preview.WeaponVisual.transform.localScale, Is.EqualTo(new Vector3(1, -.4f, 1)));
        _draft.ReloadEquipmentEdits(); Assert.That(_draft.weaponPose.localScale, Is.EqualTo(new Vector3(0, -.4f, .000001f)));
    }

    [Test, Category("GearPartEditing")]
    public void EquipmentScaleAboveThreeIsPreservedInPreviewAndItemSaves()
    {
        FitAllEquipment(); _draft.EnsureEquipmentDrafts();
        var backpack = Asset<BackpackItemSO>();
        backpack.SetEquippedVisual(_accessory, ShooterVisualLocalTransform.Identity); _draft.LoadBackpack(backpack);
        AssetDatabase.CreateAsset(_draft.previewWeapon, _folder + "/Weapon.asset");
        AssetDatabase.CreateAsset(_draft.previewArmor, _folder + "/Armor.asset");
        AssetDatabase.CreateAsset(_draft.equipment, _folder + "/Mask.asset");
        AssetDatabase.CreateAsset(backpack, _folder + "/Backpack.asset");
        var scale = new Vector3(5, 10, 25);
        _draft.weaponPose.localScale = _draft.armorPose.localScale = scale;
        _draft.equipmentPose.localScale = _draft.backpackPose.localScale = scale;
        _draft.weaponDirty = _draft.armorDirty = _draft.equipmentDirty = _draft.backpackDirty = true;
        _preview.Apply(_draft, true);
        foreach (var visual in new[] { _preview.WeaponVisual, _preview.ArmorVisual, _preview.MaskVisual, _preview.BackpackVisual })
            Assert.That(visual.transform.localScale, Is.EqualTo(scale));
        _draft.SaveEquipmentEdits(); _draft.ReloadEquipmentEdits();
        Assert.That(_draft.weaponPose.localScale, Is.EqualTo(scale));
        Assert.That(_draft.armorPose.localScale, Is.EqualTo(scale));
        Assert.That(_draft.equipmentPose.localScale, Is.EqualTo(scale));
        Assert.That(_draft.backpackPose.localScale, Is.EqualTo(scale));
        Assert.That(_draft.HasEquipmentEdits, Is.False);
    }

    [Test, Category("GearPartEditing")]
    public void SwitchingVisibilityAndPreviewSourceKeepsPendingItemPlacementEdits()
    {
        FitAllEquipment(); _draft.EnsureEquipmentDrafts();
        _draft.weaponPose.localPosition = Vector3.left * .38f; _draft.weaponDirty = true;
        _draft.armorPose.localPosition = Vector3.up * .29f; _draft.armorDirty = true;
        _preview.Apply(_draft, false); _preview.Build(_source); _preview.Apply(_draft, true);
        Assert.That(_preview.WeaponVisual.transform.localPosition, Is.EqualTo(Vector3.left * .38f));
        Assert.That(_preview.ArmorVisual.transform.localPosition, Is.EqualTo(Vector3.up * .29f));
        Assert.That(_draft.weaponDirty && _draft.armorDirty, Is.True);
        _draft.ReloadEquipmentEdits();
        Assert.That(_draft.HasEquipmentEdits, Is.False);
        Assert.That(_draft.weaponPose.localPosition == Vector3.zero && _draft.armorPose.localPosition == Vector3.zero, Is.True);
    }

    private static void ResetPlacement(UnityEngine.Object item, string prefix, ShooterVisualLocalTransform pose)
    {
        var data = new SerializedObject(item);
        data.FindProperty(prefix + "LocalPosition").vector3Value = pose.localPosition;
        data.FindProperty(prefix + "LocalEulerAngles").vector3Value = pose.localEulerAngles;
        data.FindProperty(prefix + "LocalScale").vector3Value = pose.localScale;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssertPlacement(Transform actual, ShooterVisualLocalTransform expected)
    {
        Assert.That(actual.localPosition, Is.EqualTo(expected.localPosition));
        Assert.That(Quaternion.Angle(actual.localRotation, Quaternion.Euler(expected.localEulerAngles)), Is.LessThan(.01f));
        Assert.That(actual.localScale, Is.EqualTo(expected.localScale));
    }
}
