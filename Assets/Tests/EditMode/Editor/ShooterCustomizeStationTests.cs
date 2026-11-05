using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public sealed partial class ShooterCustomizeStationTests
{
    private string _folder;
    private GameObject _source, _accessory;
    private ShooterCustomizePreview _preview;
    private ShooterCustomizeDraft _draft;
    private readonly List<Object> _created = new();
    private Scene _fixtureScene;

    [SetUp]
    public void Setup()
    {
        _fixtureScene = EditorSceneManager.NewPreviewScene();
        _folder = "Assets/__ShooterCustomizeTest_" + Guid.NewGuid().ToString("N");
        AssetDatabase.CreateFolder("Assets", _folder.Substring("Assets/".Length));
        var source = ShooterCustomizeTestObjects.Create("Visual_Shooter", _fixtureScene);
        var meshRoot = Child(source.transform, "Meshes");
        var capsule = ShooterCustomizeTestObjects.Primitive(PrimitiveType.Capsule, _fixtureScene);
        Object.DestroyImmediate(capsule.GetComponent<Collider>());
        capsule.transform.SetParent(meshRoot, false);
        capsule.transform.localPosition = Vector3.up;
        var collision = Child(source.transform, "GameplayColliderRoot");
        collision.gameObject.AddComponent<CapsuleCollider>();
        foreach (string name in new[] { "HairSocket", "EyesSocket", "MouthSocket", "FaceSocket", "BodySocket", "FeetSocket", "ArmorSocket", "BackAccessorySocket", "RightHand_WeaponSocket", "LeftHandItemSocket" }) Child(source.transform, name);
        source.AddComponent<ShooterVisualRig>().AutoResolveReferences();
        _source = PrefabUtility.SaveAsPrefabAsset(source, _folder + "/Visual.prefab");
        Object.DestroyImmediate(source);
        var accessory = ShooterCustomizeTestObjects.Primitive(PrimitiveType.Cube, _fixtureScene);
        Object.DestroyImmediate(accessory.GetComponent<Collider>());
        accessory.transform.localScale = new Vector3(.2f, .3f, .4f);
        accessory.AddComponent<WeaponView>();
        _accessory = PrefabUtility.SaveAsPrefabAsset(accessory, _folder + "/Accessory.prefab");
        Object.DestroyImmediate(accessory);
        _preview = new ShooterCustomizePreview();
        _preview.Build(_source);
        _draft = Asset<ShooterCustomizeDraft>();
        _draft.LoadLook(_source, null, _preview.Rig);
    }

    [TearDown]
    public void Cleanup()
    {
        _preview?.Dispose();
        // Persistent test assets are removed through the owned test folder below,
        // not DestroyImmediate (which Unity rejects to prevent asset data loss).
        for (int i = _created.Count - 1; i >= 0; i--)
            if (_created[i] != null && !EditorUtility.IsPersistent(_created[i])) Object.DestroyImmediate(_created[i]);
        _created.Clear();
        if (_fixtureScene.IsValid()) EditorSceneManager.ClosePreviewScene(_fixtureScene);
        if (_folder != null) AssetDatabase.DeleteAsset(_folder);
    }

    private T Asset<T>() where T : ScriptableObject { var value = ScriptableObject.CreateInstance<T>(); _created.Add(value); return value; }

    [Test]
    public void PresetCardsSortByNameAndNumbersWithoutReorderingSpawnRowsOrWeights()
    {
        var window = typeof(ShooterCustomizeStationWindow);
        var rowType = window.GetNestedType("PresetRow", BindingFlags.NonPublic);
        var lookField = rowType.GetField("look"); var weightField = rowType.GetField("weight");
        var source = Array.CreateInstance(rowType, 7);
        string[] names = { "Scraper_EyeClosed10", null, "Scraper_EyeClosed2", "Scraper_EyeClosed1", "다람쥐", "가방", "나무" };
        var originalRows = new object[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            var row = Activator.CreateInstance(rowType, true); originalRows[i] = row;
            var look = names[i] != null ? Asset<ShooterCustomizeSO>() : null;
            if (look != null) look.name = names[i];
            lookField.SetValue(row, look); weightField.SetValue(row, i + .5f); source.SetValue(row, i);
        }
        var sorter = window.GetMethod("SortedPresetRows", BindingFlags.NonPublic | BindingFlags.Static);
        string[] SortedNames() => ((Array)sorter.Invoke(null, new object[] { source })).Cast<object>()
            .Select(row => (ShooterCustomizeSO)lookField.GetValue(row)).Where(look => look != null).Select(look => look.name).ToArray();
        var sorted = SortedNames();
        Assert.That(sorted.Where(name => name.StartsWith("Scraper_")), Is.EqualTo(new[] { "Scraper_EyeClosed1", "Scraper_EyeClosed2", "Scraper_EyeClosed10" }));
        Assert.That(sorted.Where(name => !name.StartsWith("Scraper_")), Is.EqualTo(new[] { "가방", "나무", "다람쥐" }));
        var orderedRows = (Array)sorter.Invoke(null, new object[] { source });
        Assert.That(orderedRows.GetValue(orderedRows.Length - 1), Is.SameAs(originalRows[1]));
        foreach (var row in orderedRows) Assert.That(originalRows, Does.Contain(row));
        for (int i = 0; i < names.Length; i++)
        {
            Assert.That(source.GetValue(i), Is.SameAs(originalRows[i]));
            Assert.That((float)weightField.GetValue(originalRows[i]), Is.EqualTo(i + .5f));
        }
        ((ShooterCustomizeSO)lookField.GetValue(originalRows[0])).name = "Scraper_EyeClosed0";
        Assert.That(SortedNames().Where(name => name.StartsWith("Scraper_")), Is.EqualTo(new[] { "Scraper_EyeClosed0", "Scraper_EyeClosed1", "Scraper_EyeClosed2" }));
    }

    private static Transform Child(Transform parent, string name) { var child = ShooterCustomizeTestObjects.Create(name, parent.gameObject.scene).transform; child.SetParent(parent, false); return child; }
    private void Add(ShooterVisualSocketId slot) => _draft.attachments.Add(new ShooterCustomizeAttachment {
        socket = slot, visualPrefab = _accessory, occupiedRegions = ShooterVisualOccupancy.Eyes,
        localTransform = new ShooterVisualLocalTransform(new Vector3(.1f, 1.3f, .2f), new Vector3(10, 20, 30), new Vector3(.2f, .3f, .4f)) });

    [Test]
    public void PreviewLivesOutsideUserSceneAndIsCleanedUp()
    {
        var active = SceneManager.GetActiveScene();
        int rootCount = active.rootCount;
        bool dirty = active.isDirty;
        _preview.Build(_source);
        var previewScene = _preview.Root.scene;
        Assert.That(EditorSceneManager.IsPreviewScene(previewScene), Is.True);
        Assert.That(previewScene, Is.Not.EqualTo(active));
        Assert.That(active.rootCount, Is.EqualTo(rootCount));
        Assert.That(active.isDirty, Is.EqualTo(dirty));
        _preview.Dispose();
        Assert.That(previewScene.IsValid(), Is.False);
    }

    [Test]
    public void LoadingAndEditingDraftNeverChangesSourceAssets()
    {
        Add(ShooterVisualSocketId.Hair);
        var look = Asset<ShooterCustomizeSO>(); _draft.CaptureTo(look);
        string before = EditorJsonUtility.ToJson(look);
        var originalScale = _source.GetComponent<ShooterVisualRig>().BodyVisualRoot.localScale;
        _draft.LoadLook(_source, look, _preview.Rig);
        _draft.body.localScale = new Vector3(1, 1.8f, 1);
        _draft.attachments.Clear();
        _preview.Apply(_draft, true);
        Assert.That(EditorJsonUtility.ToJson(look), Is.EqualTo(before));
        Assert.That(_source.GetComponent<ShooterVisualRig>().BodyVisualRoot.localScale, Is.EqualTo(originalScale));
    }

    [Test]
    public void AccessoryIdentityTransformAndCoverageSurviveRepeatedSaveReload()
    {
        Add(ShooterVisualSocketId.Hair);
        var expected = _draft.attachments[0];
        var look = Asset<ShooterCustomizeSO>();
        for (int i = 0; i < 3; i++)
        {
            _draft.CaptureTo(look); _draft.LoadLook(_source, look, _preview.Rig); _preview.Apply(_draft, false);
            Assert.That(_draft.attachments.Count, Is.EqualTo(1));
            Assert.That(_draft.attachments[0].visualPrefab, Is.SameAs(_accessory));
            Assert.That(_draft.attachments[0].occupiedRegions, Is.EqualTo(expected.occupiedRegions));
            Assert.That(_draft.attachments[0].localTransform.localScale, Is.EqualTo(expected.localTransform.localScale));
            Assert.That(_preview.Rig.ResolveSocket(ShooterVisualSocketId.Hair).GetChild(0).localPosition,
                Is.EqualTo(expected.localTransform.localPosition));
        }
    }

    [Test]
    public void FaceHidesEyesMouthButNotHairAndRemovalRestoresThem()
    {
        Add(ShooterVisualSocketId.Eyes); Add(ShooterVisualSocketId.Mouth); Add(ShooterVisualSocketId.Hair); Add(ShooterVisualSocketId.Face);
        _preview.Apply(_draft, false);
        Assert.That(_preview.Rig.ResolveSocket(ShooterVisualSocketId.Eyes).gameObject.activeSelf, Is.False);
        Assert.That(_preview.Rig.ResolveSocket(ShooterVisualSocketId.Mouth).gameObject.activeSelf, Is.False);
        Assert.That(_preview.Rig.ResolveSocket(ShooterVisualSocketId.Hair).gameObject.activeSelf, Is.True);
        _draft.attachments.RemoveAt(3); _preview.Apply(_draft, false);
        Assert.That(_preview.Rig.ResolveSocket(ShooterVisualSocketId.Eyes).gameObject.activeSelf, Is.True);
        Assert.That(_preview.Rig.ResolveSocket(ShooterVisualSocketId.Mouth).gameObject.activeSelf, Is.True);
    }

    [Test]
    public void GearStaysSeparateAndCanBeHiddenWithoutDeletingLook()
    {
        Add(ShooterVisualSocketId.Hair);
        var item = Asset<GasMaskItemSO>(); item.SetEquippedVisualPrefab(_accessory);
        string before = EditorJsonUtility.ToJson(item);
        _draft.LoadEquipment(item);
        _draft.equipmentPose.localPosition = Vector3.one;
        _preview.Apply(_draft, true);
        Assert.That(_preview.Rig.ResolveSocket(ShooterVisualSocketId.Face).childCount, Is.EqualTo(1));
        var look = Asset<ShooterCustomizeSO>(); _draft.CaptureTo(look);
        Assert.That(look.Attachments.Count, Is.EqualTo(1));
        Assert.That(look.Attachments[0].socket, Is.EqualTo(ShooterVisualSocketId.Hair));
        Assert.That(EditorJsonUtility.ToJson(item), Is.EqualTo(before));
        _preview.Apply(_draft, false);
        Assert.That(_preview.Rig.ResolveSocket(ShooterVisualSocketId.Face).childCount, Is.Zero);
        Assert.That(_preview.Rig.ResolveSocket(ShooterVisualSocketId.Eyes).gameObject.activeSelf, Is.True);
    }

    [Test]
    public void BodyResizeChangesVisibleGeometryNotCollisionOrCameraFraming()
    {
        var collider = _preview.Root.GetComponentInChildren<Collider>(true);
        Matrix4x4 collisionMatrix = collider.transform.localToWorldMatrix;
        var frame = _preview.FramingBounds;
        _draft.body.localScale = new Vector3(.8f, 1.9f, 1.2f);
        _preview.Apply(_draft, false);
        Assert.That(_preview.Rig.BodyVisualRoot.localScale, Is.EqualTo(_draft.body.localScale));
        Assert.That(collider.transform.localToWorldMatrix, Is.EqualTo(collisionMatrix));
        Assert.That(_preview.FramingBounds, Is.EqualTo(frame));
        Assert.That(_preview.BodyHasCollider, Is.False);
    }

    [Test]
    public void SourceResolverExtractsVisualChildAndRejectsSceneObjects()
    {
        Assert.That(ShooterCustomizePreview.ResolveVisualSource(_source), Is.SameAs(_source));
        Assert.That(ShooterCustomizePreview.ResolveVisualSource(_preview.Root), Is.Null);
        Assert.That(ShooterCustomizePreview.ResolveVisualSource(_accessory), Is.Null);
    }

    [Test]
    public void RecoveryJsonKeepsUnsavedChangesAndObjectReferences()
    {
        Add(ShooterVisualSocketId.Feet); _draft.lookDirty = true;
        var restored = Asset<ShooterCustomizeDraft>();
        EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(_draft), restored);
        Assert.That(restored.visualPrefab, Is.SameAs(_source));
        Assert.That(restored.lookDirty, Is.True);
        Assert.That(restored.attachments[0].visualPrefab, Is.SameAs(_accessory));
    }

    // Global Undo flushes the editor's shared pending records and can dirty an open
    // user scene. Keep this regression for a dedicated Test Runner session only.
    [Test, Category("GlobalEditorUndo")]
    public void UndoRedoRestoresDraftAndPreview()
    {
        Undo.RecordObject(_draft, "Change Height Test");
        _draft.body.localScale = new Vector3(1, 2, 1); _draft.lookDirty = true;
        Undo.FlushUndoRecordObjects();
        Undo.PerformUndo(); _preview.Apply(_draft, false);
        Assert.That(_preview.Rig.BodyVisualRoot.localScale, Is.EqualTo(Vector3.one));
        Undo.PerformRedo(); _preview.Apply(_draft, false);
        Assert.That(_preview.Rig.BodyVisualRoot.localScale.y, Is.EqualTo(2));
        Undo.ClearUndo(_draft);
    }

    [Test]
    public void SavedAssetRoundTripKeepsEveryAttachmentAndSocketPose()
    {
        Add(ShooterVisualSocketId.Eyes); Add(ShooterVisualSocketId.Face);
        _draft.SetSocketPose(ShooterVisualSocketId.Hair, new ShooterVisualLocalTransform(Vector3.up, Vector3.one * 10, Vector3.one * .75f));
        var look = ScriptableObject.CreateInstance<ShooterCustomizeSO>();
        string path = _folder + "/SavedLook.asset";
        AssetDatabase.CreateAsset(look, path);
        _draft.CaptureTo(look); EditorUtility.SetDirty(look); AssetDatabase.SaveAssetIfDirty(look);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var saved = AssetDatabase.LoadAssetAtPath<ShooterCustomizeSO>(path);
        _draft.LoadLook(_source, saved, _preview.Rig);
        Assert.That(_draft.attachments.Count, Is.EqualTo(2));
        Assert.That(_draft.attachments[0].visualPrefab, Is.SameAs(_accessory));
        Assert.That(_draft.SocketPose(ShooterVisualSocketId.Hair).localPosition, Is.EqualTo(Vector3.up));
    }

    [Test]
    public void SavingEquipmentOnlyUpdatesItsVisualFields()
    {
        var item = ScriptableObject.CreateInstance<GasMaskItemSO>();
        AssetDatabase.CreateAsset(item, _folder + "/Mask.asset");
        float mitigation = item.GasInfectionMultiplier, durability = item.MaxFilterDurability;
        _draft.LoadEquipment(item);
        _draft.equipmentPrefab = _accessory;
        _draft.equipmentSocket = ShooterVisualSocketId.Mouth;
        _draft.equipmentPose = new ShooterVisualLocalTransform(Vector3.one, Vector3.up * 90, Vector3.one * .5f);
        _draft.equipmentDirty = true; _draft.SaveEquipment();
        Assert.That(item.EquippedVisualPrefab, Is.SameAs(_accessory));
        Assert.That(item.EquippedVisualSocket, Is.EqualTo(ShooterVisualSocketId.Mouth));
        Assert.That(item.EquippedVisualTransform.localPosition, Is.EqualTo(Vector3.one));
        Assert.That(item.GasInfectionMultiplier, Is.EqualTo(mitigation));
        Assert.That(item.MaxFilterDurability, Is.EqualTo(durability));
        Assert.That(_draft.equipmentDirty, Is.False);
        Assert.That(_draft.attachments.Count, Is.Zero);
    }

    [Test]
    public void RealVisualPrefabKeepsColliderIdentityPoseAndDimensions()
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GameScene/Shooter/Visual_Shooter.prefab");
        Assert.That(source, Is.Not.Null);
        _preview.Build(source);
        Assert.That(_preview.BodyHasCollider, Is.False);
        var collider = _preview.Root.GetComponentInChildren<CapsuleCollider>(true);
        Assert.That(collider.gameObject.name, Is.EqualTo("GameplayColliderRoot"));
        Assert.That(collider.transform.localPosition, Is.EqualTo(Vector3.up));
        Assert.That(collider.radius, Is.EqualTo(.7f));
        Assert.That(collider.height, Is.EqualTo(1.4f));
        Assert.That(collider.center, Is.EqualTo(Vector3.zero));
        var mesh = _preview.Rig.BodyVisualRoot.GetComponentInChildren<MeshRenderer>();
        Assert.That(collider.transform.localToWorldMatrix, Is.EqualTo(mesh.transform.localToWorldMatrix));
        var sourceCollider = source.GetComponentInChildren<CapsuleCollider>(true);
        Assert.That(GlobalObjectId.GetGlobalObjectIdSlow(sourceCollider).targetObjectId, Is.EqualTo(549841994017151066UL));
    }

    private static void SetReference(Object asset, string property, Object value)
    {
        var data = new SerializedObject(asset); data.FindProperty(property).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo();
    }

    private void FitAllEquipment()
    {
        var weapon = Asset<RangedWeaponSO>(); SetReference(weapon, "_weaponViewPrefab", _accessory.GetComponent<WeaponView>());
        var armor = Asset<ArmorItemSO>(); SetReference(armor, "_equippedVisualPrefab", _accessory);
        var mask = Asset<GasMaskItemSO>(); mask.SetEquippedVisualPrefab(_accessory);
        _draft.previewWeapon = weapon; _draft.previewArmor = armor; _draft.LoadEquipment(mask);
    }

    [Test] public void SimultaneousGearUsesItemOwnedMountTransformsWithoutSavingItemsOrLook()
    {
        FitAllEquipment();
        var weaponData = new SerializedObject(_draft.previewWeapon);
        weaponData.FindProperty("_weaponViewLocalPosition").vector3Value = new Vector3(.1f, .2f, .3f);
        weaponData.ApplyModifiedPropertiesWithoutUndo();
        var armorData = new SerializedObject(_draft.previewArmor);
        armorData.FindProperty("_equippedVisualLocalScale").vector3Value = new Vector3(.3f, .4f, .5f);
        armorData.ApplyModifiedPropertiesWithoutUndo();
        var snapshots = new Object[] { _draft.previewWeapon, _draft.previewArmor, _draft.equipment }
            .ToDictionary(o => o, o => EditorJsonUtility.ToJson(o));
        _preview.Apply(_draft, true);
        Assert.That(_preview.WeaponVisual.transform.parent, Is.SameAs(_preview.Rig.ResolveSocket(ShooterVisualSocketId.RightHand)));
        Assert.That(_preview.WeaponVisual.transform.localPosition, Is.EqualTo(new Vector3(.1f, .2f, .3f)));
        Assert.That(_preview.ArmorVisual.transform.parent, Is.SameAs(_preview.Rig.ResolveSocket(ShooterVisualSocketId.Armor)));
        Assert.That(_preview.ArmorVisual.transform.localScale, Is.EqualTo(new Vector3(.3f, .4f, .5f)));
        Assert.That(_preview.MaskVisual, Is.Not.Null);
        Assert.That(_preview.Root.GetComponentsInChildren<Behaviour>(true).All(b => !b.enabled), Is.True);
        Assert.That(_preview.Root.GetComponentsInChildren<Collider>(true).All(c => !c.enabled), Is.True);
        var look = Asset<ShooterCustomizeSO>(); _draft.CaptureTo(look);
        Assert.That(look.Attachments, Is.Empty); Assert.That(_draft.lookDirty, Is.False); Assert.That(_draft.equipmentDirty, Is.False);
        foreach (var pair in snapshots) Assert.That(EditorJsonUtility.ToJson(pair.Key), Is.EqualTo(pair.Value));
        _preview.Apply(_draft, false);
        Assert.That(_preview.WeaponVisual == null && _preview.ArmorVisual == null && _preview.MaskVisual == null, Is.True);
    }

    [Test] public void EquipmentHideRulesRestoreOnlyWhenAllMatchingSourcesAreRemoved()
    {
        Add(ShooterVisualSocketId.Body);
        var row = _draft.attachments[0]; row.occupiedRegions = ShooterVisualOccupancy.None;
        row.hideWhenEquipped = ShooterVisualEquipmentKind.Armor | ShooterVisualEquipmentKind.Weapon; _draft.attachments[0] = row;
        _preview.Apply(_draft, false);
        var visual = _preview.Rig.ResolveSocket(row.socket).GetChild(0).gameObject;
        _preview.Rig.SetEquipmentKinds("weapon", ShooterVisualEquipmentKind.Weapon);
        Assert.That(visual.activeSelf, Is.False);
        _preview.Rig.SetEquipmentKinds("armor", ShooterVisualEquipmentKind.Armor);
        _preview.Rig.ClearEquipmentOccupancy("weapon"); Assert.That(visual.activeSelf, Is.False);
        _preview.Rig.ClearEquipmentOccupancy("armor"); Assert.That(visual.activeSelf, Is.True);
        var look = Asset<ShooterCustomizeSO>(); _draft.CaptureTo(look); _draft.LoadLook(_source, look, _preview.Rig);
        Assert.That(_draft.attachments[0].hideWhenEquipped, Is.EqualTo(row.hideWhenEquipped));
    }

    [Test] public void ComparisonAndIndividualEquipmentTogglesKeepAppearanceAndRestoreHiding()
    {
        FitAllEquipment(); Add(ShooterVisualSocketId.Body);
        var row = _draft.attachments[0]; row.occupiedRegions = ShooterVisualOccupancy.None;
        row.hideWhenEquipped = ShooterVisualEquipmentKind.Armor; _draft.attachments[0] = row;
        _preview.Apply(_draft, true); Assert.That(_preview.IsAttachmentHidden(row), Is.True);
        _draft.ignoreEquipmentHiding = true; _preview.Apply(_draft, true);
        Assert.That(_preview.IsAttachmentHidden(row), Is.False); Assert.That(_preview.ArmorVisual, Is.Not.Null);
        _draft.ignoreEquipmentHiding = false; _draft.showPreviewArmor = false; _preview.Apply(_draft, true);
        Assert.That(_preview.IsAttachmentHidden(row), Is.False); Assert.That(_preview.ArmorVisual, Is.Null);
        Assert.That(_preview.MaskVisual != null && _preview.WeaponVisual != null, Is.True);
        _draft.showPreviewArmor = true; _preview.Apply(_draft, true); Assert.That(_preview.IsAttachmentHidden(row), Is.True);
        Assert.That(_draft.attachments.Count, Is.EqualTo(1));
    }

    [Test] public void MissingEquippedVisualDoesNotUseWorldPickupOrHideAccessories()
    {
        _draft.previewArmor = Asset<ArmorItemSO>(); _draft.previewWeapon = Asset<RangedWeaponSO>(); _draft.LoadEquipment(Asset<GasMaskItemSO>());
        _preview.Apply(_draft, true);
        Assert.That(_preview.EquipmentWarnings.Count, Is.EqualTo(3));
        Assert.That(_preview.EquippedKinds, Is.EqualTo(ShooterVisualEquipmentKind.None));
    }

    [Test] public void HiddenFaceCosmeticDoesNotSuppressMouthMountedEquipment()
    {
        Add(ShooterVisualSocketId.Face);
        var row = _draft.attachments[0]; row.hideWhenEquipped = ShooterVisualEquipmentKind.GasMask; _draft.attachments[0] = row;
        var mask = Asset<GasMaskItemSO>(); mask.SetEquippedVisualPrefab(_accessory); mask.SetVisualOccupancy(ShooterVisualSocketId.Mouth, ShooterVisualOccupancy.Mouth);
        _draft.LoadEquipment(mask); _preview.Apply(_draft, true);
        Assert.That(_preview.MaskVisual.activeInHierarchy, Is.True);
        Assert.That(_preview.Rig.ResolveSocket(ShooterVisualSocketId.Face).GetChild(0).gameObject.activeSelf, Is.False);
        _preview.Apply(_draft, false);
        Assert.That(_preview.Rig.ResolveSocket(ShooterVisualSocketId.Face).GetChild(0).gameObject.activeSelf, Is.True);
        Assert.That(_preview.Rig.ResolveSocket(ShooterVisualSocketId.Mouth).gameObject.activeSelf, Is.False);
    }

    [Test] public void AnchorsRoundTripForBodyHairMouthAndEquipmentWithoutMovingCollider()
    {
        FitAllEquipment();
        var collider = _preview.Root.GetComponentInChildren<Collider>(true); var initial = collider.transform.localToWorldMatrix;
        var look = Asset<ShooterCustomizeSO>();
        foreach (var slot in new[] { ShooterVisualSocketId.Body, ShooterVisualSocketId.Hair, ShooterVisualSocketId.Mouth, ShooterVisualSocketId.Armor, ShooterVisualSocketId.RightHand })
        {
            Assert.That(_preview.CanMoveSocket(slot), Is.True);
            var pose = new ShooterVisualLocalTransform(new Vector3(.1f, .2f, .3f), Vector3.up * 25, Vector3.one * .8f);
            _draft.SetSocketPose(slot, pose); _draft.CaptureTo(look); _draft.LoadLook(_source, look, _preview.Rig); _preview.Apply(_draft, true);
            Assert.That(_preview.Rig.ResolveSocket(slot).localPosition, Is.EqualTo(pose.localPosition));
            _draft.SetSocketPose(slot, _preview.DefaultSocketPose(slot)); _preview.Apply(_draft, true);
            Assert.That(_preview.Rig.ResolveSocket(slot).localPosition, Is.EqualTo(Vector3.zero));
        }
        Assert.That(collider.transform.localToWorldMatrix, Is.EqualTo(initial));
    }

    [Test, Category("GlobalEditorUndo")] public void RemoveAccessoryUndoRestoresExactHiddenAttachment()
    {
        Add(ShooterVisualSocketId.Face); Add(ShooterVisualSocketId.Mouth); _preview.Apply(_draft, false);
        Assert.That(_preview.IsAttachmentHidden(_draft.attachments[1]), Is.True);
        Undo.RecordObject(_draft, "Remove fitting accessory test"); _draft.RemoveAttachment(1); Undo.FlushUndoRecordObjects();
        Assert.That(_draft.attachments.Count, Is.EqualTo(1)); Undo.PerformUndo();
        Assert.That(_draft.attachments.Count, Is.EqualTo(2)); Assert.That(_draft.attachments[1].socket, Is.EqualTo(ShooterVisualSocketId.Mouth));
        Undo.PerformRedo(); Assert.That(_draft.attachments.Count, Is.EqualTo(1)); Undo.ClearUndo(_draft);
    }

    [Test] public void RuntimeEquipmentControllerPublishesAndClearsCosmeticOnlyKinds()
    {
        Add(ShooterVisualSocketId.Hair); var row = _draft.attachments[0]; row.occupiedRegions = ShooterVisualOccupancy.None;
        row.hideWhenEquipped = ShooterVisualEquipmentKind.Weapon; _draft.attachments[0] = row; _preview.Apply(_draft, false);
        var controller = _preview.Root.AddComponent<ShooterEquipmentController>(); controller.enabled = false;
        var weapon = Child(_preview.Root.transform, "WeaponView").gameObject.AddComponent<WeaponView>();
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(ShooterEquipmentController).GetField("_currentWeaponView", flags).SetValue(controller, weapon);
        var sync = typeof(ShooterEquipmentController).GetMethod("SyncVisualOccupancy", flags); sync.Invoke(controller, null);
        var hair = _preview.Rig.ResolveSocket(row.socket).GetChild(0).gameObject; Assert.That(hair.activeSelf, Is.False);
        typeof(ShooterEquipmentController).GetField("_currentWeaponView", flags).SetValue(controller, null); sync.Invoke(controller, null);
        Assert.That(hair.activeSelf, Is.True);
    }

    [Test] public void RealPlayerAndScraperMountsResolveWithoutSubstitutingPickupPrefabs()
    {
        foreach (var target in ShooterCustomizeCatalog.Discover().Where(t => t.Name == "Player" || t.Name == "ScraperShooterConfig"))
        {
            _preview.Build(target.Visual, target.Prefab); _draft.LoadLook(target.Visual, null, _preview.Rig);
            _draft.previewWeapon = AssetDatabase.FindAssets("t:RangedWeaponSO").Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<RangedWeaponSO>).First(w => w != null && w.WeaponViewPrefab != null);
            _draft.previewArmor = AssetDatabase.FindAssets("t:ArmorItemSO").Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ArmorItemSO>).First(a => a != null && a.EquippedVisualPrefab != null);
            _draft.LoadEquipment(null); _preview.Apply(_draft, true);
            Assert.That(_preview.WeaponVisual, Is.Not.Null, target.Name); Assert.That(_preview.ArmorVisual, Is.Not.Null, target.Name);
            Assert.That(_preview.EquipmentWarnings, Is.Empty, target.Name);
        }
    }

    [Test] public void ImportedFbxChildCanBeWornWithoutTouchingTheUserScene()
    {
        var active = SceneManager.GetActiveScene(); bool dirty = active.isDirty; int roots = active.rootCount;
        // Use a real model child deliberately; gameplay mask assets normally reference a prefab root.
        var child = AssetDatabase.FindAssets("t:Model", new[] { "Assets/Art/Mesh" }).Select(AssetDatabase.GUIDToAssetPath)
            .SelectMany(AssetDatabase.LoadAllAssetsAtPath).OfType<GameObject>().First(m => m.transform.parent != null && m.GetComponentInChildren<Renderer>(true) != null);
        var item = Asset<GasMaskItemSO>(); item.SetEquippedVisualPrefab(child);
        _draft.LoadEquipment(item); _preview.Apply(_draft, true);
        Assert.That(_preview.MaskVisual, Is.Not.Null);
        Assert.That(_preview.MaskVisual.scene, Is.EqualTo(_preview.Root.scene));
        Assert.That(_preview.MaskVisual.GetComponentsInChildren<Renderer>(true), Is.Not.Empty);
        Assert.That(active.isDirty, Is.EqualTo(dirty)); Assert.That(active.rootCount, Is.EqualTo(roots));
        _draft.LoadEquipment(null); _preview.Apply(_draft, true);
        Assert.That(_preview.MaskVisual, Is.Null);
    }

    [Test] public void CapsuleMaskAlignmentUsesTheBodyAxisWithoutChangingPrefabOrAnchor()
    {
        var visual = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GameScene/Shooter/Visual_Shooter.prefab");
        var mask = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GameScene/Shooter/Accesory/Eyes_Thief_Eye_Mask_01.prefab");
        _preview.Build(visual); _draft.LoadLook(visual, null, _preview.Rig);
        var socket = _preview.Rig.ResolveSocket(ShooterVisualSocketId.Eyes); var original = socket.localPosition;
        var row = new ShooterCustomizeAttachment { socket = ShooterVisualSocketId.Eyes, visualPrefab = mask,
            localTransform = ShooterVisualLocalTransform.Identity };
        Assert.That(ShooterCustomizeAccessoryPlacement.TryFit(_preview, row, false, out var pose), Is.True);
        Assert.That(pose.localPosition.z, Is.EqualTo(-.5f).Within(.002));
        Assert.That(pose.localPosition.y, Is.EqualTo(0)); Assert.That(pose.localScale, Is.EqualTo(Vector3.one));
        Assert.That(socket.localPosition, Is.EqualTo(original)); Assert.That(mask.transform.localPosition, Is.EqualTo(Vector3.zero));
        row.localTransform = pose; _draft.attachments.Add(row); _preview.Apply(_draft, false);
        Assert.That(ShooterCustomizeAccessoryPlacement.TryFit(_preview, row, false, out var again), Is.True);
        Assert.That(Vector3.Distance(pose.localPosition, again.localPosition), Is.LessThan(.002));
    }

    [Test] public void CurvedEyeAlignmentCompensatesGeometryDepthAndPreservesHeightRotationScale()
    {
        var visual = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GameScene/Shooter/Visual_Shooter.prefab");
        var eyes = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GameScene/Shooter/Accesory/Eyes_Clossed_01.prefab");
        _preview.Build(visual); _draft.LoadLook(visual, null, _preview.Rig);
        var row = new ShooterCustomizeAttachment { socket = ShooterVisualSocketId.Eyes, visualPrefab = eyes,
            localTransform = ShooterVisualLocalTransform.Identity };
        Assert.That(ShooterCustomizeAccessoryPlacement.TryFit(_preview, row, true, out var pose), Is.True);
        Assert.That(pose.localPosition.z, Is.LessThan(-.01f)); Assert.That(pose.localPosition.y, Is.EqualTo(0));
        Assert.That(pose.localEulerAngles, Is.EqualTo(row.localTransform.localEulerAngles)); Assert.That(pose.localScale, Is.EqualTo(row.localTransform.localScale));
        row.localTransform = pose;
        Assert.That(ShooterCustomizeAccessoryPlacement.TryFit(_preview, row, true, out var again), Is.True);
        Assert.That(Vector3.Distance(pose.localPosition, again.localPosition), Is.LessThan(.002));
        Assert.That(eyes.transform.localPosition, Is.EqualTo(Vector3.zero));
    }

    [Test] public void AccessoryDefaultsAreCopiedAndCannotMutateSavedCharacterPlacements()
    {
        var definition = Asset<ShooterVisualAttachmentSO>();
        var pose = new ShooterVisualLocalTransform(new Vector3(0, .1f, -.5f), Vector3.zero, Vector3.one);
        definition.Configure("mask", _accessory, ShooterVisualSocketId.Eyes, pose, ShooterVisualOccupancy.Eyes, ShooterVisualEquipmentKind.GasMask);
        var old = definition.CreatePlacement(); _draft.attachments.Add(old);
        var look = Asset<ShooterCustomizeSO>(); _draft.CaptureTo(look); string before = EditorJsonUtility.ToJson(look);
        definition.Configure("mask", _accessory, ShooterVisualSocketId.Face, ShooterVisualLocalTransform.Identity, ShooterVisualOccupancy.Mouth, ShooterVisualEquipmentKind.Armor);
        Assert.That(EditorJsonUtility.ToJson(look), Is.EqualTo(before));
        Assert.That(look.Attachments[0].definition, Is.SameAs(definition));
        Assert.That(look.Attachments[0].socket, Is.EqualTo(ShooterVisualSocketId.Eyes));
        Assert.That(look.Attachments[0].localTransform.localPosition, Is.EqualTo(pose.localPosition));
        Assert.That(look.Attachments[0].hideWhenEquipped, Is.EqualTo(ShooterVisualEquipmentKind.GasMask));
        Assert.That(definition.CreatePlacement().socket, Is.EqualTo(ShooterVisualSocketId.Face));
    }

    [Test] public void PerCharacterAccessoryEditsDoNotChangeTheDefinitionOrAnotherLook()
    {
        var definition = Asset<ShooterVisualAttachmentSO>();
        definition.Configure("eyes", _accessory, ShooterVisualSocketId.Eyes, ShooterVisualLocalTransform.Identity,
            ShooterVisualOccupancy.None, ShooterVisualEquipmentKind.None);
        var a = definition.CreatePlacement(); var b = definition.CreatePlacement(); string before = EditorJsonUtility.ToJson(definition);
        a.localTransform.localPosition = Vector3.up; a.hideWhenEquipped = ShooterVisualEquipmentKind.Armor;
        Assert.That(b.localTransform.localPosition, Is.EqualTo(Vector3.zero)); Assert.That(b.hideWhenEquipped, Is.EqualTo(ShooterVisualEquipmentKind.None));
        Assert.That(EditorJsonUtility.ToJson(definition), Is.EqualTo(before));
    }

    [Test] public void DefinitionRegistrationIsIdempotentAndKeepsCalibratedPlacement()
    {
        var root = ShooterCustomizeTestObjects.Primitive(PrimitiveType.Cube, _fixtureScene); Object.DestroyImmediate(root.GetComponent<Collider>());
        root.name = "Eyes_Test"; var prefab = PrefabUtility.SaveAsPrefabAsset(root, _folder + "/PureCosmetic.prefab"); Object.DestroyImmediate(root);
        var first = ShooterCustomizeAccessoryCatalog.Register(prefab, _folder + "/Definitions");
        first.SetPlacement(ShooterVisualSocketId.Eyes, new ShooterVisualLocalTransform(Vector3.back * .5f, Vector3.zero, Vector3.one));
        EditorUtility.SetDirty(first); AssetDatabase.SaveAssetIfDirty(first);
        var second = ShooterCustomizeAccessoryCatalog.Register(prefab, _folder + "/Definitions");
        Assert.That(second, Is.SameAs(first)); Assert.That(second.LocalTransform.localPosition.z, Is.EqualTo(-.5f));
        Assert.That(AssetDatabase.FindAssets("t:ShooterVisualAttachmentSO", new[] { _folder }).Length, Is.EqualTo(1));
    }

    [Test] public void FolderRegistrationExcludesWipAndGameplayEquipment()
    {
        var sources = ShooterCustomizeAccessoryCatalog.SourcePrefabs(); Assert.That(sources, Is.Not.Empty);
        Assert.That(sources.All(p => !AssetDatabase.GetAssetPath(p).Contains("/WIP/") && !AssetDatabase.GetAssetPath(p).Contains("/GasMask/")), Is.True);
        Assert.That(ShooterCustomizeAccessoryCatalog.IsEligible(_accessory), Is.False, "WeaponView is gameplay equipment, not permanent cosmetic data");
        Assert.That(ShooterCustomizeAccessoryCatalog.SuggestedSocket("Head_Prop_Sunglass_01"), Is.EqualTo(ShooterVisualSocketId.Eyes));
        Assert.That(ShooterCustomizeAccessoryCatalog.SuggestedSocket("Head_Prop_Cap_01"), Is.EqualTo(ShooterVisualSocketId.Hair));
    }

    [Test] public void RepairedGasMaskLinksPointToRealRootGameObjects()
    {
        foreach (var path in AssetDatabase.FindAssets("t:GasMaskItemSO", new[] { "Assets/ScriptableObject/GameScene/GameItem/GasMask" }).Select(AssetDatabase.GUIDToAssetPath))
        {
            var item = AssetDatabase.LoadAssetAtPath<GasMaskItemSO>(path);
            Assert.That(item.EquippedVisualPrefab, Is.Not.Null, path);
            Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(item.EquippedVisualPrefab, out string guid, out long id), Is.True);
            Assert.That(id, Is.Not.EqualTo(100100000), path);
            _draft.LoadEquipment(item); _preview.Apply(_draft, true); Assert.That(_preview.MaskVisual, Is.Not.Null, path);
        }
    }

    [Test] public void FourEquipmentModelsRenderWithoutChangingItemsOrSavedLook()
    {
        FitAllEquipment(); var bag = Asset<BackpackItemSO>();
        var pose = new ShooterVisualLocalTransform(new Vector3(.1f, .8f, -.3f), new Vector3(0, 25, 0), Vector3.one * .4f);
        bag.SetEquippedVisual(_accessory, pose); _draft.LoadBackpack(bag);
        string before = EditorJsonUtility.ToJson(bag); _preview.Apply(_draft, true);
        Assert.That(_preview.WeaponVisual != null && _preview.ArmorVisual != null && _preview.MaskVisual != null && _preview.BackpackVisual != null, Is.True);
        Assert.That(_preview.BackpackVisual.transform.parent, Is.SameAs(_preview.Rig.ResolveSocket(ShooterVisualSocketId.Back)));
        Assert.That(_preview.BackpackVisual.transform.localPosition, Is.EqualTo(pose.localPosition));
        Assert.That(_preview.BackpackVisual.transform.localScale, Is.EqualTo(pose.localScale));
        Assert.That(_preview.BackpackVisual.activeInHierarchy, Is.True);
        Assert.That(EditorJsonUtility.ToJson(bag), Is.EqualTo(before));
        var look = Asset<ShooterCustomizeSO>(); _draft.CaptureTo(look); Assert.That(look.Attachments, Is.Empty);
        Assert.That(_draft.lookDirty || _draft.equipmentDirty || _draft.backpackDirty, Is.False);
        _preview.Apply(_draft, false); Assert.That(_preview.BackpackVisual, Is.Null);
    }

    [Test] public void BackpackHidesOnlyMatchingCosmeticsAndComparisonRestoresThem()
    {
        Add(ShooterVisualSocketId.Back); var row = _draft.attachments[0]; row.occupiedRegions = ShooterVisualOccupancy.None;
        row.hideWhenEquipped = ShooterVisualEquipmentKind.Backpack; _draft.attachments[0] = row;
        var bag = Asset<BackpackItemSO>(); bag.SetEquippedVisual(_accessory, ShooterVisualLocalTransform.Identity); _draft.LoadBackpack(bag);
        _preview.Apply(_draft, true); Assert.That(_preview.IsAttachmentHidden(row), Is.True);
        Assert.That(_preview.BackpackVisual.activeInHierarchy, Is.True, "Back equipment must not hide its own socket");
        _draft.ignoreEquipmentHiding = true; _preview.Apply(_draft, true); Assert.That(_preview.IsAttachmentHidden(row), Is.False);
        _draft.ignoreEquipmentHiding = false; _draft.showPreviewBackpack = false; _preview.Apply(_draft, true);
        Assert.That(_preview.IsAttachmentHidden(row), Is.False); Assert.That(_preview.BackpackVisual, Is.Null);
        Assert.That(_draft.HasPreviewEquipment, Is.True); _draft.LoadBackpack(null); Assert.That(_draft.HasPreviewEquipment, Is.False);
        Assert.That((int)ShooterVisualEquipmentKind.GasMask, Is.EqualTo(4)); Assert.That((int)ShooterVisualEquipmentKind.Backpack, Is.EqualTo(8));
    }

    [Test] public void MissingBackpackVisualNeverFallsBackToWorldPickup()
    {
        var bag = Asset<BackpackItemSO>(); SetReference(bag, "_worldVisualPrefab", _accessory); _draft.LoadBackpack(bag);
        _preview.Apply(_draft, true); Assert.That(_preview.BackpackVisual, Is.Null);
        Assert.That(_preview.EquippedKinds, Is.EqualTo(ShooterVisualEquipmentKind.None)); Assert.That(_preview.EquipmentWarnings, Is.Not.Empty);
        bag.SetEquippedVisual(_accessory, ShooterVisualLocalTransform.Identity); _draft.LoadBackpack(bag);
        Object.DestroyImmediate(_preview.Rig.ResolveSocket(ShooterVisualSocketId.Back).gameObject);
        _preview.Apply(_draft, true); Assert.That(_preview.BackpackVisual, Is.Null, "Missing back socket is not substituted with the body/pickup mount");
    }

    [Test] public void BackpackFittingSavePreservesStatsAndRoundTripsOnlyVisualData()
    {
        var bag = Asset<BackpackItemSO>(); var data = new SerializedObject(bag);
        data.FindProperty("_inventorySlotCapacity").intValue = 13; data.FindProperty("_moveSpeedMultiplier").floatValue = .82f;
        data.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.CreateAsset(bag, _folder + "/Bag.asset");
        _draft.LoadBackpack(bag); _draft.backpackPrefab = _accessory;
        _draft.backpackPose = new ShooterVisualLocalTransform(Vector3.back * .2f, Vector3.up * 15, Vector3.one * .5f);
        _draft.backpackDirty = true; _draft.SaveBackpack();
        Assert.That(bag.InventorySlotCapacity, Is.EqualTo(13)); Assert.That(bag.MoveSpeedMultiplier, Is.EqualTo(.82f).Within(.00001f));
        Assert.That(_draft.backpackDirty, Is.False); Assert.That(_draft.lookDirty, Is.False);
        _draft.backpackPose = ShooterVisualLocalTransform.Identity; _draft.LoadBackpack(bag);
        Assert.That(_draft.backpackPose.localPosition, Is.EqualTo(Vector3.back * .2f)); Assert.That(_draft.backpackPrefab, Is.SameAs(_accessory));
        var restored = Asset<ShooterCustomizeDraft>(); EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(_draft), restored);
        Assert.That(restored.previewBackpack, Is.SameAs(bag)); Assert.That(restored.backpackPose.localScale, Is.EqualTo(Vector3.one * .5f));
    }

    [Test] public void RuntimeBackpackTracksInventoryDropReequipVisibilityAndReenable()
    {
        var bag = Asset<BackpackItemSO>(); bag.SetEquippedVisual(_accessory, ShooterVisualLocalTransform.Identity);
        var inventory = _preview.Root.AddComponent<PlayerInventoryRuntime>();
        SetReference(inventory, "_equippedBackpack", bag);
        var controller = _preview.Root.AddComponent<ShooterEquipmentController>(); controller.enabled = false;
        SetReference(controller, "_playerInventory", inventory);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        void Invoke(string name) => typeof(ShooterEquipmentController).GetMethod(name, flags).Invoke(controller, null);
        Invoke("OnEnable"); Assert.That(controller.BackpackVisualInstance, Is.Not.Null);
        Assert.That(controller.BackpackVisualInstance.transform.parent, Is.SameAs(_preview.Rig.ResolveSocket(ShooterVisualSocketId.Back)));
        controller.SetVisible(false); Assert.That(controller.BackpackVisualInstance.activeSelf, Is.False);
        controller.SetVisible(true); Assert.That(controller.BackpackVisualInstance.activeSelf, Is.True);
        Assert.That(inventory.TryDetachEquippedBackpack(out _, out _, out _), Is.True); Assert.That(controller.BackpackVisualInstance, Is.Null);
        Assert.That(inventory.TryEquipBackpackFromWorld(bag, null, out _), Is.True); Assert.That(controller.BackpackVisualInstance, Is.Not.Null);
        Invoke("OnDisable"); var visual = controller.BackpackVisualInstance;
        Assert.That(inventory.TryDetachEquippedBackpack(out _, out _, out _), Is.True); Assert.That(controller.BackpackVisualInstance, Is.SameAs(visual));
        Invoke("OnEnable"); Assert.That(controller.BackpackVisualInstance, Is.Null); Invoke("OnDisable");
    }

    [Test] public void BackFitCompensatesAnOffsetPivotWithoutChangingAnchorsRotationScaleOrAssets()
    {
        var root = ShooterCustomizeTestObjects.Create("Offset backpack", _fixtureScene);
        var mesh = ShooterCustomizeTestObjects.Primitive(PrimitiveType.Cube, _fixtureScene);
        Object.DestroyImmediate(mesh.GetComponent<Collider>());
        mesh.transform.SetParent(root.transform, false); mesh.transform.localPosition = new Vector3(.4f, 1.5f, .7f);
        var model = PrefabUtility.SaveAsPrefabAsset(root, _folder + "/OffsetBag.prefab"); Object.DestroyImmediate(root);
        var bag = Asset<BackpackItemSO>();
        var original = new ShooterVisualLocalTransform(Vector3.one, Vector3.up * 25, new Vector3(.5f, .7f, .6f));
        bag.SetEquippedVisual(model, original); _draft.LoadBackpack(bag);
        _draft.SetSocketPose(ShooterVisualSocketId.Back, new ShooterVisualLocalTransform(Vector3.up, Vector3.up * 15, new Vector3(.8f, 1.2f, .9f)));
        _preview.Apply(_draft, true);
        string itemJson = EditorJsonUtility.ToJson(bag), prefabJson = EditorJsonUtility.ToJson(model);
        var socketPose = ShooterVisualLocalTransform.FromTransform(_preview.BackpackMount);
        Assert.That(ShooterCustomizeAccessoryPlacement.TryFitBackpack(_preview, original, out var fitted), Is.True);
        Assert.That(fitted.localEulerAngles, Is.EqualTo(original.localEulerAngles)); Assert.That(fitted.localScale, Is.EqualTo(original.localScale));
        _draft.backpackPose = fitted; _preview.Apply(_draft, true);
        AssertBackFit();
        Assert.That(ShooterVisualLocalTransform.FromTransform(_preview.BackpackMount), Is.EqualTo(socketPose));
        Assert.That(EditorJsonUtility.ToJson(bag), Is.EqualTo(itemJson)); Assert.That(EditorJsonUtility.ToJson(model), Is.EqualTo(prefabJson));
        Assert.That(ShooterCustomizeAccessoryPlacement.TryFitBackpack(_preview, fitted, out var again), Is.True);
        Assert.That(Vector3.Distance(again.localPosition, fitted.localPosition), Is.LessThan(.0001f), "Repeated fitting is stable");
    }

    [Test] public void AllFiveBackpackMeshesCanFitTheRealCapsuleWithoutSavingItems()
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GameScene/Shooter/Visual_Shooter.prefab");
        _preview.Build(source); _draft.LoadLook(source, null, _preview.Rig);
        for (int level = 1; level <= 5; level++)
        {
            var bag = AssetDatabase.LoadAssetAtPath<BackpackItemSO>($"Assets/ScriptableObject/GameScene/GameItem/Backpack/BackPack_LV{level}.asset");
            string before = EditorJsonUtility.ToJson(bag);
            _draft.LoadBackpack(bag); _preview.Apply(_draft, true);
            Assert.That(ShooterCustomizeAccessoryPlacement.TryFitBackpack(_preview, _draft.backpackPose, out var pose), Is.True, bag.name);
            _draft.backpackPose = pose; _preview.Apply(_draft, true); AssertBackFit();
            Assert.That(EditorJsonUtility.ToJson(bag), Is.EqualTo(before), bag.name);
        }
    }

    private void AssertBackFit()
    {
        Assert.That(ShooterCustomizeAccessoryPlacement.TryBounds(_preview.Root.transform, _preview.Rig.BodyVisualRoot, out var body), Is.True);
        Assert.That(ShooterCustomizeAccessoryPlacement.TryBounds(_preview.Root.transform, _preview.BackpackVisual.transform, out var bag), Is.True);
        Assert.That(bag.center.x, Is.EqualTo(body.center.x).Within(.0001f));
        Assert.That(bag.center.y, Is.EqualTo(body.center.y).Within(.0001f));
        Assert.That(bag.max.z, Is.EqualTo(body.min.z + body.size.x * .01f).Within(.0001f));
    }

    [Test] public void ExistingBackpacksHavePureModelsAndEveryCanonicalRigHasABackSocket()
    {
        for (int level = 1; level <= 5; level++)
        {
            var bag = AssetDatabase.LoadAssetAtPath<BackpackItemSO>($"Assets/ScriptableObject/GameScene/GameItem/Backpack/BackPack_LV{level}.asset");
            Assert.That(bag.EquippedVisualPrefab, Is.Not.Null, bag.name);
            Assert.That(bag.EquippedVisualPrefab.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(bag.EquippedVisualPrefab.GetComponentsInChildren<MonoBehaviour>(true), Is.Empty);
            _draft.LoadBackpack(bag); _preview.Apply(_draft, true); Assert.That(_preview.BackpackVisual, Is.Not.Null, bag.name);
        }
        foreach (var target in ShooterCustomizeCatalog.Discover().Where(t => t.HasVisualRig))
        {
            _preview.Build(target.Visual, target.Prefab); _draft.LoadLook(target.Visual, null, _preview.Rig);
            Assert.That(_preview.Rig.ResolveSocket(ShooterVisualSocketId.Back), Is.Not.Null, target.Name);
        }
    }
}
