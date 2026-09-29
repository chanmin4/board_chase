/*
 * Role hierarchy: Gameplay > Items > Weapons > Runtime
 * Primary responsibility: Handles ThrowableZoneVisualRuntime combat resolution, damage, projectile, or status changes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Independent world-space fallback for throwable impact areas.
/// It deliberately does not depend on SectorPaint or MaskRenderManager, so a
/// grenade/healing zone remains readable even while those systems are missing.
/// </summary>
[DisallowMultipleComponent]
public sealed class ThrowableZoneVisualRuntime : MonoBehaviour
{
    private const int CircleSegments = 48;

    private Mesh _mesh;
    private Material _material;
    private Material _outlineMaterial;
    private LineRenderer _outline;

    // Purpose: Creates the create owned by this workflow.
    public static ThrowableZoneVisualRuntime Create(
        Vector3 worldPosition,
        float radius,
        float lifetimeSeconds,
        Color color,
        string visualName)
    {
        GameObject visualObject = new GameObject(
            string.IsNullOrWhiteSpace(visualName)
                ? "ThrowableZoneVisual"
                : visualName);

        visualObject.transform.position =
            worldPosition + Vector3.up * 0.045f;

        ThrowableZoneVisualRuntime runtime =
            visualObject.AddComponent<ThrowableZoneVisualRuntime>();

        bool built = runtime.Build(
            Mathf.Max(0.01f, radius),
            Mathf.Max(0.05f, lifetimeSeconds),
            color);

        if (!built)
        {
            Debug.LogError(
                $"[ThrowableDebug] Runtime zone build failed. name={visualObject.name}, " +
                $"position={visualObject.transform.position}, radius={radius:0.###}, " +
                $"lifetime={lifetimeSeconds:0.###}, color={color}",
                visualObject);
            Destroy(visualObject);
            return null;
        }

        Debug.Log(
            $"[ThrowableDebug] Runtime zone created. name={visualObject.name}, " +
            $"position={visualObject.transform.position}, radius={radius:0.###}, " +
            $"lifetime={lifetimeSeconds:0.###}, color={color}",
            visualObject);

        return runtime;
    }

    // Purpose: Builds the build from the current authored data.
    private bool Build(float radius, float lifetimeSeconds, Color color)
    {
        MeshFilter filter = gameObject.AddComponent<MeshFilter>();
        MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();

        _mesh = BuildCircleMesh(radius);
        _material = BuildTransparentMaterial(color);
        _outlineMaterial = BuildTransparentMaterial(
            WithMinimumAlpha(color, 0.95f));

        if (_mesh == null ||
            _material == null ||
            _outlineMaterial == null)
        {
            return false;
        }

        filter.sharedMesh = _mesh;
        renderer.sharedMaterial = _material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.sortingOrder = 40;

        _outline = gameObject.AddComponent<LineRenderer>();
        _outline.useWorldSpace = false;
        _outline.loop = true;
        _outline.positionCount = CircleSegments;
        _outline.startWidth = Mathf.Max(0.06f, radius * 0.035f);
        _outline.endWidth = _outline.startWidth;
        _outline.sharedMaterial = _outlineMaterial;
        _outline.startColor = WithMinimumAlpha(color, 0.9f);
        _outline.endColor = WithMinimumAlpha(color, 0.9f);
        _outline.shadowCastingMode = ShadowCastingMode.Off;
        _outline.receiveShadows = false;
        _outline.sortingOrder = 41;

        for (int i = 0; i < CircleSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / CircleSegments;
            _outline.SetPosition(
                i,
                new Vector3(
                    Mathf.Cos(angle) * radius,
                    0.012f,
                    Mathf.Sin(angle) * radius));
        }

        Destroy(gameObject, lifetimeSeconds);
        return true;
    }

    // Purpose: Builds the circle mesh from the current authored data.
    private static Mesh BuildCircleMesh(float radius)
    {
        Vector3[] vertices = new Vector3[CircleSegments + 1];
        Vector2[] uv = new Vector2[CircleSegments + 1];
        int[] triangles = new int[CircleSegments * 3];

        vertices[0] = Vector3.zero;
        uv[0] = new Vector2(0.5f, 0.5f);

        for (int i = 0; i < CircleSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / CircleSegments;
            float x = Mathf.Cos(angle);
            float z = Mathf.Sin(angle);

            vertices[i + 1] = new Vector3(x * radius, 0f, z * radius);
            uv[i + 1] = new Vector2(x * 0.5f + 0.5f, z * 0.5f + 0.5f);

            int triangle = i * 3;
            triangles[triangle] = 0;
            triangles[triangle + 1] = (i + 1) % CircleSegments + 1;
            triangles[triangle + 2] = i + 1;
        }

        Mesh mesh = new Mesh
        {
            name = "ThrowableZoneCircle"
        };

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // Purpose: Builds the transparent material from the current authored data.
    private static Material BuildTransparentMaterial(Color color)
    {
        Shader shader =
            Shader.Find("Sprites/Default") ??
            Shader.Find("Universal Render Pipeline/Unlit") ??
            Shader.Find("Unlit/Color") ??
            Shader.Find("Hidden/Internal-Colored");

        if (shader == null)
        {
            Debug.LogError(
                "[ThrowableDebug] No supported runtime zone shader was found.");
            return null;
        }

        Material material = new Material(shader)
        {
            name = "ThrowableZoneVisual (Runtime)",
            color = color,
            renderQueue = 3100
        };

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);

        if (material.HasProperty("_Blend"))
            material.SetFloat("_Blend", 0f);

        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);

        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);

        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);

        if (material.HasProperty("_Cull"))
            material.SetFloat("_Cull", (float)CullMode.Off);

        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        return material;
    }

    // Purpose: Performs the with minimum alpha workflow for this component.
    private static Color WithMinimumAlpha(Color color, float minimumAlpha)
    {
        color.a = Mathf.Max(color.a, minimumAlpha);
        return color;
    }

    // Purpose: Releases owned resources before this component is destroyed.
    private void OnDestroy()
    {
        if (_mesh != null)
            Destroy(_mesh);

        if (_material != null)
            Destroy(_material);

        if (_outlineMaterial != null)
            Destroy(_outlineMaterial);
    }
}
