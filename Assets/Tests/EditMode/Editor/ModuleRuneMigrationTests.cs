using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class ModuleRuneMigrationTests
{
    [Test]
    public void OldItemIdentityNowResolvesToAnUnequippableValuable()
    {
        const string path = "Assets/ScriptableObject/GameScene/GameItem/PassiveItem/MagazineSmall.asset";
        var item = AssetDatabase.LoadAssetAtPath<ValuableItemSO>(path);
        Assert.NotNull(item);
        Assert.IsTrue(item.WasPassive);
        Assert.AreEqual(ItemKind.Valuable, item.Kind);
        Assert.AreEqual("military", item.ValuableCategoryId);
        Assert.AreEqual("082fcc57332c7b040aebd117a97f6a4a", AssetDatabase.AssetPathToGUID(path));
        var catalog = Resources.Load<RuntimeContentCatalogSO>("RuntimeContentCatalog");
        Assert.IsTrue(catalog.TryResolveItem(item.Guid, out var resolved));
        Assert.AreSame(item, resolved);
    }

    [TestCase(PlayerShopItemRarity.Normal, 1, "Ⅰ")]
    [TestCase(PlayerShopItemRarity.Rare, 2, "Ⅱ")]
    [TestCase(PlayerShopItemRarity.Unique, 3, "Ⅲ")]
    [TestCase(PlayerShopItemRarity.Legendary, 4, "Ⅳ")]
    [TestCase(PlayerShopItemRarity.Mythic, 5, "Ⅴ")]
    public void RuneOrderDoesNotDependOnSerializedRarityEnumOrder(PlayerShopItemRarity rarity, int tier, string numeral)
    {
        Assert.AreEqual(tier, ModuleRuneStyle.Tier(rarity));
        Assert.AreEqual(numeral, ModuleRuneStyle.Numeral(rarity));
        var frame = ModuleRuneStyle.GetFrame(rarity);
        Assert.NotNull(frame);
        Assert.AreEqual(0f, frame.texture.GetPixel(0, 0).a);
        Assert.Greater(frame.texture.GetPixel(48, 48).a, .99f);
        Assert.AreSame(frame, ModuleRuneStyle.GetFrame(rarity));
    }

    [Test]
    public void RebindingFromModuleToEmptyDoesNotLeaveARune()
    {
        var go = new GameObject("Module icon test", typeof(RectTransform), typeof(Image));
        try
        {
            var module = AssetDatabase.LoadAssetAtPath<PassiveItemSO>("Assets/Resources/PassiveModules/Module_Penetration.asset");
            Assert.NotNull(module);
            var host = go.GetComponent<Image>();
            ModuleRuneView.Bind(host, module);
            Assert.IsFalse(host.enabled);
            Assert.IsTrue(go.transform.GetChild(0).gameObject.activeSelf);
            ModuleRuneView.Bind(host, null);
            Assert.IsFalse(go.transform.GetChild(0).gameObject.activeSelf);
        }
        finally { Object.DestroyImmediate(go); }
    }
}
