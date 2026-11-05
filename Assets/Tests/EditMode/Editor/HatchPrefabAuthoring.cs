using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Explicit, closed-editor hatch import/authoring. Never runs on a normal editor startup.</summary>
public static class HatchPrefabAuthoring
{
    private const string Output = "Library/HatchPrefabAuthoring";
    private const string Source = @"D:\다운로드\lowpoly-hatch-trap-door\source\HATCH.fbx";
    private const string Model = "Assets/Art/Models/Hatch/HATCH.fbx";
    private const string Closed = "Assets/Prefabs/GameScene/Sector/ClosedHatch.prefab";
    private const string Opened = "Assets/Prefabs/GameScene/Sector/OpenedHatch.prefab";
    private const string MeshFolder = "Assets/Art/Mesh/Hatch";
    private const string MaterialFolder = "Assets/Art/Materials/GameScene/Hatch";
    private const string Pending = "HatchPrefabAuthoring.Play";
    private const string ModeKey = "HatchPrefabAuthoring.Mode";
    [Serializable] private sealed class Hashes { public string[] paths, hashes; }
    [Serializable] private sealed class MeshInfo
    {
        public string path; public Vector3 position, scale; public Quaternion rotation;
        public Bounds bounds; public Vector3[] vertices; public int[][] triangles;
    }

    public static void Audit()
    {
        RequireExplicit(); Directory.CreateDirectory(Output);
        try
        {
            CaptureHashes(); EnsureFolder("Assets/Art/Models/Hatch");
            if (!File.Exists(Model)) File.Copy(Source, Model);
            Check(Hash(Source) == Hash(Model), "Imported FBX must exactly match supplied original.");
            AssetDatabase.ImportAsset(Model, ImportAssetOptions.ForceSynchronousImport);
            var importer = (ModelImporter)AssetImporter.GetAtPath(Model);
            importer.isReadable = true; importer.importAnimation = false;
            importer.importLights = false; importer.importCameras = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(Model);
            Check(root != null, "Imported hatch model exists.");
            var lines = new List<string>();
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = filter.sharedMesh;
                lines.Add($"{PathOf(filter.transform)} vertices={mesh.vertexCount} submeshes={mesh.subMeshCount} bounds={mesh.bounds} transform={filter.transform.localPosition} {filter.transform.localEulerAngles} {filter.transform.localScale}");
                foreach (var material in filter.GetComponent<Renderer>().sharedMaterials)
                    lines.Add($" material={material?.name} shader={material?.shader?.name} color={material?.color}");
                // Native imported, readable geometry for hinge inspection; Library only.
                File.WriteAllText(Output + "/mesh-" + filter.name + ".json", JsonUtility.ToJson(new MeshInfo
                { path = PathOf(filter.transform), bounds = mesh.bounds, position = filter.transform.localPosition,
                    rotation = filter.transform.localRotation, scale = filter.transform.localScale,
                    vertices = mesh.vertices, triangles = Enumerable.Range(0, mesh.subMeshCount).Select(i => mesh.GetTriangles(i)).ToArray() }, true));
            }
            File.WriteAllLines(Output + "/audit.txt", lines);
            StartPlay("audit");
        }
        catch (Exception error) { Fail(error); }
    }

    public static void Create()
    {
        RequireExplicit(); Directory.CreateDirectory(Output);
        try
        {
            if (File.Exists(Output + "/PASSED.txt")) File.Delete(Output + "/PASSED.txt");
            Check(File.Exists(Output + "/audit-passed.txt"), "Run the native model audit first.");
            Check(Hash(Source) == Hash(Model), "Source FBX is unchanged.");
            EnsureFolder(MeshFolder); EnsureFolder(MaterialFolder);
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(Model);
            var filters = source.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh.vertexCount > 0).ToArray();
            Check(filters.Length == 1 && filters[0].sharedMesh.subMeshCount == 4, "Expected the audited single cover mesh.");
            var filter = filters[0];
            var matrix = source.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            var vertices = filter.sharedMesh.vertices.Select(matrix.MultiplyPoint3x4).ToArray();
            Bounds original = new Bounds(vertices[0], Vector3.zero); foreach (var v in vertices.Skip(1)) original.Encapsulate(v);
            float factor = 2.4f / Mathf.Max(original.size.x, original.size.z);
            Vector3 offset = new Vector3(-original.center.x * factor, .18f - original.min.y * factor, -original.center.z * factor);
            Mesh cover = Object.Instantiate(filter.sharedMesh); cover.name = "Hatch_Cover";
            cover.vertices = vertices.Select(v => v * factor + offset).ToArray();
            var normalMatrix = matrix.inverse.transpose;
            cover.normals = filter.sharedMesh.normals.Select(v => normalMatrix.MultiplyVector(v).normalized).ToArray();
            if (cover.tangents.Length == cover.vertexCount)
                cover.tangents = filter.sharedMesh.tangents.Select(t =>
                { Vector3 direction = matrix.MultiplyVector(new Vector3(t.x, t.y, t.z)).normalized; return new Vector4(direction.x, direction.y, direction.z, t.w); }).ToArray();
            cover.RecalculateBounds(); cover = SaveOwned(cover, MeshFolder + "/Hatch_Cover.asset");
            Material[] materials = filter.GetComponent<Renderer>().sharedMaterials.Select((m, index) =>
                SaveOwned(Lit(m.color, m.name, .30f, .25f), MaterialFolder + "/Hatch_Cover_" + index + ".mat")).ToArray();
            var steel = SaveOwned(Lit(new Color(.13f, .15f, .17f), "Hatch_Frame_Steel", .38f, .4f), MaterialFolder + "/Hatch_Frame_Steel.mat");
            var shaftMaterials = new[] { new Color(.095f, .105f, .115f), new Color(.028f, .034f, .042f), new Color(.004f, .006f, .009f) }
                .Select((c, i) => SaveOwned(Lit(c, "Hatch_Shaft_Depth_" + i, .05f, 0), MaterialFolder + "/Hatch_Shaft_Depth_" + i + ".mat")).ToArray();
            var black = SaveOwned(new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Hatch_Opening_Black", color = new Color(.002f, .003f, .004f) },
                MaterialFolder + "/Hatch_Opening_Black.mat");
            var ring = SaveOwned(RingMesh("Hatch_Frame_Ring", 1.34f, 1.10f, 0, .18f), MeshFolder + "/Hatch_Frame_Ring.asset");
            var collision = SaveOwned(CylinderMesh("Hatch_Cover_Collision", 1.20f, .18f, .295f, 24), MeshFolder + "/Hatch_Cover_Collision.asset");
            var shaft = SaveOwned(ShaftMesh(), MeshFolder + "/Hatch_Shaft_Interior.asset");
            var bottom = SaveOwned(DiscMesh("Hatch_Shaft_Bottom", 1.10f, -3.2f), MeshFolder + "/Hatch_Shaft_Bottom.asset");
            var mask = SaveOwned(DiscMesh("Hatch_FlatFloorMask", 1.10f, .012f), MeshFolder + "/Hatch_FlatFloorMask.asset");
            var axle = SaveOwned(CylinderMesh("Hatch_Hinge_Axle", .055f, -.82f, .82f, 16), MeshFolder + "/Hatch_Hinge_Axle.asset");
            var root = new GameObject("ClosedHatch");
            try
            {
                int layer = LayerMask.NameToLayer("SectorStructure"); root.layer = layer >= 0 ? layer : 0;
                var frame = Geometry(root.transform, "Frame", ring, new[] { steel });
                frame.AddComponent<MeshCollider>().sharedMesh = ring;
                var pivot = new GameObject("LidPivot"); pivot.layer = root.layer;
                pivot.transform.SetParent(root.transform, false); pivot.transform.localPosition = new Vector3(0, .205f, -1.16f);
                var lid = Geometry(pivot.transform, "Lid", cover, materials); lid.transform.localPosition = -pivot.transform.localPosition;
                var lidCollider = lid.AddComponent<MeshCollider>(); lidCollider.sharedMesh = collision; lidCollider.convex = true;
                var hinge = Geometry(root.transform, "HingeAxle", axle, new[] { steel });
                hinge.transform.localPosition = new Vector3(0, .205f, -1.16f); hinge.transform.localRotation = Quaternion.Euler(0, 0, 90);
                for (int i = -1; i <= 1; i += 2)
                {
                    var foot = GameObject.CreatePrimitive(PrimitiveType.Cube); foot.name = "HingeMount_" + (i < 0 ? "Left" : "Right");
                    foot.layer = root.layer; foot.transform.SetParent(root.transform, false);
                    foot.transform.localPosition = new Vector3(i * .73f, .125f, -1.19f); foot.transform.localScale = new Vector3(.19f, .25f, .24f);
                    foot.GetComponent<Renderer>().sharedMaterial = steel;
                }
                var interior = new GameObject("Shaft_Interior"); interior.layer = root.layer; interior.transform.SetParent(root.transform, false);
                Geometry(interior.transform, "InnerWalls", shaft, shaftMaterials);
                Geometry(interior.transform, "DeepBlackBottom", bottom, new[] { black }); interior.SetActive(false);
                var flat = Geometry(root.transform, "FlatFloorMask_DisableWhenFloorHasHole", mask, new[] { black }); flat.SetActive(false);
                SavePrefab(root, Closed);
            }
            finally { Object.DestroyImmediate(root); }
            var variant = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Closed));
            try
            {
                variant.name = "OpenedHatch"; variant.transform.Find("LidPivot").localRotation = Quaternion.Euler(-100, 0, 0);
                variant.transform.Find("Shaft_Interior").gameObject.SetActive(true);
                variant.transform.Find("FlatFloorMask_DisableWhenFloorHasHole").gameObject.SetActive(true);
                PrefabUtility.RecordPrefabInstancePropertyModifications(variant);
                PrefabUtility.RecordPrefabInstancePropertyModifications(variant.transform.Find("LidPivot"));
                PrefabUtility.RecordPrefabInstancePropertyModifications(variant.transform.Find("Shaft_Interior").gameObject);
                PrefabUtility.RecordPrefabInstancePropertyModifications(variant.transform.Find("FlatFloorMask_DisableWhenFloorHasHole").gameObject);
                SavePrefab(variant, Opened);
            }
            finally { Object.DestroyImmediate(variant); }
            VerifyPrefab(Closed, false); VerifyPrefab(Opened, true);
            File.WriteAllText(Output + "/edit-passed.txt", "Two hatch prefabs, shared URP materials, hinge pose, open annular collision and shaft assets verified.");
            StartPlay("create");
        }
        catch (Exception error) { Fail(error); }
    }

    private static Material Lit(Color color, string name, float smoothness, float metallic)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit"); Check(shader != null, "URP Lit is available.");
        var material = new Material(shader) { name = name, color = color, enableInstancing = true };
        material.SetFloat("_Smoothness", smoothness); material.SetFloat("_Metallic", metallic); return material;
    }
    private static GameObject Geometry(Transform parent, string name, Mesh mesh, Material[] materials)
    {
        var go = new GameObject(name); go.layer = parent.gameObject.layer; go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterials = materials; return go;
    }
    private static void SavePrefab(GameObject root, string path)
    {
        CheckOwned(path); PrefabUtility.SaveAsPrefabAsset(root, path, out bool saved); Check(saved, "Prefab save: " + path); RegisterOwned(path);
    }
    private static T SaveOwned<T>(T asset, string path) where T : Object
    {
        CheckOwned(path); var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing == null) AssetDatabase.CreateAsset(asset, path);
        else { EditorUtility.CopySerialized(asset, existing); Object.DestroyImmediate(asset); asset = existing; EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset); }
        RegisterOwned(path); return asset;
    }
    private static void CheckOwned(string path)
    {
        if (!File.Exists(path) && !File.Exists(path + ".meta")) return;
        string file = Output + "/owned-assets.json";
        var owned = File.Exists(file) ? JsonUtility.FromJson<Hashes>(File.ReadAllText(file)) : new Hashes { paths = Array.Empty<string>(), hashes = Array.Empty<string>() };
        int index = Array.IndexOf(owned.paths, path);
        Check(index >= 0 && owned.hashes[index] == AssetDatabase.AssetPathToGUID(path), "Refusing to overwrite an existing non-owned asset: " + path);
    }
    private static void RegisterOwned(string path)
    {
        string file = Output + "/owned-assets.json";
        var owned = File.Exists(file) ? JsonUtility.FromJson<Hashes>(File.ReadAllText(file)) : new Hashes { paths = Array.Empty<string>(), hashes = Array.Empty<string>() };
        if (owned.paths.Contains(path)) return;
        owned.paths = owned.paths.Append(path).ToArray(); owned.hashes = owned.hashes.Append(AssetDatabase.AssetPathToGUID(path)).ToArray();
        File.WriteAllText(file, JsonUtility.ToJson(owned, true));
    }
    private sealed class GeometryBuilder
    {
        private readonly List<Vector3> vertices = new();
        private readonly List<Vector2> uvs = new();
        private readonly List<int>[] triangles;
        public GeometryBuilder(int submeshes = 1) => triangles = Enumerable.Range(0, submeshes).Select(_ => new List<int>()).ToArray();
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int submesh = 0)
        {
            int index = vertices.Count; vertices.AddRange(new[] { a, b, c, d });
            uvs.AddRange(new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up });
            triangles[submesh].AddRange(new[] { index, index + 1, index + 2, index, index + 2, index + 3 });
        }
        public void Triangle(Vector3 a, Vector3 b, Vector3 c)
        {
            int index = vertices.Count; vertices.AddRange(new[] { a, b, c });
            uvs.AddRange(new[] { Vector2.zero, Vector2.right, Vector2.up }); triangles[0].AddRange(new[] { index, index + 1, index + 2 });
        }
        public Mesh Build(string name)
        {
            var mesh = new Mesh { name = name }; mesh.SetVertices(vertices); mesh.SetUVs(0, uvs); mesh.subMeshCount = triangles.Length;
            for (int i = 0; i < triangles.Length; i++) mesh.SetTriangles(triangles[i], i);
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
    }
    private static Vector3 Circle(float radius, float y, int i, int segments = 48)
    { float angle = i * Mathf.PI * 2 / segments; return new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius); }
    private static Mesh RingMesh(string name, float outer, float inner, float bottom, float top)
    {
        var b = new GeometryBuilder();
        for (int i = 0; i < 48; i++)
        {
            b.Quad(Circle(outer, top, i), Circle(inner, top, i), Circle(inner, top, i + 1), Circle(outer, top, i + 1));
            b.Quad(Circle(outer, bottom, i + 1), Circle(inner, bottom, i + 1), Circle(inner, bottom, i), Circle(outer, bottom, i));
            b.Quad(Circle(outer, bottom, i), Circle(outer, top, i), Circle(outer, top, i + 1), Circle(outer, bottom, i + 1));
            b.Quad(Circle(inner, bottom, i), Circle(inner, bottom, i + 1), Circle(inner, top, i + 1), Circle(inner, top, i));
        }
        return b.Build(name);
    }
    private static Mesh CylinderMesh(string name, float radius, float bottom, float top, int segments)
    {
        var b = new GeometryBuilder();
        for (int i = 0; i < segments; i++)
        {
            b.Quad(Circle(radius, bottom, i, segments), Circle(radius, top, i, segments), Circle(radius, top, i + 1, segments), Circle(radius, bottom, i + 1, segments));
            b.Triangle(new Vector3(0, top, 0), Circle(radius, top, i + 1, segments), Circle(radius, top, i, segments));
            b.Triangle(new Vector3(0, bottom, 0), Circle(radius, bottom, i, segments), Circle(radius, bottom, i + 1, segments));
        }
        return b.Build(name);
    }
    private static Mesh DiscMesh(string name, float radius, float y)
    {
        var b = new GeometryBuilder();
        for (int i = 0; i < 48; i++) b.Triangle(new Vector3(0, y, 0), Circle(radius, y, i + 1), Circle(radius, y, i));
        return b.Build(name);
    }
    private static Mesh ShaftMesh()
    {
        // The ring already supplies the collar's inner wall from y=0 to y=.18.
        // Start the shaft at its bottom edge, not behind that wall (coplanar z-fighting).
        var b = new GeometryBuilder(3); float[] levels = { 0, -.55f, -1.5f, -3.2f };
        for (int level = 0; level < 3; level++)
            for (int i = 0; i < 48; i++)
                b.Quad(Circle(1.10f, levels[level + 1], i), Circle(1.10f, levels[level + 1], i + 1),
                    Circle(1.10f, levels[level], i + 1), Circle(1.10f, levels[level], i), level);
        return b.Build("Hatch_Shaft_Interior");
    }
    private static void VerifyPrefab(string path, bool opened)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Check(root.transform.localScale == Vector3.one && root.transform.localPosition == Vector3.zero, "Placement origin and unit scale.");
            Check(root.GetComponentsInChildren<MonoBehaviour>(true).Length == 0, "New geometry does not attach gameplay/save scripts.");
            var lid = root.transform.Find("LidPivot");
            Check(Quaternion.Angle(lid.localRotation, opened ? Quaternion.Euler(-100, 0, 0) : Quaternion.identity) < .001f, "Correct cover pose.");
            Check(root.transform.Find("Shaft_Interior").gameObject.activeSelf == opened, "Shaft visibility matches pose.");
            Check(root.transform.Find("FlatFloorMask_DisableWhenFloorHasHole").gameObject.activeSelf == opened, "Flat-floor visual mask matches pose.");
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Check(renderer.sharedMaterials.All(m => m != null && m.shader != null && m.shader.name.StartsWith("Universal Render Pipeline/")), "All materials use URP.");
                Check(renderer.GetComponent<MeshFilter>()?.sharedMesh != null, "No missing mesh.");
            }
            Check(root.transform.Find("Frame").GetComponent<MeshCollider>().convex == false, "Frame collision preserves opening.");
            Check(root.transform.Find("LidPivot/Lid").GetComponent<MeshCollider>().convex, "Cover has simple convex collision.");
            var shaft = root.transform.Find("Shaft_Interior/InnerWalls").GetComponent<MeshFilter>().sharedMesh;
            Check(shaft.subMeshCount == 3 && shaft.bounds.min.y < -3, "Hollow shaft extends below floor with three dark depth bands.");
            Check(shaft.bounds.max.y <= root.transform.Find("Frame").GetComponent<MeshFilter>().sharedMesh.bounds.min.y + .00001f,
                "Shaft and ring collar do not overlap.");
            for (int index = 0; index < shaft.vertexCount; index++)
                Check(Vector3.Dot(new Vector3(shaft.vertices[index].x, 0, shaft.vertices[index].z).normalized, shaft.normals[index]) < -.99f, "Shaft normals face inward.");
            Check(!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)), "Prefab GUID is valid.");
            if (opened) Check(PrefabUtility.GetPrefabAssetType(AssetDatabase.LoadAssetAtPath<GameObject>(path)) == PrefabAssetType.Variant, "OpenedHatch is a ClosedHatch variant.");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void RequireExplicit()
    {
        if (!Environment.GetCommandLineArgs().Contains("-hatchPrefabAuthoring"))
            throw new InvalidOperationException("Use the explicit closed-editor hatch authoring launcher.");
    }
    private static void StartPlay(string mode)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(CombatLabRuntime.IsolationKey, true);
        SessionState.SetString(ModeKey, mode); SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod] private static void Register() => EditorApplication.update += VerifyPlay;
    private static void VerifyPlay()
    {
        if (!SessionState.GetBool(Pending, false) || !EditorApplication.isPlaying) return;
        SessionState.SetBool(Pending, false);
        try
        {
            Check(CombatLabRuntime.IsIsolatedSession, "Save isolation is enabled.");
            if (SaveSystem.Active != null)
            { Check(!SaveSystem.Active.LoadSaveDataFromDisk(), "Disk load blocked."); Check(!SaveSystem.Active.TrySaveRunDataToDisk(), "Disk save blocked."); }
            if (SessionState.GetString(ModeKey, "") == "create")
            {
                VerifyCreatedPlay(); VerifyHashes();
                if (File.Exists(Output + "/failure.txt")) File.Delete(Output + "/failure.txt");
                File.WriteAllText(Output + "/PASSED.txt",
                    "PASS ClosedHatch and OpenedHatch native prefab roundtrip; URP materials; -100 degree hinge; 3.2m hollow shaft; flat-floor mask; variant inheritance; closed/open collider raycasts; isolated Play Mode renders; original project/source/save hashes preserved.");
                EditorApplication.Exit(0); return;
            }
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Model));
            var shader = Shader.Find("Universal Render Pipeline/Lit"); Check(shader != null, "URP Lit is available.");
            var temporary = new List<Material>();
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                renderer.sharedMaterials = renderer.sharedMaterials.Select(m =>
                {
                    var replacement = new Material(shader) { color = m != null ? m.color : Color.gray };
                    replacement.SetFloat("_Smoothness", .25f); temporary.Add(replacement); return replacement;
                }).ToArray();
            Render(instance, "audit-angled.png", false); Render(instance, "audit-top.png", true);
            Object.DestroyImmediate(instance); foreach (var material in temporary) Object.DestroyImmediate(material);
            VerifyHashes(); File.WriteAllText(Output + "/audit-passed.txt", "Native FBX import and isolated rendering passed. Original project files preserved.");
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Fail(error); }
    }
    private static void VerifyCreatedPlay()
    {
        foreach (string path in new[] { Closed, Opened })
        {
            bool opened = path == Opened; VerifyPrefab(path, opened);
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            GameObject ground = null; Mesh floorMesh = null; Material floorMaterial = null;
            try
            {
                Physics.SyncTransforms();
                int mask = 1 << root.layer;
                bool hit = Physics.Raycast(new Vector3(0, 4, 0), Vector3.down, out var lidHit, 8, mask, QueryTriggerInteraction.Ignore);
                Check(opened ? !hit : hit && lidHit.collider.name == "Lid", "Opening center collider switches with cover pose.");
                Check(Physics.Raycast(new Vector3(1.24f, 4, 0), Vector3.down, out var rimHit, 8, mask, QueryTriggerInteraction.Ignore) && rimHit.collider.name == "Frame", "Stationary ring retains collision.");
                Transform lid = root.transform.Find("LidPivot/Lid");
                Vector3 point = lid.TransformPoint(lid.GetComponent<MeshCollider>().sharedMesh.bounds.center);
                Vector3 normal = lid.up;
                Check(Physics.Raycast(point + normal * 2, -normal, out var coverHit, 4, mask, QueryTriggerInteraction.Ignore) && coverHit.collider.name == "Lid", "Cover collision follows hinge.");
                floorMaterial = Lit(new Color(.42f, .45f, .48f), "Temporary floor", .05f, 0);
                ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.name = "Temporary existing flat floor";
                ground.transform.position = new Vector3(0, -.05f, 0); ground.transform.localScale = new Vector3(9, .1f, 9);
                Object.DestroyImmediate(ground.GetComponent<Collider>()); ground.GetComponent<Renderer>().sharedMaterial = floorMaterial;
                Render(root, opened ? "OpenedHatch-flat-floor.png" : "ClosedHatch.png", false, true);
                if (opened)
                {
                    Render(root, "OpenedHatch-top.png", true, true);
                    Object.DestroyImmediate(ground);
                    var builder = new GeometryBuilder();
                    for (int i = 0; i < 48; i++)
                    {
                        Vector3 a = Circle(1, 0, i), b = Circle(1, 0, i + 1);
                        Vector3 outerA = a * (4.5f / Mathf.Max(Mathf.Abs(a.x), Mathf.Abs(a.z)));
                        Vector3 outerB = b * (4.5f / Mathf.Max(Mathf.Abs(b.x), Mathf.Abs(b.z)));
                        builder.Quad(outerA, a * 1.10f, b * 1.10f, outerB);
                    }
                    floorMesh = builder.Build("Temporary floor with circular hole");
                    ground = Geometry(root.transform, "Temporary floor with circular hole", floorMesh, new[] { floorMaterial });
                    root.transform.Find("FlatFloorMask_DisableWhenFloorHasHole").gameObject.SetActive(false);
                    Render(root, "OpenedHatch-real-shaft.png", false, true);
                    Check(Physics.Raycast(new Vector3(0, 4, 0), Vector3.down, 8, mask, QueryTriggerInteraction.Ignore) == false, "Decorative shaft does not block opening with a cylinder cap collider.");
                    root.transform.Find("FlatFloorMask_DisableWhenFloorHasHole").gameObject.SetActive(true);
                }
            }
            finally
            {
                if (ground != null) Object.DestroyImmediate(ground);
                Object.DestroyImmediate(root);
                if (floorMesh != null) Object.DestroyImmediate(floorMesh);
                if (floorMaterial != null) Object.DestroyImmediate(floorMaterial);
            }
        }
        Check(Object.FindFirstObjectByType<StageProgressionManager>() == null, "No gameplay progression created in preview.");
    }
    private static void Render(GameObject root, string name, bool top, bool hatchOnly = false)
    {
        var bounds = BoundsOf(root);
        if (hatchOnly)
        {
            var renderers = root.GetComponentsInChildren<Renderer>().Where(r => !PathOf(r.transform).Contains("Shaft_Interior") && !r.name.StartsWith("Temporary")).ToArray();
            bounds = renderers[0].bounds; foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
        }
        var cameraGo = new GameObject("Temporary hatch camera"); var lightGo = new GameObject("Temporary hatch light");
        var camera = cameraGo.AddComponent<Camera>(); var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional; light.intensity = 1.6f; light.transform.rotation = Quaternion.Euler(50, -25, 0);
        camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(root.scene);
        float extent = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        camera.transform.position = bounds.center + (top ? Vector3.up : new Vector3(1.6f, 2.2f, -1.2f).normalized) * extent * 3;
        camera.transform.LookAt(bounds.center, top ? Vector3.forward : Vector3.up);
        camera.orthographic = true; camera.orthographicSize = extent * .72f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.12f, .15f, .18f);
        camera.nearClipPlane = .001f; camera.farClipPlane = extent * 10 + 10;
        var rt = new RenderTexture(1200, 1000, 24); var texture = new Texture2D(1200, 1000, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        try
        {
            rt.Create(); camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); texture.Apply();
            File.WriteAllBytes(Output + "/" + name, texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null; RenderTexture.active = previous; rt.Release();
            Object.DestroyImmediate(rt); Object.DestroyImmediate(texture); Object.DestroyImmediate(cameraGo); Object.DestroyImmediate(lightGo);
        }
    }
    private static Bounds BoundsOf(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(); Check(renderers.Length > 0, "Visible geometry exists.");
        Bounds bounds = renderers[0].bounds; foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds); return bounds;
    }
    private static string PathOf(Transform transform) => transform.parent == null ? transform.name : PathOf(transform.parent) + "/" + transform.name;
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = path.Substring(0, path.LastIndexOf('/')); EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
    }
    private static string Hash(string path)
    { using var stream = File.OpenRead(path); using var sha = SHA256.Create(); return Convert.ToBase64String(sha.ComputeHash(stream)); }
    private static void CaptureHashes()
    {
        string file = Output + "/original-hashes.json";
        if (!File.Exists(file))
        {
            var paths = new[] { "Assets/Prefabs/GameScene/Sector", "Assets/ScriptableObject/GameScene/Stage", "Assets/Art", "Assets/Scenes", "ProjectSettings" }
                .Where(Directory.Exists).SelectMany(p => Directory.GetFiles(p, "*", SearchOption.AllDirectories))
                .Select(p => p.Replace('\\', '/')).Append(Source).ToArray();
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
            for (int i = 0; i < entries.paths.Length; i++) Check(File.Exists(entries.paths[i]) && entries.hashes[i] == Hash(entries.paths[i]), "Original changed: " + entries.paths[i]);
            Debug.Log("[Hatch] Byte-preserved " + entries.paths.Length + " files: " + Path.GetFileName(file));
        }
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Fail(Exception error)
    {
        SessionState.SetBool(Pending, false); Directory.CreateDirectory(Output); File.WriteAllText(Output + "/failure.txt", error.ToString());
        Debug.LogException(error); EditorApplication.Exit(1);
    }
}
