using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public sealed class ShooterCustomizeStationTests
{
    private string _folder;
    private GameObject _source, _accessory;
    private ShooterCustomizePreview _preview;
    private ShooterCustomizeDraft _draft;
    private readonly List<Object> _created = new();

    [SetUp]
    public void Setup()
    {
        _folder = "Assets/__ShooterCustomizeTest_" + Guid.NewGuid().ToString("N");
        AssetDatabase.CreateFolder("Assets", _folder.Substring("Assets/".Length));
        var source = new GameObject("Visual_Shooter");
        var meshRoot = Child(source.transform, "Meshes");
        var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Object.DestroyImmediate(capsule.GetComponent<Collider>());
        capsule.transform.SetParent(meshRoot, false);
        capsule.transform.localPosition = Vector3.up;
        var collision = Child(source.transform, "GameplayColliderRoot");
        collision.gameObject.AddComponent<CapsuleCollider>();
        foreach (string name in new[] { "HairSocket", "EyesSocket", "MouthSocket", "FaceSocket", "BodySocket", "FeetSocket", "ArmorSocket", "BackAccessorySocket" }) Child(source.transform, name);
        source.AddComponent<ShooterVisualRig>().AutoResolveReferences();
        _source = PrefabUtility.SaveAsPrefabAsset(source, _folder + "/Visual.prefab");
        Object.DestroyImmediate(source);
        var accessory = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(accessory.GetComponent<Collider>());
        accessory.transform.localScale = new Vector3(.2f, .3f, .4f);
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
        for (int i = _created.Count - 1; i >= 0; i--) if (_created[i] != null) Object.DestroyImmediate(_created[i]);
        _created.Clear();
        if (_folder != null) AssetDatabase.DeleteAsset(_folder);
    }

    private T Asset<T>() where T : ScriptableObject { var value = ScriptableObject.CreateInstance<T>(); _created.Add(value); return value; }
    private static Transform Child(Transform parent, string name) { var child = new GameObject(name).transform; child.SetParent(parent, false); return child; }
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

    [Test]
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
}
