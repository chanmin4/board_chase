using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class ItemHandAndCategoryTests
{
    private readonly List<Object> _temporary = new();
    private string _folder;

    [SetUp]
    public void SetUp()
    {
        _folder = "Assets/__ItemHandTests_" + Guid.NewGuid().ToString("N");
        AssetDatabase.CreateFolder("Assets", Path.GetFileName(_folder));
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = _temporary.Count - 1; i >= 0; i--)
            if (_temporary[i] != null && !AssetDatabase.Contains(_temporary[i]))
                Object.DestroyImmediate(_temporary[i]);
        _temporary.Clear();
        AssetDatabase.DeleteAsset(_folder);
    }

    [Test]
    public void WorldVisualKeepsSavedTransformAndDoesNotRequireWeaponView()
    {
        MedicalItemSO item = Temporary<MedicalItemSO>();
        GameObject prefab = Prop();
        prefab.transform.localPosition = new Vector3(0.2f, 0.3f, 0.4f);
        prefab.transform.localRotation = Quaternion.Euler(20f, 30f, 40f);
        prefab.transform.localScale = new Vector3(0.02f, 0.03f, 0.04f);
        Set(item, "_worldVisualPrefab", prefab);
        GameObject socket = Prop();
        GameObject held = UsableHeldVisualUtility.Create(item, socket.transform);
        _temporary.Add(held);
        Transform model = held.transform.GetChild(0);
        Assert.AreEqual(prefab.transform.localPosition, model.localPosition);
        Assert.Less(Quaternion.Angle(prefab.transform.localRotation, model.localRotation), 0.001f);
        Assert.AreEqual(prefab.transform.localScale, model.localScale);
        Assert.AreSame(socket.transform, held.transform.parent);
        Assert.IsNull(model.GetComponent<WeaponView>());
    }

    [Test]
    public void WorldVisualOffsetsAffectMountWithoutOverwritingModel()
    {
        MedicalItemSO item = Temporary<MedicalItemSO>();
        GameObject prefab = Prop();
        prefab.transform.localScale = Vector3.one * 0.04f;
        Set(item, "_worldVisualPrefab", prefab);
        using (SerializedObject serialized = new(item))
        {
            serialized.FindProperty("_heldViewLocalPosition").vector3Value = Vector3.up;
            serialized.FindProperty("_heldViewLocalScale").vector3Value = Vector3.one * 2f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        GameObject held = UsableHeldVisualUtility.Create(item, Prop().transform);
        _temporary.Add(held);
        Assert.AreEqual(Vector3.up, held.transform.localPosition);
        Assert.AreEqual(Vector3.one * 2f, held.transform.localScale);
        Assert.AreEqual(Vector3.one * 0.04f, held.transform.GetChild(0).localScale);
    }

    [Test]
    public void HeldDisplayDisablesOnlyClonedPhysics()
    {
        MedicalItemSO item = Temporary<MedicalItemSO>();
        GameObject prefab = Prop();
        prefab.AddComponent<BoxCollider>();
        Rigidbody body = prefab.AddComponent<Rigidbody>();
        Set(item, "_worldVisualPrefab", prefab);
        GameObject held = UsableHeldVisualUtility.Create(item, Prop().transform);
        _temporary.Add(held);
        Assert.IsFalse(held.GetComponentInChildren<Collider>().enabled);
        Assert.IsTrue(held.GetComponentInChildren<Rigidbody>().isKinematic);
        Assert.IsFalse(held.GetComponentInChildren<Rigidbody>().detectCollisions);
        Assert.IsTrue(prefab.GetComponent<Collider>().enabled);
        Assert.IsFalse(body.isKinematic);
    }

    [Test]
    public void OverrideKeepsLegacyAbsoluteOffsetsAndThrowableNeverUsesDisplayFallback()
    {
        ThrowableUsableItemSO item = Temporary<ThrowableUsableItemSO>();
        Set(item, "_worldVisualPrefab", Prop());
        Assert.IsNull(item.HandVisualPrefab);
        WeaponView view = Prop().AddComponent<WeaponView>();
        view.transform.localScale = Vector3.one * 8f;
        Set(item, "_heldViewPrefab", view);
        GameObject held = UsableHeldVisualUtility.Create(item, Prop().transform);
        _temporary.Add(held);
        Assert.IsNotNull(held.GetComponent<WeaponView>());
        Assert.AreEqual(Vector3.one, held.transform.localScale);
    }

    [Test]
    public void MissingVisualOrSocketDoesNotInstantiateAnything()
    {
        Assert.IsNull(UsableHeldVisualUtility.Create(null, Prop().transform));
        Assert.IsNull(UsableHeldVisualUtility.Create(Temporary<MedicalItemSO>(), Prop().transform));
        Assert.IsNull(UsableHeldVisualUtility.Create(Temporary<MedicalItemSO>(), null));
    }

    [Test]
    public void TypeConversionPreservesIdentityCommonFieldsAndLootReferenceThenRestores()
    {
        PassiveItemSO item = Asset<PassiveItemSO>("Medicine");
        using (SerializedObject serialized = new(item))
        {
            serialized.FindProperty("_purchasePrice").intValue = 137;
            serialized.FindProperty("_maxStack").intValue = 9;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        AssetDatabase.SaveAssetIfDirty(item);
        string originalPath = AssetDatabase.GetAssetPath(item);
        string guid = AssetDatabase.AssetPathToGUID(originalPath);
        string runtimeGuid = item.Guid;
        LootTableSO loot = Asset<LootTableSO>("Loot");
        loot.Pool.rarityTiers[0].items.Add(new LootItemEntry { item = item, minAmount = 1, maxAmount = 1 });
        EditorUtility.SetDirty(loot);
        AssetDatabase.SaveAssetIfDirty(loot);
        string lootPath = AssetDatabase.GetAssetPath(loot);

        ItemSO moved = ItemCategoryMoveUtility.Move(item, typeof(MedicalItemSO), _folder + "/Medical", out var record, false);
        Assert.IsInstanceOf<MedicalItemSO>(moved);
        Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(moved)));
        Assert.AreEqual(runtimeGuid, moved.Guid);
        Assert.AreEqual(137, moved.PurchasePrice);
        Assert.AreEqual(9, moved.MaxStack);
        Assert.AreEqual(moved, loot.Pool.rarityTiers[0].items[0].item, "Already loaded loot references must also be rebound.");
        AssetDatabase.ImportAsset(lootPath, ImportAssetOptions.ForceUpdate);
        Assert.AreEqual(moved, AssetDatabase.LoadAssetAtPath<LootTableSO>(lootPath).Pool.rarityTiers[0].items[0].item);

        ItemSO restored = ItemCategoryMoveUtility.Restore(record);
        Assert.IsInstanceOf<PassiveItemSO>(restored);
        Assert.AreEqual(originalPath, AssetDatabase.GetAssetPath(restored));
        Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(originalPath));
        Assert.AreEqual(137, restored.PurchasePrice);
    }

    [Test]
    public void IncompatibleTypedQuestReferenceBlocksConversion()
    {
        RangedWeaponSO weapon = Asset<RangedWeaponSO>("Gun");
        QuestDefinitionSO quest = Asset<QuestDefinitionSO>("Quest");
        using (SerializedObject serialized = new(quest))
        {
            SerializedProperty objectives = serialized.FindProperty("_objectives");
            objectives.arraySize = 1;
            objectives.GetArrayElementAtIndex(0).FindPropertyRelative("_requiredWeapon").objectReferenceValue = weapon;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        AssetDatabase.SaveAssetIfDirty(quest);
        string originalPath = AssetDatabase.GetAssetPath(weapon);
        Assert.Throws<InvalidOperationException>(() =>
            ItemCategoryMoveUtility.Move(weapon, typeof(MedicalItemSO), _folder + "/Medical", out _, false));
        Assert.IsInstanceOf<RangedWeaponSO>(AssetDatabase.LoadAssetAtPath<ItemSO>(originalPath));
    }

    [Test]
    public void RestoreDoesNotOverwriteEditsMadeAfterTheMove()
    {
        ItemSO moved = ItemCategoryMoveUtility.Move(Asset<PassiveItemSO>("Item"), typeof(MedicalItemSO),
            _folder + "/Medical", out var record, false);
        using (SerializedObject serialized = new(moved))
        {
            serialized.FindProperty("_purchasePrice").intValue = 999;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        AssetDatabase.SaveAssetIfDirty(moved);
        Assert.Throws<InvalidOperationException>(() => ItemCategoryMoveUtility.Restore(record));
        Assert.AreEqual(999, moved.PurchasePrice);
    }

    [Test]
    public void SameTypeMovePreservesSpecializedSubtype()
    {
        InfectionCureItemSO item = Asset<InfectionCureItemSO>("Cure");
        ItemSO moved = ItemCategoryMoveUtility.Move(item, item.GetType(), _folder + "/Medical", out _, false);
        Assert.IsInstanceOf<InfectionCureItemSO>(moved);
    }

    [Test]
    public void UnrelatedMalformedPrefabGuidDoesNotRequireUnityDependencyParsing()
    {
        using StringReader reader = new("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n" +
            "--- !u!1001 &1\nPrefabInstance:\n" +
            "  target: {fileID: 4, guid: a0000000000000000000000000001, type: 3}\n");
        Assert.AreEqual(false, ItemCategoryMoveUtility.TextContainsGuidReference(reader,
            "1234567890abcdef1234567890abcdef"));
    }

    [Test]
    public void TextReferenceFilterKeepsDirectReferencesEvenWithOtherMalformedGuids()
    {
        using StringReader reader = new("\uFEFF%YAML 1.1\r\n" +
            "  target: {fileID: 4, guid: broken, type: 3}\r\n" +
            "  item: {fileID: 11400000, guid: 1234567890ABCDEF1234567890ABCDEF, type: 2}\r\n");
        Assert.AreEqual(true, ItemCategoryMoveUtility.TextContainsGuidReference(reader,
            "1234567890abcdef1234567890abcdef"));
    }

    [TestCase("binary data")]
    [TestCase("")]
    public void NonYamlAssetsStillRequireDependencyDatabaseLookup(string contents)
    {
        using StringReader reader = new(contents);
        Assert.IsNull(ItemCategoryMoveUtility.TextContainsGuidReference(reader,
            "1234567890abcdef1234567890abcdef"));
    }

    private T Temporary<T>() where T : ScriptableObject
    {
        T value = ScriptableObject.CreateInstance<T>();
        _temporary.Add(value);
        return value;
    }

    private T Asset<T>(string name) where T : ScriptableObject
    {
        T value = ScriptableObject.CreateInstance<T>();
        value.name = name;
        AssetDatabase.CreateAsset(value, _folder + "/" + name + ".asset");
        return value;
    }

    private GameObject Prop()
    {
        GameObject value = new("Prop");
        _temporary.Add(value);
        return value;
    }

    private static void Set(Object owner, string field, Object value)
    {
        using SerializedObject serialized = new(owner);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
