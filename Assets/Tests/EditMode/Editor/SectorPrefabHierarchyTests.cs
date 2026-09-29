using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class SectorPrefabHierarchyTests
{
    private const string Root = "Assets/Prefabs/GameScene/Sector/";
    private const string Presets = "Assets/ScriptableObject/GameScene/Stage/Sector/Preset/";
    private static GameObject Load(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);
    private static string Parent(GameObject prefab) => AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(prefab));
    private static SectorPresetSO Preset(string name) => AssetDatabase.LoadAssetAtPath<SectorPresetSO>(Presets + "RaidRooms/" + name + "/SectorPreset_" + name + "_01.asset");

    [Test]
    public void FamilyBasesInheritCommonFoundationAndHomeRemainsSeparate()
    {
        Assert.That(AssetDatabase.AssetPathToGUID(Root + "Bases/Sector_Base.prefab"), Is.EqualTo("483ae65600a15d5428514f0fc8d5b096"));
        foreach (string name in new[] { "Normal", "Boss", "Armory", "Medical", "Treasure", "Generator", "Hatch", "Extraction" })
        {
            var prefab = Load(Root + "Bases/" + name + "SectorBase.prefab");
            Assert.That(prefab, Is.Not.Null, name);
            Assert.That(Parent(prefab), Is.EqualTo(Root + "Bases/Sector_Base.prefab"), name);
            var runtime = prefab.GetComponent<SectorRuntime>();
            Assert.That(runtime, Is.Not.Null, name);
            Assert.That(runtime.EnemySpawnPointRoot, Is.Not.Null, name);
            Assert.That(runtime.PlayerStartPoint, Is.Not.Null, name);
        }
        var home = Load(Root + "HomeSector.prefab");
        Assert.That(home, Is.Not.Null);
        Assert.That(Parent(home), Is.EqualTo(Root + "Bases/Sector_Base.prefab"));
        Assert.That(AssetDatabase.AssetPathToGUID(Root + "HomeSector.prefab"), Is.EqualTo("f6b5c4d3e2a14987b6c5d4e3f2a1b098"));
    }

    [Test]
    public void AuthoredVariantsHaveTheRightFamilySpawnPointsAndNoMissingScripts()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { Root + "Rooms" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = Load(path);
            string family = path.Substring((Root + "Rooms/").Length).Split('/')[0];
            Assert.That(Parent(prefab), Is.EqualTo(Root + "Bases/" + (family == "Start" ? "Sector_Base" : family + "SectorBase") + ".prefab"), path);
            var runtime = prefab.GetComponent<SectorRuntime>();
            Assert.That(runtime, Is.Not.Null, path);
            if (family != "Start")
            {
                // Prefab assets are not active scene objects; EnabledForSpawning also checks
                // activeInHierarchy, so inspect authored markers instead of the runtime filter.
                var spawns = runtime.EnemySpawnPointRoot.GetComponentsInChildren<EnemySpawnPoint>(true);
                Assert.That(spawns, Is.Not.Empty, path);
                Assert.That(spawns.Any(point => point.gameObject.activeSelf &&
                    new SerializedObject(point).FindProperty("_enabledForSpawning").boolValue), Is.True, path);
            }
            foreach (var t in prefab.GetComponentsInChildren<Transform>(true))
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject), Is.Zero, path + "/" + t.name);
            Assert.That(PrefabUtility.GetPropertyModifications(prefab).Count(m => m.target == null), Is.Zero, path);
        }
    }

    [Test]
    public void FiveStagesUseSpecialRoomPoolsAndDedicatedBossVariants()
    {
        var roles = new[] { RaidRoomRole.Armory, RaidRoomRole.Medical, RaidRoomRole.Vault, RaidRoomRole.Generator, RaidRoomRole.Hatch, RaidRoomRole.Extraction };
        for (int n = 1; n <= 5; n++)
        {
            var stage = AssetDatabase.LoadAssetAtPath<StageDefinitionSO>($"Assets/ScriptableObject/GameScene/Stage/Stage{n}Definition.asset");
            foreach (var role in roles)
            {
                var binding = stage.RaidRuntime.FindRoom(role);
                Assert.That(binding, Is.Not.Null, $"{n}/{role}");
                Assert.That(binding.TryPickSectorPreset(123, new Vector2Int(2, 3), out var preset), Is.True);
                string family = role == RaidRoomRole.Vault ? "Treasure" : role.ToString();
                Assert.That(Parent(preset.SectorPrefab.gameObject), Is.EqualTo(Root + "Bases/" + family + "SectorBase.prefab"));
            }
            var boss = AssetDatabase.LoadAssetAtPath<SectorPresetSO>($"{Presets}BossPreset/SectorPreset_Boss_Stage{n}.asset");
            Assert.That(Parent(boss.SectorPrefab.gameObject), Is.EqualTo(Root + "Bases/BossSectorBase.prefab"));
        }
    }

    [Test]
    public void VariantPickerIsDeterministicWeightedAndDoesNotConsumeGlobalRandom()
    {
        var a = Preset("Medical"); var b = Preset("Armory");
        Assert.That(a, Is.Not.Null); Assert.That(b, Is.Not.Null);
        var binding = new RaidRoomPresetBinding { role = RaidRoomRole.Medical, sectorVariants = new()
        {
            new() { preset = a, weight = 1 }, new() { preset = b, weight = 3 }
        } };
        var state = Random.state;
        int count = 0;
        for (int seed = 0; seed < 1000; seed++)
        {
            Assert.That(binding.TryPickSectorPreset(seed, new Vector2Int(1, 4), out var first), Is.True);
            Assert.That(binding.TryPickSectorPreset(seed, new Vector2Int(1, 4), out var second), Is.True);
            Assert.That(second, Is.SameAs(first));
            if (first == b) count++;
        }
        Assert.That(count, Is.InRange(650, 850));
        Assert.That(JsonUtility.ToJson(Random.state), Is.EqualTo(JsonUtility.ToJson(state)));
    }

    [Test]
    public void InvalidVariantsUseLegacyFallbackOrLeaveNormalPoolUntouched()
    {
        var valid = Preset("Medical");
        var boss = AssetDatabase.LoadAssetAtPath<SectorPresetSO>(Presets + "BossPreset/SectorPreset_Boss_Stage1.asset");
        var binding = new RaidRoomPresetBinding { sectorPreset = valid, sectorVariants = new()
        {
            null, new() { preset = null, weight = 1 }, new() { preset = valid, weight = 0 },
            new() { preset = valid, weight = -1 }, new() { preset = boss, weight = 100 }
        } };
        Assert.That(binding.TryPickSectorPreset(1, Vector2Int.zero, out var picked), Is.True);
        Assert.That(picked, Is.SameAs(valid));
        binding.sectorPreset = null;
        Assert.That(binding.TryPickSectorPreset(1, Vector2Int.zero, out picked), Is.False);
        Assert.That(picked, Is.Null);
        binding.sectorVariants = null;
        Assert.That(binding.TryPickSectorPreset(1, Vector2Int.zero, out _), Is.False);
    }
}
