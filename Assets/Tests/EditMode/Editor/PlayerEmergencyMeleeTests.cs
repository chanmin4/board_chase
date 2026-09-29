using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class PlayerEmergencyMeleeTests
{
    private readonly List<Object> _created = new();

    [TearDown]
    public void TearDown()
    {
        for (int i = _created.Count - 1; i >= 0; i--) if (_created[i] != null) Object.DestroyImmediate(_created[i]);
        _created.Clear();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EmptySlotAlwaysAllowsFists(bool noAmmo)
        => Assert.IsTrue(PlayerEmergencyMeleeProfile.IsAvailable(null, noAmmo));

    [TestCase(false, false)]
    [TestCase(true, true)]
    public void GunOnlyAllowsFallbackWithoutFireableAmmo(bool noAmmo, bool allowed)
        => Assert.AreEqual(allowed, PlayerEmergencyMeleeProfile.IsAvailable(Asset<RangedWeaponSO>(), noAmmo));

    [TestCase(false)]
    [TestCase(true)]
    public void LegacyPlayerMeleeWeaponIsNotAnEmergencyAttack(bool noAmmo)
        => Assert.IsFalse(PlayerEmergencyMeleeProfile.IsAvailable(Asset<MeleeWeaponSO>(), noAmmo));

    [Test]
    public void UninitializedAmmoIsNotMistakenForAnEmptyGun()
    {
        GameObject owner = new("UninitializedAmmoTest");
        _created.Add(owner);
        owner.SetActive(false);
        Assert.IsFalse(owner.AddComponent<PlayerBulletLoadoutRuntime>().HasNoFireableAmmunition);
    }

    [TestCase(0, 0, true)]
    [TestCase(1, 0, false)]
    [TestCase(0, 4, false)]
    [TestCase(1, 4, false)]
    public void ActualMagazineAndReserveDecideWhetherGunBashIsAvailable(int loaded, int reserve, bool expected)
    {
        GameObject owner = new("EmergencyAmmoTest");
        _created.Add(owner);
        owner.SetActive(false);
        PlayerStatsRuntime stats = owner.AddComponent<PlayerStatsRuntime>();
        EntityEquipmentRuntime equipment = owner.AddComponent<EntityEquipmentRuntime>();
        PlayerInventoryRuntime inventory = owner.AddComponent<PlayerInventoryRuntime>();
        PlayerBulletLoadoutRuntime ammo = owner.AddComponent<PlayerBulletLoadoutRuntime>();
        RangedWeaponSO gun = Asset<RangedWeaponSO>();
        BulletSO bullet = Asset<BasicBulletSO>();
        Assert.IsTrue(gun.IsCompatibleWith(bullet));
        equipment.EquipWeapon(gun);
        Set(ammo, "_statsRuntime", stats);
        Set(ammo, "_equipmentRuntime", equipment);
        Set(ammo, "_inventoryRuntime", inventory);
        Invoke(ammo, "BuildRuntimeSlots");
        object magazine = Invoke(ammo, "GetActiveWeaponMagazine");
        Set(magazine, "bullet", bullet);
        Set(magazine, "currentAmmo", loaded);
        if (reserve > 0)
        {
            PlayerInventoryItemStack stack = new();
            stack.Set(bullet, reserve);
            Set(inventory, "_items", new List<PlayerInventoryItemStack> { stack });
        }
        Assert.AreEqual(expected, ammo.HasNoFireableAmmunition);
        Assert.AreEqual(reserve, inventory.GetInventoryItemAmount(bullet), "Checking fallback must not consume reserve ammo.");
    }

    [Test]
    public void PlayerDamageOverridesLegacyProfileDamageAndKeepsTiming()
    {
        UnarmedAttackProfileSO source = Asset<UnarmedAttackProfileSO>();
        Set(source, "_damage", 999f);
        PlayerEmergencyMeleeProfile profile = new(source, 5f);
        Assert.AreEqual(5f, profile.Damage);
        Assert.AreEqual(source.CooldownSeconds, profile.CooldownSeconds);
        Assert.AreEqual(source.HitDelaySeconds, profile.HitDelaySeconds);
        Assert.AreEqual(source.ResolveDamageTargetMask(ShooterFaction.Player).value,
            profile.ResolveDamageTargetMask(ShooterFaction.Player).value);
        Assert.AreEqual(999f, source.Damage, "The shared enemy profile is not modified.");
        Assert.AreEqual(0f, new PlayerEmergencyMeleeProfile(source, -5f).Damage);
    }

    [TestCase(0f)]
    [TestCase(0.1f)]
    [TestCase(3f)]
    public void FixedDamageIgnoresDamageReductionAndAmplification(float multiplier)
    {
        Damageable target = Target();
        target.SetDamageTakenMultiplier(multiplier);
        float eventDamage = 0f;
        target.OnDamaged += (_, damage) => eventDamage += damage;
        target.ReceiveFixedMeleeAttack(5f);
        Assert.AreEqual(95f, target.CurrentHealth);
        Assert.AreEqual(5f, eventDamage);
    }

    [Test]
    public void FixedDamageIgnoresArmorWithoutBreakingOrdinaryDamage()
    {
        Damageable target = Target();
        PlayerStatsConfigSO armored = Asset<PlayerStatsConfigSO>();
        Set(armored, "_baseArmorClass", 10);
        Set(target, "_statConfig", armored);
        target.ReceiveAnAttack(5f, null, 0);
        float ordinaryDamage = 100f - target.CurrentHealth;
        Assert.Less(ordinaryDamage, 5f);
        target.SetCurrentHealth(100f);
        target.ReceiveFixedMeleeAttack(5f);
        Assert.AreEqual(95f, target.CurrentHealth);
    }

    [Test]
    public void FixedDamageDoesNotBypassInvulnerabilityOrDeath()
    {
        Damageable target = Target();
        InvulnerabilityController invulnerability = target.gameObject.AddComponent<InvulnerabilityController>();
        Set(target, "_invulnerabilityController", invulnerability);
        invulnerability.SetPersistentSource("test", true);
        target.ReceiveFixedMeleeAttack(5f);
        Assert.AreEqual(100f, target.CurrentHealth);
        invulnerability.SetPersistentSource("test", false);
        target.IsDead = true;
        target.ReceiveFixedMeleeAttack(5f);
        Assert.AreEqual(100f, target.CurrentHealth);
    }

    [Test]
    public void NegativeAndZeroDamageDoNotProduceHits()
    {
        Damageable target = Target();
        int hits = 0;
        target.OnDamaged += (_, _) => hits++;
        target.ReceiveFixedMeleeAttack(-5f);
        target.ReceiveFixedMeleeAttack(0f);
        Assert.AreEqual(100f, target.CurrentHealth);
        Assert.AreEqual(0, hits);
    }

    [Test]
    public void ConfigAndRuntimeHaveIndependentMeleeDamage()
    {
        PlayerStatsConfigSO config = Asset<PlayerStatsConfigSO>();
        Assert.AreEqual(5f, config.MeleeDamage);
        Set(config, "_meleeDamage", 7f);
        GameObject owner = new("MeleeStatsTest");
        _created.Add(owner);
        owner.SetActive(false);
        PlayerStatsRuntime stats = owner.AddComponent<PlayerStatsRuntime>();
        Set(stats, "_playerStatsConfig", config);
        typeof(PlayerStatsRuntime).GetMethod("Rebuild", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(stats, null);
        Assert.AreEqual(7f, stats.MeleeDamage);
        Assert.AreEqual(37, (int)PlayerStatId.MeleeDamage);
        Assert.AreEqual(36, (int)PlayerStatId.ArmorInfectionDurabilityLossMultiplier);
    }

    private Damageable Target()
    {
        GameObject owner = new("MeleeDamageTest");
        _created.Add(owner);
        Damageable target = owner.AddComponent<Damageable>();
        target.ApplyMaxHealthFromStats(100f, true);
        return target;
    }

    private T Asset<T>() where T : ScriptableObject
    {
        T value = ScriptableObject.CreateInstance<T>();
        Assert.IsTrue(value != null, "Unity must create a native ScriptableObject for this test.");
        _created.Add(value);
        return value;
    }

    private static object Invoke(object owner, string method)
        => owner.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(owner, null);

    private static void Set(object owner, string name, object value)
    {
        for (Type type = owner.GetType(); type != null; type = type.BaseType)
        {
            FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field == null) continue;
            field.SetValue(owner, value);
            return;
        }
        throw new MissingFieldException(owner.GetType().Name, name);
    }
}
