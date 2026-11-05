using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class CombatModulesTests
{
    [Test]
    public void DefaultWeaponWearIs005PerSuccessfulShot()
    {
        var weapon = ScriptableObject.CreateInstance<RangedWeaponSO>();
        var actor = new GameObject("Temporary player equipment wear probe");
        actor.SetActive(false);
        try
        {
            Assert.That(weapon.DurabilityLossPerFire, Is.EqualTo(.05f).Within(.000001f));
            var equipment = actor.AddComponent<EntityEquipmentRuntime>();
            equipment.EquipWeapon(0, weapon, 1, 100f, 100f);
            Assert.That(equipment.ApplyCurrentWeaponFireWear(), Is.True);
            Assert.That(equipment.CurrentWeaponDurability, Is.EqualTo(99.95f).Within(.00001f));
        }
        finally { UnityEngine.Object.DestroyImmediate(actor); UnityEngine.Object.DestroyImmediate(weapon); }
    }

    [Test]
    public void EnemyWeaponsNeverWearOrJamAndConditionDoesNotBlockFiring()
    {
        var weapon = ScriptableObject.CreateInstance<RangedWeaponSO>();
        var actor = new GameObject("Temporary enemy equipment wear probe");
        actor.SetActive(false);
        try
        {
            actor.AddComponent<EnemyShooterStatsRuntime>();
            var equipment = actor.AddComponent<EntityEquipmentRuntime>();
            equipment.EquipWeapon(0, weapon, 1, 37.5f, 100f);
            for (int shot = 0; shot < 100; shot++) Assert.That(equipment.ApplyCurrentWeaponFireWear(), Is.False);
            Assert.That(equipment.CurrentWeaponDurability, Is.EqualTo(37.5f));
            equipment.SetWeaponJammed(0, true);
            Assert.That(equipment.IsWeaponJammed(0), Is.False);
            equipment.EquipWeapon(0, weapon, 1, 0f, 100f);
            Assert.That(equipment.CanFireCurrentWeapon, Is.True);
        }
        finally { UnityEngine.Object.DestroyImmediate(actor); UnityEngine.Object.DestroyImmediate(weapon); }
    }

    [Test]
    public void PocketGrantsArePermanentDeduplicatedAndCapped()
    {
        var profile = new Save();
        Assert.That(profile.PocketSlotBonus, Is.Zero);
        profile.questPocketSlotRewards.Add(new() { questId = "q", rewardIndex = 0, slotAmount = 2 });
        profile.questPocketSlotRewards.Add(new() { questId = "q", rewardIndex = 0, slotAmount = 2 });
        Assert.That(profile.PocketSlotBonus, Is.EqualTo(2));
        profile.questPocketSlotRewards.Add(new() { questId = "q2", rewardIndex = 0, slotAmount = 4 });
        Assert.That(profile.PocketSlotBonus, Is.EqualTo(4));
    }

    [Test]
    public void MalfunctionChanceUsesPerAttemptWearCurve()
    {
        var weapon = ScriptableObject.CreateInstance<RangedWeaponSO>();
        try
        {
            Assert.That(weapon.MalfunctionChance(1f), Is.Zero);
            Assert.That(weapon.MalfunctionChance(.90f), Is.Zero);
            float firstBucket = weapon.MalfunctionChance(.899999f);
            Assert.That(firstBucket, Is.GreaterThan(0f));
            Assert.That(weapon.MalfunctionChance(.89f), Is.EqualTo(firstBucket).Within(.00000001f));
            Assert.That(weapon.MalfunctionChance(.88999f), Is.EqualTo(firstBucket * 2f).Within(.00000001f));
            float reference = weapon.MalfunctionChance(.30f);
            Assert.That(reference, Is.EqualTo(.009543571f).Within(.00000001f));
            Assert.That(1d - Math.Pow(1d - reference, 30), Is.EqualTo(.25d).Within(.000001d));
            Assert.That(weapon.MalfunctionChance(0f), Is.EqualTo(reference * 1.5f).Within(.00000001f));
            for (int percent = 89; percent >= 0; percent--)
                Assert.That(weapon.MalfunctionChance(percent / 100f),
                    Is.EqualTo(firstBucket * (90 - percent)).Within(.0000001f));
            float previous = 0f;
            for (int step = 100; step >= 0; step--)
            {
                float chance = weapon.MalfunctionChance(step / 100f);
                Assert.That(chance, Is.GreaterThanOrEqualTo(previous));
                previous = chance;
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(weapon); }
    }

    [Test]
    public void LegacyRarityNumbersRemainStable()
    {
        Assert.That((int)PlayerShopItemRarity.Normal, Is.EqualTo(0));
        Assert.That((int)PlayerShopItemRarity.Rare, Is.EqualTo(1));
        Assert.That((int)PlayerShopItemRarity.Unique, Is.EqualTo(3));
        Assert.That((int)PlayerShopItemRarity.Legendary, Is.EqualTo(2));
        Assert.That((int)PlayerShopItemRarity.Mythic, Is.EqualTo(4));
    }

    [Test]
    public void ModulePartsAndDefinitionsResolveFromSaveCatalog()
    {
        var rules = PassiveModuleRulesSO.Active;
        var catalog = Resources.Load<RuntimeContentCatalogSO>("RuntimeContentCatalog");
        Assert.That(rules, Is.Not.Null);
        Assert.That(catalog, Is.Not.Null);
        foreach (var rarity in Enum.GetValues(typeof(PlayerShopItemRarity)).Cast<PlayerShopItemRarity>())
        {
            var tier = rules.Find(rarity);
            Assert.That(tier, Is.Not.Null);
            Assert.That(tier.parts.Rarity, Is.EqualTo(rarity));
            Assert.That(tier.parts.MaxStack, Is.GreaterThan(1));
            Assert.That(catalog.TryResolveItem(tier.parts.Guid, out var resolved), Is.True);
            Assert.That(resolved, Is.SameAs(tier.parts));
        }
        foreach (var source in rules.enemyPool)
        {
            Assert.That(catalog.TryResolveItem(source.Guid, out _), Is.True);
            foreach (var rarity in Enum.GetValues(typeof(PlayerShopItemRarity)).Cast<PlayerShopItemRarity>())
                if(source.FindRoll(rarity)!=null)
                    Assert.That(PassiveInstanceRuntime.CanCreate(source,out _,rarity),Is.True);
        }
    }

    [TestCase("NormalEnemy/Scraper/ScraperShooterConfig.asset", 2, PlayerShopItemRarity.Normal, PlayerShopItemRarity.Normal)]
    [TestCase("NormalEnemy/Raider/RaiderShooterConfig.asset", 3, PlayerShopItemRarity.Normal, PlayerShopItemRarity.Rare)]
    [TestCase("NormalEnemy/Agent/AgentShooterConfig.asset", 4, PlayerShopItemRarity.Rare, PlayerShopItemRarity.Unique)]
    [TestCase("Boss/PunisherConfig.asset", 3, PlayerShopItemRarity.Normal, PlayerShopItemRarity.Rare)]
    [TestCase("Boss/Stage2SniperBossConfig.asset", 4, PlayerShopItemRarity.Normal, PlayerShopItemRarity.Rare)]
    [TestCase("Boss/Stage3RocketBossConfig.asset", 4, PlayerShopItemRarity.Rare, PlayerShopItemRarity.Rare)]
    public void EnemyProfilesHaveAuthoredSlotAndRarityRules(string relativePath, int slots, PlayerShopItemRarity min, PlayerShopItemRarity max)
    {
        var config = AssetDatabase.LoadAssetAtPath<EnemyShooterConfigSO>(
            "Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/" + relativePath);
        Assert.That(config, Is.Not.Null);
        Assert.That(config.PassiveSlotCount, Is.EqualTo(slots));
        Assert.That(config.PassiveMinimumRarity, Is.EqualTo(min));
        Assert.That(config.PassiveMaximumRarity, Is.EqualTo(max));
    }
}
