using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Melon
{
    /// <summary>
    /// Captures the tiered bullet prefabs with the project's real Icon Maker camera,
    /// lighting and materials. Existing PNG meta files are intentionally preserved.
    /// </summary>
    public static class BulletTierIconBatchRenderer
    {
        private const string DemoScenePath = "Assets/IconMaker/Scenes/Demo.unity";
        private const string PrefabRoot = "Assets/Prefabs/GameScene/TreasureObject/Bullet";
        private const string IconRoot = "Assets/Art/GameItem/Icon/Bullet";
        private const string PendingRequestPath = "Library/BulletTierIconCapture.request";
        private const int TierCount = 8;
        private const int Resolution = 512;
        private const int CaptureLayer = 31;
        private const float FrameFill = 0.78f;
        private const float TargetDistance = 3f;

        private static readonly string[] Families =
        {
            "LightBullet",
            "MediumBullet",
            "HeavyBullet",
            "SniperBullet",
            "ShotgunShell"
        };

        private static readonly Quaternion[] HorizontalCandidates =
        {
            Quaternion.identity,
            Quaternion.Euler(0f, 90f, 0f),
            Quaternion.Euler(0f, -90f, 0f),
            Quaternion.Euler(0f, 0f, 90f),
            Quaternion.Euler(0f, 0f, -90f),
            Quaternion.Euler(90f, 0f, 0f),
            Quaternion.Euler(-90f, 0f, 0f)
        };

        [MenuItem("Tools/Icon Maker/Bullet Tier Icons/Render All 40 Icons", priority = 30)]
        public static void RenderAllFromMenu()
        {
            RenderAll(showCompletionDialog: true);
        }

        [MenuItem("Tools/Icon Maker/Bullet Tier Icons/Render All 40 Icons", true)]
        private static bool CanRenderAll()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode &&
                   !EditorApplication.isCompiling &&
                   !EditorApplication.isUpdating;
        }

        [InitializeOnLoadMethod]
        private static void ProcessRequestedCaptureAfterCompile()
        {
            if (!File.Exists(PendingRequestPath))
                return;

            // A request is only valid for the edit session that created it. This avoids
            // an old failed compile unexpectedly starting a batch capture days later.
            DateTime age = File.GetLastWriteTimeUtc(PendingRequestPath);
            if (DateTime.UtcNow - age > TimeSpan.FromHours(12))
            {
                File.Delete(PendingRequestPath);
                return;
            }

            EditorApplication.delayCall += TryProcessPendingRequest;
        }

        private static void TryProcessPendingRequest()
        {
            if (!File.Exists(PendingRequestPath))
                return;

            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling ||
                EditorApplication.isUpdating)
            {
                Debug.LogWarning(
                    "[BulletTierIconBatchRenderer] Actual bullet icon capture is queued. " +
                    "Exit Play Mode, then use Tools > Icon Maker > Bullet Tier Icons > Render All 40 Icons.");
                return;
            }

            File.Delete(PendingRequestPath);
            RenderAll(showCompletionDialog: false);
        }

        public static void RenderAll(bool showCompletionDialog)
        {
            if (!CanRenderAll())
            {
                Debug.LogWarning("[BulletTierIconBatchRenderer] Capture is available only in Edit Mode after compilation finishes.");
                return;
            }

            List<CaptureEntry> entries = BuildEntries();
            if (entries.Count != Families.Length * TierCount)
                return;

            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene demoScene = SceneManager.GetSceneByPath(DemoScenePath);
            bool loadedDemoForCapture = !demoScene.IsValid() || !demoScene.isLoaded;
            Scene captureScene = default;

            try
            {
                if (loadedDemoForCapture)
                    demoScene = EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Additive);

                if (!demoScene.IsValid() || !demoScene.isLoaded)
                    throw new InvalidOperationException($"Could not load Icon Maker scene: {DemoScenePath}");

                SceneManager.SetActiveScene(demoScene);
                GameObject sourceRoot = FindIconMakerRoot(demoScene);
                if (sourceRoot == null)
                    throw new InvalidOperationException("Icon Maker scene does not contain an ImageManager root.");

                // Camera.Render does not render ordinary scene objects stored in an editor
                // Preview Scene. Use an unsaved additive scene and isolate it by layer instead.
                captureScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                GameObject captureRig = UnityEngine.Object.Instantiate(sourceRoot);
                captureRig.name = "Bullet Tier Icon Capture Rig";
                SceneManager.MoveGameObjectToScene(captureRig, captureScene);
                captureRig.SetActive(true);

                Camera camera = captureRig.GetComponentInChildren<Camera>(true);
                Light light = captureRig.GetComponentInChildren<Light>(true);
                Transform visualRoot = FindDescendant(captureRig.transform, "Visual");
                if (camera == null || light == null || visualRoot == null)
                    throw new InvalidOperationException("Icon Maker capture rig is missing Camera, Light, or Visual root.");

                ClearChildren(visualRoot);
                ConfigureCaptureRig(camera, light, visualRoot);

                List<CapturedIcon> capturedIcons = new List<CapturedIcon>(entries.Count);

                for (int i = 0; i < entries.Count; i++)
                {
                    CaptureEntry entry = entries[i];
                    EditorUtility.DisplayProgressBar(
                        "Rendering actual bullet icons",
                        $"{entry.Family} LV{entry.Tier} ({i + 1}/{entries.Count})",
                        (float)i / entries.Count);

                    capturedIcons.Add(new CapturedIcon(
                        entry,
                        CaptureOne(entry, captureScene, visualRoot, camera)));
                }

                // Do not replace any existing icon until every capture passed validation.
                // This keeps a single bad camera/render setup from blanking all 40 assets.
                for (int i = 0; i < capturedIcons.Count; i++)
                {
                    CapturedIcon captured = capturedIcons[i];
                    string absolutePath = ToAbsolutePath(captured.Entry.IconPath);
                    Directory.CreateDirectory(Path.GetDirectoryName(absolutePath) ?? Application.dataPath);
                    File.WriteAllBytes(absolutePath, captured.Png);
                }

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                for (int i = 0; i < entries.Count; i++)
                {
                    ConfigureSpriteImporter(entries[i].IconPath);
                    EditorUtility.DisplayProgressBar(
                        "Importing actual bullet icons",
                        $"{entries[i].Family} LV{entries[i].Tier} ({i + 1}/{entries.Count})",
                        (float)i / entries.Count);
                }

                AssetDatabase.SaveAssets();
                Debug.Log(
                    $"[BulletTierIconBatchRenderer] Rendered {entries.Count} actual 512x512 transparent icons " +
                    "with the Icon Maker camera and lighting. Existing icon GUIDs were preserved.");

                if (showCompletionDialog)
                {
                    EditorUtility.DisplayDialog(
                        "Bullet Icon Capture",
                        $"Rendered {entries.Count} actual bullet icons.\n\n" +
                        "The existing PNG paths and GUIDs were preserved.",
                        "OK");
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (showCompletionDialog)
                    EditorUtility.DisplayDialog("Bullet Icon Capture Failed", exception.Message, "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();

                if (captureScene.IsValid() && captureScene.isLoaded)
                    EditorSceneManager.CloseScene(captureScene, removeScene: true);

                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                    SceneManager.SetActiveScene(previousActiveScene);

                if (loadedDemoForCapture && demoScene.IsValid() && demoScene.isLoaded)
                    EditorSceneManager.CloseScene(demoScene, removeScene: true);
            }
        }

        private static List<CaptureEntry> BuildEntries()
        {
            List<CaptureEntry> entries = new List<CaptureEntry>(Families.Length * TierCount);

            foreach (string family in Families)
            {
                for (int tier = 1; tier <= TierCount; tier++)
                {
                    string prefabPath = $"{PrefabRoot}/{family}/{family}LV{tier}.prefab";
                    string iconPath = $"{IconRoot}/{family}/{family}LV{tier}.png";
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefab == null)
                    {
                        Debug.LogError($"[BulletTierIconBatchRenderer] Bullet prefab is missing: {prefabPath}");
                        return new List<CaptureEntry>();
                    }

                    entries.Add(new CaptureEntry(family, tier, prefab, iconPath));
                }
            }

            return entries;
        }

        private static GameObject FindIconMakerRoot(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<ImageManager>(true) != null)
                    return root;
            }

            return null;
        }

        private static void ConfigureCaptureRig(Camera camera, Light light, Transform visualRoot)
        {
            visualRoot.localPosition = Vector3.zero;
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localScale = Vector3.one;

            camera.transform.position = Vector3.zero;
            camera.transform.rotation = Quaternion.identity;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.cullingMask = 1 << CaptureLayer;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.fieldOfView = 60f;
            camera.aspect = 1f;
            camera.orthographic = false;
            camera.usePhysicalProperties = false;
            camera.gameObject.SetActive(true);

            light.gameObject.SetActive(true);
            light.enabled = true;
            light.cullingMask = 1 << CaptureLayer;
            light.transform.position = camera.transform.position;
            light.transform.rotation = camera.transform.rotation;
        }

        private static byte[] CaptureOne(
            CaptureEntry entry,
            Scene captureScene,
            Transform visualRoot,
            Camera camera)
        {
            GameObject instance = UnityEngine.Object.Instantiate(entry.Prefab);
            instance.name = entry.Prefab.name;
            SceneManager.MoveGameObjectToScene(instance, captureScene);
            instance.transform.SetParent(visualRoot, worldPositionStays: false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            try
            {
                PrepareTarget(instance);
                OrientHorizontally(instance.transform);
                FitToCamera(instance.transform, camera);

                return CapturePng(camera, $"{entry.Family} LV{entry.Tier}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void PrepareTarget(GameObject root)
        {
            SetLayerRecursively(root, CaptureLayer);

            foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                behaviour.enabled = false;

            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = true;
        }

        private static void OrientHorizontally(Transform target)
        {
            Quaternion bestRotation = Quaternion.identity;
            float bestScore = float.NegativeInfinity;

            foreach (Quaternion candidate in HorizontalCandidates)
            {
                target.localRotation = candidate;
                if (!TryGetRendererBounds(target.gameObject, out Bounds bounds))
                    continue;

                float horizontalRatio = bounds.size.x / Mathf.Max(bounds.size.y, 0.0001f);
                float depthPenalty = bounds.size.z / Mathf.Max(bounds.size.x, 0.0001f);
                float score = horizontalRatio - depthPenalty * 0.05f;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestRotation = candidate;
                }
            }

            target.localRotation = bestRotation;
        }

        private static void FitToCamera(Transform target, Camera camera)
        {
            if (!TryGetRendererBounds(target.gameObject, out Bounds bounds))
                throw new InvalidOperationException($"Prefab has no enabled Renderer: {target.name}");

            Vector3 desiredCenter = camera.transform.position + camera.transform.forward * TargetDistance;
            target.position += desiredCenter - bounds.center;

            if (!TryGetRendererBounds(target.gameObject, out bounds))
                return;

            float halfHeight = Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * TargetDistance;
            float halfWidth = halfHeight * camera.aspect;
            float widthScale = (halfWidth * 2f * FrameFill) / Mathf.Max(bounds.size.x, 0.0001f);
            float heightScale = (halfHeight * 2f * FrameFill) / Mathf.Max(bounds.size.y, 0.0001f);
            float scale = Mathf.Min(widthScale, heightScale);
            target.localScale *= scale;

            if (TryGetRendererBounds(target.gameObject, out bounds))
                target.position += desiredCenter - bounds.center;
        }

        private static byte[] CapturePng(Camera camera, string captureLabel)
        {
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            CameraClearFlags previousFlags = camera.clearFlags;
            Color previousColor = camera.backgroundColor;

            RenderTexture target = RenderTexture.GetTemporary(
                Resolution,
                Resolution,
                24,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Default,
                4);
            Texture2D texture = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false, false);

            try
            {
                camera.targetTexture = target;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.Render();

                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0f, 0f, Resolution, Resolution), 0, 0, false);
                texture.Apply(false, false);

                ValidateCapture(texture, captureLabel);
                return texture.EncodeToPNG();
            }
            finally
            {
                camera.targetTexture = previousTarget;
                camera.clearFlags = previousFlags;
                camera.backgroundColor = previousColor;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void ValidateCapture(Texture2D texture, string captureLabel)
        {
            Color32[] pixels = texture.GetPixels32();
            int visiblePixelCount = 0;
            int minX = Resolution;
            int minY = Resolution;
            int maxX = -1;
            int maxY = -1;

            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a <= 4)
                    continue;

                visiblePixelCount++;
                int x = i % Resolution;
                int y = i / Resolution;
                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
            }

            int visibleWidth = maxX >= minX ? maxX - minX + 1 : 0;
            int visibleHeight = maxY >= minY ? maxY - minY + 1 : 0;
            if (visiblePixelCount < 256 || visibleWidth < 32 || visibleHeight < 8)
            {
                throw new InvalidOperationException(
                    $"Icon capture is blank or too small: {captureLabel}. " +
                    $"visiblePixels={visiblePixelCount}, bounds={visibleWidth}x{visibleHeight}. " +
                    "Existing PNG files were not replaced.");
            }
        }

        private static void ConfigureSpriteImporter(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException($"Could not load TextureImporter: {assetPath}");

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = Resolution;
            importer.SaveAndReimport();
        }

        private static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
                .ToArray();

            if (renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return true;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root.name == name)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDescendant(root.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static void ClearChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(root.GetChild(i).gameObject);
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            root.layer = layer;
            foreach (Transform child in root.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        private static string ToAbsolutePath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
                throw new InvalidOperationException("Could not resolve the Unity project root.");

            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private readonly struct CaptureEntry
        {
            public CaptureEntry(string family, int tier, GameObject prefab, string iconPath)
            {
                Family = family;
                Tier = tier;
                Prefab = prefab;
                IconPath = iconPath;
            }

            public string Family { get; }
            public int Tier { get; }
            public GameObject Prefab { get; }
            public string IconPath { get; }
        }

        private readonly struct CapturedIcon
        {
            public CapturedIcon(CaptureEntry entry, byte[] png)
            {
                Entry = entry;
                Png = png;
            }

            public CaptureEntry Entry { get; }
            public byte[] Png { get; }
        }
    }
}
