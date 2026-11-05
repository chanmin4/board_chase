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

/// <summary>Explicit one-time Base collider migration and isolated native physics/navigation verification.</summary>
public static class SectorBaseColliderVerification
{
    private const string Output = "Library/SectorBaseColliderVerification";
    private const string Base = "Assets/Prefabs/GameScene/Sector/Bases/Sector_Base.prefab";
    private const string Normal = "Assets/Prefabs/GameScene/Sector/Bases/NormalSectorBase.prefab";
    private const string Pending = "SectorBaseColliderVerification.Play";
    private static readonly List<string> Passes = new();
    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public static void Run()
    {
        if (!Environment.GetCommandLineArgs().Contains("-sectorBaseColliderVerification"))
            throw new InvalidOperationException("Use the explicit closed-editor collider verification launcher.");
        Directory.CreateDirectory(Output);
        try
        {
            CaptureHashes();
            var paths = AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/Prefabs/GameScene/Sector" })
                .Select(AssetDatabase.GUIDToAssetPath).Where(p => p.EndsWith(".prefab") && SectorBaseRectangleVerification.InheritsBase(p)).OrderBy(p => p).ToArray();
            Assert.Greater(paths.Length, 5);
            string file = Output + "/gameplay-before.json";
            Hashes before;
            if (File.Exists(file)) before = JsonUtility.FromJson<Hashes>(File.ReadAllText(file));
            else
            {
                before = new Hashes { paths = paths, hashes = paths.Select(SectorBaseRectangleVerification.CaptureStateIgnoringCornerShape).ToArray() };
                File.WriteAllText(file, JsonUtility.ToJson(before));
            }
            string backup = Output + "/Sector_Base.before.prefab";
            if (!File.Exists(backup)) { File.Copy(Base, backup); File.Copy(Base + ".meta", backup + ".meta"); }
            ApplyColliders();
            for (int i = 0; i < before.paths.Length; i++)
            {
                Assert.AreEqual(SectorBaseRectangleVerification.CanonicalState(before.hashes[i]),
                    SectorBaseRectangleVerification.CanonicalState(SectorBaseRectangleVerification.CaptureStateIgnoringCornerShape(before.paths[i])),
                    "Only trimmed corner size/center may change: " + before.paths[i]);
                VerifyPrefab(before.paths[i]);
            }
            Pass(before.paths.Length + " Base descendants inherit 8 fitted corner colliders; all other collider data, hierarchy, props, spawn points, bounds, portals and nav refs unchanged");
            File.WriteAllLines(Output + "/edit-results.txt", Passes);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SessionState.SetBool(CombatLabRuntime.IsolationKey, true); SessionState.SetBool(Pending, true);
            EditorApplication.EnterPlaymode();
        }
        catch (Exception error) { Fail(error); }
    }

    private static BoxCollider[] Corners(GameObject root) => root.GetComponentsInChildren<BoxCollider>(true)
        .Where(SectorBaseRectangleVerification.IsTrimmedCeilingCollider).ToArray();
    private static void ApplyColliders()
    {
        var root = PrefabUtility.LoadPrefabContents(Base);
        try
        {
            var corners = Corners(root); Assert.AreEqual(8, corners.Length);
            bool changed = false;
            foreach (var box in corners)
            {
                var bounds = box.GetComponent<MeshFilter>().sharedMesh.bounds;
                if ((box.center - bounds.center).sqrMagnitude < 1e-12f && (box.size - bounds.size).sqrMagnitude < 1e-12f) continue;
                box.center = bounds.center; box.size = bounds.size;
                PrefabUtility.RecordPrefabInstancePropertyModifications(box); changed = true;
            }
            if (changed) { PrefabUtility.SaveAsPrefabAsset(root, Base, out bool saved); Assert.IsTrue(saved); }
            Pass("Sector_Base: only center/size of 8 protruding ceiling end BoxColliders fitted to clipped meshes; enabled/layer/trigger/material and transforms preserved");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    private static void VerifyPrefab(string path)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try { VerifyGeometry(root); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    private static void VerifyGeometry(GameObject root)
    {
        var corners = Corners(root); Assert.AreEqual(8, corners.Length);
        var rectangle = SectorBaseRectangleVerification.OuterRectangle(root);
        foreach (var box in corners)
        {
            var mesh = box.GetComponent<MeshFilter>().sharedMesh;
            Assert.Less(Vector3.Distance(mesh.bounds.center, box.center), .00001f, root.name);
            Assert.Less(Vector3.Distance(mesh.bounds.size, box.size), .00001f, root.name);
            Assert.IsTrue(SectorBaseRectangleVerification.InsideXZ(
                SectorBaseRectangleVerification.BoundsIn(box.transform, new Bounds(box.center, box.size), root.transform), rectangle), root.name);
        }
    }

    [InitializeOnLoadMethod] private static void RegisterPlay() => EditorApplication.update += VerifyPlay;
    private static void VerifyPlay()
    {
        if (!SessionState.GetBool(Pending, false) || !EditorApplication.isPlaying) return;
        SessionState.SetBool(Pending, false);
        try
        {
            Assert.IsTrue(CombatLabRuntime.IsIsolatedSession);
            if (SaveSystem.Active != null)
            { Assert.IsFalse(SaveSystem.Active.LoadSaveDataFromDisk()); Assert.IsFalse(SaveSystem.Active.TrySaveRunDataToDisk()); }
            Assert.IsNull(Object.FindFirstObjectByType<StageProgressionManager>());
            var builder = new GameObject("Temporary collider navigation test").AddComponent<StageSectorInstantiator>();
            var sectors = (List<SectorRuntime>)typeof(StageSectorInstantiator).GetField("_generatedSectors", Private).GetValue(builder);
            int index = 0;
            foreach (string path in new[] { Normal, Normal.Replace(".prefab", "_HalfSize.prefab"), Normal.Replace(".prefab", "_QuarterSize.prefab"),
                "Assets/Prefabs/GameScene/Sector/Rooms/Normal/Normal_2.prefab" })
                sectors.Add(Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), Vector3.right * index++ * 340, Quaternion.identity).GetComponent<SectorRuntime>());
            Physics.SyncTransforms();
            typeof(StageSectorInstantiator).GetMethod("RebuildGeneratedSectorNavMeshes", Private).Invoke(builder, null);
            foreach (var sector in sectors)
            {
                VerifyGeometry(sector.gameObject); VerifyPhysics(sector.gameObject);
                Assert.NotNull(sector.GetComponent<NavMeshSurface>().navMeshData);
                Assert.IsTrue(NavMesh.SamplePosition(sector.PlayerStartPoint.position, out var spawn, 2, NavMesh.AllAreas));
                var portals = sector.GetComponentsInChildren<SectorPortal>(true); Assert.AreEqual(4, portals.Length);
                foreach (var portal in portals)
                {
                    Assert.IsTrue(NavMesh.SamplePosition(portal.ArrivalPosition, out var arrival, 2, NavMesh.AllAreas));
                    var route = new NavMeshPath(); Assert.IsTrue(NavMesh.CalculatePath(spawn.position, arrival.position, NavMesh.AllAreas, route));
                    Assert.AreEqual(NavMeshPathStatus.PathComplete, route.status);
                }
                Assert.AreEqual(sector.GetWorldBounds(), sector.GetComponentInChildren<SectorPaint>(true).GetWorldBounds());
                Pass(sector.name + ": 8 removed protrusions no longer collide, retained cap centers still collide, runtime NavMesh and 4 portal routes pass; paint bounds unchanged");
            }
            SectorBaseRectangleVerification.RenderRoom(sectors[3].gameObject, "normal-2-no-grid.png", false, Output);
            var wireframe = AddColliderWireframe(sectors[3].gameObject);
            SectorBaseRectangleVerification.RenderRoom(sectors[3].gameObject, "normal-2-corner-colliders.png", true, Output);
            Object.DestroyImmediate(wireframe.GetComponent<MeshFilter>().sharedMesh);
            Object.DestroyImmediate(wireframe.GetComponent<MeshRenderer>().sharedMaterial);
            Object.DestroyImmediate(wireframe);
            VerifyHashes();
            File.WriteAllLines(Output + "/results.txt", File.ReadAllLines(Output + "/edit-results.txt").Concat(Passes).Append("PASSED"));
            if (File.Exists(Output + "/failure.txt")) File.Delete(Output + "/failure.txt");
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Fail(error); }
    }

    private static void VerifyPhysics(GameObject root)
    {
        var corners = Corners(root);
        // Runtime Object.Instantiate clones have no PrefabUtility correspondence.
        // Read the preserved original cap prefab directly for the pre-trim shape.
        var original = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath("69b86f669782a594782cbae3bc4601c3"))?.GetComponent<BoxCollider>();
        Assert.NotNull(original);
        foreach (var box in corners)
        {
            var previous = new Bounds(original.center, original.size);
            var fitted = new Bounds(box.center, box.size);
            var removed = previous.center;
            if (fitted.min.x > previous.min.x + .01f) removed.x = (previous.min.x + fitted.min.x) * .5f;
            else { Assert.Less(fitted.max.x, previous.max.x - .01f); removed.x = (previous.max.x + fitted.max.x) * .5f; }
            var removedWorld = box.transform.TransformPoint(removed);
            Assert.IsFalse(Physics.OverlapSphere(removedWorld, .01f, ~0, QueryTriggerInteraction.Collide).Any(c => corners.Contains(c)), "Removed overhang is still solid: " + root.name);
            Assert.IsTrue(Physics.OverlapSphere(box.transform.TransformPoint(box.center), .01f, ~0, QueryTriggerInteraction.Collide).Contains(box), "Retained corner lost collision: " + root.name);
        }
    }
    private static GameObject AddColliderWireframe(GameObject root)
    {
        var vertices = new List<Vector3>(); var indices = new List<int>();
        foreach (var box in SectorBaseRectangleVerification.Caps(root).Select(f => f.GetComponent<BoxCollider>()))
        {
            int offset = vertices.Count;
            for (int i = 0; i < 8; i++)
            {
                var point = box.center + Vector3.Scale(box.size * .5f,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                vertices.Add(box.transform.TransformPoint(point) + Vector3.up * .1f);
                foreach (int axis in new[] { 1, 2, 4 }) if ((i & axis) == 0) { indices.Add(offset + i); indices.Add(offset + (i | axis)); }
            }
        }
        var go = new GameObject("Temporary ceiling collider wireframes");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, root.scene);
        var mesh = new Mesh { name = "Temporary collider lines" }; mesh.SetVertices(vertices); mesh.SetIndices(indices, MeshTopology.Lines, 0); mesh.RecalculateBounds();
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var material = new Material(Shader.Find("Universal Render Pipeline/Unlit")); material.SetColor("_BaseColor", new Color(.2f, 1, .35f));
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        return go;
    }

    [Serializable] private sealed class Hashes { public string[] paths; public string[] hashes; }
    private static string Hash(string path)
    { using var sha = SHA256.Create(); using var stream = File.OpenRead(path); return Convert.ToBase64String(sha.ComputeHash(stream)); }
    private static void CaptureHashes()
    {
        string file = Output + "/original-hashes.json";
        if (!File.Exists(file))
        {
            var paths = new[] { "Assets/Prefabs/GameScene/Sector", "Assets/ScriptableObject/GameScene/Stage", "Assets/Art", "Assets/Scenes", "ProjectSettings" }
                .Where(Directory.Exists).SelectMany(p => Directory.GetFiles(p, "*", SearchOption.AllDirectories))
                .Select(p => p.Replace('\\', '/')).Where(p => p != Base).ToArray();
            File.WriteAllText(file, JsonUtility.ToJson(new Hashes { paths = paths, hashes = paths.Select(Hash).ToArray() }));
        }
        file = Output + "/save-hashes.json";
        if (!File.Exists(file))
        {
            var paths = Directory.Exists(Application.persistentDataPath) ? Directory.GetFiles(Application.persistentDataPath)
                .Where(p => p.Contains(".es3") || p.Contains(".vaccine")).ToArray() : Array.Empty<string>();
            File.WriteAllText(file, JsonUtility.ToJson(new Hashes { paths = paths, hashes = paths.Select(Hash).ToArray() }));
        }
    }
    private static void VerifyHashes()
    {
        foreach (string file in new[] { Output + "/original-hashes.json", Output + "/save-hashes.json" })
        {
            var entries = JsonUtility.FromJson<Hashes>(File.ReadAllText(file));
            for (int i = 0; i < entries.paths.Length; i++) Assert.AreEqual(entries.hashes[i], Hash(entries.paths[i]), "Unrequested write: " + entries.paths[i]);
            Pass(entries.paths.Length + " existing files byte-preserved: " + Path.GetFileName(file));
        }
    }
    private static void Pass(string message) { Passes.Add("PASS " + message); Debug.Log("[Sector collider PASS] " + message); }
    private static void Fail(Exception error)
    {
        SessionState.SetBool(Pending, false); Directory.CreateDirectory(Output);
        File.WriteAllText(Output + "/failure.txt", error.ToString()); Debug.LogException(error); EditorApplication.Exit(1);
    }
}
