using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

/// <summary>Explicit native inspection/authoring entry point; never runs on normal editor startup.</summary>
public static class SectorBaseRectangleVerification
{
    private const string Output = "Library/SectorBaseRectangleVerification";
    private const string Base = "Assets/Prefabs/GameScene/Sector/Bases/Sector_Base.prefab";
    private const string Normal = "Assets/Prefabs/GameScene/Sector/Bases/NormalSectorBase.prefab";
    private const string MeshFolder = "Assets/Art/Mesh/Sector";
    private const string SmoothMesh = MeshFolder + "/SectorFloor_NoGrid_4m.asset";
    private const string SmoothMaterial = "Assets/Art/Materials/GameScene/SectorThemes/LabFloor_NoGrid.mat";
    private const string Pending = "SectorBaseRectangleVerification.Play";
    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly List<string> Passes = new();

    public static void Run()
    {
        RequireExplicit(); Directory.CreateDirectory(Output);
        try
        {
            Assert.IsTrue(File.Exists(Output + "/audit.txt"), "Inspect native mesh bounds first.");
            CaptureHashes();
            var paths = AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/Prefabs/GameScene/Sector" })
                .Select(AssetDatabase.GUIDToAssetPath).Where(p => p.EndsWith(".prefab") && InheritsBase(p)).OrderBy(p => p).ToArray();
            Assert.Greater(paths.Length, 5);
            string stateFile = Output + "/gameplay-before.json";
            Dictionary<string, string> states;
            if (File.Exists(stateFile))
            {
                var entries = JsonUtility.FromJson<Hashes>(File.ReadAllText(stateFile));
                states = entries.paths.Select((p, i) => (p, state: entries.hashes[i])).ToDictionary(e => e.p, e => e.state);
            }
            else
            {
                states = paths.ToDictionary(p => p, CaptureState);
                File.WriteAllText(stateFile, JsonUtility.ToJson(new Hashes { paths = paths, hashes = paths.Select(p => states[p]).ToArray() }));
            }
            string backup = Output + "/Sector_Base.before.prefab";
            if (!File.Exists(backup)) File.Copy(Base, backup);
            if (!File.Exists(backup + ".meta")) File.Copy(Base + ".meta", backup + ".meta");
            ApplyBase();
            foreach (string path in paths)
            {
                Assert.AreEqual(CanonicalState(states[path]), CanonicalState(CaptureState(path)), "Gameplay state/hierarchy changed: " + path);
                VerifyInheritedVisuals(path);
            }
            Pass(paths.Length + " Base descendants: hierarchy, colliders, all gameplay references, props, anchors, portals and baked NavMesh refs preserved");
            VerifyThemeCompatibility(); VerifyHashes();
            File.WriteAllLines(Output + "/edit-results.txt", Passes);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SessionState.SetBool(CombatLabRuntime.IsolationKey, true);
            SessionState.SetBool(Pending, true);
            EditorApplication.EnterPlaymode();
        }
        catch (Exception error) { Fail(error); }
    }

    private static void RequireExplicit()
    {
        if (!Environment.GetCommandLineArgs().Contains("-sectorBaseRectangleVerification"))
            throw new InvalidOperationException("Use the explicit closed-editor verification launcher.");
    }

    private static Transform FloorRoot(GameObject root) => root.transform.Find("SectorVisual").Cast<Transform>()
        .Single(t => t.name.StartsWith("Sector_Board", StringComparison.Ordinal));
    internal static MeshFilter[] Caps(GameObject root) => root.GetComponentsInChildren<MeshFilter>(true)
        .Where(f => f.transform.parent != null && f.transform.parent.name == "CeilRoot").ToArray();
    // The four long, central ceiling panels establish the straight outer border.
    // The shorter end panels currently overshoot that border at every corner.
    internal static Bounds OuterRectangle(GameObject root)
    {
        var middles = Caps(root).GroupBy(f => f.transform.parent)
            .Select(g => g.OrderByDescending(f => BoundsIn(f.transform, f.sharedMesh.bounds, root.transform).size.sqrMagnitude).First()).ToArray();
        Assert.AreEqual(4, middles.Length);
        var result = BoundsIn(middles[0].transform, middles[0].sharedMesh.bounds, root.transform);
        foreach (var middle in middles.Skip(1)) result.Encapsulate(BoundsIn(middle.transform, middle.sharedMesh.bounds, root.transform));
        return result;
    }

    private static void ApplyBase()
    {
        if (!AssetDatabase.IsValidFolder(MeshFolder)) AssetDatabase.CreateFolder("Assets/Art/Mesh", "Sector");
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(SmoothMesh);
        if (mesh == null)
        {
            // Existing VSplatterFloor is a flat 4m surface; its 5m BoxCollider extends
            // underneath neighbouring tiles. Preserve both that visual footprint and Y=0.
            mesh = new Mesh { name = "SectorFloor_NoGrid_4m" };
            mesh.vertices = new[] { new Vector3(-2, 0, -2), new Vector3(-2, 0, 2), new Vector3(2, 0, 2), new Vector3(2, 0, -2) };
            mesh.normals = Enumerable.Repeat(Vector3.up, 4).ToArray();
            mesh.tangents = Enumerable.Repeat(new Vector4(1, 0, 0, -1), 4).ToArray();
            mesh.uv = new[] { Vector2.zero, new Vector2(0, 4), new Vector2(4, 4), new Vector2(4, 0) };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 }; mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, SmoothMesh);
        }
        var material = AssetDatabase.LoadAssetAtPath<Material>(SmoothMaterial);
        Assert.NotNull(mesh); Assert.NotNull(material);
        var root = PrefabUtility.LoadPrefabContents(Base);
        try
        {
            int floors = 0, corners = 0, alreadyClipped = 0;
            foreach (var filter in FloorRoot(root).GetComponentsInChildren<MeshFilter>(true))
            {
                var box = filter.GetComponent<BoxCollider>();
                if (box == null) continue;
                Assert.Less(Vector3.Distance(mesh.bounds.size, filter.sharedMesh.bounds.size), .001f);
                Assert.Less(Vector3.Distance(mesh.bounds.center, filter.sharedMesh.bounds.center), .001f);
                filter.sharedMesh = mesh;
                var renderer = filter.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
                PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer); floors++;
            }
            Assert.Greater(floors, 0);
            var rectangle = OuterRectangle(root);
            foreach (var filter in Caps(root))
            {
                Bounds bounds = BoundsIn(filter.transform, filter.sharedMesh.bounds, root.transform);
                if (InsideXZ(bounds, rectangle))
                {
                    if (AssetDatabase.GetAssetPath(filter.sharedMesh).StartsWith(MeshFolder + "/SectorCeil_Rectangle_", StringComparison.Ordinal)) alreadyClipped++;
                    continue;
                }
                var clipped = Object.Instantiate(filter.sharedMesh);
                clipped.name = "SectorCeil_Rectangle_" + corners;
                var vertices = clipped.vertices;
                for (int i = 0; i < vertices.Length; i++)
                {
                    var point = root.transform.InverseTransformPoint(filter.transform.TransformPoint(vertices[i]));
                    point.x = Mathf.Clamp(point.x, rectangle.min.x, rectangle.max.x);
                    point.z = Mathf.Clamp(point.z, rectangle.min.z, rectangle.max.z);
                    vertices[i] = filter.transform.InverseTransformPoint(root.transform.TransformPoint(point));
                }
                // Axis-aligned ceiling end panels retain their closed topology, UVs, material,
                // normals and submeshes; only their outward end plane is brought inward.
                clipped.vertices = vertices; clipped.RecalculateBounds();
                string target = MeshFolder + "/" + clipped.name + ".asset";
                var existing = AssetDatabase.LoadAssetAtPath<Mesh>(target);
                if (existing == null) { AssetDatabase.CreateAsset(clipped, target); existing = clipped; }
                else { Object.DestroyImmediate(clipped); }
                filter.sharedMesh = existing; PrefabUtility.RecordPrefabInstancePropertyModifications(filter); corners++;
                Assert.IsTrue(InsideXZ(BoundsIn(filter.transform, existing.bounds, root.transform), rectangle));
            }
            Assert.AreEqual(8, corners + alreadyClipped, "Two overshooting end panels per side must be trimmed.");
            PrefabUtility.SaveAsPrefabAsset(root, Base, out bool saved); Assert.IsTrue(saved);
            Pass("Sector_Base only: " + floors + " smooth floor meshes/materials and 8 ceiling end meshes (all 4 corners); no collider/transform changes");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    internal static bool InsideXZ(Bounds inner, Bounds outer) => inner.min.x >= outer.min.x - .002f &&
        inner.max.x <= outer.max.x + .002f && inner.min.z >= outer.min.z - .002f && inner.max.z <= outer.max.z + .002f;
    internal static bool InheritsBase(string path)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        for (int i = 0; prefab != null && i < 30; i++, prefab = PrefabUtility.GetCorrespondingObjectFromSource(prefab))
            if (AssetDatabase.GetAssetPath(prefab) == Base) return true;
        return false;
    }
    private static string Reference(Object value, Transform root)
    {
        if (value == null) return "null";
        string asset = AssetDatabase.GetAssetPath(value);
        if (!string.IsNullOrEmpty(asset))
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out string guid, out long id); return guid + ":" + id;
        }
        var transform = value is GameObject go ? go.transform : value is Component component ? component.transform : null;
        return transform != null && (transform == root || transform.IsChildOf(root))
            ? PathOf(transform, root) + ":" + value.GetType().FullName : value.GetType().FullName + ":" + value.name;
    }
    private static string CaptureState(string path) => CaptureStateInternal(path, false);
    internal static string CaptureStateIgnoringCornerShape(string path) => CaptureStateInternal(path, true);
    internal static bool IsTrimmedCeilingCollider(Component component)
    {
        if (!(component is BoxCollider) || component.transform.parent == null || component.transform.parent.name != "CeilRoot") return false;
        var filter = component.GetComponent<MeshFilter>();
        return filter != null && AssetDatabase.GetAssetPath(filter.sharedMesh)
            .StartsWith(MeshFolder + "/SectorCeil_Rectangle_", StringComparison.Ordinal);
    }
    private static string CaptureStateInternal(string path, bool ignoreCornerShape)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var rows = new List<string>();
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                rows.Add(PathOf(child, root.transform) + ":" + child.name + ":" + child.gameObject.layer + ":" +
                    child.gameObject.activeSelf + ":" + child.localPosition.ToString("F5") + ":" +
                    child.localRotation.ToString("F5") + ":" + child.localScale.ToString("F5"));
                foreach (var component in child.GetComponents<Component>())
                {
                    Assert.NotNull(component, "Missing script: " + path);
                    if (component is Transform || component is MeshFilter || component is Renderer) continue;
                    string json = EditorJsonUtility.ToJson(component);
                    if (ignoreCornerShape && IsTrimmedCeilingCollider(component))
                    {
                        const string shape = "\"m_(Size|Center)\"\\s*:\\s*\\{[^}]*\\}";
                        Assert.AreEqual(2, Regex.Matches(json, shape).Count);
                        json = Regex.Replace(json, shape, "\"m_$1\":{}");
                    }
                    rows.Add(component.GetType().FullName + ":" + Regex.Replace(json, "\"instanceID\"\\s*:\\s*-?\\d+", "\"instanceID\":0"));
                    var property = new SerializedObject(component).GetIterator();
                    while (property.Next(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference)
                            rows.Add(property.propertyPath + ":" + Reference(property.objectReferenceValue, root.transform));
                }
            }
            rows.Add(root.GetComponent<SectorRuntime>().GetWorldBounds().ToString("F5"));
            return string.Join("\n", rows);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    internal static string CanonicalState(string state)
    {
        // Unity may enumerate a Variant's added siblings in a different order after its
        // source is saved, even when both prefab files' hierarchy records are unchanged.
        // Compare ownership/name, transforms, component data and references by path, not
        // temporary enumeration order. Keep component/array reference order within a node.
        var rows = state.Split('\n');
        var blocks = new List<string>(); var block = new List<string>();
        for (int i = 0; i < rows.Length - 1; i++)
        {
            string row = Regex.Replace(rows[i], @"\[\d+\]", "");
            if (Regex.IsMatch(row, @":\d+:(True|False):\(") && block.Count > 0)
            { blocks.Add(string.Join("\n", block)); block.Clear(); }
            block.Add(row);
        }
        if (block.Count > 0) blocks.Add(string.Join("\n", block));
        return string.Join("\n", blocks.OrderBy(b => b, StringComparer.Ordinal)) + "\n" + rows[rows.Length - 1];
    }
    private static void VerifyInheritedVisuals(string path)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Assert.IsTrue(Caps(root).All(f => InsideXZ(BoundsIn(f.transform, f.sharedMesh.bounds, root.transform), OuterRectangle(root))), path);
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(SmoothMesh);
            foreach (var filter in FloorRoot(root).GetComponentsInChildren<MeshFilter>(true).Where(f => f.GetComponent<BoxCollider>() != null))
                Assert.AreEqual(mesh, filter.sharedMesh, "Floor mesh inheritance: " + path);
            if (root.GetComponent<SectorAppearance>() == null)
                Assert.IsTrue(FloorRoot(root).GetComponentsInChildren<MeshRenderer>(true)
                    .All(r => r.sharedMaterial == AssetDatabase.LoadAssetAtPath<Material>(SmoothMaterial)), "Floor material inheritance: " + path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    private static void VerifyThemeCompatibility()
    {
        var root = PrefabUtility.LoadPrefabContents(Normal);
        try
        {
            var before = root.GetComponentsInChildren<Renderer>(true).ToDictionary(r => r, r => (r.enabled, materials: r.sharedMaterials));
            var appearance = root.AddComponent<SectorAppearance>(); appearance.RegisterExistingSurfaces();
            appearance.SetTheme(AssetDatabase.LoadAssetAtPath<SectorThemeSO>(SectorMakerWindow.NoGridThemePath)); appearance.Apply();
            Assert.AreEqual(1, appearance.GeneratedVisuals.Count); appearance.Restore();
            foreach (var entry in before)
            { Assert.AreEqual(entry.Value.enabled, entry.Key.enabled); CollectionAssert.AreEqual(entry.Value.materials, entry.Key.sharedMaterials); }
            Pass("Sector Maker theme apply/restore returns to the new smooth Base without duplicate components or shells");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    [Serializable] private sealed class Hashes { public string[] paths; public string[] hashes; }
    private static string Hash(string path)
    { using var sha = SHA256.Create(); return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path))); }
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
        foreach (string path in new[] { Output + "/original-hashes.json", Output + "/save-hashes.json" })
        {
            var values = JsonUtility.FromJson<Hashes>(File.ReadAllText(path));
            for (int i = 0; i < values.paths.Length; i++) Assert.AreEqual(values.hashes[i], Hash(values.paths[i]), "Unrequested write: " + values.paths[i]);
            Pass(values.paths.Length + " original files byte-preserved: " + Path.GetFileName(path));
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
            var builder = new GameObject("Temporary rectangle navigation test").AddComponent<StageSectorInstantiator>();
            var sectors = (List<SectorRuntime>)typeof(StageSectorInstantiator).GetField("_generatedSectors", Private).GetValue(builder);
            int index = 0;
            foreach (string path in new[] { Normal, Normal.Replace(".prefab", "_HalfSize.prefab"), Normal.Replace(".prefab", "_QuarterSize.prefab"),
                "Assets/Prefabs/GameScene/Sector/Rooms/Normal/Normal_2.prefab" })
            {
                var sector = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), Vector3.right * index++ * 340, Quaternion.identity).GetComponent<SectorRuntime>();
                sectors.Add(sector);
            }
            Physics.SyncTransforms();
            typeof(StageSectorInstantiator).GetMethod("RebuildGeneratedSectorNavMeshes", Private).Invoke(builder, null);
            foreach (var sector in sectors)
            {
                Assert.NotNull(sector.GetComponent<NavMeshSurface>().navMeshData);
                Assert.IsTrue(NavMesh.SamplePosition(sector.PlayerStartPoint.position, out var spawn, 2, NavMesh.AllAreas));
                foreach (var portal in sector.GetComponentsInChildren<SectorPortal>(true))
                {
                    Assert.IsTrue(NavMesh.SamplePosition(portal.ArrivalPosition, out var arrival, 2, NavMesh.AllAreas));
                    var route = new NavMeshPath();
                    Assert.IsTrue(NavMesh.CalculatePath(spawn.position, arrival.position, NavMesh.AllAreas, route));
                    Assert.AreEqual(NavMeshPathStatus.PathComplete, route.status);
                }
                Assert.AreEqual(sector.GetWorldBounds(), sector.GetComponentInChildren<SectorPaint>(true).GetWorldBounds());
                Assert.IsTrue(Caps(sector.gameObject).All(f => InsideXZ(BoundsIn(f.transform, f.sharedMesh.bounds, sector.transform), OuterRectangle(sector.gameObject))));
                Pass(sector.name + ": rectangular silhouette, runtime NavMesh, spawn and 4 complete portal routes, paint bounds pass");
            }
            RenderRoom(sectors[3].gameObject, "normal-2-after.png", false);
            RenderRoom(sectors[3].gameObject, "normal-2-top.png", true);
            VerifyHashes();
            File.WriteAllLines(Output + "/results.txt", File.ReadAllLines(Output + "/edit-results.txt").Concat(Passes).Append("PASSED"));
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Fail(error); }
    }
    internal static void RenderRoom(GameObject root, string filename, bool top, string outputFolder = null)
    {
        var cameraGo = new GameObject("Temporary rectangle camera"); var lightGo = new GameObject("Temporary rectangle light");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraGo, root.scene);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightGo, root.scene);
        var camera = cameraGo.AddComponent<Camera>(); var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional; light.intensity = 1.3f; light.transform.rotation = Quaternion.Euler(55, -30, 0);
        var bounds = root.GetComponent<SectorRuntime>().GetWorldBounds();
        camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(root.scene);
        camera.transform.position = bounds.center + (top ? new Vector3(0, 200, 0) : new Vector3(0, 170, -110));
        camera.transform.LookAt(bounds.center, top ? Vector3.forward : Vector3.up);
        camera.orthographic = true; camera.orthographicSize = bounds.size.z * .87f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.09f, .12f, .15f); camera.farClipPlane = 600;
        var rt = new RenderTexture(1400, 1100, 24); var texture = new Texture2D(1400, 1100, TextureFormat.RGB24, false); var previous = RenderTexture.active;
        try
        {
            rt.Create(); camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); texture.Apply(); File.WriteAllBytes((outputFolder ?? Output) + "/" + filename, texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null; RenderTexture.active = previous; rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(texture);
            Object.DestroyImmediate(cameraGo); Object.DestroyImmediate(lightGo);
        }
    }
    private static void Pass(string message) { Passes.Add("PASS " + message); Debug.Log("[Sector rectangle PASS] " + message); }
    private static void Fail(Exception error)
    {
        SessionState.SetBool(Pending, false); Directory.CreateDirectory(Output);
        File.WriteAllText(Output + "/failure.txt", error.ToString()); Debug.LogException(error); EditorApplication.Exit(1);
    }

    public static void Audit()
    {
        RequireExplicit();
        try { WriteAudit(); EditorApplication.Exit(0); }
        catch (Exception error) { Fail(error); }
    }

    [InitializeOnLoadMethod]
    private static void RegisterReadOnlyAudit()
    {
        // An explicit Library request may inspect a still-closing Editor without touching
        // assets/scenes or requesting shutdown. Authoring still requires our own launcher.
        EditorApplication.delayCall += () =>
        {
            string request = Output + "/read-only-audit.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(request);
            try { WriteAudit(); }
            catch (Exception error) { Directory.CreateDirectory(Output); File.WriteAllText(Output + "/audit-failure.txt", error.ToString()); }
        };
    }

    private static void WriteAudit()
    {
        Directory.CreateDirectory(Output);
        GameObject root = null;
        try
        {
            root = PrefabUtility.LoadPrefabContents(Base);
            var lines = new List<string>();
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                string path = PathOf(filter.transform, root.transform);
                if (!path.Contains("CeilRoot") && !path.Contains("corner") && !path.Contains("Sector_Board")) continue;
                var renderer = filter.GetComponent<MeshRenderer>();
                var mesh = filter.sharedMesh;
                lines.Add(path + " | mesh=" + AssetDatabase.GetAssetPath(mesh) + " | local=" + mesh.bounds.ToString("F4") +
                    " | root=" + BoundsIn(filter.transform, mesh.bounds, root.transform).ToString("F4") +
                    " | enabled=" + renderer.enabled + " | verts=" + mesh.vertexCount);
            }
            lines.Add("runtime=" + root.GetComponent<SectorRuntime>().GetWorldBounds().ToString("F4"));
            File.WriteAllLines(Output + "/audit.txt", lines);
            Debug.Log("[Sector rectangle] Audit complete: " + lines.Count + " entries");
        }
        finally { if (root != null) PrefabUtility.UnloadPrefabContents(root); }
    }

    private static string PathOf(Transform child, Transform root)
    {
        var parts = new List<string>();
        while (child != root) { parts.Add(child.name + "[" + child.GetSiblingIndex() + "]"); child = child.parent; }
        parts.Reverse(); return string.Join("/", parts);
    }

    internal static Bounds BoundsIn(Transform source, Bounds local, Transform target)
    {
        var bounds = new Bounds(target.InverseTransformPoint(source.TransformPoint(local.min)), Vector3.zero);
        for (int i = 0; i < 8; i++)
        {
            var point = new Vector3((i & 1) == 0 ? local.min.x : local.max.x,
                (i & 2) == 0 ? local.min.y : local.max.y, (i & 4) == 0 ? local.min.z : local.max.z);
            bounds.Encapsulate(target.InverseTransformPoint(source.TransformPoint(point)));
        }
        return bounds;
    }
}
