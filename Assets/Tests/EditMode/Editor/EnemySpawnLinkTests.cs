using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>Read-only checks. Authored spawn links must work without startup repair scripts.</summary>
public sealed class EnemySpawnLinkTests
{
    private const string ConfigRoot = "Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/";
    private const string PrefabRoot = "Assets/Prefabs/GameScene/Shooter/EnemyShooter/";
    private const string StageRoot = "Assets/ScriptableObject/GameScene/Stage/";

    [TestCase("Boss/Stage1RevolverBossConfig", "Boss/Stage1RevolverBoss")]
    [TestCase("Boss/PunisherConfig", "Boss/Punisher")]
    [TestCase("Boss/Stage2SniperBossConfig", "Boss/Stage2UniqueSniperBoss")]
    [TestCase("Boss/Stage3RocketBossConfig", "Boss/Stage3RocketBoss")]
    [TestCase("Boss/Stage4LMGBossConfig", "Boss/Stage4LMGBoss")]
    [TestCase("Boss/Stage5BossTemplateConfig", "Boss/Stage5BossTemplate")]
    [TestCase("NormalEnemy/Scraper/ScraperShooterConfig", "Scraper/Scraper")]
    [TestCase("NormalEnemy/Scraper/ScraperStage2ShooterConfig", "Scraper/Scraper")]
    [TestCase("NormalEnemy/Scraper/ScraperStage3ShooterConfig", "Scraper/Scraper")]
    public void AuthoredConfigResolvesTheExpectedEnemyComponent(string configName, string prefabName)
    {
        var config = AssetDatabase.LoadAssetAtPath<EnemyShooterConfigSO>(ConfigRoot + configName + ".asset");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + prefabName + ".prefab");
        Assert.That(config, Is.Not.Null, configName);
        Assert.That(prefab, Is.Not.Null, prefabName);
        var shooter = prefab.GetComponentInChildren<EnemyShooter>(true);
        Assert.That(shooter, Is.Not.Null, prefabName);
        Assert.That(config.EnemyShooterPrefab, Is.EqualTo(shooter), configName);
        Assert.That(config.IsValid, Is.True, configName);
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    public void StageBossBattleCanBePickedAndEveryEnabledSpawnIsValid(int stageNumber)
    {
        var stage = AssetDatabase.LoadAssetAtPath<StageDefinitionSO>(StageRoot + $"Stage{stageNumber}Definition.asset");
        Assert.That(stage, Is.Not.Null);
        Assert.That(stage.TryPickBossBattle(12345, new Vector2Int(3, 3), out var encounter, out var sector), Is.True, stage.name);
        Assert.That(sector.IsValid, Is.True, sector.name);
        Assert.That(encounter.IsValid, Is.True, encounter.name);
        foreach (var wave in encounter.Waves)
        foreach (var candidate in wave.enemyWavePresetCandidates.Where(c => c != null && c.weight > 0))
        {
            Assert.That(candidate.preset, Is.Not.Null, encounter.name);
            Assert.That(candidate.preset.Enemies, Is.Not.Empty, candidate.preset.name);
            foreach (var spawn in candidate.preset.Enemies)
            {
                Assert.That(spawn, Is.Not.Null, candidate.preset.name);
                Assert.That(spawn.archetype, Is.Not.Null, candidate.preset.name);
                Assert.That(spawn.archetype.EnemyShooterPrefab, Is.Not.Null, spawn.archetype.name);
                Assert.That(spawn.archetype.IsValid, Is.True, spawn.archetype.name);
                Assert.That(spawn.count, Is.GreaterThan(0), candidate.preset.name);
            }
        }
    }

    [Test]
    public void StageFiveEncounterAndQuestsUseTheSameAuthoredBossTemplate()
    {
        var encounter = AssetDatabase.LoadAssetAtPath<StageBattleEncounterPresetSO>(StageRoot + "EnemyWavePreset/Boss/Stage5TempBossEncounter.asset");
        var expectedWave = AssetDatabase.LoadAssetAtPath<StageEnemyWavePresetSO>(StageRoot + "EnemyWavePreset/Boss/Stage5BossWaveTemplate.asset");
        var boss = AssetDatabase.LoadAssetAtPath<EnemyShooterConfigSO>(ConfigRoot + "Boss/Stage5BossTemplateConfig.asset");
        Assert.That(encounter, Is.Not.Null);
        Assert.That(expectedWave, Is.Not.Null);
        Assert.That(boss, Is.Not.Null);
        Assert.That(encounter.TryPickEnemyWavePreset(0, 12345, out var wave), Is.True);
        Assert.That(wave, Is.EqualTo(expectedWave));
        Assert.That(wave.Enemies.Any(e => e != null && e.archetype == boss && e.count > 0), Is.True);
        foreach (string name in new[] { "RunMain_05_Stage5_Boss", "Campaign_20_Stage5_Template" })
        {
            var quest = AssetDatabase.LoadAssetAtPath<QuestDefinitionSO>("Assets/ScriptableObject/GameScene/Quest/" + name + ".asset");
            Assert.That(quest, Is.Not.Null, name);
            Assert.That(quest.Objectives.Any(o => o.TargetConfig == boss), Is.True, name);
        }
    }

    [Test]
    public void CompletedOneShotRepairScriptsAreNotInstalled()
    {
        foreach (string name in new[] { "BossConfigPrefabLinkProvisioner", "StandardEnemyConfigPrefabLinkRepair" })
            Assert.That(File.Exists("Assets/Scripts/Authoring/Editor/" + name + ".cs"), Is.False, name);
    }
}
