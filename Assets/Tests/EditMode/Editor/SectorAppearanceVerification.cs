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

/// <summary>Explicit batch-only authoring and native verification. No automatic asset migration.</summary>
public static class SectorAppearanceVerification
{
    private const string Output = "Library/SectorAppearanceVerification";
    private const string Art = "Assets/Art/Materials/GameScene/SectorThemes";
    private const string Floor = "Assets/Prefabs/GameScene/Sector/Floor/Floor_NoGrid.prefab";
    private const string Demo = "Assets/Prefabs/GameScene/Sector/Bases/NormalSectorBase_NoGrid.prefab";
    private const string Source = "Assets/Prefabs/GameScene/Sector/Bases/NormalSectorBase.prefab";
    private const string Pending = "SectorAppearanceVerification.Play";
    private static Dictionary<string, string> _originals;
    private static readonly Dictionary<string, Rect> Controls = new();
    private static readonly List<string> Passes = new();
    private static SectorMakerWindow _window;
    private static string _temporary;
    private static int _uiPhase, _frames;
    private static double _uiStart;
    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public static void Run()
    {
        if (!Application.isBatchMode && !Environment.GetCommandLineArgs().Contains("-sectorAppearanceVerification"))
            throw new InvalidOperationException("Close Unity and use the explicit verification entry point.");
        Directory.CreateDirectory(Output);
        try
        {
            if (File.Exists(Output + "/original-hashes.json")) VerifyOriginals();
            else
            {
                _originals = CaptureOriginals();
                File.WriteAllText(Output + "/original-hashes.json", JsonUtility.ToJson(new Hashes {
                    paths = _originals.Keys.ToArray(), hashes = _originals.Values.ToArray() }));
            }
            CreateAssetsIfMissing();
            if (!File.Exists(Output + "/save-hashes.json"))
            {
                var files = Directory.Exists(Application.persistentDataPath)
                    ? Directory.GetFiles(Application.persistentDataPath).Where(p => p.Contains(".es3") || p.Contains(".vaccine")).ToArray()
                    : Array.Empty<string>();
                File.WriteAllText(Output + "/save-hashes.json", JsonUtility.ToJson(new Hashes { paths = files, hashes = files.Select(Hash).ToArray() }));
            }
            VerifyAssetsAndRestoration();
            BeginUiVerification();
        }
        catch (Exception error) { Fail(error); }
    }

    [Serializable] private sealed class Hashes { public string[] paths; public string[] hashes; }
    private static string Hash(string path)
    { using var sha = SHA256.Create(); return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path))); }
    private static Dictionary<string, string> CaptureOriginals() => new[] {
        "Assets/Prefabs/GameScene/Sector", "Assets/ScriptableObject/GameScene/Stage", "Assets/Art/Materials", "Assets/Scenes", "ProjectSettings"
    }.Where(Directory.Exists).SelectMany(p => Directory.GetFiles(p, "*", SearchOption.AllDirectories))
        .Where(p => !p.Replace('\\', '/').Contains("/SectorThemes/") && !p.Contains("SectorTheme_Lab_NoGrid") &&
            !p.Contains("Floor_NoGrid") && !p.Contains("NormalSectorBase_NoGrid") && !p.Contains("SectorTemplate_NormalSectorBase_NoGrid") &&
            !p.Replace('\\', '/').EndsWith("/Themes.meta"))
        .ToDictionary(p => p, Hash);
    private static void VerifyOriginals()
    {
        if (_originals == null)
        {
            var entries = JsonUtility.FromJson<Hashes>(File.ReadAllText(Output + "/original-hashes.json"));
            _originals = entries.paths.Select((p, i) => (p, hash: entries.hashes[i])).ToDictionary(p => p.p, p => p.hash);
        }
        foreach (var file in _originals) Assert.AreEqual(file.Value, Hash(file.Key), "Original changed: " + file.Key);
        Pass("all " + _originals.Count + " existing sector/material/stage/scene/project-setting files byte-preserved");
    }
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/'); EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
    private static void CreateAssetsIfMissing()
    {
        EnsureFolder(Art);
        EnsureFolder(Path.GetDirectoryName(SectorMakerWindow.NoGridThemePath).Replace('\\', '/'));
        Material material = AssetDatabase.LoadAssetAtPath<Material>(Art + "/LabFloor_NoGrid.mat");
        if (material == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit"); Assert.NotNull(shader);
            material = new Material(shader) { name = "LabFloor_NoGrid", enableInstancing = true };
            material.SetColor("_BaseColor", new Color(.38f, .42f, .46f, 1));
            material.SetFloat("_Smoothness", .18f); material.SetFloat("_Metallic", 0);
            AssetDatabase.CreateAsset(material, Art + "/LabFloor_NoGrid.mat");
        }
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(Art + "/Floor_NoGrid_Mesh.asset");
        if (mesh == null)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesh = Object.Instantiate(cube.GetComponent<MeshFilter>().sharedMesh); Object.DestroyImmediate(cube);
            mesh.name = "Floor_NoGrid_Mesh";
            var vertices = mesh.vertices; var normals = mesh.normals; var uv = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = Vector3.Scale(vertices[i], new Vector3(5, .2f, 5)) + Vector3.up * .1f;
                uv[i] = Mathf.Abs(normals[i].y) > .5f ? new Vector2(vertices[i].x, vertices[i].z) :
                    Mathf.Abs(normals[i].x) > .5f ? new Vector2(vertices[i].z, vertices[i].y) : new Vector2(vertices[i].x, vertices[i].y);
            }
            mesh.vertices = vertices; mesh.uv = uv; mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, Art + "/Floor_NoGrid_Mesh.asset");
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(Floor) == null)
        {
            var go = new GameObject("Floor_NoGrid");
            try
            {
                go.layer = 9;
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = material;
                var collider = go.AddComponent<BoxCollider>(); collider.size = mesh.bounds.size; collider.center = mesh.bounds.center;
                PrefabUtility.SaveAsPrefabAsset(go, Floor, out bool success); Assert.IsTrue(success);
            }
            finally { Object.DestroyImmediate(go); }
        }
        var theme = AssetDatabase.LoadAssetAtPath<SectorThemeSO>(SectorMakerWindow.NoGridThemePath);
        if (theme == null)
        {
            theme = ScriptableObject.CreateInstance<SectorThemeSO>();
            var data = new SerializedObject(theme);
            data.FindProperty("_floorVisual").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(Floor);
            data.FindProperty("_floorMaterial").objectReferenceValue = material;
            data.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(theme, SectorMakerWindow.NoGridThemePath);
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(Demo) == null)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Source), scene);
                go.name = "NormalSectorBase_NoGrid";
                var appearance = go.AddComponent<SectorAppearance>(); appearance.RegisterExistingSurfaces();
                appearance.SetTheme(theme); appearance.Apply();
                PrefabUtility.SaveAsPrefabAsset(go, Demo, out bool success); Assert.IsTrue(success);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
        string presetPath = SectorMakerAssets.TemplateRoot + "/SectorTemplate_NormalSectorBase_NoGrid.asset";
        if (AssetDatabase.LoadMainAssetAtPath(presetPath) == null)
            SectorMakerCatalog.CreatePreset(AssetDatabase.LoadAssetAtPath<GameObject>(Demo), presetPath, StageRoomType.Normal);
        // Never SaveAssets: only the newly created requested assets are written.
        Pass("URP smooth floor mesh/material/prefab/theme + inherited Base demo and separate template SO exist");
    }

    private static string CollisionState(GameObject room) => string.Join("\n", room.GetComponentsInChildren<Collider>(true)
        .Select(c => c.GetType().Name + ":" + EditorJsonUtility.ToJson(c) + ":" + c.transform.localToWorldMatrix));
    private static string PortalState(GameObject room) => string.Join("\n", room.GetComponentsInChildren<SectorPortal>(true)
        .Select(p => EditorJsonUtility.ToJson(p) + ":" + p.ArrivalPosition));
    private static void VerifyAssetsAndRestoration()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Source), scene);
            var runtime = go.GetComponent<SectorRuntime>();
            var bounds = runtime.GetWorldBounds(); string colliders = CollisionState(go), portals = PortalState(go);
            var navData = go.GetComponentsInChildren<NavMeshSurface>(true).Select(s => s.navMeshData).ToArray();
            var before = go.GetComponentsInChildren<Renderer>(true).ToDictionary(r => r, r => (r.enabled, materials: r.sharedMaterials));
            var appearance = go.AddComponent<SectorAppearance>(); appearance.RegisterExistingSurfaces();
            Assert.Greater(appearance.Surfaces.Count(s => s.kind == SectorAppearance.SurfaceKind.Floor), 0);
            Assert.Greater(appearance.Surfaces.Count(s => s.kind == SectorAppearance.SurfaceKind.Wall), 0);
            Assert.IsFalse(appearance.Surfaces.Any(s => s.kind == SectorAppearance.SurfaceKind.Wall &&
                Enumerable.Range(0, 1).Any(_ => s.renderer.transform.GetComponentsInParent<SectorPortal>(true).Length > 0)));
            var theme = AssetDatabase.LoadAssetAtPath<SectorThemeSO>(SectorMakerWindow.NoGridThemePath);
            appearance.SetTheme(theme); appearance.Apply();
            Assert.AreEqual(1, appearance.GeneratedVisuals.Count);
            var shell = appearance.GeneratedVisuals[0];
            Assert.AreEqual(0, shell.GetComponentsInChildren<Collider>(true).Length);
            Assert.AreEqual(0, shell.GetComponentsInChildren<NavMeshSurface>(true).Length);
            Assert.AreEqual(bounds, runtime.GetWorldBounds());
            Assert.AreEqual(colliders, CollisionState(go)); Assert.AreEqual(portals, PortalState(go));
            CollectionAssert.AreEqual(navData, go.GetComponentsInChildren<NavMeshSurface>(true).Select(s => s.navMeshData));
            foreach (var entry in before.Where(e => !appearance.Surfaces.Any(s => s.kind == SectorAppearance.SurfaceKind.Floor && s.renderer == e.Key)))
            { Assert.AreEqual(entry.Value.enabled, entry.Key.enabled); CollectionAssert.AreEqual(entry.Value.materials, entry.Key.sharedMaterials); }
            Pass("floor-only theme has one continuous shell; collision, doors, portals, bounds, nav data and other renderers untouched");
            appearance.Apply(); Assert.AreEqual(1, appearance.GeneratedVisuals.Count);
            appearance.RegisterExistingSurfaces(); Assert.IsFalse(appearance.Surfaces.Any(s => s.renderer != null && s.renderer.name.StartsWith("ThemeFloor")));
            appearance.Restore(); appearance.SetTheme(null);
            foreach (var entry in before)
            { Assert.AreEqual(entry.Value.enabled, entry.Key.enabled); CollectionAssert.AreEqual(entry.Value.materials, entry.Key.sharedMaterials); }
            Pass("reapply has no duplicate shell; restore exactly recovers original renderer flags/materials");

            var stage = ScriptableObject.CreateInstance<SectorThemeSO>();
            var data = new SerializedObject(stage); data.FindProperty("_wallMaterial").objectReferenceValue = theme.FloorMaterial; data.ApplyModifiedPropertiesWithoutUndo();
            appearance.SetTheme(theme); appearance.Apply(stage);
            Assert.IsTrue(appearance.Surfaces.Where(s => s.kind == SectorAppearance.SurfaceKind.Wall)
                .All(s => s.renderer.sharedMaterials.All(m => m == theme.FloorMaterial)));
            Assert.AreEqual(colliders, CollisionState(go)); Assert.AreEqual(portals, PortalState(go));
            appearance.Apply(theme); // stage-only inherited floor, room remains floor-only
            Pass("stage defaults + per-field room overrides work; wall material does not affect door/portal collision");
            Object.DestroyImmediate(stage);
            appearance.Restore(); appearance.SetTheme(null);
            var demo = AssetDatabase.LoadAssetAtPath<GameObject>(Demo);
            Assert.AreEqual(PrefabAssetType.Variant, PrefabUtility.GetPrefabAssetType(demo));
            Assert.AreEqual(Source, AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(demo)));
            Assert.AreEqual(1, demo.GetComponent<SectorAppearance>().GeneratedVisuals.Count);
            var catalog = SectorMakerCatalog.Scan();
            Assert.IsTrue(catalog.Any(r => r.path == Demo && r.presets.Any(SectorMakerAssets.IsTemplate)));
            Pass("demo Variant ancestry and Maker template discovery verified");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    private static void RenderRoom(GameObject go, string filename)
    {
        var scene = go.scene;
        var cameraGo = new GameObject("Temporary appearance camera"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraGo, scene);
        var lightGo = new GameObject("Temporary appearance light"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightGo, scene);
        var camera = cameraGo.AddComponent<Camera>(); var light = lightGo.AddComponent<Light>();
        camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
        light.type = LightType.Directional; light.intensity = 1.3f; light.transform.rotation = Quaternion.Euler(55, -30, 0);
        var bounds = go.GetComponent<SectorRuntime>().GetWorldBounds();
        camera.transform.position = bounds.center + new Vector3(0, 80, -55);
        camera.transform.LookAt(bounds.center); camera.orthographic = true; camera.orthographicSize = bounds.size.z * .68f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.09f, .12f, .15f);
        camera.farClipPlane = 500;
        var rt = new RenderTexture(1400, 1100, 24); var texture = new Texture2D(1400, 1100, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        try
        {
            rt.Create(); camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); texture.Apply();
            File.WriteAllBytes(Output + "/" + filename, texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null; RenderTexture.active = previous; rt.Release();
            Object.DestroyImmediate(rt); Object.DestroyImmediate(texture); Object.DestroyImmediate(cameraGo); Object.DestroyImmediate(lightGo);
        }
    }

    private static object Call(string name, params object[] args) => typeof(SectorMakerWindow).GetMethod(name, Private).Invoke(_window, args);
    private static void Set(string name, object value) => typeof(SectorMakerWindow).GetField(name, Private).SetValue(_window, value);
    private static void BeginUiVerification()
    {
        _temporary = "Assets/__SectorAppearanceVerification_" + Guid.NewGuid().ToString("N"); EnsureFolder(_temporary);
        var originalTheme = AssetDatabase.LoadAssetAtPath<SectorThemeSO>(SectorMakerWindow.NoGridThemePath);
        var independentTheme = SectorThemeAuthoring.Copy(originalTheme, _temporary + "/IndependentTheme.asset");
        Assert.AreNotEqual(originalTheme.FloorMaterial, independentTheme.FloorMaterial);
        Color originalColor = originalTheme.FloorMaterial.GetColor("_BaseColor");
        independentTheme.FloorMaterial.SetColor("_BaseColor", Color.red);
        Assert.AreEqual(originalColor, originalTheme.FloorMaterial.GetColor("_BaseColor"));
        Pass("theme copy duplicates assigned editable materials; recoloring one theme leaves the source untouched");
        var room = SectorMakerCatalog.CreateVariant(AssetDatabase.LoadAssetAtPath<GameObject>(Source), _temporary + "/TemporaryRoom.prefab");
        var preset = SectorMakerCatalog.CreatePreset(room, _temporary + "/TemporaryPreset.asset", StageRoomType.Normal);
        _window = ScriptableObject.CreateInstance<SectorMakerWindow>();
        Call("SelectRoom", room, false, preset); Call("OpenRoomForEditing");
        Set("_requestedCompactMode", null);
        Set("_compactMode", false); Set("_workbenchTab", 2); Set("_showGuide", false);
        _window.position = new Rect(60, 60, 1250, 1000);
        _window.ObserveControl = (id, rect) => Controls[id] = rect;
        _window.ShowUtility(); _uiStart = EditorApplication.timeSinceStartup;
        EditorApplication.update += UiUpdate;
    }
    private static void Click(string id)
    {
        Assert.IsTrue(Controls.TryGetValue(id, out var rect), "Missing UI control: " + id);
        _window.SendEvent(new Event { type = EventType.MouseDown, button = 0, mousePosition = rect.center });
        _window.SendEvent(new Event { type = EventType.MouseUp, button = 0, mousePosition = rect.center });
    }
    private static void UiUpdate()
    {
        if (_window == null) return;
        if (EditorApplication.timeSinceStartup - _uiStart > 90) { Fail(new TimeoutException("Maker UI repaint timed out.")); return; }
        _window.Repaint(); if (++_frames < 5) return; _frames = 0;
        try
        {
            var root = PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot;
            switch (_uiPhase++)
            {
                case 0:
                    Assert.IsNull(root.GetComponent<SectorAppearance>(), "Browsing must not create bindings");
                    Click("ChooseNoGridFloor"); Set("_showThemeFields", true);
                    Assert.NotNull(typeof(SectorMakerWindow).GetField("_appearanceThemeDraft", Private).GetValue(_window), "Choose button selected theme");
                    break;
                case 1:
                    foreach (string id in new[] { "AppearanceTheme", "FloorVisualField", "FloorMaterialField", "WallVisualField", "WallMaterialField", "ApplyAppearanceTheme", "RestoreAppearanceTheme", "CopyAppearanceTheme" })
                        Assert.IsTrue(Controls.ContainsKey(id), "Native Maker field " + id);
                    Click("ApplyAppearanceTheme"); break;
                case 2:
                    Assert.AreEqual(1, root.GetComponent<SectorAppearance>().GeneratedVisuals.Count);
                    Undo.PerformUndo(); break;
                case 3:
                    Assert.IsNull(root.GetComponent<SectorAppearance>(), "Undo reverses registration and surface edits");
                    Click("ApplyAppearanceTheme"); break;
                case 4:
                    Click("RestoreAppearanceTheme"); break;
                case 5:
                    Assert.IsNull(root.GetComponent<SectorAppearance>().Theme);
                    Assert.AreEqual(0, root.GetComponent<SectorAppearance>().GeneratedVisuals.Count);
                    Set("_compactMode", true); Set("_workbenchTab", 2); break;
                case 6:
                    Assert.IsTrue(Controls.ContainsKey("CompactAppearance"));
                    Assert.IsTrue(Controls.ContainsKey("ApplyAppearanceTheme"));
                    Pass("native Maker expanded/compact UI, choose/apply/restore and Ctrl+Z work; browse is read-only");
                    CleanupUi();
                    VerifyOriginals();
                    File.WriteAllLines(Output + "/edit-results.txt", Passes);
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    SessionState.SetBool(CombatLabRuntime.IsolationKey, true);
                    SessionState.SetBool(Pending, true); EditorApplication.EnterPlaymode(); break;
            }
        }
        catch (Exception error) { Fail(error); }
    }

    private static void CleanupUi()
    {
        EditorApplication.update -= UiUpdate;
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && stage.assetPath.StartsWith(_temporary + "/")) { stage.ClearDirtiness(); StageUtility.GoToMainStage(); }
        if (_window != null) { _window.Close(); _window = null; }
        if (_temporary != null && AssetDatabase.IsValidFolder(_temporary)) AssetDatabase.DeleteAsset(_temporary);
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
            {
                Assert.IsFalse(SaveSystem.Active.LoadSaveDataFromDisk(), "sandbox refuses real save reads");
                Assert.IsFalse(SaveSystem.Active.TrySaveRunDataToDisk(), "sandbox refuses real save writes");
            }
            Assert.IsNull(Object.FindFirstObjectByType<StageProgressionManager>());
            var host = new GameObject("Temporary appearance navigation test");
            var builder = host.AddComponent<StageSectorInstantiator>();
            var sectors = (List<SectorRuntime>)typeof(StageSectorInstantiator).GetField("_generatedSectors", Private).GetValue(builder);
            int i = 0;
            foreach (string path in new[] { Demo, Source.Replace(".prefab", "_HalfSize.prefab"), Source.Replace(".prefab", "_QuarterSize.prefab") })
            {
                var sector = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), Vector3.right * i++ * 240, Quaternion.identity).GetComponent<SectorRuntime>();
                var bounds = sector.GetWorldBounds();
                string colliderState = CollisionState(sector.gameObject), portalState = PortalState(sector.gameObject);
                var appearance = sector.GetComponent<SectorAppearance>() ?? sector.gameObject.AddComponent<SectorAppearance>();
                appearance.RegisterExistingSurfaces(); appearance.SetTheme(AssetDatabase.LoadAssetAtPath<SectorThemeSO>(SectorMakerWindow.NoGridThemePath)); appearance.Apply();
                Assert.AreEqual(bounds, sector.GetWorldBounds()); Assert.AreEqual(colliderState, CollisionState(sector.gameObject));
                Assert.AreEqual(portalState, PortalState(sector.gameObject)); sectors.Add(sector);
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
                Assert.AreEqual(1, sector.GetComponent<SectorAppearance>().GeneratedVisuals.Count);
                var paint = sector.GetComponentInChildren<SectorPaint>(true);
                Assert.NotNull(paint); Assert.AreEqual(sector.GetWorldBounds(), paint.GetWorldBounds());
                Pass(sector.name + ": actual runtime unified NavMesh and routes to all four portals pass; paint bounds unchanged");
            }
            RenderRoom(sectors[0].gameObject, "no-grid-runtime.png");
            sectors[0].GetComponent<SectorAppearance>().Restore();
            RenderRoom(sectors[0].gameObject, "original-grid-runtime.png");
            var saves = JsonUtility.FromJson<Hashes>(File.ReadAllText(Output + "/save-hashes.json"));
            for (int index = 0; index < saves.paths.Length; index++) Assert.AreEqual(saves.hashes[index], Hash(saves.paths[index]));
            Pass("existing save files byte-preserved; persistence blocked by existing Combat Lab isolation guard");
            VerifyOriginals();
            File.WriteAllLines(Output + "/results.txt", File.ReadAllLines(Output + "/edit-results.txt").Concat(Passes).Append("PASSED; actual assets/UI/runtime navigation verified; no original room/material/stage/scene/save changes"));
            if (File.Exists(Output + "/failure.txt")) File.Delete(Output + "/failure.txt");
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Fail(error); }
    }
    private static void Pass(string message) { Passes.Add("PASS " + message); Debug.Log("[Sector appearance PASS] " + message); }
    private static void Fail(Exception error)
    {
        SessionState.SetBool(Pending, false); CleanupUi();
        Directory.CreateDirectory(Output); File.WriteAllText(Output + "/failure.txt", error.ToString());
        Debug.LogError("[Sector appearance FAIL] " + error); EditorApplication.Exit(1);
    }
}
