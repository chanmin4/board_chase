/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Vision
 * Primary responsibility: Handles VisionVisibilityTarget camera, vision, range, or aiming calculations and presentation.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class VisionVisibilityTarget : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Enemy _enemy;
    [SerializeField] private Damageable _damageable;
    [SerializeField] private Transform _visibilityPoint;

    [Header("Renderers")]
    [Tooltip("Renderers hidden when this target is outside player vision. If empty, child renderers are collected automatically.")]
    [SerializeField] private Renderer[] _renderers;

    [Header("Runtime Don't Touch")]
    [ReadOnly] [SerializeField] private bool _isVisible = true;

    // Runtime visual instances such as weapon views, armor, muzzle particles,
    // and cosmetics must join this gate in the same frame they are created.
    // An array cached one frame later lets an unseen enemy's gun flash before
    // the next visibility refresh. Keep the registration and the temporary
    // pre-hide state separate so normal renderer state changes are preserved.
    private readonly HashSet<Renderer> _registeredRenderers = new();
    private readonly Dictionary<Renderer, bool> _rendererStatesBeforeVisionHide = new();
    private readonly HashSet<Renderer> _excludedRenderers = new();
    private bool _hiddenByVision;

    public Enemy Enemy => _enemy;
    public Damageable Damageable => _damageable;
    public bool IsVisible => _isVisible;

    public Vector3 VisibilityPoint
    {
        get
        {
            if (_visibilityPoint != null)
                return _visibilityPoint.position;

            if (_damageable != null && _damageable.MainMeshRenderer != null)
                return _damageable.MainMeshRenderer.bounds.center;

            return transform.position;
        }
    }

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        ResolveRefs();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        ResolveRefs();
        CacheRenderersIfNeeded();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        VisionVisibilitySystem.Instance?.Register(this);
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        SetVisionVisible(true);
        VisionVisibilitySystem.Instance?.Unregister(this);
    }

    // Purpose: Updates the vision visible state.
    public void SetVisionVisible(bool visible)
    {
        if (_isVisible == visible && visible == !_hiddenByVision)
            return;

        _isVisible = visible;
        CacheRenderersIfNeeded();

        if (!visible)
        {
            HideRenderersByVision();
            return;
        }

        RestoreRenderersHiddenByVision();
    }

    // Purpose: Refreshes the displayed or cached renderers.
    public void RefreshRenderers()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        RegisterRenderers(_renderers);
        PruneRendererRegistrations();
    }

    /// <summary>
    /// Registers a newly created visual hierarchy with the current whole-enemy
    /// visibility decision immediately. Call this directly after Instantiate,
    /// before activating the view or emitting its VFX.
    /// </summary>
    public void RegisterRuntimeVisualRoot(Transform visualRoot)
    {
        if (visualRoot == null)
            return;

        RegisterRenderers(visualRoot.GetComponentsInChildren<Renderer>(true));
    }

    /// <summary>
    /// Removes a renderer from normal whole-enemy visibility gating. Aim laser
    /// uses this because it has its own warning rule: it may be shown when its
    /// ray enters player vision even if the enemy body remains hidden.
    /// </summary>
    public void ExcludeRendererFromVisionGate(Renderer renderer)
    {
        if (renderer == null)
            return;

        _excludedRenderers.Add(renderer);
        _registeredRenderers.Remove(renderer);

        if (_rendererStatesBeforeVisionHide.TryGetValue(renderer, out bool wasEnabled))
        {
            if (_hiddenByVision)
                renderer.enabled = wasEnabled;

            _rendererStatesBeforeVisionHide.Remove(renderer);
        }
    }

    // Purpose: Hides the renderers by vision UI or visual state.
    private void HideRenderersByVision()
    {
        if (_hiddenByVision)
            return;

        _hiddenByVision = true;

        foreach (Renderer targetRenderer in _registeredRenderers)
        {
            if (targetRenderer == null)
                continue;

            _rendererStatesBeforeVisionHide[targetRenderer] = targetRenderer.enabled;
            targetRenderer.enabled = false;
        }
    }

    // Purpose: Restores the saved renderers hidden by vision state.
    private void RestoreRenderersHiddenByVision()
    {
        if (!_hiddenByVision)
            return;

        _hiddenByVision = false;

        foreach (KeyValuePair<Renderer, bool> entry in _rendererStatesBeforeVisionHide)
        {
            Renderer targetRenderer = entry.Key;

            if (targetRenderer != null)
                targetRenderer.enabled = entry.Value;
        }

        _rendererStatesBeforeVisionHide.Clear();
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        if (_enemy == null)
            _enemy = GetComponent<Enemy>() ?? GetComponentInParent<Enemy>();

        if (_damageable == null)
            _damageable = GetComponent<Damageable>() ?? GetComponentInParent<Damageable>();
    }

    // Purpose: Caches renderers if needed for later reuse.
    private void CacheRenderersIfNeeded()
    {
        if (!HasConfiguredRenderer())
            _renderers = GetComponentsInChildren<Renderer>(true);

        RegisterRenderers(_renderers);
    }

    // Purpose: Checks whether this component has configured renderer.
    private bool HasConfiguredRenderer()
    {
        if (_renderers == null)
            return false;

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
                return true;
        }

        return false;
    }

    // Purpose: Registers renderers with its owning runtime service.
    private void RegisterRenderers(Renderer[] renderers)
    {
        if (renderers == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null || _excludedRenderers.Contains(renderer) ||
                _registeredRenderers.Contains(renderer))
            {
                continue;
            }

            _registeredRenderers.Add(renderer);

            if (_hiddenByVision)
            {
                _rendererStatesBeforeVisionHide[renderer] = renderer.enabled;
                renderer.enabled = false;
            }
        }
    }

    // Purpose: Performs the prune renderer registrations workflow for this component.
    private void PruneRendererRegistrations()
    {
        List<Renderer> stale = null;

        foreach (Renderer renderer in _registeredRenderers)
        {
            if (renderer != null)
                continue;

            stale ??= new List<Renderer>();
            stale.Add(renderer);
        }

        if (stale == null)
            return;

        for (int i = 0; i < stale.Count; i++)
        {
            _registeredRenderers.Remove(stale[i]);
            _rendererStatesBeforeVisionHide.Remove(stale[i]);
            _excludedRenderers.Remove(stale[i]);
        }
    }
}
