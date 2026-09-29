/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Vision
 * Primary responsibility: Handles VisionDiscoveredVisibilityTarget camera, vision, range, or aiming calculations and presentation.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

/// <summary>
/// Hides a world pickup or treasure until player vision reveals it once. Discovery
/// is intentionally sticky for the lifetime of the spawned stage object.
/// </summary>
[DisallowMultipleComponent]
public sealed class VisionDiscoveredVisibilityTarget : MonoBehaviour
{
    [Header("Visibility")]
    [Tooltip("Optional reveal point. If empty, the combined renderer bounds center is used.")]
    [SerializeField] private Transform _visibilityPoint;

    [Tooltip("Renderers hidden before discovery. Empty arrays are populated from children.")]
    [SerializeField] private Renderer[] _renderers;

    [Header("Runtime Don't Touch")]
    [ReadOnly] [SerializeField] private bool _isDiscovered;

    private bool[] _rendererStates;

    public bool IsDiscovered => _isDiscovered;

    public Vector3 VisibilityPoint
    {
        get
        {
            if (_visibilityPoint != null)
                return _visibilityPoint.position;

            CacheRenderersIfNeeded();
            bool hasBounds = false;
            Bounds bounds = default;

            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer target = _renderers[i];

                if (target == null)
                    continue;

                if (!hasBounds)
                {
                    bounds = target.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(target.bounds);
                }
            }

            return hasBounds ? bounds.center : transform.position;
        }
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        CacheRenderersIfNeeded();
        ApplyVisibility();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        ApplyVisibility();
    }

    // Purpose: Processes frame work that must run after the regular Update pass.
    private void LateUpdate()
    {
        if (_isDiscovered)
            return;

        EntityVisionController viewer = VisionVisibilitySystem.Instance != null
            ? VisionVisibilitySystem.Instance.Viewer
            : null;

        if (viewer == null || !viewer.CanRevealVisualPoint(VisibilityPoint, 0f))
            return;

        _isDiscovered = true;
        ApplyVisibility();
        enabled = false;
    }

    // Purpose: Releases owned resources before this component is destroyed.
    private void OnDestroy()
    {
        SetRenderersVisible(true);
    }

    // Purpose: Refreshes the displayed or cached renderers.
    public void RefreshRenderers()
    {
        SetRenderersVisible(true);
        _renderers = GetComponentsInChildren<Renderer>(true);
        _rendererStates = new bool[_renderers.Length];

        for (int i = 0; i < _renderers.Length; i++)
            _rendererStates[i] = _renderers[i] != null && _renderers[i].enabled;

        ApplyVisibility();
        enabled = !_isDiscovered;
    }

    // Purpose: Performs the mark discovered workflow for this component.
    public void MarkDiscovered()
    {
        _isDiscovered = true;
        ApplyVisibility();
        enabled = false;
    }

    // Purpose: Caches renderers if needed for later reuse.
    private void CacheRenderersIfNeeded()
    {
        if (_renderers == null || _renderers.Length == 0)
            _renderers = GetComponentsInChildren<Renderer>(true);

        if (_rendererStates != null && _rendererStates.Length == _renderers.Length)
            return;

        _rendererStates = new bool[_renderers.Length];

        for (int i = 0; i < _renderers.Length; i++)
            _rendererStates[i] = _renderers[i] != null && _renderers[i].enabled;
    }

    // Purpose: Applies the requested visibility change.
    private void ApplyVisibility()
    {
        CacheRenderersIfNeeded();
        SetRenderersVisible(_isDiscovered);
    }

    // Purpose: Updates the renderers visible state.
    private void SetRenderersVisible(bool visible)
    {
        if (_renderers == null)
            return;

        for (int i = 0; i < _renderers.Length; i++)
        {
            Renderer target = _renderers[i];

            if (target == null)
                continue;

            bool originalState = _rendererStates != null && i < _rendererStates.Length
                ? _rendererStates[i]
                : true;
            target.enabled = visible && originalState;
        }
    }
}
