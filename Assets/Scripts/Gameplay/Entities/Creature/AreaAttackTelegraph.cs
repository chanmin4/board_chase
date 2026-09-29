/*
 * Role hierarchy: Gameplay > Entities > Creature
 * Primary responsibility: Handles AreaAttackTelegraphStyle combat resolution, damage, projectile, or status changes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public struct AreaAttackTelegraphStyle
{
    public Color baseColor;
    public Color fillColor;
    [Min(12)] public int segments;
    public float yOffset;

    public static AreaAttackTelegraphStyle Default
    {
        get
        {
            return new AreaAttackTelegraphStyle
            {
                baseColor = new Color(1f, 0f, 0f, 0.22f),
                fillColor = new Color(1f, 0.05f, 0.02f, 0.55f),
                segments = 72,
                yOffset = 0.05f
            };
        }
    }
}

public sealed class AreaAttackTelegraph : MonoBehaviour
{
    private Transform _base;
    private Transform _fill;
    private Material _baseMaterial;
    private Material _fillMaterial;
    private Mesh _mesh;

    private float _radius;
    private float _duration;
    private float _elapsed;
    private bool _completed;
    private bool _persistent;
    private float _yOffset;

    // Purpose: Spawns circle for the current runtime context.
    public static AreaAttackTelegraph SpawnCircle(
        Vector3 center,
        float radius,
        float duration,
        AreaAttackTelegraphStyle style,
        Transform parent = null)
    {
        if (style.segments <= 0)
            style = AreaAttackTelegraphStyle.Default;

        GameObject go = new GameObject("AreaAttackTelegraph_Circle");

        if (parent != null)
            go.transform.SetParent(parent, true);

        AreaAttackTelegraph telegraph = go.AddComponent<AreaAttackTelegraph>();
        telegraph.InitializeCircle(center, radius, duration, style, false);
        return telegraph;
    }

    /// <summary>
    /// Creates the same filled warning disc used by enemy arc projectiles, but
    /// keeps it alive until the owner explicitly destroys it.
    /// </summary>
    public static AreaAttackTelegraph SpawnPersistentCircle(
        Vector3 center,
        float radius,
        AreaAttackTelegraphStyle style,
        Transform parent = null)
    {
        if (style.segments <= 0)
            style = AreaAttackTelegraphStyle.Default;

        GameObject go = new GameObject("AreaAttackTelegraph_PersistentCircle");

        if (parent != null)
            go.transform.SetParent(parent, true);

        AreaAttackTelegraph telegraph = go.AddComponent<AreaAttackTelegraph>();
        telegraph.InitializeCircle(center, radius, 1f, style, true);
        return telegraph;
    }

    // Purpose: Initializes circle for use by this component.
    private void InitializeCircle(
        Vector3 center,
        float radius,
        float duration,
        AreaAttackTelegraphStyle style,
        bool persistent)
    {
        _radius = Mathf.Max(0.01f, radius);
        _duration = Mathf.Max(0.01f, duration);
        _persistent = persistent;
        _yOffset = style.yOffset;

        center.y += _yOffset;

        transform.position = center;
        transform.rotation = Quaternion.identity;

        _mesh = CreateDiscMesh(Mathf.Max(12, style.segments));

        _base = CreateDisc("Base", style.baseColor, out _baseMaterial);
        _fill = CreateDisc("Fill", style.fillColor, out _fillMaterial);

        _base.localScale = new Vector3(_radius, 1f, _radius);
        _fill.localScale = persistent
            ? new Vector3(_radius, 1f, _radius)
            : new Vector3(0.01f, 1f, 0.01f);
    }

    // Purpose: Updates the persistent circle state.
    public void SetPersistentCircle(Vector3 center, float radius)
    {
        if (_completed || !_persistent)
            return;

        _radius = Mathf.Max(0.01f, radius);
        center.y += _yOffset;
        transform.position = center;

        Vector3 scale = new Vector3(_radius, 1f, _radius);

        if (_base != null)
            _base.localScale = scale;

        if (_fill != null)
            _fill.localScale = scale;
    }

    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
        if (_completed || _persistent)
            return;

        _elapsed += Time.deltaTime;

        float t = Mathf.Clamp01(_elapsed / _duration);
        float currentRadius = Mathf.Lerp(0.01f, _radius, t);

        if (_fill != null)
            _fill.localScale = new Vector3(currentRadius, 1f, currentRadius);

        if (t >= 1f)
            CompleteAndDestroy();
    }

    // Purpose: Performs the complete and destroy workflow for this component.
    public void CompleteAndDestroy()
    {
        if (_completed)
            return;

        _completed = true;
        Destroy(gameObject);
    }

    // Purpose: Creates the disc owned by this workflow.
    private Transform CreateDisc(string objectName, Color color, out Material material)
    {
        GameObject disc = new GameObject(objectName);
        disc.transform.SetParent(transform, false);

        MeshFilter filter = disc.AddComponent<MeshFilter>();
        MeshRenderer renderer = disc.AddComponent<MeshRenderer>();

        filter.sharedMesh = _mesh;

        material = CreateTransparentMaterial(color);
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        return disc.transform;
    }

    // Purpose: Creates the transparent material owned by this workflow.
    private static Material CreateTransparentMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        Material material = new Material(shader);
        material.name = "Runtime_AreaTelegraph";
        material.renderQueue = 3900;
        material.SetOverrideTag("RenderType", "Transparent");

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);

        material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.SetInt("_Cull", (int)CullMode.Off);

        material.EnableKeyword("_ALPHABLEND_ON");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        return material;
    }

    // Purpose: Creates the disc mesh owned by this workflow.
    private static Mesh CreateDiscMesh(int segments)
    {
        Mesh mesh = new Mesh();
        mesh.name = "AreaAttackTelegraphDisc";

        Vector3[] vertices = new Vector3[segments + 1];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;

        for (int i = 0; i < segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }

        for (int i = 0; i < segments; i++)
        {
            int current = i + 1;
            int next = i == segments - 1 ? 1 : current + 1;

            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = next;
            triangles[i * 3 + 2] = current;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    // Purpose: Releases owned resources before this component is destroyed.
    private void OnDestroy()
    {
        if (_baseMaterial != null)
            Destroy(_baseMaterial);

        if (_fillMaterial != null)
            Destroy(_fillMaterial);

        if (_mesh != null)
            Destroy(_mesh);
    }
}
