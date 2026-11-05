using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Registered visual surfaces; gameplay hierarchy, transforms and colliders are never changed.
/// A smooth floor is one visual shell over the original collision tiles, not another physical floor.
/// Baselines are serialized so a saved Variant can restore its original appearance.
/// </summary>
[DisallowMultipleComponent]
public sealed class SectorAppearance : MonoBehaviour
{
    public enum SurfaceKind { Floor, Wall }

    [Serializable] public sealed class Surface
    {
        public Renderer renderer;
        public SurfaceKind kind;
        public Material[] originalMaterials;
        public bool originalEnabled;
    }

    [SerializeField] private SectorThemeSO _theme;
    [SerializeField] private Transform _floorRoot;
    [SerializeField] private List<Surface> _surfaces = new();
    [SerializeField, HideInInspector] private List<GameObject> _generatedVisuals = new();
    public SectorThemeSO Theme => _theme;
    public Transform FloorRoot => _floorRoot;
    public IReadOnlyList<Surface> Surfaces => _surfaces;
    public IReadOnlyList<GameObject> GeneratedVisuals => _generatedVisuals;

    private void Awake()
    {
        if (_theme != null) Apply();
    }

    public void SetTheme(SectorThemeSO theme) => _theme = theme;

    /// <summary>Explicit setup, called by Maker or when a stage theme is requested. Browsing never calls this.</summary>
    public void RegisterExistingSurfaces()
    {
        var registered = new HashSet<Renderer>();
        foreach (var surface in _surfaces) if (surface.renderer != null) registered.Add(surface.renderer);
        var visual = transform.Find("SectorVisual");
        if (visual == null) return;
        if (_floorRoot == null)
            foreach (Transform child in visual)
                if (child.name.StartsWith("Sector_Board", StringComparison.Ordinal) || child.name == "Floor")
                { _floorRoot = child; break; }
        if (_floorRoot != null)
            foreach (var renderer in _floorRoot.GetComponentsInChildren<MeshRenderer>(true))
                AddSurface(renderer, SurfaceKind.Floor, registered);
        var walls = visual.Find("Barriers&Portal");
        if (walls == null) walls = visual.Find("Walls");
        if (walls != null)
            foreach (var renderer in walls.GetComponentsInChildren<MeshRenderer>(true))
                if (!IsProtectedWallPart(renderer.transform, walls)) AddSurface(renderer, SurfaceKind.Wall, registered);
    }

    private bool IsGenerated(Transform candidate)
    {
        if (candidate.name == "ThemeFloor_NoCollision" || candidate.name == "ThemeWall_NoCollision") return true;
        foreach (var go in _generatedVisuals)
            if (go != null && (candidate == go.transform || candidate.IsChildOf(go.transform))) return true;
        return false;
    }

    private void AddSurface(Renderer renderer, SurfaceKind kind, HashSet<Renderer> registered)
    {
        if (renderer == null || IsGenerated(renderer.transform)) return;
        if (!registered.Add(renderer)) return;
        _surfaces.Add(new Surface { renderer = renderer, kind = kind,
            originalMaterials = renderer.sharedMaterials, originalEnabled = renderer.enabled });
    }

    private static bool IsProtectedWallPart(Transform part, Transform root)
    {
        for (var current = part; current != null && current != root; current = current.parent)
        {
            string label = current.name.ToLowerInvariant();
            if (label.Contains("door") || label.Contains("portal") || label.Contains("particle") ||
                current.GetComponent<SectorPortal>() != null || current.GetComponent<Animator>() != null)
                return true;
        }
        return false;
    }

    public void Restore(Action<Object> destroy = null)
    {
        foreach (var surface in _surfaces)
        {
            if (surface.renderer == null) continue;
            surface.renderer.enabled = surface.originalEnabled;
            surface.renderer.sharedMaterials = surface.originalMaterials;
        }
        foreach (var go in _generatedVisuals)
        {
            if (go == null) continue;
            // Destroy is deferred in Play Mode: disable immediately to avoid a visible duplicate.
            if (destroy != null) destroy(go);
            else if (Application.isPlaying) { go.SetActive(false); Destroy(go); }
            else DestroyImmediate(go);
        }
        _generatedVisuals.Clear();
    }

    public void Apply(SectorThemeSO stageDefault = null, Action<Object> destroy = null, Action<GameObject> created = null)
    {
        GameObject floor = _theme != null && _theme.FloorVisual != null ? _theme.FloorVisual : stageDefault?.FloorVisual;
        GameObject wall = _theme != null && _theme.WallVisual != null ? _theme.WallVisual : stageDefault?.WallVisual;
        Material floorMaterial = _theme != null && _theme.FloorMaterial != null ? _theme.FloorMaterial : stageDefault?.FloorMaterial;
        Material wallMaterial = _theme != null && _theme.WallMaterial != null ? _theme.WallMaterial : stageDefault?.WallMaterial;
        if (!SectorThemeSO.IsVisualValid(floor) || !SectorThemeSO.IsVisualValid(wall))
            throw new ArgumentException("외형 프리팹은 루트에 MeshFilter + MeshRenderer가 있는 단일 메시여야 합니다.");
        Restore(destroy);
        Bounds floorBounds = default;
        bool hasFloorBounds = _floorRoot != null && TryFloorBounds(out floorBounds);
        foreach (var surface in _surfaces)
        {
            var renderer = surface.renderer;
            if (renderer == null) continue;
            Material material = surface.kind == SurfaceKind.Floor ? floorMaterial : wallMaterial;
            if (material != null) ReplaceMaterials(renderer, material);
            if (surface.kind == SurfaceKind.Floor && floor != null && hasFloorBounds)
                renderer.enabled = false;
            if (surface.kind == SurfaceKind.Wall && wall != null && surface.originalEnabled)
            {
                var mesh = renderer.GetComponent<MeshFilter>();
                if (mesh == null || mesh.sharedMesh == null) continue;
                MakeVisual(wall, renderer.transform, mesh.sharedMesh.bounds, wallMaterial, renderer, created);
                renderer.enabled = false;
            }
        }
        if (floor != null && hasFloorBounds)
            MakeVisual(floor, _floorRoot, floorBounds, floorMaterial, FirstFloorRenderer(), created);
    }

    private Renderer FirstFloorRenderer()
    {
        foreach (var surface in _surfaces)
            if (surface.kind == SurfaceKind.Floor && surface.renderer != null) return surface.renderer;
        return null;
    }

    private static void ReplaceMaterials(Renderer renderer, Material material)
    {
        var materials = renderer.sharedMaterials;
        for (int i = 0; i < materials.Length; i++) materials[i] = material;
        renderer.sharedMaterials = materials;
    }

    private void MakeVisual(GameObject prefab, Transform parent, Bounds bounds, Material material,
        Renderer source, Action<GameObject> created)
    {
        var mesh = prefab.GetComponent<MeshFilter>().sharedMesh;
        var size = mesh.bounds.size;
        var scale = new Vector3(bounds.size.x / Mathf.Max(.00001f, size.x),
            size.y > .00001f ? bounds.size.y / size.y : 1f, bounds.size.z / Mathf.Max(.00001f, size.z));
        var go = new GameObject(parent == _floorRoot ? "ThemeFloor_NoCollision" : "ThemeWall_NoCollision");
        go.transform.SetParent(parent, false);
        go.transform.localScale = scale;
        go.transform.localPosition = bounds.center - Vector3.Scale(mesh.bounds.center, scale);
        if (parent == _floorRoot && size.y <= .00001f)
        {
            var position = go.transform.localPosition;
            position.y = bounds.max.y - mesh.bounds.center.y;
            go.transform.localPosition = position;
        }
        go.layer = source != null ? source.gameObject.layer : parent.gameObject.layer;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = prefab.GetComponent<MeshRenderer>().sharedMaterials;
        if (material != null) ReplaceMaterials(renderer, material);
        if (source != null)
        {
            renderer.renderingLayerMask = source.renderingLayerMask;
            renderer.shadowCastingMode = source.shadowCastingMode;
            renderer.receiveShadows = source.receiveShadows;
        }
        _generatedVisuals.Add(go);
        created?.Invoke(go);
    }

    private bool TryFloorBounds(out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        foreach (var surface in _surfaces)
        {
            if (surface.kind != SurfaceKind.Floor || surface.renderer == null || !surface.originalEnabled) continue;
            var box = surface.renderer.GetComponent<BoxCollider>();
            var mesh = surface.renderer.GetComponent<MeshFilter>();
            Bounds local;
            if (box != null && box.enabled && !box.isTrigger) local = new Bounds(box.center, box.size);
            else if (mesh != null && mesh.sharedMesh != null) local = mesh.sharedMesh.bounds;
            else continue;
            for (int corner = 0; corner < 8; corner++)
            {
                var p = local.center + Vector3.Scale(local.extents,
                    new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                p = _floorRoot.InverseTransformPoint(surface.renderer.transform.TransformPoint(p));
                if (!found) { bounds = new Bounds(p, Vector3.zero); found = true; }
                else bounds.Encapsulate(p);
            }
        }
        return found;
    }

    public static void ApplyStageDefault(SectorRuntime sector, SectorThemeSO theme)
    {
        if (sector == null || theme == null) return;
        var appearance = sector.GetComponent<SectorAppearance>();
        if (appearance == null) appearance = sector.gameObject.AddComponent<SectorAppearance>();
        appearance.RegisterExistingSurfaces();
        appearance.Apply(theme);
    }
}
