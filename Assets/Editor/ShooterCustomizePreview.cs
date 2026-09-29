using System;
using UnityEditor;
using UnityEngine;

// Owns an isolated preview scene; never inserts preview objects into the user's open scene.
public sealed class ShooterCustomizePreview : IDisposable
{
    private PreviewRenderUtility _renderer;
    private ShooterCustomizeSO _applied;
    private GameObject _equipment;
    public GameObject Root { get; private set; }
    public ShooterVisualRig Rig { get; private set; }
    public Camera Camera => _renderer?.camera;
    public Bounds Bounds { get; private set; }
    public Bounds FramingBounds { get; private set; }
    public bool BodyHasCollider { get; private set; }

    public static GameObject ResolveVisualSource(GameObject input)
    {
        if (input == null || !EditorUtility.IsPersistent(input)) return null;
        var rigs = input.GetComponentsInChildren<ShooterVisualRig>(true);
        if (rigs.Length > 0) return rigs[0].gameObject;
        foreach (var t in input.GetComponentsInChildren<Transform>(true))
            if (t.name == "Visual_Shooter" || (t == input.transform && t.GetComponentInChildren<Renderer>(true) != null &&
                Array.Exists(t.GetComponentsInChildren<Transform>(true), child => child.name == "Meshes"))) return t.gameObject;
        return null;
    }

    public void Build(GameObject source)
    {
        Dispose();
        if (source == null) return;
        _renderer = new PreviewRenderUtility();
        _renderer.cameraFieldOfView = 30f;
        _renderer.camera.clearFlags = CameraClearFlags.SolidColor;
        _renderer.camera.backgroundColor = new Color(.105f, .135f, .17f);
        _renderer.ambientColor = new Color(.48f, .48f, .5f);
        _renderer.lights[0].intensity = 1.25f;
        _renderer.lights[0].transform.rotation = Quaternion.Euler(35, 145, 0);
        _renderer.lights[1].intensity = .8f;
        _renderer.lights[1].transform.rotation = Quaternion.Euler(340, 20, 0);
        // Instantiating into the preview scene avoids dirtying the active scene.
        Root = _renderer.InstantiatePrefabInScene(source);
        Root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        Root.name = "Shooter Customize - isolated preview";
        foreach (var behaviour in Root.GetComponentsInChildren<Behaviour>(true)) behaviour.enabled = false;
        foreach (var collider in Root.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        Rig = Root.GetComponent<ShooterVisualRig>() ?? Root.AddComponent<ShooterVisualRig>();
        Rig.AutoResolveReferences();
        BodyHasCollider = Rig.BodyVisualRootContainsCollider;
        _applied = ScriptableObject.CreateInstance<ShooterCustomizeSO>();
        _applied.hideFlags = HideFlags.HideAndDontSave;
        UpdateBounds();
        Frame();
    }

    public void Apply(ShooterCustomizeDraft draft, bool showEquipment)
    {
        if (Rig == null) return;
        if (_equipment != null) UnityEngine.Object.DestroyImmediate(_equipment);
        _equipment = null;
        Rig.ClearEquipmentOccupancy("CustomizePreview");
        draft.CaptureTo(_applied);
        Rig.ApplyCustomize(_applied);
        if (showEquipment && draft.equipment != null && draft.equipmentPrefab != null)
        {
            Transform socket = Rig.ResolveSocket(draft.equipmentSocket);
            if (socket != null)
            {
                _equipment = UnityEngine.Object.Instantiate(draft.equipmentPrefab, socket);
                draft.equipmentPose.ApplyTo(_equipment.transform);
                Rig.SetEquipmentOccupancy("CustomizePreview", draft.ResolvedEquipmentCoverage);
                Rig.SetEquipmentFaceSlotOccupied("CustomizePreview", draft.equipmentSocket == ShooterVisualSocketId.Face);
            }
        }
        foreach (var behaviour in Root.GetComponentsInChildren<Behaviour>(true)) behaviour.enabled = false;
        foreach (var collider in Root.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        UpdateBounds();
    }

    public void UpdateBounds()
    {
        bool found = false;
        var bounds = new Bounds(Vector3.up, Vector3.one * 2);
        if (Root != null) foreach (var renderer in Root.GetComponentsInChildren<Renderer>())
        {
            if (!renderer.enabled) continue;
            if (!found) { bounds = renderer.bounds; found = true; } else bounds.Encapsulate(renderer.bounds);
        }
        Bounds = bounds;
    }

    public void Frame() { FramingBounds = Bounds; }

    public Texture Render(Rect rect, Vector2 orbit, float zoom, Vector3 pivot)
    {
        if (_renderer == null || rect.width < 2 || rect.height < 2) return null;
        float radius = Mathf.Max(.2f, FramingBounds.extents.magnitude);
        float aspect = rect.width / rect.height;
        float verticalHalfFov = 15 * Mathf.Deg2Rad;
        float halfFov = Mathf.Min(verticalHalfFov, Mathf.Atan(Mathf.Tan(verticalHalfFov) * aspect));
        // Leave room for live proportion edits without moving the camera on every slider tick.
        float distance = radius / Mathf.Sin(halfFov) * 1.35f / Mathf.Max(.1f, zoom);
        Quaternion rotation = Quaternion.Euler(orbit.y, orbit.x, 0);
        _renderer.camera.transform.SetPositionAndRotation(pivot - rotation * Vector3.forward * distance, rotation);
        _renderer.camera.nearClipPlane = Mathf.Max(.005f, distance - radius * 3);
        _renderer.camera.farClipPlane = distance + radius * 10 + 10;
        _renderer.BeginPreview(rect, GUIStyle.none);
        Texture result;
        try { _renderer.Render(true, false); }
        finally { result = _renderer.EndPreview(); }
        return result;
    }

    public void Dispose()
    {
        if (_renderer != null) _renderer.Cleanup();
        _renderer = null;
        if (_applied != null) UnityEngine.Object.DestroyImmediate(_applied);
        _applied = null;
        Root = null;
        Rig = null;
        _equipment = null;
    }
}
