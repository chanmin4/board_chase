using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class LegacyMetaCleanupTests
{
    [Test] public void OldMetaLevelsCannotProducePurchasingOptionsOrStatBonuses()
    {
        var catalog=ScriptableObject.CreateInstance<MetaUpgradeCatalogSO>();
        try
        {
            var saved=new MetaUpgradeSaveData();
            foreach(MetaUpgradeId id in Enum.GetValues(typeof(MetaUpgradeId)))saved.SetLevel(id,100);
            int modifiers=0;catalog.ApplyMetaModifiers(saved,_=>modifiers++);
            Assert.That(modifiers,Is.Zero);
            var preview=catalog.BuildSnapshot(saved,int.MaxValue);
            Assert.That(preview.currency,Is.Zero);Assert.That(preview.upgrades,Is.Empty);
        }
        finally {UnityEngine.Object.DestroyImmediate(catalog);}
    }

    [Test] public void LegacySavePayloadAndCurrentQuestRewardsSurviveSerializationTogether()
    {
        var profile=new Save();profile.MetaUpgrades.SetLevel(MetaUpgradeId.MaxHealth,4);
        profile.QuestStatRewards.Add(new QuestStatRewardSaveData {questId="compat-test",rewardIndex=0,stat=PlayerStatId.MaxHealth,modifierType=StatModifierType.FlatAdd,value=10});
        var copy=JsonUtility.FromJson<Save>(JsonUtility.ToJson(profile));
        Assert.That(copy.MetaUpgrades.GetLevel(MetaUpgradeId.MaxHealth),Is.EqualTo(4));
        Assert.That(copy.QuestStatRewards[0].questId,Is.EqualTo("compat-test"));
        Assert.That(copy.QuestStatRewards[0].value,Is.EqualTo(10));
    }

    [Test] public void CatalogKeepsItsGuidButIsNotAVisibleGameSystemToolType()
    {
        Assert.That(AssetDatabase.GUIDToAssetPath("bf6a677aafa387f459b521e906869148"),
            Is.EqualTo("Assets/Scripts/WIP/Legacy/MetaProgression/Assets/METAUpgradeCatalog.asset"));
        var supports=typeof(CharacterStatusMakerWindow).GetMethod("Supports",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That((bool)supports.Invoke(null,new object[]{typeof(MetaUpgradeCatalogSO)}),Is.False);
        Assert.That((bool)supports.Invoke(null,new object[]{typeof(PassiveModuleRulesSO)}),Is.True);
    }

    [Test] public void PassiveRecyclingAndRerollShareTheSameMaterialSource()
    {
        var rules=ScriptableObject.CreateInstance<PassiveModuleRulesSO>();
        var passive=ScriptableObject.CreateInstance<PassiveItemSO>();
        var parts=ScriptableObject.CreateInstance<ItemSO>();
        var replacement=ScriptableObject.CreateInstance<ItemSO>();
        try
        {
            rules.tiers=new[]{new PassivePartsTier {rarity=passive.Rarity,parts=parts,rerollCost=3}};
            var before=rules.RecyclingRecipeFor(passive);
            Assert.That(before.output,Is.SameAs(rules.Find(passive.Rarity).parts));Assert.That(before.seconds,Is.EqualTo(30));
            rules.tiers[0].parts=replacement;rules.recyclingSeconds=45;
            var after=rules.RecyclingRecipeFor(passive);
            Assert.That(after.output,Is.SameAs(replacement));Assert.That(after.seconds,Is.EqualTo(45));
            Assert.That(before.output,Is.SameAs(parts)); // Already registered jobs keep their original result.
            Assert.That(typeof(PassivePartsTier).GetField("dismantleAmount"),Is.Null);
        }
        finally {UnityEngine.Object.DestroyImmediate(rules);UnityEngine.Object.DestroyImmediate(passive);UnityEngine.Object.DestroyImmediate(parts);UnityEngine.Object.DestroyImmediate(replacement);}
    }
}
