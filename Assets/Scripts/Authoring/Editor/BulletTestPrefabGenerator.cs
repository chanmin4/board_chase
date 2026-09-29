/*
 * Role hierarchy: Authoring > Editor
 * Primary responsibility: Provides the BulletTestPrefabGenerator authoring tool and its editor workflow.
 * Execution boundary: Editor only (Assembly-CSharp-Editor)
 */

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class BulletTestPrefabGenerator
{
    private const string SourceModelPath =
        "Assets/Art/Mesh/Bullet/WIP/WeaponsPack-Bullets.fbx";
    private const string SourceTexturePath =
        "Assets/Art/Mesh/Bullet/WIP/Texture_BaseColor.png";
    private const string MaterialPath =
        "Assets/Art/Mesh/Bullet/WIP/WeaponsPackBulletMaterial.mat";
    private const string TemplatePrefabPath =
        "Assets/Prefabs/GameScene/TreasureObject/Bullet/BulletLV0.prefab";
    private const string OutputFolder =
        "Assets/Prefabs/GameScene/TreasureObject/Bullet/WIP";

    private static readonly string[] SourceMeshNames =
    {
        "SM_Bullet_01",
        "SM_Bullet_02",
        "SM_Bullet_03",
        "SM_Bullet_04",
        "SM_Bullet_05",
        "SM_Bullet_06",
        "SM_Bullet_07",
        "SM_Bullet_08",
        "SM_Bullet_09",
        "SM_Bullet_010"
    };

    private static bool _isGenerating;

    static BulletTestPrefabGenerator()
    {
        EditorApplication.delayCall += GenerateMissingPrefabs;
    }

    // Intentionally not exposed as a Tools menu item. This is a temporary
    // test-prefab maintenance API, not a content-authoring workflow.
    // Purpose: Performs the regenerate prefabs workflow for this component.
    public static void RegeneratePrefabs()
    {
        Generate(true);
    }

    // Purpose: Generates missing prefabs from the authored data.
    public static void GenerateMissingPrefabs()
    {
        Generate(false);
    }

    // Purpose: Generates generate from the authored data.
    private static void Generate(bool force)
    {
        if (_isGenerating || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (!force && AreAllOutputsPresent())
            return;

        GameObject sourceModel =
            AssetDatabase.LoadAssetAtPath<GameObject>(SourceModelPath);
        GameObject templatePrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(TemplatePrefabPath);

        if (sourceModel == null || templatePrefab == null)
            return;

        Dictionary<string, Mesh> sourceMeshes = CollectSourceMeshes(sourceModel);
        if (sourceMeshes.Count != SourceMeshNames.Length)
        {
            Debug.LogError(
                $"[BulletTestPrefabGenerator] Expected {SourceMeshNames.Length} named bullet meshes in {SourceModelPath}, but resolved {sourceMeshes.Count}. " +
                $"Expected names: {string.Join(", ", SourceMeshNames)}");
            return;
        }

        _isGenerating = true;

        try
        {
            Material material = GetOrCreateMaterial();
            if (material == null)
                return;

            for (int i = 0; i < SourceMeshNames.Length; i++)
            {
                string outputPath = $"{OutputFolder}/BulletTest{i + 1}.prefab";
                if (!force && AssetDatabase.LoadAssetAtPath<GameObject>(outputPath) != null)
                    continue;

                CreatePrefab(
                    templatePrefab,
                    sourceMeshes[SourceMeshNames[i]],
                    material,
                    i + 1,
                    outputPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        finally
        {
            _isGenerating = false;
        }
    }

    // Purpose: Performs the are all outputs present workflow for this component.
    private static bool AreAllOutputsPresent()
    {
        for (int i = 0; i < SourceMeshNames.Length; i++)
        {
            string path = $"{OutputFolder}/BulletTest{i + 1}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                return false;
        }

        return true;
    }

    // Purpose: Collects source meshes for this workflow.
    private static Dictionary<string, Mesh> CollectSourceMeshes(GameObject sourceModel)
    {
        Dictionary<string, Mesh> meshes = new(StringComparer.Ordinal);
        Transform[] transforms = sourceModel.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];

            if (Array.IndexOf(SourceMeshNames, current.name) < 0)
                continue;

            Mesh mesh = null;
            MeshFilter meshFilter = current.GetComponent<MeshFilter>();
            if (meshFilter != null)
                mesh = meshFilter.sharedMesh;

            if (mesh == null)
            {
                SkinnedMeshRenderer skinned = current.GetComponent<SkinnedMeshRenderer>();
                if (skinned != null)
                    mesh = skinned.sharedMesh;
            }

            if (mesh != null)
                meshes[current.name] = mesh;
        }

        return meshes;
    }

    // Purpose: Returns the current or create material.
    private static Material GetOrCreateMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(SourceTexturePath);

        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            if (shader == null)
            {
                Debug.LogError("[BulletTestPrefabGenerator] No URP Lit or Standard shader is available.");
                return null;
            }

            material = new Material(shader)
            {
                name = "WeaponsPackBulletMaterial"
            };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", texture);
        else
            material.mainTexture = texture;

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);

        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", 0.15f);

        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.35f);

        EditorUtility.SetDirty(material);
        return material;
    }

    // Purpose: Creates the prefab owned by this workflow.
    private static void CreatePrefab(
        GameObject templatePrefab,
        Mesh sourceMesh,
        Material material,
        int testIndex,
        string outputPath)
    {
        GameObject instance =
            PrefabUtility.InstantiatePrefab(templatePrefab) as GameObject;

        if (instance == null)
            return;

        try
        {
            instance.name = $"BulletTest{testIndex}";
            Transform visualRoot = FindDescendant(instance.transform, "Visual");

            if (visualRoot == null)
            {
                Debug.LogError(
                    $"[BulletTestPrefabGenerator] Template has no Visual child: {TemplatePrefabPath}",
                    templatePrefab);
                return;
            }

            for (int childIndex = visualRoot.childCount - 1; childIndex >= 0; childIndex--)
                UnityEngine.Object.DestroyImmediate(visualRoot.GetChild(childIndex).gameObject);

            GameObject meshObject = new(SourceMeshNames[testIndex - 1]);
            meshObject.transform.SetParent(visualRoot, false);
            meshObject.transform.localPosition = Vector3.zero;
            meshObject.transform.localRotation = Quaternion.identity;
            meshObject.transform.localScale = Vector3.one;

            MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = sourceMesh;

            MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();
            int materialCount = Mathf.Max(1, sourceMesh != null ? sourceMesh.subMeshCount : 1);
            Material[] materials = new Material[materialCount];
            for (int i = 0; i < materials.Length; i++)
                materials[i] = material;
            meshRenderer.sharedMaterials = materials;

            PrefabUtility.SaveAsPrefabAsset(instance, outputPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    // Purpose: Finds the requested descendant.
    private static Transform FindDescendant(Transform root, string objectName)
    {
        if (root == null)
            return null;

        if (root.name == objectName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDescendant(root.GetChild(i), objectName);
            if (found != null)
                return found;
        }

        return null;
    }
}

public sealed class BulletTestPrefabSourcePostprocessor : AssetPostprocessor
{
    // Purpose: Responds to the postprocess all assets callback.
    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        for (int i = 0; i < importedAssets.Length; i++)
        {
            if (importedAssets[i] !=
                    "Assets/Art/Mesh/Bullet/WIP/WeaponsPack-Bullets.fbx" &&
                importedAssets[i] !=
                    "Assets/Art/Mesh/Bullet/WIP/Texture_BaseColor.png")
            {
                continue;
            }

            EditorApplication.delayCall +=
                BulletTestPrefabGenerator.RegeneratePrefabs;
            break;
        }
    }
}
