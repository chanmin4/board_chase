using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class DemoRouteAndLootRulesTests
{
    private const string Items = "Assets/ScriptableObject/GameScene/GameItem/";

    [TestCase(1, new[] { 2, 3 })]
    [TestCase(2, new[] { 4 })]
    [TestCase(3, new[] { 4 })]
    [TestCase(4, new int[0])]
    public void DemoHatchesForkAndRejoin(int from, int[] expected)
    {
        var flow = AssetDatabase.LoadAssetAtPath<StageRunFlowSO>("Assets/ScriptableObject/GameScene/Stage/StageRunFlow.asset");
        Assert.NotNull(flow);
        Assert.IsTrue(flow.TryGetStage(from, out var stage));
        CollectionAssert.AreEqual(expected, flow.GetHatchDestinations(stage).Select(x => x.StageIndex).ToArray());
    }

    [Test]
    public void InfectionOverflowAndCapacityDecreaseKeepRemainder()
    {
        var ordinary = new InfectionExposureSaveData { gauge = 83f };
        Assert.AreEqual(1, InfectionAggravationMath.Accumulate(ordinary, 50f, 100f, 20, 0f));
        Assert.AreEqual(33f, ordinary.gauge);
        var reduced = new InfectionExposureSaveData { gauge = 113f };
        Assert.AreEqual(1, InfectionAggravationMath.Accumulate(reduced, 0f, 100f, 20, 0f));
        Assert.AreEqual(13f, reduced.gauge);
        Assert.AreEqual(.1f, InfectionAggravationStatusSO.Active.Settings.maxHealthLossFraction);
        Assert.AreEqual(.5f, InfectionAggravationStatusSO.Active.Settings.currentHealthLossExposureFraction);
        Assert.AreEqual(0f, InfectionAggravationStatusSO.Active.Settings.naturalGaugeRecoveryPerSecond);
    }

    [Test]
    public void NewStackLimitsKeepOrdinaryAmmoAndUseSeparateInventorySlotsForMaterials()
    {
        Assert.AreEqual(1, AssetDatabase.LoadAssetAtPath<ItemSO>(Items + "Valuable/Gunpowder.asset").MaxStack);
        Assert.AreEqual(1, AssetDatabase.LoadAssetAtPath<ItemSO>(Items + "Currency/Money.asset").MaxStack);
        Assert.AreEqual(1, AssetDatabase.LoadAssetAtPath<ItemSO>(Items + "Consumable/Throwable/Grenade.asset").MaxStack);
        Assert.AreEqual(120, AssetDatabase.LoadAssetAtPath<ItemSO>(Items + "Bullet/MediumBulletLV2.asset").MaxStack);
    }

    [Test]
    public void InfectionAmmoUsesRarityAndOnlyFitsAssaultRifles()
    {
        var ar = AssetDatabase.LoadAssetAtPath<RangedWeaponSO>(Items + "Weapon/AssaultRifle1.asset");
        var sniper = AssetDatabase.LoadAssetAtPath<RangedWeaponSO>(Items + "Weapon/SniperRifle1.asset");
        var pistol = AssetDatabase.LoadAssetAtPath<RangedWeaponSO>(Items + "Weapon/Pistol1.asset");
        var rarities = new System.Collections.Generic.HashSet<PlayerShopItemRarity>();
        foreach (string suffix in new[] { "Normal", "Rare", "Unique", "Legendary", "Mythic" })
        {
            var ammo = AssetDatabase.LoadAssetAtPath<BulletSO>(Items + "Bullet/InfectionAR_" + suffix + ".asset");
            Assert.NotNull(ammo, suffix);
            Assert.Greater(ammo.InfectionExposureOnHit, 0f);
            Assert.IsTrue(ar.IsCompatibleWith(ammo));
            Assert.IsFalse(sniper.IsCompatibleWith(ammo));
            Assert.IsFalse(pistol.IsCompatibleWith(ammo));
            Assert.IsTrue(rarities.Add(ammo.Rarity));
        }
        Assert.AreEqual(.2f, sniper.PenetrationClassBonus);
        Assert.AreEqual(-.2f, pistol.PenetrationClassBonus);
    }

    [Test]
    public void AuthoredPassiveLootIsExcludedAndPenetrationBarterIsQuestLocked()
    {
        var passive = AssetDatabase.LoadAssetAtPath<PassiveItemSO>("Assets/Resources/PassiveModules/Module_Penetration.asset");
        var material = AssetDatabase.LoadAssetAtPath<ItemSO>(Items + "Valuable/Gunpowder.asset");
        var tier = new LootRarityTier();
        tier.items.Add(new LootItemEntry { item = passive });
        tier.items.Add(new LootItemEntry { item = material });
        var selection = new LootCategorySelection(tier, null);
        Assert.AreSame(material, selection.Pick(new System.Random(17)).item);
        var shop = AssetDatabase.LoadAssetAtPath<ShopConfigSO>("Assets/ScriptableObject/GameScene/Shop/HomeShopConfig.asset");
        var offer = shop.Products.Single(p => p.ProductId == "passive_penetration_barter");
        Assert.AreSame(passive, offer.Item);
        Assert.IsFalse(offer.AvailableByDefault);
        Assert.That(offer.TradeRequirements.Any(r => r.Item == material && r.Amount == 2));
    }
}
