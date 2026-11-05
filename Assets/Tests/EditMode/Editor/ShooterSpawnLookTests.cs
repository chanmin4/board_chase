using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>임시 preview scene/임시 asset 폴더만 사용. 실제 캐릭터 설정은 읽기만 한다.</summary>
public sealed class ShooterSpawnLookTests
{
    private readonly List<Object> _objects = new();
    private Scene _scene;
    private string _folder;
    private ShooterCustomizeSO _a, _b, _c;
    private ShooterCustomizePoolSO _pool;
    private EnemyShooterConfigSO _config;
    private ShooterVisualRig _rig;

    [SetUp] public void SetUp()
    {
        _scene = EditorSceneManager.NewPreviewScene();
        var root = ShooterCustomizeTestObjects.Create("Spawn appearance fixture", _scene);
        var body = ShooterCustomizeTestObjects.Create("Meshes", _scene); body.transform.SetParent(root.transform, false);
        var colliderRoot = ShooterCustomizeTestObjects.Create("GameplayColliderRoot", _scene); colliderRoot.transform.SetParent(root.transform, false);
        colliderRoot.AddComponent<CapsuleCollider>();
        foreach (string slot in new[] { "HairSocket", "EyesSocket", "MouthSocket", "FaceSocket", "ArmorSocket" })
        { var child = ShooterCustomizeTestObjects.Create(slot, _scene); child.transform.SetParent(root.transform, false); }
        _rig = root.AddComponent<ShooterVisualRig>(); _rig.AutoResolveReferences();
        _a = Look("A", 1); _b = Look("B", 2); _c = Look("C", 3);
        _pool = Asset<ShooterCustomizePoolSO>(); _pool.SetEntries(new[] { _a, _b, _c }, new[] { 50f, 30f, 20f });
        _config = Asset<EnemyShooterConfigSO>();
    }

    [TearDown] public void TearDown()
    {
        if (_scene.IsValid()) EditorSceneManager.ClosePreviewScene(_scene);
        foreach (var obj in _objects) if (obj != null && !EditorUtility.IsPersistent(obj)) Object.DestroyImmediate(obj);
        _objects.Clear();
        if (_folder != null) AssetDatabase.DeleteAsset(_folder);
    }

    private T Asset<T>() where T : ScriptableObject { var value = ScriptableObject.CreateInstance<T>(); _objects.Add(value); return value; }
    private ShooterCustomizeSO Look(string name, float height)
    {
        var look = Asset<ShooterCustomizeSO>(); look.name = name;
        look.Capture(null, Color.white, new ShooterVisualLocalTransform(Vector3.zero, Vector3.zero, new Vector3(1, height, 1)), null, null);
        return look;
    }
    private static void Set(Object target, string property, object value)
    {
        var so = new SerializedObject(target); var p = so.FindProperty(property);
        if (value is int number) p.intValue = number; else p.objectReferenceValue = value as Object;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    private void Bind(ShooterSpawnLookMode mode)
    {
        Set(_config, "_spawnLookMode", (int)mode); Set(_config, "_fixedSpawnLook", _a); Set(_config, "_spawnLookPool", _pool);
    }
    private void TempFolder()
    {
        if (_folder != null) return;
        _folder = "Assets/__ShooterSpawnLookTest_" + Guid.NewGuid().ToString("N");
        AssetDatabase.CreateFolder("Assets", _folder.Substring(7));
    }
    private ShooterCustomizeCatalog.Target Target(bool npc = false)
    {
        TempFolder();
        var prefab = PrefabUtility.SaveAsPrefabAsset(_rig.gameObject, _folder + "/Character.prefab");
        if (!npc) AssetDatabase.CreateAsset(_config, _folder + "/Config.asset");
        return new ShooterCustomizeCatalog.Target { Name = "Fixture", Path = _folder + (npc ? "/Character.prefab" : "/Config.asset"),
            Prefab = prefab, Visual = prefab, Config = npc ? null : _config, OutputFolderOverride = _folder + "/Looks" };
    }

    [Test] public void WeightedBoundariesAreExact()
    {
        Assert.That(_pool.Pick(0), Is.SameAs(_a)); Assert.That(_pool.Pick(.499999), Is.SameAs(_a));
        Assert.That(_pool.Pick(.5), Is.SameAs(_b)); Assert.That(_pool.Pick(.799999), Is.SameAs(_b));
        Assert.That(_pool.Pick(.8), Is.SameAs(_c)); Assert.That(_pool.Pick(1), Is.SameAs(_c));
    }
    [Test] public void WeightedDistributionMatchesAuthoredShares()
    {
        var counts = new Dictionary<ShooterCustomizeSO, int> { [_a] = 0, [_b] = 0, [_c] = 0 };
        for (int i = 0; i < 10000; i++) counts[_pool.Pick((i + .5) / 10000)]++;
        Assert.That(counts[_a], Is.EqualTo(5000)); Assert.That(counts[_b], Is.EqualTo(3000)); Assert.That(counts[_c], Is.EqualTo(2000));
    }
    [Test] public void LegacyPoolsWithoutWeightsStayUniform()
    {
        var so = new SerializedObject(_pool); so.FindProperty("_weights").ClearArray(); so.ApplyModifiedPropertiesWithoutUndo();
        Assert.That(_pool.TotalWeight, Is.EqualTo(3)); Assert.That(_pool.Pick(.4), Is.SameAs(_b));
    }
    [Test] public void EmptyNullZeroNegativeAndNonFiniteRowsNeverWin()
    {
        _pool.SetEntries(new[] { null, _a, _b, _c }, new[] { 99f, -1f, float.PositiveInfinity, 2f });
        Assert.That(_pool.Pick(0), Is.SameAs(_c)); Assert.That(_pool.Pick(1), Is.SameAs(_c));
        _pool.SetEntries(new[] { _a, _b }, new[] { 0f, float.NaN }); Assert.That(_pool.GetRandom(), Is.Null);
        _pool.SetEntries(null, null); Assert.That(_pool.GetRandom(), Is.Null);
        Assert.That(_pool.Pick(double.NaN), Is.Null);
    }
    [Test] public void SelectionDoesNotConsumeGameplayRandomness()
    {
        var before = UnityEngine.Random.state;
        try { for (int i = 0; i < 20; i++) _pool.GetRandom(); Assert.That(UnityEngine.Random.state, Is.EqualTo(before)); }
        finally { UnityEngine.Random.state = before; }
    }
    [Test] public void ExistingConfigsDefaultToUnchangedPrefab()
    { Assert.That(_config.SpawnLookMode, Is.EqualTo(ShooterSpawnLookMode.ExistingPrefab)); Assert.That(_config.PickSpawnLook(), Is.Null); }
    [Test] public void FixedConfigAlwaysReturnsItsOwnLook()
    { Bind(ShooterSpawnLookMode.Fixed); for (int i = 0; i < 20; i++) Assert.That(_config.PickSpawnLook(), Is.SameAs(_a)); }
    [Test] public void BossAndNamedCannotRandomizeEvenWithMalformedAuthoredMode()
    {
        Bind(ShooterSpawnLookMode.RandomPool);
        foreach (var role in new[] { EnemyShooterRole.Boss, EnemyShooterRole.NamedEnemy })
        { Set(_config, "_enemyRole", (int)role); Assert.That(_config.AllowsRandomSpawnLook, Is.False); Assert.That(_config.PickSpawnLook(), Is.SameAs(_a)); }
    }
    [Test] public void StandardAndGuardCanUsePools()
    {
        foreach (var role in new[] { EnemyShooterRole.StandardEnemy, EnemyShooterRole.BossGuard })
        { Set(_config, "_enemyRole", (int)role); Assert.That(_config.AllowsRandomSpawnLook, Is.True); }
    }
    [Test] public void EmptyRandomPoolPreservesPrefab()
    { Bind(ShooterSpawnLookMode.RandomPool); _pool.SetEntries(null, null); Assert.That(_config.PickSpawnLook(), Is.Null); }
    [Test] public void SameSessionDoesNotRerollOrReapply()
    {
        Bind(ShooterSpawnLookMode.RandomPool); var session = new ShooterSpawnLookSession();
        Assert.That(session.ApplyOnce(_config, _rig), Is.True); var selected = session.Selected;
        _rig.gameObject.SetActive(false); _rig.gameObject.SetActive(true);
        Assert.That(session.ApplyOnce(_config, _rig), Is.False); Assert.That(session.Selected, Is.SameAs(selected));
    }
    [Test] public void LateRigAwakeDoesNotOverwriteSelectedSpawnLook()
    {
        _rig.SetDefaultCustomize(_a); _rig.ApplyCustomize(_b);
        typeof(ShooterVisualRig).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_rig, null);
        Assert.That(_rig.BodyVisualRoot.localScale.y, Is.EqualTo(2));
    }
    [Test] public void RebindingToUnconfiguredTargetRestoresDefaultLook()
    {
        _rig.SetDefaultCustomize(_a); Bind(ShooterSpawnLookMode.Fixed); Set(_config, "_fixedSpawnLook", _c);
        var session = new ShooterSpawnLookSession(); session.ApplyOnce(_config, _rig);
        session.ApplyOnce(Asset<EnemyShooterConfigSO>(), _rig);
        Assert.That(session.Selected, Is.Null); Assert.That(_rig.BodyVisualRoot.localScale.y, Is.EqualTo(1));
    }
    [Test] public void NoConfigurationDoesNotTouchCurrentRig()
    {
        _rig.ApplyCustomize(_c); new ShooterSpawnLookSession().ApplyOnce(_config, _rig);
        Assert.That(_rig.BodyVisualRoot.localScale.y, Is.EqualTo(3));
    }
    [Test] public void ApplyingLookLeavesColliderAndEquipmentIdentityUntouched()
    {
        var collider = _rig.GetComponentInChildren<CapsuleCollider>(); var matrix = collider.transform.localToWorldMatrix;
        var equipment = ShooterCustomizeTestObjects.Create("Real equipment", _scene); equipment.transform.SetParent(_rig.ResolveSocket(ShooterVisualSocketId.Armor), false);
        Bind(ShooterSpawnLookMode.Fixed); Set(_config, "_fixedSpawnLook", _c);
        new ShooterSpawnLookSession().ApplyOnce(_config, _rig);
        Assert.That(collider.transform.localToWorldMatrix, Is.EqualTo(matrix)); Assert.That(equipment, Is.Not.Null);
        Assert.That(equipment.transform.parent, Is.SameAs(_rig.ResolveSocket(ShooterVisualSocketId.Armor)));
    }
    [Test] public void MissingRigCanBeRetriedWithoutConsumingSession()
    {
        Bind(ShooterSpawnLookMode.Fixed); var session = new ShooterSpawnLookSession();
        Assert.That(session.ApplyOnce(_config, null), Is.False); Assert.That(session.ApplyOnce(_config, _rig), Is.True);
    }
    [Test] public void CatalogGroupsActualRoleRatherThanFolderName()
    {
        Set(_config, "_enemyRole", (int)EnemyShooterRole.Boss);
        Assert.That(ShooterCustomizeCatalog.GroupFor(_config, "Assets/NormalEnemy/Raider/X.asset"), Is.EqualTo("Boss"));
        Set(_config, "_enemyRole", 0);
        Assert.That(ShooterCustomizeCatalog.GroupFor(_config, "Assets/NormalEnemy/Raider/X.asset"), Is.EqualTo("일반 적"));
    }
    [Test] public void TargetValidationRejectsFixedMultipleAndDisabledRandom()
    {
        var target = Target(true);
        Assert.That(ShooterCustomizeCatalog.Validate(target, ShooterSpawnLookMode.Fixed, new[] { _a, _b }, null), Is.Not.Null);
        Assert.That(ShooterCustomizeCatalog.Validate(target, ShooterSpawnLookMode.RandomPool, new[] { _a }, null), Is.Not.Null);
        Assert.That(ShooterCustomizeCatalog.Validate(target, ShooterSpawnLookMode.Fixed, new[] { _a }, null), Is.Null);
    }
    [Test] public void AllZeroWeightsAreBlockedBeforeSave()
    { Assert.That(ShooterCustomizeCatalog.Validate(Target(), ShooterSpawnLookMode.RandomPool, new[] { _a }, new[] { 0f }), Is.Not.Null); }
    [Test] public void CopiesHaveIndependentContentAndStableUniqueIds()
    {
        var target = Target(); var draft = Asset<ShooterCustomizeDraft>(); draft.LoadLook(target.Visual, _a, _rig);
        var one = ShooterCustomizeCatalog.CreateLook(target, draft, "Look");
        var two = ShooterCustomizeCatalog.CreateLook(target, draft, "Look");
        Assert.That(one.CustomizeId, Is.Not.EqualTo(two.CustomizeId));
        var id = one.CustomizeId; AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(one), "Renamed");
        Assert.That(one.CustomizeId, Is.EqualTo(id)); draft.tint = Color.red; draft.CaptureTo(one);
        Assert.That(two.BodyTint, Is.EqualTo(Color.white)); Assert.That(_a.BodyTint, Is.EqualTo(Color.white));
    }
    [Test] public void SavingEnemyPoolKeepsStatsAndSourcePrefabUntouched()
    {
        var target = Target(); var draft = Asset<ShooterCustomizeDraft>(); draft.LoadLook(target.Visual, _a, _rig);
        var look = ShooterCustomizeCatalog.CreateLook(target, draft, "Look");
        string prefabBefore = System.IO.File.ReadAllText(AssetDatabase.GetAssetPath(target.Prefab));
        float health = _config.MaxHealth;
        ShooterCustomizeCatalog.SaveBinding(target, ShooterSpawnLookMode.RandomPool, new[] { look }, new[] { 7f });
        Assert.That(_config.SpawnLookPool.Customizes[0], Is.SameAs(look)); Assert.That(_config.SpawnLookPool.WeightAt(0), Is.EqualTo(7));
        Assert.That(_config.MaxHealth, Is.EqualTo(health));
        Assert.That(System.IO.File.ReadAllText(AssetDatabase.GetAssetPath(target.Prefab)), Is.EqualTo(prefabBefore));
    }
    [Test] public void NpcSaveKeepsVariantParentAndBaseLookUnchanged()
    {
        var target = Target(true); var basePrefab = target.Prefab;
        string baseGuid = AssetDatabase.AssetPathToGUID(target.Path);
        var instance = PrefabUtility.InstantiatePrefab(basePrefab, _scene) as GameObject;
        var variant = PrefabUtility.SaveAsPrefabAsset(instance, _folder + "/NpcVariant.prefab");
        Object.DestroyImmediate(instance);
        target.Prefab = variant; target.Visual = variant; target.Path = _folder + "/NpcVariant.prefab";
        string variantGuid = AssetDatabase.AssetPathToGUID(target.Path);
        var draft = Asset<ShooterCustomizeDraft>(); draft.LoadLook(target.Visual, _a, _rig);
        var look = ShooterCustomizeCatalog.CreateLook(target, draft, "Fixed");
        ShooterCustomizeCatalog.SaveBinding(target, ShooterSpawnLookMode.Fixed, new[] { look }, null);
        variant = AssetDatabase.LoadAssetAtPath<GameObject>(target.Path);
        Assert.That(PrefabUtility.GetPrefabAssetType(variant), Is.EqualTo(PrefabAssetType.Variant));
        Assert.That(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(variant))), Is.EqualTo(baseGuid));
        Assert.That(variant.GetComponent<ShooterVisualRig>().CharacterCustomize, Is.SameAs(look));
        Assert.That(variant.GetComponent<ShooterVisualRig>().DefaultCustomize, Is.Null);
        Assert.That(basePrefab.GetComponent<ShooterVisualRig>().DefaultCustomize, Is.Null);
        Assert.That(AssetDatabase.AssetPathToGUID(target.Path), Is.EqualTo(variantGuid));
    }
    [Test] public void NpcRemovingOverrideDoesNotDeleteOriginalPrefabLook()
    {
        var target = Target(true); var draft = Asset<ShooterCustomizeDraft>(); draft.LoadLook(target.Visual, _a, _rig);
        var original = ShooterCustomizeCatalog.CreateLook(target, draft, "Original");
        var changed = ShooterCustomizeCatalog.CreateLook(target, draft, "Changed");
        var contents = PrefabUtility.LoadPrefabContents(target.Path);
        try { contents.GetComponent<ShooterVisualRig>().SetDefaultCustomize(original); PrefabUtility.SaveAsPrefabAsset(contents, target.Path); }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
        ShooterCustomizeCatalog.SaveBinding(target, ShooterSpawnLookMode.Fixed, new[] { changed }, null);
        ShooterCustomizeCatalog.SaveBinding(target, ShooterSpawnLookMode.ExistingPrefab, null, null);
        var savedRig = AssetDatabase.LoadAssetAtPath<GameObject>(target.Path).GetComponent<ShooterVisualRig>();
        Assert.That(savedRig.CharacterCustomize, Is.Null); Assert.That(savedRig.DefaultCustomize, Is.SameAs(original));
    }
    [Test] public void CatalogBrowsingDoesNotModifyRealAuthoringAssets()
    {
        var targets = ShooterCustomizeCatalog.Discover(); Assert.That(targets.Any(t => t.Group == "Player"), Is.True);
        Assert.That(targets.Any(t => t.Group == "NPC"), Is.True); Assert.That(targets.Any(t => t.Group == "Boss"), Is.True);
        Assert.That(targets.Any(t => t.Name.EndsWith("BasePrefab")), Is.False);
        var before = targets.ToDictionary(t => t.Path, t => System.IO.File.ReadAllText(t.Path));
        ShooterCustomizeCatalog.Discover();
        foreach (var pair in before) Assert.That(System.IO.File.ReadAllText(pair.Key), Is.EqualTo(pair.Value), pair.Key);
    }

    [Test] public void CanonicalMonstersHaveOneConfigAndRealInheritedRig()
    {
        var targets = ShooterCustomizeCatalog.Discover();
        foreach (string family in new[] { "Scraper", "Raider", "Agent", "Butcher" })
        {
            var members = targets.Where(t => t.Path.Contains("/NormalEnemy/" + family + "/")).ToArray();
            Assert.That(members.Length, Is.EqualTo(1), family);
            Assert.That(members[0].DisplayName, Is.EqualTo(family));
            Assert.That(members[0].HasVisualRig, Is.True, family + " must support actual spawning, not just preview");
            Assert.That(members[0].Prefab.GetComponentsInChildren<ShooterVisualRig>(true).Length, Is.EqualTo(1));
        }
        var common = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GameScene/Shooter/Visual_Shooter.prefab").GetComponent<ShooterVisualRig>();
        Assert.That(common, Is.Not.Null); Assert.That(common.BodyVisualRootContainsCollider, Is.False);
        Assert.That(common.DefaultCustomize, Is.Null, "Adding the rig must not apply an arbitrary default look to all characters.");
        foreach (string path in new[] { "Assets/Prefabs/GameScene/Shooter/EnemyShooter/EnemyBasePrefab.prefab", "Assets/Prefabs/GameScene/Shooter/HomeNPC/NpcBasePrefab.prefab" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            Assert.That(prefab.GetComponentsInChildren<ShooterVisualRig>(true).Length, Is.EqualTo(1), path);
        }
    }

    [Test] public void StageEquipmentUsesOneConfigAndFallsBackOutsideExplicitStages()
    {
        var normal = Asset<ShooterLoadoutTableSO>(); var stage2 = Asset<ShooterLoadoutTableSO>();
        Set(_config, "_loadoutTable", normal);
        var data = new SerializedObject(_config); var entries = data.FindProperty("_stageLoadouts"); entries.arraySize = 1;
        entries.GetArrayElementAtIndex(0).FindPropertyRelative("stage").intValue = 2;
        entries.GetArrayElementAtIndex(0).FindPropertyRelative("loadout").objectReferenceValue = stage2;
        data.ApplyModifiedPropertiesWithoutUndo();
        string before = EditorJsonUtility.ToJson(_config);
        foreach (int stage in new[] { -1, 0, 1, 3, 4, 5, 6 }) Assert.That(_config.ResolveLoadoutTable(stage), Is.SameAs(normal));
        Assert.That(_config.ResolveLoadoutTable(2), Is.SameAs(stage2));
        Assert.That(EditorJsonUtility.ToJson(_config), Is.EqualTo(before));
    }

    [Test] public void MigratedStagesUseCanonicalTargetsAndUpgradeIsRetired()
    {
        var targets = ShooterCustomizeCatalog.Discover();
        foreach (string family in new[] { "Scraper", "Raider", "Agent" })
        {
            var config = targets.Single(t => t.Name == family + "ShooterConfig").Config;
            foreach (int stage in family == "Scraper" ? new[] { 2, 3 } : family == "Raider" ? new[] { 2, 4 } : new[] { 4 })
                Assert.That(config.ResolveLoadoutTable(stage).name, Is.EqualTo(family + "Stage" + stage + "LoadoutTable"));
        }
        var raider = targets.Single(t => t.Name == "RaiderShooterConfig").Config;
        Assert.That(raider.ResolveLoadoutTable(3), Is.SameAs(raider.LoadoutTable), "Upgrade equipment must not survive as a hidden stage 3 variant.");
        Assert.That(raider.MaxHealth, Is.EqualTo(200), "Keep basic Raider, not the 300 HP Upgrade.");
        Assert.That(targets.Any(t => t.Name.Contains("RaiderUpgrade") || t.Name.Contains("Stage2ShooterConfig") || t.Name.Contains("Stage3ShooterConfig") || t.Name.Contains("Stage4ShooterConfig")), Is.False);
        // Check the current playable stage graph plus every migrated wave. Old, unreferenced
        // Virus-era fixtures (e.g. 3Roller) are not Shooter spawn data and are left untouched.
        var stagePaths = Enumerable.Range(1, 5)
            .Select(stage => $"Assets/ScriptableObject/GameScene/Stage/Stage{stage}Definition.asset").ToArray();
        foreach (string path in stagePaths) Assert.That(AssetDatabase.LoadAssetAtPath<StageDefinitionSO>(path), Is.Not.Null, path);
        var activeWavePaths = AssetDatabase.GetDependencies(stagePaths, true)
            .Where(path => AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(StageEnemyWavePresetSO)).ToArray();
        Assert.That(activeWavePaths.Length, Is.GreaterThan(0), "Current stages must resolve real spawn waves.");
        string[] migratedWaves =
        {
            "Stage2_MixedPatrol", "Stage2_MixedSquad", "Stage2_RaiderPair", "Stage2_RaiderSolo",
            "Stage3_AgentThreat", "Stage3_RaiderPair", "Stage3_RaiderSquad", "Stage3_RaiderWithScraper", "Stage3_RaiderWithScrapers",
            "Stage4_AgentLeadRaiderPair", "Stage4_AgentPair", "Stage4_AgentRaiderPatrol", "Stage4_AgentSquad"
        };
        foreach (string path in activeWavePaths.Concat(migratedWaves.Select(name =>
                     "Assets/ScriptableObject/GameScene/Stage/EnemyWavePreset/" + name + ".asset")).Distinct())
        {
            var wave = AssetDatabase.LoadAssetAtPath<StageEnemyWavePresetSO>(path);
            Assert.That(wave, Is.Not.Null, path);
            Assert.That(wave.Enemies, Is.Not.Empty, path);
            foreach (var entry in wave.Enemies)
                Assert.That(entry != null && entry.archetype != null && entry.archetype.EnemyShooterPrefab != null, Is.True, path);
        }
        var loot = AssetDatabase.LoadAssetAtPath<LootProgressionConfigSO>("Assets/ScriptableObject/GameScene/GameItem/LootProgressionConfig.asset");
        Assert.That(loot.Sources.Count(s => s.enemyShooterConfig == raider), Is.EqualTo(1));
        Assert.That(loot.Sources.Any(s => s.sourceId == "raider_upgrade"), Is.False);
    }
}
