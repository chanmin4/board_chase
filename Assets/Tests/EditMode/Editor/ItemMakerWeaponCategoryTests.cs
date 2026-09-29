using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class ItemMakerWeaponCategoryTests
{
    private readonly List<Object> _temporary = new();
    private string _folder;
    private const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;

    [SetUp]
    public void SetUp()
    {
        _folder = "Assets/__ItemMakerWeaponTests_" + Guid.NewGuid().ToString("N");
        AssetDatabase.CreateFolder("Assets", Path.GetFileName(_folder));
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = _temporary.Count - 1; i >= 0; i--)
            if (_temporary[i] != null && !AssetDatabase.Contains(_temporary[i])) Object.DestroyImmediate(_temporary[i]);
        _temporary.Clear();
        AssetDatabase.DeleteAsset(_folder);
    }

    [TestCase(RangedWeaponType.Pistol, "WeaponPistol")]
    [TestCase(RangedWeaponType.Revolver, "WeaponPistol")]
    [TestCase(RangedWeaponType.SubmachineGun, "WeaponSmg")]
    [TestCase(RangedWeaponType.AssaultRifle, "WeaponAr")]
    [TestCase(RangedWeaponType.LightMachineGun, "WeaponLmg")]
    [TestCase(RangedWeaponType.Shotgun, "WeaponShotgun")]
    [TestCase(RangedWeaponType.SniperRifle, "WeaponSniper")]
    [TestCase(RangedWeaponType.RocketLauncher, "WeaponSpecial")]
    public void FirearmsAppearInTheirChildAndParent(RangedWeaponType type, string child)
    {
        RangedWeaponSO item = Temporary<RangedWeaponSO>();
        ItemCategoryMoveUtility.ConfigureWeaponType(item, type);
        Assert.AreEqual(child, Category(item));
        Assert.IsTrue(Matches(item, "Weapon"));
        Assert.IsTrue(Matches(item, child));
        Assert.IsFalse(Matches(item, "Bullet"));
    }

    [TestCase(typeof(BasicBulletSO), "BulletLight")]
    [TestCase(typeof(LightBulletSO), "BulletLight")]
    [TestCase(typeof(MediumBulletSO), "BulletMedium")]
    [TestCase(typeof(HeavyBulletSO), "BulletHeavy")]
    [TestCase(typeof(ShotgunShellSO), "BulletShotgun")]
    [TestCase(typeof(SniperBulletSO), "BulletSniper")]
    [TestCase(typeof(RocketBulletSO), "BulletSpecial")]
    [TestCase(typeof(InfectionInjectionBulletSO), "BulletSpecial")]
    public void BulletsUseTheirExistingFamily(Type type, string child)
    {
        ItemSO item = (ItemSO)ScriptableObject.CreateInstance(type);
        _temporary.Add(item);
        Assert.AreEqual(child, Category(item));
        Assert.IsTrue(Matches(item, child));
        Assert.IsTrue(Matches(item, "Bullet"));
        Assert.IsFalse(Matches(item, "Weapon"));
    }

    [Test]
    public void DedicatedMeleeRemainsWipAndConsumableHierarchyIsUnchanged()
    {
        MeleeWeaponSO knife = Temporary<MeleeWeaponSO>();
        Assert.AreEqual("MeleeWip", Category(knife));
        Assert.IsFalse(Matches(knife, "Weapon"));
        Assert.IsTrue(Matches(Temporary<MedicalItemSO>(), "Consumable"));
        Assert.IsTrue(Matches(Temporary<ThrowableUsableItemSO>(), "Consumable"));
    }

    [Test]
    public void SidebarContainsEveryCategoryOnceAndChildrenFollowTheirParent()
    {
        Array order = (Array)typeof(ItemMakerWindow).GetField("CategoryOrder", StaticPrivate).GetValue(null);
        string[] names = order.Cast<object>().Select(value => value.ToString()).ToArray();
        CollectionAssert.AreEquivalent(Enum.GetNames(order.GetType().GetElementType()), names);
        Assert.AreEqual(names.Length, names.Distinct().Count());
        Assert.Less(Array.IndexOf(names, "Weapon"), Array.IndexOf(names, "WeaponSmg"));
        Assert.Less(Array.IndexOf(names, "WeaponSpecial"), Array.IndexOf(names, "Bullet"));
        Assert.Less(Array.IndexOf(names, "BulletSpecial"), Array.IndexOf(names, "Consumable"));
    }

    [Test]
    public void EveryCreateTemplateProducesItsDisplayedCategory()
    {
        IEnumerable templates = (IEnumerable)typeof(ItemMakerWindow).GetField("ItemTemplates", StaticPrivate).GetValue(null);
        foreach (object template in templates)
        {
            Type type = template.GetType();
            ItemSO item = (ItemSO)ScriptableObject.CreateInstance((Type)type.GetProperty("Type").GetValue(template));
            _temporary.Add(item);
            object weaponType = type.GetProperty("WeaponType").GetValue(template);
            if (weaponType != null) ItemCategoryMoveUtility.ConfigureWeaponType(item, (RangedWeaponType)weaponType);
            Assert.AreEqual(type.GetProperty("Category").GetValue(template).ToString(), Category(item),
                type.GetProperty("Label").GetValue(template).ToString());
        }
    }

    [Test]
    public void SameClassGunReclassificationAndRestorePreserveGuidAndTuning()
    {
        RangedWeaponSO gun = Asset<RangedWeaponSO>("Gun");
        SetFloat(gun, "_damageMultiplier", 1.73f);
        string path = AssetDatabase.GetAssetPath(gun);
        string guid = AssetDatabase.AssetPathToGUID(path);
        ItemSO[] moved = ItemCategoryMoveUtility.MoveBatch(new[]
        {
            Request(gun, typeof(RangedWeaponSO), "AR", RangedWeaponType.AssaultRifle)
        }, out var batch, false);
        Assert.AreEqual(RangedWeaponType.AssaultRifle, ((RangedWeaponSO)moved[0]).WeaponType);
        Assert.AreEqual(BulletFamily.Medium, ((RangedWeaponSO)moved[0]).RequiredBulletFamily);
        Assert.AreEqual(1.73f, ((RangedWeaponSO)moved[0]).DamageMultiplier);
        Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(moved[0])));
        RangedWeaponSO restored = (RangedWeaponSO)ItemCategoryMoveUtility.RestoreBatch(batch)[0];
        Assert.AreEqual(path, AssetDatabase.GetAssetPath(restored));
        Assert.AreEqual(RangedWeaponType.Pistol, restored.WeaponType);
        Assert.AreEqual(1.73f, restored.DamageMultiplier);
    }

    [Test]
    public void ShotgunConversionAndRestorePreserveOriginalPelletSettings()
    {
        ShotgunWeaponSO gun = Asset<ShotgunWeaponSO>("Shotgun");
        SetFloat(gun, "_pelletSpreadAngleDegrees", 17f);
        RangedWeaponSO moved = (RangedWeaponSO)ItemCategoryMoveUtility.Move(gun, typeof(RangedWeaponSO),
            _folder + "/SMG", out var record, false, RangedWeaponType.SubmachineGun);
        Assert.AreEqual(RangedWeaponType.SubmachineGun, moved.WeaponType);
        Assert.AreEqual(RangedFirePattern.SingleProjectile, moved.FirePattern);
        ShotgunWeaponSO restored = (ShotgunWeaponSO)ItemCategoryMoveUtility.Restore(record);
        Assert.AreEqual(RangedWeaponType.Shotgun, restored.WeaponType);
        Assert.AreEqual(17f, restored.PelletSpreadAngleDegrees);
    }

    [Test]
    public void ChangingToShotgunUsesTheActualShotgunClass()
    {
        RangedWeaponSO gun = Asset<RangedWeaponSO>("Gun");
        ShotgunWeaponSO moved = (ShotgunWeaponSO)ItemCategoryMoveUtility.Move(gun, typeof(ShotgunWeaponSO),
            _folder + "/Shotgun", out var record, false, RangedWeaponType.Shotgun);
        Assert.AreEqual(RangedFirePattern.PelletSpread, moved.FirePattern);
        Assert.AreEqual(BulletFamily.ShotgunShell, moved.RequiredBulletFamily);
        ItemCategoryMoveUtility.Restore(record);
    }

    [Test]
    public void InvalidFixedWeaponTypeStopsWholeBatchBeforeAnyChange()
    {
        RangedWeaponSO first = Asset<RangedWeaponSO>("First");
        ShotgunWeaponSO second = Asset<ShotgunWeaponSO>("Second");
        Assert.Throws<InvalidOperationException>(() => ItemCategoryMoveUtility.MoveBatch(new[]
        {
            Request(first, typeof(RangedWeaponSO), "AR", RangedWeaponType.AssaultRifle),
            Request(second, typeof(ShotgunWeaponSO), "AR", RangedWeaponType.AssaultRifle)
        }, out _, false));
        Assert.AreEqual(RangedWeaponType.Pistol, first.WeaponType);
        Assert.AreEqual(_folder + "/First.asset", AssetDatabase.GetAssetPath(first));
        Assert.IsFalse(AssetDatabase.IsValidFolder(_folder + "/AR"));
    }

    [Test]
    public void BulletFamilyConversionKeepsDamageAndRestores()
    {
        LightBulletSO bullet = Asset<LightBulletSO>("Round");
        SetFloat(bullet, "_healthDamage", 47f);
        HeavyBulletSO moved = (HeavyBulletSO)ItemCategoryMoveUtility.Move(bullet, typeof(HeavyBulletSO),
            _folder + "/Heavy", out var record, false);
        Assert.AreEqual(BulletFamily.Heavy, moved.Family);
        Assert.AreEqual(47f, moved.HealthDamage);
        Assert.IsInstanceOf<LightBulletSO>(ItemCategoryMoveUtility.Restore(record));
    }

    private ItemCategoryMoveUtility.MoveRequest Request(ItemSO item, Type type, string folder, RangedWeaponType weaponType)
        => new() { item = item, destinationType = type, destinationFolder = _folder + "/" + folder, destinationWeaponType = weaponType };

    private T Temporary<T>() where T : ScriptableObject
    {
        T value = ScriptableObject.CreateInstance<T>();
        _temporary.Add(value);
        return value;
    }

    private T Asset<T>(string name) where T : ScriptableObject
    {
        T value = Temporary<T>();
        AssetDatabase.CreateAsset(value, _folder + "/" + name + ".asset");
        return value;
    }

    private static void SetFloat(Object item, string field, float value)
    {
        using SerializedObject serialized = new(item);
        serialized.FindProperty(field).floatValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static object CategoryEnum(string name)
        => Enum.Parse(typeof(ItemMakerWindow).GetNestedType("ItemCatalogCategory", BindingFlags.NonPublic), name);
    private static string Category(ItemSO item)
        => typeof(ItemMakerWindow).GetMethod("GetCategory", StaticPrivate).Invoke(null, new object[] { item }).ToString();
    private static bool Matches(ItemSO item, string name)
        => (bool)typeof(ItemMakerWindow).GetMethod("MatchesCategory", StaticPrivate).Invoke(null, new[] { item, CategoryEnum(name) });
}
