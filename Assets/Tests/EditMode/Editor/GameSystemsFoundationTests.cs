using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class GameSystemsFoundationTests
{
    [TestCase(0,1,0)]
    [TestCase(9,1,9)]
    [TestCase(10,2,0)]
    [TestCase(31,3,1)]
    public void ExperienceCurveUsesCumulativeThresholds(long total,int level,long intoLevel)
    {
        var curve=new ExperienceRules{experiencePerLevel=new[]{10,20,30},maximumLevel=4};
        Assert.That(curve.Level(total),Is.EqualTo(level));
        Assert.That(curve.IntoLevel(total),Is.EqualTo(intoLevel));
        Assert.That(curve.Level(long.MaxValue),Is.EqualTo(4));
    }

    [TestCase(101,1f,101)]
    [TestCase(101,.5f,50)]
    [TestCase(-1,.5f,0)]
    public void SettlementFloorsOnlyAtFinalSettlement(int pending,float multiplier,int expected)
        =>Assert.That(GameProgressionRuntime.SettlementAmount(pending,multiplier),Is.EqualTo(expected));

    [Test]
    public void LevelBonusesAreDerivedRatherThanRegrantedOnRebuild()
    {
        var curve=new ExperienceRules{experiencePerLevel=new[]{1},maximumLevel=10};
        float Resolve(){float hp=0;curve.ApplyRewards(9,m=>hp+=m.value);return hp;}
        Assert.That(Resolve(),Is.EqualTo(20));
        Assert.That(Resolve(),Is.EqualTo(20));
    }

    [Test]
    public void IndependentAssetsContainRecipesAndStageOwnedReplacement()
    {
        var rules=AssetDatabase.LoadAssetAtPath<RecyclingRulesSO>("Assets/Resources/RecyclingRules.asset");
        Assert.That(rules,Is.Not.Null);
        Assert.That(rules.recycling.baseSlots,Is.EqualTo(2));
        Assert.That(rules.recycling.recipes.Length,Is.EqualTo(10));
        foreach(var recipe in rules.recycling.recipes)
        {
            Assert.That(recipe.kind,Is.Not.EqualTo(ItemKind.Passive));
            Assert.That(recipe.output,Is.Not.Null);
            Assert.That(recipe.seconds,Is.EqualTo(recipe.kind==ItemKind.Weapon?120:recipe.kind==ItemKind.Armor?60:30));
        }
        var stage=AssetDatabase.LoadAssetAtPath<StageDefinitionSO>("Assets/ScriptableObject/GameScene/Stage/Stage3Definition.asset");
        Assert.That(stage.EncounterRules.replacementEnemy,Is.Not.Null);
    }

    [Test]
    public void DifferentEffectGroupsCanSharePenaltyStats()
    {
        var armor=AssetDatabase.LoadAssetAtPath<PassiveItemSO>("Assets/Resources/PassiveModules/Module_Armor.asset");
        var speed=AssetDatabase.LoadAssetAtPath<PassiveItemSO>("Assets/Resources/PassiveModules/Module_MoveSpeed.asset");
        Assert.That(armor,Is.Not.Null);Assert.That(speed,Is.Not.Null);
        Assert.That(armor.SharesEffectWith(speed),Is.False);
        Assert.That(armor.SharesEffectWith(armor),Is.True);
    }

    [Test]
    public void FixedModulesAreValidCreationSourcesButNotRerollable()
    {
        foreach(string name in new[]{"Dash","Empty"})
        {
            var item=AssetDatabase.LoadAssetAtPath<PassiveItemSO>($"Assets/Resources/PassiveModules/Module_{name}.asset");
            Assert.That(item,Is.Not.Null);
            Assert.That(PassiveInstanceRuntime.CanCreate(item,out _),Is.True);
            Assert.That(item.AllowNumericReroll,Is.False);
        }
    }

    [Test]
    public void BossProtectionChecksReciprocalLinksAndRealExitCoordinates()
    {
        var map=new StageMapLayout{startSectorCoord=Vector2Int.zero};
        map.rooms.Add(new(Vector2Int.zero,StageRoomType.Normal,StageRoomConnectionFlags.Right));
        map.rooms.Add(new(Vector2Int.right,StageRoomType.Normal,StageRoomConnectionFlags.Left|StageRoomConnectionFlags.Right|StageRoomConnectionFlags.Up));
        map.rooms.Add(new(new(2,0),StageRoomType.Normal,StageRoomConnectionFlags.Left));
        map.rooms.Add(new(new(1,1),StageRoomType.Boss,StageRoomConnectionFlags.Down));
        map.extractionRooms.Add(new(2,0));
        Assert.That(StageSystemPolicies.ExitsReachableWithout(map,new(1,1)),Is.True);
        Assert.That(StageSystemPolicies.ExitsReachableWithout(map,new(1,0)),Is.False);
        map.rooms[2].connections=StageRoomConnectionFlags.None;
        Assert.That(StageSystemPolicies.ExitsReachableWithout(map,new(1,1)),Is.False);
    }
}
