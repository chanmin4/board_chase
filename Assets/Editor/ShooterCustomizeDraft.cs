using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Editor-only working copy. Nothing touches a project asset until an explicit save.
public sealed partial class ShooterCustomizeDraft : ScriptableObject
{
    public GameObject visualPrefab;
    public ShooterCustomizeSO target;
    public Color tint = Color.white;
    public ShooterVisualLocalTransform body = ShooterVisualLocalTransform.Identity;
    public List<ShooterVisualSocketPose> sockets = new();
    public List<ShooterCustomizeAttachment> attachments = new();
    public bool lookDirty;
    public GasMaskItemSO equipment;
    public GameObject equipmentPrefab;
    public ShooterVisualSocketId equipmentSocket = ShooterVisualSocketId.Face;
    public ShooterVisualOccupancy equipmentCoverage;
    public ShooterVisualLocalTransform equipmentPose = ShooterVisualLocalTransform.Identity;
    public bool equipmentDirty;

    // Fitting selections are editor-only. CaptureTo deliberately never copies these into a look.
    public WeaponSO previewWeapon;
    public ArmorItemSO previewArmor;
    public BackpackItemSO previewBackpack;
    public GameObject backpackPrefab;
    public ShooterVisualLocalTransform backpackPose = ShooterVisualLocalTransform.Identity;
    public ShooterVisualOccupancy backpackCoverage = ShooterVisualOccupancy.Back;
    public bool backpackDirty;
    public bool showPreviewBackpack = true;
    public bool showPreviewWeapon = true, showPreviewArmor = true, showPreviewMask = true;
    public bool ignoreEquipmentHiding;
    public bool HasPreviewEquipment => equipment != null || previewArmor != null || previewWeapon != null || previewBackpack != null;

    public void LoadBackpack(BackpackItemSO item)
    {
        previewBackpack = item;
        backpackPrefab = item != null ? item.EquippedVisualPrefab : null;
        backpackPose = item != null ? item.EquippedVisualTransform : ShooterVisualLocalTransform.Identity;
        backpackCoverage = item != null ? (ShooterVisualOccupancy)new SerializedObject(item).FindProperty("_visualCoverage").intValue : ShooterVisualOccupancy.Back;
        backpackDirty = false;
    }

    public void SaveBackpack()
    {
        if (previewBackpack == null) return;
        Undo.RecordObject(previewBackpack, "Save Backpack Visual Placement");
        previewBackpack.SetEquippedVisual(backpackPrefab, backpackPose);
        EditorUtility.SetDirty(previewBackpack); AssetDatabase.SaveAssetIfDirty(previewBackpack);
        backpackDirty = false;
    }

    public void RemoveAttachment(int index)
    {
        if (index < 0 || index >= attachments.Count) return;
        attachments.RemoveAt(index);
        lookDirty = true;
    }

    public void LoadLook(GameObject source, ShooterCustomizeSO look, ShooterVisualRig baseline)
    {
        visualPrefab = source;
        target = look;
        tint = look != null ? look.BodyTint : Color.white;
        body = look != null ? look.BodyVisualTransform : ShooterVisualLocalTransform.FromTransform(baseline.BodyVisualRoot);
        sockets.Clear();
        attachments.Clear();
        if (look != null)
        {
            sockets.AddRange(look.SocketPoses);
            attachments.AddRange(look.Attachments); // Preserve prefab identity AND occupied regions, including hidden parts.
        }
        foreach (ShooterVisualSocketId id in System.Enum.GetValues(typeof(ShooterVisualSocketId)))
            if (!sockets.Exists(p => p.socket == id) && baseline.ResolveSocket(id) != null)
                sockets.Add(new ShooterVisualSocketPose { socket = id,
                    localTransform = ShooterVisualLocalTransform.FromTransform(baseline.ResolveSocket(id)) });
        lookDirty = false;
    }

    public ShooterVisualLocalTransform SocketPose(ShooterVisualSocketId id)
    {
        int index = sockets.FindIndex(p => p.socket == id);
        return index >= 0 ? sockets[index].localTransform : ShooterVisualLocalTransform.Identity;
    }

    public void SetSocketPose(ShooterVisualSocketId id, ShooterVisualLocalTransform pose)
    {
        int index = sockets.FindIndex(p => p.socket == id);
        var value = new ShooterVisualSocketPose { socket = id, localTransform = pose };
        if (index < 0) sockets.Add(value); else sockets[index] = value;
        lookDirty = true;
    }

    public void CaptureTo(ShooterCustomizeSO destination)
    {
        destination.Capture(visualPrefab, tint, body, sockets, attachments);
    }

    public void LoadEquipment(GasMaskItemSO item)
    {
        equipment = item;
        equipmentPrefab = item != null ? item.EquippedVisualPrefab : null;
        equipmentSocket = item != null ? item.EquippedVisualSocket : ShooterVisualSocketId.Face;
        // Read the authored value, not VisualCoverage's resolved fallback, so switching Face -> Mouth is safe.
        equipmentCoverage = item != null
            ? (ShooterVisualOccupancy)new SerializedObject(item).FindProperty("_visualCoverage").intValue
            : ShooterVisualOccupancy.None;
        equipmentPose = item != null ? item.EquippedVisualTransform : ShooterVisualLocalTransform.Identity;
        equipmentDirty = false;
    }

    public void SaveEquipment()
    {
        if (equipment == null) return;
        Undo.RecordObject(equipment, "Save Equipment Fitting");
        equipment.SetEquippedVisualPrefab(equipmentPrefab);
        equipment.SetVisualOccupancy(equipmentSocket, equipmentCoverage);
        equipment.SetEquippedVisualTransform(equipmentPose);
        EditorUtility.SetDirty(equipment);
        AssetDatabase.SaveAssetIfDirty(equipment);
        equipmentDirty = false;
    }

    public ShooterVisualOccupancy ResolvedEquipmentCoverage => equipmentPrefab == null ? ShooterVisualOccupancy.None :
        equipmentSocket == ShooterVisualSocketId.Face ? equipmentCoverage | ShooterVisualOccupancy.Eyes | ShooterVisualOccupancy.Mouth :
        equipmentCoverage != ShooterVisualOccupancy.None ? equipmentCoverage :
        equipmentSocket == ShooterVisualSocketId.Mouth ? ShooterVisualOccupancy.Mouth :
        equipmentSocket == ShooterVisualSocketId.Eyes ? ShooterVisualOccupancy.Eyes : ShooterVisualOccupancy.None;
}
