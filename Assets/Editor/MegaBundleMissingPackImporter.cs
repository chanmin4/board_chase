using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Explicit batch entrypoint: import only reviewed missing packs, using filtered vendor URP overlays.</summary>
public static class MegaBundleMissingPackImporter
{
    private const string Output = "Library/MegaBundleImport";
    [Serializable] private class AssetSpec { public string path, guid; public bool folder; }
    [Serializable] private class Pack { public string name, root, basePackage, urpPackage, vendorUrp; public AssetSpec[] assets; }
    [Serializable] private class Original { public string path, sha256; }
    [Serializable] private class Plan { public Pack[] packs; public Original[] originals; public string[] skipExisting; }
    private static Plan _plan;
    private static int _step;
    private static bool _pending, _completed, _finished;
    private static double _started;
    private static readonly List<string> Results = new();

    public static void VerifyOnly()
    {
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Verification must use an explicitly requested batch session.");
        _plan = JsonUtility.FromJson<Plan>(File.ReadAllText(Output + "/plan.json"));
        try { Validate(); Finish(true); }
        catch (Exception ex) { Results.Add("FAIL " + ex); Finish(false); }
    }

    public static void Run()
    {
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Use an explicitly requested batch session outside Play Mode.");
        _plan = JsonUtility.FromJson<Plan>(File.ReadAllText(Output + "/plan.json"));
        if (_plan?.packs == null || _plan.packs.Length != 12)
            throw new InvalidOperationException("Expected the reviewed 12 missing-pack plan.");
        foreach (var pack in _plan.packs)
        {
            if (!pack.root.StartsWith("Assets/AssetsPack/", StringComparison.Ordinal) ||
                Directory.Exists(pack.root) || File.Exists(pack.root + ".meta"))
                throw new InvalidOperationException("A new-pack destination already exists: " + pack.root);
        }
        Progress("CHECK verifying pre-existing files before import");
        VerifyOriginals();
        _step = 0; _started = EditorApplication.timeSinceStartup;
        AssetDatabase.importPackageCompleted += Imported;
        AssetDatabase.importPackageFailed += ImportFailed;
        AssetDatabase.importPackageCancelled += ImportCancelled;
        EditorApplication.update += Tick;
        Progress("READY 12 new packs; 23 existing packs excluded; no global pipeline settings imported");
    }

    private static void Tick()
    {
        if (_finished) return;
        try
        {
            if (EditorApplication.timeSinceStartup - _started > 2400)
                throw new TimeoutException("Missing-pack import exceeded 40 minutes.");
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (_pending)
            {
                if (!_completed) return;
                _pending = false; _completed = false; _step++;
            }
            if (_step >= _plan.packs.Length * 2)
            {
                Validate(); Finish(true); return;
            }
            var pack = _plan.packs[_step / 2];
            foreach (var asset in pack.assets)
            {
                string actual = AssetDatabase.GUIDToAssetPath(asset.guid);
                if (!string.IsNullOrEmpty(actual) && !string.Equals(actual, asset.path, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Import would relocate an existing GUID: " + asset.guid + ": " + actual);
            }
            string file = _step % 2 == 0 ? pack.basePackage : pack.urpPackage;
            Progress($"IMPORT {_step + 1}/24 {pack.name} {(_step % 2 == 0 ? "base" : "vendor URP")}");
            _pending = true;
            // Unity's native package paths use forward slashes, including on Windows.
            AssetDatabase.ImportPackage(Path.GetFullPath(file).Replace('\\', '/'), false);
        }
        catch (Exception ex) { Results.Add("FAIL " + ex); Finish(false); }
    }

    private static void Imported(string name) { _completed = true; Progress("IMPORTED " + name); }
    private static void ImportFailed(string name, string error) { Results.Add("FAIL import " + name + ": " + error); Finish(false); }
    private static void ImportCancelled(string name) { Results.Add("FAIL import cancelled " + name); Finish(false); }

    private static void Validate()
    {
        Progress("VERIFY original files, imported GUIDs, prefabs, materials and native URP previews");
        if (!(GraphicsSettings.currentRenderPipeline is UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset))
            throw new InvalidOperationException("The existing project pipeline is not URP.");
        foreach (var pack in _plan.packs)
        {
            foreach (var asset in pack.assets)
            {
                if (!string.Equals(asset.path, AssetDatabase.GUIDToAssetPath(asset.guid), StringComparison.OrdinalIgnoreCase) ||
                    !File.Exists(asset.path + ".meta") || (!asset.folder && !File.Exists(asset.path)))
                    throw new InvalidOperationException("Imported asset/GUID missing: " + asset.path + " " + asset.guid);
            }
            string[] materials = AssetDatabase.FindAssets("t:Material", new[] { pack.root });
            var shaderNames = new HashSet<string>();
            foreach (var guid in materials)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                var shader = material != null ? material.shader : null;
                if (shader == null || shader.name == "Hidden/InternalErrorShader" || !shader.isSupported ||
                    shader.name == "Standard" || shader.name == "Standard (Specular setup)" || shader.name.StartsWith("Legacy Shaders/"))
                    throw new InvalidOperationException("New-pack material has missing / unsupported / unconverted shader: " + path + " => " + shader?.name);
                shaderNames.Add(shader.name);
                foreach (var property in material.GetTexturePropertyNames())
                {
                    var texture = material.GetTexture(property);
                    if (texture != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(texture)))
                        throw new InvalidOperationException("New-pack material has a nonpersistent texture: " + path);
                }
            }
            var prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { pack.root })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToArray();
            if (prefabs.Length == 0 || materials.Length == 0)
                throw new InvalidOperationException("Pack has no imported prefab/material content: " + pack.name);
            foreach (string path in prefabs)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) throw new InvalidOperationException("Prefab cannot load: " + path);
                RepairBuiltinDefaultsOnlyInNewPrefab(path, prefab);
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
                {
                    var rendererMaterials = renderer.sharedMaterials;
                    for (int materialIndex = 0; materialIndex < rendererMaterials.Length; materialIndex++)
                    {
                        var material = rendererMaterials[materialIndex];
                        // A disabled particle-trail slot is intentionally empty in the vendor prefabs.
                        var particle = renderer is ParticleSystemRenderer ? renderer.GetComponent<ParticleSystem>() : null;
                        if (material == null && materialIndex == 1 && particle != null && !particle.trails.enabled) continue;
                        if (material == null || material.shader == null || material.shader.name == "Hidden/InternalErrorShader" || !material.shader.isSupported)
                            throw new InvalidOperationException("Prefab material reference missing/unsupported: " + path + " slot " + materialIndex);
                    }
                }
            }
            int SampleScore(string path)
            {
                string name = Path.GetFileNameWithoutExtension(path);
                if (name.Contains("BG") || name.Contains("Plane")) return -100;
                if (name.Contains("Castle") || name.Contains("House")) return 90;
                if (name.Contains("Tree") || name.Contains("Palm")) return 80;
                if (name.Contains("Sofa") || name.Contains("Bed")) return 70;
                return 0;
            }
            string sample = prefabs.Where(p => AssetDatabase.LoadAssetAtPath<GameObject>(p).GetComponentInChildren<MeshRenderer>() != null)
                .OrderByDescending(SampleScore).ThenBy(p => p, StringComparer.Ordinal).FirstOrDefault();
            if (sample == null) throw new InvalidOperationException("No mesh-rendered preview sample: " + pack.name);
            Preview(pack, sample);
            Results.Add($"PASS {pack.name} -> {pack.root}; prefabs={prefabs.Length}, materials={materials.Length}; shaders={string.Join(", ", shaderNames.OrderBy(s => s))}; vendor={pack.vendorUrp}; preview={sample}");
            Progress("VERIFIED " + pack.name);
        }
        VerifyOriginals();
        Results.Add("PASS all " + _plan.originals.Length + " pre-existing Assets / Packages / ProjectSettings files byte-identical (SHA-256)");
        Results.Add("PASS excluded existing packs: " + string.Join(", ", _plan.skipExisting));
    }

    private static void RepairBuiltinDefaultsOnlyInNewPrefab(string path, GameObject prefab)
    {
        bool IsBuiltinLegacy(Material material) => material != null && material.shader != null &&
            (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(material)) || AssetDatabase.GetAssetPath(material) == "Resources/unity_builtin_extra") &&
            (material.shader.name == "Standard" || material.shader.name.StartsWith("Legacy Shaders/") ||
             material.shader.name.StartsWith("Particles/") || material.shader.name.StartsWith("Mobile/Particles/"));
        if (!prefab.GetComponentsInChildren<Renderer>(true).Any(r => r.sharedMaterials.Any(IsBuiltinLegacy))) return;
        var pipeline = (UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
        var contents = PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach (var renderer in contents.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                    if (IsBuiltinLegacy(materials[i]))
                    {
                        var replacement = renderer is ParticleSystemRenderer ? pipeline.defaultParticleMaterial : pipeline.defaultMaterial;
                        if (replacement == null || replacement.shader == null || !replacement.shader.isSupported)
                            throw new InvalidOperationException("Current URP default material is unavailable: " + path);
                        materials[i] = replacement;
                    }
                renderer.sharedMaterials = materials;
            }
            PrefabUtility.SaveAsPrefabAsset(contents, path);
            Results.Add("INFO restored new-pack prefab uses equivalent URP default materials: " + path);
        }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
    }

    private static void Preview(Pack pack, string asset)
    {
        var preview = new PreviewRenderUtility();
        Texture2D pixels = null;
        try
        {
            preview.cameraFieldOfView = 32;
            preview.camera.clearFlags = CameraClearFlags.SolidColor;
            preview.camera.backgroundColor = new Color(.11f, .14f, .18f);
            preview.ambientColor = new Color(.6f, .6f, .6f);
            preview.lights[0].intensity = 1.4f;
            preview.lights[0].transform.rotation = Quaternion.Euler(40, 140, 0);
            preview.lights[1].intensity = .8f;
            preview.lights[1].transform.rotation = Quaternion.Euler(340, 20, 0);
            var instance = preview.InstantiatePrefabInScene(AssetDatabase.LoadAssetAtPath<GameObject>(asset));
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            foreach (var component in instance.GetComponentsInChildren<Behaviour>(true)) component.enabled = false;
            var renderers = instance.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
            if (renderers.Length == 0) throw new InvalidOperationException("Preview has no visible renderer: " + asset);
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            float radius = Mathf.Max(.2f, bounds.extents.magnitude);
            Vector3 direction = new Vector3(1.2f, .85f, -1.3f).normalized;
            preview.camera.transform.position = bounds.center + direction * radius * 4.4f;
            preview.camera.transform.LookAt(bounds.center);
            preview.camera.nearClipPlane = Mathf.Max(.01f, radius * .02f);
            preview.camera.farClipPlane = radius * 12 + 10;
            preview.BeginPreview(new Rect(0, 0, 360, 260), GUIStyle.none);
            Texture image;
            try { preview.Render(true, false); }
            finally { image = preview.EndPreview(); }
            var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = image as RenderTexture;
                pixels = new Texture2D(360, 260, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, 360, 260), 0, 0); pixels.Apply();
            }
            finally { RenderTexture.active = previous; }
            Directory.CreateDirectory(Output + "/previews");
            File.WriteAllBytes(Output + "/previews/" + pack.name + ".png", pixels.EncodeToPNG());
        }
        finally { if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels); preview.Cleanup(); }
    }

    private static void VerifyOriginals()
    {
        System.Threading.Tasks.Parallel.ForEach(_plan.originals,
            new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = 4 }, file =>
        {
            if (!File.Exists(file.path)) throw new IOException("Pre-existing file was removed: " + file.path);
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(file.path);
            string hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
            if (hash != file.sha256) throw new IOException("Pre-existing file changed; import stopped: " + file.path);
        });
    }

    private static void Progress(string line)
    {
        Directory.CreateDirectory(Output);
        File.AppendAllText(Output + "/progress.txt", DateTime.Now.ToString("s") + " " + line + "\n");
        Debug.Log("[MegaBundle missing packs] " + line);
    }

    private static void Finish(bool success)
    {
        if (_finished) return; _finished = true;
        EditorApplication.update -= Tick;
        AssetDatabase.importPackageCompleted -= Imported;
        AssetDatabase.importPackageFailed -= ImportFailed;
        AssetDatabase.importPackageCancelled -= ImportCancelled;
        Results.Add("SUCCESS=" + success);
        File.WriteAllLines(Output + "/results.txt", Results);
        Progress(success ? "DONE 12 packs imported + URP verified; originals preserved" : "STOPPED see results.txt");
        EditorApplication.Exit(success ? 0 : 1);
    }
}
