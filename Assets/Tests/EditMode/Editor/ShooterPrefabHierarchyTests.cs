using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>Read-only authoring checks: character prefab inheritance must not depend on a specific enemy.</summary>
public sealed class ShooterPrefabHierarchyTests
{
    private const string Root = "Assets/Prefabs/GameScene/Shooter/";
    private const string EnemyBase = Root + "EnemyShooter/EnemyBasePrefab.prefab";
    private const string NpcBase = Root + "HomeNPC/NpcBasePrefab.prefab";
    private const string Visual = Root + "Visual_Shooter.prefab";

    private static GameObject Load(string path)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Assert.That(prefab, Is.Not.Null, path);
        return prefab;
    }

    private static string Parent(GameObject prefab) =>
        AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(prefab));

    private static Object Reference(Component component, string property)
    {
        Assert.That(component, Is.Not.Null, property);
        using var serialized = new SerializedObject(component);
        var field = serialized.FindProperty(property);
        Assert.That(field, Is.Not.Null, property);
        return field.objectReferenceValue;
    }

    [Test]
    public void EnemyBaseUsesVisualShooterDirectlyAndRetainsItsAssetIdentity()
    {
        var prefab = Load(EnemyBase);
        Assert.That(PrefabUtility.GetPrefabAssetType(prefab), Is.EqualTo(PrefabAssetType.Regular));
        Assert.That(AssetDatabase.AssetPathToGUID(EnemyBase), Is.EqualTo("a605ddf42f7592244b0708ea97b2fd1e"));
        var visual = prefab.transform.Find("Visual/Visual_Shooter");
        Assert.That(visual, Is.Not.Null);
        Assert.That(Parent(visual.gameObject), Is.EqualTo(Visual));
        Assert.That(prefab.GetComponent<EnemyShooter>(), Is.Not.Null);
        Assert.That(prefab.GetComponentInChildren<RaiderEffectController>(true), Is.Null);
    }

    [Test]
    public void EveryEnemyAndBossIsADirectVariantOfEnemyBase()
    {
        int count = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { Root + "EnemyShooter" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = Load(path);
            if (path == EnemyBase || prefab.GetComponent<EnemyShooter>() == null) continue;
            Assert.That(Parent(prefab), Is.EqualTo(EnemyBase), path);
            Assert.That(prefab.name, Is.EqualTo(Path.GetFileNameWithoutExtension(path)), path);
            count++;
        }
        Assert.That(count, Is.GreaterThanOrEqualTo(16));
    }

    [Test]
    public void SharedEnemySocketsEffectsAndDeathVisualAreConnected()
    {
        var prefab = Load(EnemyBase);
        var sockets = prefab.transform.Find("Visual/Visual_Shooter/Root/Socket");
        var equipment = prefab.GetComponentInChildren<ShooterEquipmentController>(true);
        Assert.That(Reference(equipment, "_weaponRoot"), Is.EqualTo(sockets.Find("RightHand_WeaponSocket")));
        Assert.That(Reference(equipment, "_armorVisualRoot"), Is.EqualTo(sockets.Find("ArmorSocket")));
        Assert.That(Reference(prefab.GetComponentInChildren<EntityEquipmentRuntime>(true), "_armorVisualRoot"),
            Is.EqualTo(sockets.Find("ArmorSocket")));

        var effects = prefab.GetComponentInChildren<EnemyShooterEffectController>(true);
        foreach (string field in new[] { "_footTrailParticles", "_deathParticles", "_dashParticles", "_damageable" })
            Assert.That(Reference(effects, field), Is.Not.Null, field);
        Assert.That(Reference(prefab.GetComponent<EnemyShooter>(), "_effects"), Is.EqualTo(effects));

        var corpse = prefab.GetComponentInChildren<ShooterDeathCorpseController>(true);
        var alive = Reference(corpse, "_aliveVisualRoot") as GameObject;
        var dead = Reference(corpse, "_corpseVisualRoot") as GameObject;
        Assert.That(alive, Is.EqualTo(prefab.transform.Find("Visual/Visual_Shooter").gameObject));
        Assert.That(dead, Is.Not.Null);
        Assert.That(dead.transform.IsChildOf(alive.transform), Is.False, "Disabling the live model must not hide the corpse.");
    }

    [Test]
    public void NpcBaseInheritsVisualShooterWithoutEnemyCombatOrAuthoredQuestOffers()
    {
        var prefab = Load(NpcBase);
        Assert.That(Parent(prefab), Is.EqualTo(Visual));
        Assert.That(prefab.GetComponent<HomeQuestNpc>(), Is.Not.Null);
        Assert.That(prefab.GetComponent<EnemyShooter>(), Is.Null);
        Assert.That(prefab.GetComponent<HomeQuestNpc>().OfferedQuests, Is.Empty);
        Assert.That(Reference(prefab.GetComponent<HomeQuestNpc>(), "_conversationAudio"), Is.Not.Null);
    }

    [TestCase("HomeQuestNpc", 0, "Main NPC", "a4f1c2bc11574914a8dac1fb1d848fa9")]
    [TestCase("HomeItemNpc", 1, "Item NPC", "80b61f6ea6d449e4af9680d41a630142")]
    [TestCase("HomeStatsNpc", 2, "Stats NPC", "2146c4b1327c4bb1a19b49757d9780e2")]
    public void NpcVariantsKeepTheirRoleIdentityAndQuestOffers(string name, int role, string display, string guid)
    {
        var prefab = Load(Root + "HomeNPC/" + name + ".prefab");
        var npc = prefab.GetComponent<HomeQuestNpc>();
        Assert.That(Parent(prefab), Is.EqualTo(NpcBase));
        Assert.That((int)npc.Role, Is.EqualTo(role));
        Assert.That(npc.DisplayName, Is.EqualTo(display));
        Assert.That(npc.OfferedQuests.Length, Is.GreaterThan(0));
        Assert.That(npc.OfferedQuests.All(q => q != null), Is.True);
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(npc, out string actualGuid, out long fileId);
        Assert.That(actualGuid, Is.EqualTo(guid));
        Assert.That(fileId, Is.EqualTo(4428478864266524587L));
    }

    [Test]
    public void MigratedEnemyConfigsResolveTheirSpawnComponent()
    {
        const string configs = "Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/";
        foreach (string path in new[] { "Boss/Stage5BossTemplateConfig.asset", "BossGuard/Stage1BossGuardConfig.asset",
            "BossGuard/Stage2BossGuardConfig.asset", "BossGuard/Stage3BossGuardConfig.asset",
            "NormalEnemy/Butcher/ButcherShooterConfig.asset" })
        {
            var config = AssetDatabase.LoadAssetAtPath<EnemyShooterConfigSO>(configs + path);
            Assert.That(config, Is.Not.Null, path);
            Assert.That(config.EnemyShooterPrefab, Is.Not.Null, path);
            Assert.That(Parent(config.EnemyShooterPrefab.gameObject), Is.EqualTo(EnemyBase), path);
        }
    }

    [Test]
    public void CharacterPrefabsHaveNoMissingScriptsOrOverrideTargets()
    {
        foreach (var folder in new[] { "EnemyShooter", "HomeNPC", "Player" })
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { Root + folder }))
        {
            var prefab = Load(AssetDatabase.GUIDToAssetPath(guid));
            foreach (var child in prefab.GetComponentsInChildren<Transform>(true))
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject),
                    Is.Zero, prefab.name + "/" + child.name);
            var changes = PrefabUtility.GetPropertyModifications(prefab);
            if (changes != null)
                Assert.That(changes.All(c => c.target != null), Is.True, prefab.name);
        }
    }
}
