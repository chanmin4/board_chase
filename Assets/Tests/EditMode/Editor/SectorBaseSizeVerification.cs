using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

/// <summary>
/// Explicit batch authoring/verification of new empty normal-sector size variants.
/// Does not change existing bases, Stage connections, materials, scenes or saves.
/// Geometry and authored anchors scale; actor/projectile/drop parents stay unit scale.
/// </summary>
public static class SectorBaseSizeVerification
{
    private const string Source = "Assets/Prefabs/GameScene/Sector/Bases/NormalSectorBase.prefab";
    private const string Half = "Assets/Prefabs/GameScene/Sector/Bases/NormalSectorBase_HalfSize.prefab";
    private const string Quarter = "Assets/Prefabs/GameScene/Sector/Bases/NormalSectorBase_QuarterSize.prefab";
    private const string Pending = "SectorBaseSizeVerification.Pending";
    private static Dictionary<string, string> _originalHashes;

    // This is deliberately never an InitializeOnLoad authoring task or a menu auto-action.
    public static void CreateAndVerify()
    {
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Close the editor and use the explicit batch entry point.");
        try
        {
            _originalHashes = CaptureOriginals();
            CreateVariant(Half, .5f);
            CreateVariant(Quarter, .25f);
            VerifyAuthored(Half, .5f);
            VerifyAuthored(Quarter, .25f);
            VerifyOriginals();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SessionState.SetBool(Pending, true);
            EditorApplication.EnterPlaymode();
        }
        catch (Exception ex) { Fail(ex); }
    }

    private static void CreateVariant(string destination, float scale)
    {
        Assert.IsFalse(File.Exists(destination), "Existing user files must never be overwritten: " + destination);
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Source);
        Assert.NotNull(source);
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source, scene);
            Assert.AreEqual(Vector3.one, instance.transform.localScale);
            instance.name = Path.GetFileNameWithoutExtension(destination);
            var visual = instance.transform.Find("SectorVisual");
            var anchors = instance.transform.Find("TransformPoint");
            Assert.NotNull(visual); Assert.NotNull(anchors);
            foreach (Transform part in visual)
            {
                // A Light's range/intensity is world-space, not proportional to Transform scale.
                // Keep its height and settings, but recenter X/Z above the smaller floor.
                bool lighting = part.GetComponentInChildren<Light>(true) != null;
                var position = part.localPosition;
                part.localPosition = lighting
                    ? new Vector3(position.x * scale, position.y, position.z * scale)
                    : position * scale;
                if (!lighting) part.localScale *= scale;
                PrefabUtility.RecordPrefabInstancePropertyModifications(part);
            }
            anchors.localPosition *= scale;
            anchors.localScale *= scale;
            PrefabUtility.RecordPrefabInstancePropertyModifications(anchors);
            foreach (var surface in instance.GetComponentsInChildren<NavMeshSurface>(true))
            {
                // A pre-baked NavMesh ignores scaling. The existing Stage builder rebuilds
                // a unit-scale unified surface from the resized physical floor/walls.
                surface.RemoveData(); surface.navMeshData = null;
                PrefabUtility.RecordPrefabInstancePropertyModifications(surface);
            }
            PrefabUtility.SaveAsPrefabAsset(instance, destination, out bool saved);
            Assert.IsTrue(saved, destination);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
        AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceSynchronousImport);
    }

    private static void VerifyAuthored(string path, float scale)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Source);
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Assert.NotNull(asset); Assert.AreEqual(PrefabAssetType.Variant, PrefabUtility.GetPrefabAssetType(asset));
        Assert.AreEqual(Source, AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(asset)));
        Assert.AreEqual(Vector3.one, asset.transform.localScale);
        Assert.AreEqual(source.GetComponentsInChildren<Component>(true).Length, asset.GetComponentsInChildren<Component>(true).Length);
        Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(asset));
        foreach (var surface in asset.GetComponentsInChildren<NavMeshSurface>(true)) Assert.IsNull(surface.navMeshData);
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var original = ((GameObject)PrefabUtility.InstantiatePrefab(source, scene)).GetComponent<SectorRuntime>();
            var resized = ((GameObject)PrefabUtility.InstantiatePrefab(asset, scene)).GetComponent<SectorRuntime>();
            Physics.SyncTransforms();
            var originalBounds = original.GetWorldBounds(); var bounds = resized.GetWorldBounds();
            Assert.Greater(originalBounds.size.x, 0); Assert.Greater(originalBounds.size.z, 0);
            Assert.That(bounds.size.x, Is.EqualTo(originalBounds.size.x * scale).Within(.001f));
            Assert.That(bounds.size.z, Is.EqualTo(originalBounds.size.z * scale).Within(.001f));
            Assert.That(Vector3.Distance(resized.PlayerStartPoint.position, original.PlayerStartPoint.position * scale), Is.LessThan(.001f));
            var a = original.GetComponentsInChildren<SectorPortal>(true);
            var b = resized.GetComponentsInChildren<SectorPortal>(true);
            Assert.AreEqual(4, b.Length); Assert.AreEqual(a.Length, b.Length);
            for (int i = 0; i < a.Length; i++)
                Assert.That(Vector3.Distance(b[i].ArrivalPosition, a[i].ArrivalPosition * scale), Is.LessThan(.001f));
            Assert.NotNull(resized.WorldItemContainer); Assert.AreEqual(Vector3.one, resized.WorldItemContainer.lossyScale);
            var serializedSpawner = new SerializedObject(resized.GetComponentInChildren<SectorEnemySpawner>(true));
            foreach (string field in new[] { "_spawnedEnemiesRoot", "_projectileRoot" })
                Assert.AreEqual(Vector3.one, ((Transform)serializedSpawner.FindProperty(field).objectReferenceValue).lossyScale);
            var originalMaterials = original.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).ToArray();
            var materials = resized.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).ToArray();
            CollectionAssert.AreEqual(originalMaterials, materials);
            var originalLight = original.GetComponentInChildren<Light>(true);
            var light = resized.GetComponentInChildren<Light>(true);
            Assert.AreEqual(originalLight.range, light.range); Assert.AreEqual(originalLight.intensity, light.intensity);
            Assert.AreEqual(originalLight.transform.position.y, light.transform.position.y);
            Debug.Log($"[Sector size PASS] {asset.name}: {bounds.size.x} x {bounds.size.z}, spawn={resized.PlayerStartPoint.position}, unit actor/drop/projectile parents, materials unchanged.");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    [InitializeOnLoadMethod] private static void Register()
    { EditorApplication.update -= VerifyInPlay; EditorApplication.update += VerifyInPlay; }

    private static void VerifyInPlay()
    {
        if (!Application.isBatchMode || !SessionState.GetBool(Pending, false) || !EditorApplication.isPlaying) return;
        SessionState.SetBool(Pending, false);
        var temporary = new List<Object>();
        try
        {
            Assert.IsNull(SaveSystem.Active);
            var host = new GameObject("Temporary sector size verification"); temporary.Add(host);
            var manager = host.AddComponent<SectorStateManager>();
            var portals = host.AddComponent<SectorPortalManager>();
            Set(portals, "_sectorStateManager", manager);
            var builder = host.AddComponent<StageSectorInstantiator>();
            var sectors = (List<SectorRuntime>)typeof(StageSectorInstantiator).GetField("_generatedSectors", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(builder);
            foreach (string path in new[] { Half, Quarter })
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var instance = Object.Instantiate(asset, new Vector3(sectors.Count * 500, 0, 0), Quaternion.identity, host.transform);
                foreach (var spawner in instance.GetComponentsInChildren<SectorEnemySpawner>(true)) spawner.enabled = false;
                var sector = instance.GetComponent<SectorRuntime>();
                sector.SetRuntimeInfo(new Vector2Int(sectors.Count, 0), true, false); sectors.Add(sector);
            }
            var layout = new StageMapLayout { gridSize = 2, startSectorCoord = Vector2Int.zero, goalRoomCoord = new Vector2Int(99, 99) };
            foreach (var sector in sectors) layout.rooms.Add(new StageRoomNode(sector.Coord, StageRoomType.Normal, StageRoomConnectionFlags.None));
            manager.RegisterGeneratedStageMap(layout, sectors);
            foreach (var sector in sectors) { sector.SetOpened(true); sector.SetCleared(true); }
            Physics.SyncTransforms();
            typeof(StageSectorInstantiator).GetMethod("RebuildGeneratedSectorNavMeshes", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(builder, null);
            foreach (var sector in sectors)
            {
                Assert.NotNull(sector.GetComponent<NavMeshSurface>().navMeshData);
                Assert.IsTrue(NavMesh.SamplePosition(sector.PlayerStartPoint.position, out var spawn, 2f, NavMesh.AllAreas), "Spawn not on the resized NavMesh: " + sector.name);
                foreach (var portal in sector.GetComponentsInChildren<SectorPortal>(true))
                {
                    Assert.IsTrue(NavMesh.SamplePosition(portal.ArrivalPosition, out var arrival, 2f, NavMesh.AllAreas), "Portal arrival not on resized NavMesh: " + sector.name);
                    var route = new NavMeshPath();
                    Assert.IsTrue(NavMesh.CalculatePath(spawn.position, arrival.position, NavMesh.AllAreas, route));
                    Assert.AreEqual(NavMeshPathStatus.PathComplete, route.status);
                }
                var agentGo = new GameObject("Unit size temporary agent"); agentGo.transform.SetParent(sector.WorldItemContainer, false);
                agentGo.transform.position = spawn.position; var agent = agentGo.AddComponent<NavMeshAgent>();
                Assert.AreEqual(Vector3.one, agentGo.transform.lossyScale); Assert.IsTrue(agent.isOnNavMesh);
                Debug.Log($"[Sector size Play PASS] {sector.name}: unified NavMesh, full-size agent, spawn-to-all-four-portals paths complete.");
            }
            var channel = ScriptableObject.CreateInstance<SectorRuntimeEventChannelSO>(); temporary.Add(channel);
            channel.OnEventRaised += sector => manager.SetPlayerCurrentSector(sector, false);
            Set(portals, "_currentSectorChangedEvent", channel);
            var from = sectors[0].XMax.portal.GetComponentInChildren<SectorPortal>(true);
            var to = sectors[1].XMin.portal.GetComponentInChildren<SectorPortal>(true);
            Assert.NotNull(from); Assert.NotNull(to);
            from.Initialize(portals, sectors[0], SectorPortalDirection.XMax); from.SetLink(sectors[1], to); from.SetAvailable(true);
            to.Initialize(portals, sectors[1], SectorPortalDirection.XMin); to.SetLink(sectors[0], from); to.SetAvailable(true);
            manager.SetPlayerCurrentSector(sectors[0], false);
            var actor = new GameObject("Unit size temporary portal actor"); temporary.Add(actor);
            actor.AddComponent<CharacterController>(); actor.transform.position = from.ArrivalPosition;
            Assert.IsTrue(portals.TryMoveThroughPortal(from, actor.transform));
            Assert.AreSame(sectors[1].transform, actor.transform.parent); Assert.AreSame(sectors[1], manager.CurrentSector);
            Assert.AreEqual(Vector3.one, actor.transform.lossyScale); Assert.IsTrue(actor.GetComponent<CharacterController>().enabled);
            Assert.That(Vector3.Distance(to.ArrivalPosition, actor.transform.position), Is.LessThan(.001f));
            Debug.Log("[Sector size verification] PASSED: authored half/quarter variants, runtime NavMesh/pathfinding, cross-size portal travel; no original asset, working scene, Stage connection or save changed.");
            EditorApplication.Exit(0);
        }
        catch (Exception ex) { Fail(ex); }
        finally { for (int i = temporary.Count - 1; i >= 0; i--) if (temporary[i] != null) Object.DestroyImmediate(temporary[i]); }
    }

    private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static string Hash(string path)
    { using var algorithm = SHA256.Create(); return Convert.ToBase64String(algorithm.ComputeHash(File.ReadAllBytes(path))); }
    private static Dictionary<string, string> CaptureOriginals()
    {
        var files = AssetDatabase.GetDependencies(Source, true).Where(p => p.EndsWith(".prefab"));
        files = files.Concat(Directory.GetFiles("Assets/ScriptableObject/GameScene/Stage", "Stage*Definition.asset"))
            .Concat(new[] { "Assets/Scenes/RUNScene.unity" });
        return files.Distinct().ToDictionary(p => p, Hash);
    }
    private static void VerifyOriginals()
    { foreach (var entry in _originalHashes) Assert.AreEqual(entry.Value, Hash(entry.Key), "Existing authored data changed: " + entry.Key); }
    private static void Fail(Exception error)
    { SessionState.SetBool(Pending, false); Debug.LogError("[Sector size verification FAIL] " + error); if (Application.isBatchMode) EditorApplication.Exit(1); }
}
