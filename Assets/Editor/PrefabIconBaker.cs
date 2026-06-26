#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class PrefabIconBaker
{
    private const string OutputFolder = "Assets/Generated/PrefabIcons";

    [MenuItem("Tools/VSplatter/Bake Selected Prefab Icons")]
    private static void BakeSelectedPrefabIcons()
    {
        if (!Directory.Exists(OutputFolder))
            Directory.CreateDirectory(OutputFolder);

        foreach (Object selected in Selection.objects)
        {
            GameObject prefab = selected as GameObject;

            if (prefab == null)
                continue;

            BakePrefab(prefab);
        }

        AssetDatabase.Refresh();
    }

    private static void BakePrefab(GameObject prefab)
    {
        GameObject root = new GameObject($"IconBakeRoot_{prefab.name}");
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

        if (instance == null)
        {
            Object.DestroyImmediate(root);
            return;
        }

        instance.transform.SetParent(root.transform, false);
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;

        Bounds bounds = CalculateBounds(instance);

        Vector3 centerOffset = -bounds.center;
        instance.transform.position += centerOffset;

        Camera camera = CreateCamera(root.transform, bounds);
        Light light = CreateLight(root.transform);

        RenderTexture rt = new RenderTexture(
            512,
            512,
            24,
            RenderTextureFormat.ARGB32);

        Texture2D texture = new Texture2D(
            512,
            512,
            TextureFormat.RGBA32,
            false);

        camera.targetTexture = rt;
        RenderTexture previous = RenderTexture.active;

        camera.Render();

        RenderTexture.active = rt;
        texture.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
        texture.Apply();

        RenderTexture.active = previous;
        camera.targetTexture = null;

        string path = $"{OutputFolder}/{prefab.name}_Icon.png";
        File.WriteAllBytes(path, texture.EncodeToPNG());

        Object.DestroyImmediate(texture);
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(root);

        Debug.Log($"Baked prefab icon: {path}");
    }

    private static Camera CreateCamera(Transform parent, Bounds bounds)
    {
        GameObject cameraObject = new GameObject("IconBakeCamera");
        cameraObject.transform.SetParent(parent, false);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        camera.orthographic = true;
        camera.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z) * 1.35f;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 100f;

        Vector3 direction = new Vector3(0.7f, 0.55f, -0.7f).normalized;
        float distance = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) * 4f;

        cameraObject.transform.position = direction * distance;
        cameraObject.transform.LookAt(Vector3.zero);

        return camera;
    }

    private static Light CreateLight(Transform parent)
    {
        GameObject lightObject = new GameObject("IconBakeLight");
        lightObject.transform.SetParent(parent, false);

        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.5f;

        lightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

        return light;
    }

    private static Bounds CalculateBounds(GameObject instance)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
            return new Bounds(instance.transform.position, Vector3.one);

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }
}
#endif