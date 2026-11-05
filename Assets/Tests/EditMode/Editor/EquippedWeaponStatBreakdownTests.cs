using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class EquippedWeaponStatBreakdownTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private GameObject _root;
    private RangedWeaponSO _weapon;
    private PassiveItemSO _passive;
    private ArmorItemSO _armor;
    private PlayerStatsRuntime _stats;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("Weapon breakdown fixture");
        _root.SetActive(false);
        _stats = _root.AddComponent<PlayerStatsRuntime>();
        var passives = _root.AddComponent<PlayerPassiveInventoryRuntime>();
        _weapon = ScriptableObject.CreateInstance<RangedWeaponSO>();
        _weapon.Combat.shotKickDistance = 35f;
        _passive = ScriptableObject.CreateInstance<PassiveItemSO>();
        ((List<PassiveItemSO>)typeof(PlayerPassiveInventoryRuntime)
            .GetField("_equippedItems", Private).GetValue(passives)).Add(_passive);
        Set(_stats, "_fallbackWeapon", _weapon);
        Set(_stats, "_passiveInventory", passives);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_root);
        Object.DestroyImmediate(_weapon);
        Object.DestroyImmediate(_passive);
        if (_armor != null) Object.DestroyImmediate(_armor);
    }

    [TestCase(StatModifierType.FlatAdd, 13f, 50f, 13f)]
    [TestCase(StatModifierType.PercentAdd, 20f, 44.4f, 7.4f)]
    [TestCase(StatModifierType.FlatAdd, -100f, 0f, -37f)]
    public void PassiveDeltaUsesActualResolverIncludingPercentageAndClamp(
        StatModifierType type, float amount, float expectedFinal, float expectedDelta)
    {
        typeof(WeaponSO).GetField("_statModifiers", Private).SetValue(_weapon,
            new[] { Modifier(StatModifierType.FlatAdd, 2f) });
        Set(_passive, "_statModifiers", new[] { Modifier(type, amount) });
        Rebuild();
        Assert.That(_stats.TryGetWeaponStatBreakdown(PlayerStatId.ShotKickDistance, out var row), Is.True);
        Assert.That(row.weaponValue, Is.EqualTo(37f).Within(0.0001f));
        Assert.That(row.passiveDelta, Is.EqualTo(expectedDelta).Within(0.0001f));
        Assert.That(row.finalValue, Is.EqualTo(expectedFinal).Within(0.0001f));
        Assert.That(row.finalValue, Is.EqualTo(_stats.ShotKickDistance).Within(0.0001f));
        Assert.That(row.weaponValue + row.passiveDelta + row.otherDelta,
            Is.EqualTo(row.finalValue).Within(0.0001f));
    }

    [Test]
    public void LaterWeaponOverrideIsNotFalselyReportedAsPassiveBonus()
    {
        typeof(WeaponSO).GetField("_statModifiers", Private).SetValue(_weapon,
            new[] { Modifier(StatModifierType.Override, 20f) });
        Set(_passive, "_statModifiers", new[] { Modifier(StatModifierType.FlatAdd, 13f) });
        Rebuild();
        Assert.That(_stats.TryGetWeaponStatBreakdown(PlayerStatId.ShotKickDistance, out var row), Is.True);
        Assert.That(row.weaponValue, Is.EqualTo(20f));
        Assert.That(row.passiveDelta, Is.EqualTo(0f));
        Assert.That(row.finalValue, Is.EqualTo(20f));
    }

    [Test]
    public void ArmorBonusRemainsOtherRatherThanBeingLabeledPassive()
    {
        var equipment = _root.AddComponent<EntityEquipmentRuntime>();
        _armor = ScriptableObject.CreateInstance<ArmorItemSO>();
        Set(_armor, "_statModifiers", new[] { Modifier(StatModifierType.FlatAdd, 5f) });
        equipment.EquipArmor(_armor);
        Set(_stats, "_equipmentRuntime", equipment);
        typeof(WeaponSO).GetField("_statModifiers", Private).SetValue(_weapon,
            new[] { Modifier(StatModifierType.FlatAdd, 2f) });
        Set(_passive, "_statModifiers", new[] { Modifier(StatModifierType.FlatAdd, 13f) });
        Rebuild();
        Assert.That(_stats.TryGetWeaponStatBreakdown(PlayerStatId.ShotKickDistance, out var row), Is.True);
        Assert.That(row.weaponValue, Is.EqualTo(37f));
        Assert.That(row.passiveDelta, Is.EqualTo(13f));
        Assert.That(row.otherDelta, Is.EqualTo(5f));
        Assert.That(row.finalValue, Is.EqualTo(55f));
    }

    [Test]
    public void SharedWeaponAssetAloneDoesNotAuthorizeEquippedDetail()
    {
        Rebuild();
        Assert.That(_stats.IsActiveWeaponSlot(null, 0, _weapon), Is.False);
        var selection = new ItemDetailSelection(_weapon, 1);
        Assert.That(selection.playerEquipmentOwner, Is.Null);
        Assert.That(selection.playerWeaponSlotIndex, Is.EqualTo(-1));
        Assert.That(_stats.TryGetWeaponStatBreakdown(PlayerStatId.MaxHealth, out _), Is.False);
    }

    private static PlayerStatModifier Modifier(StatModifierType type, float value) => new()
    { stat = PlayerStatId.ShotKickDistance, type = type, value = value };

    private void Rebuild() => typeof(PlayerStatsRuntime).GetMethod("Rebuild", Private).Invoke(_stats, null);
    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, Private).SetValue(target, value);
}
