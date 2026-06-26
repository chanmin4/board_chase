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

    private bool[] _rendererStatesBeforeVisionHide;
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

    private void Reset()
    {
        ResolveRefs();
    }

    private void Awake()
    {
        ResolveRefs();
        CacheRenderersIfNeeded();
    }

    private void OnEnable()
    {
        VisionVisibilitySystem.Instance?.Register(this);
    }

    private void OnDisable()
    {
        SetVisionVisible(true);
        VisionVisibilitySystem.Instance?.Unregister(this);
    }

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

    private void HideRenderersByVision()
    {
        if (_hiddenByVision)
            return;

        _hiddenByVision = true;

        if (_renderers == null)
            return;

        if (_rendererStatesBeforeVisionHide == null ||
            _rendererStatesBeforeVisionHide.Length != _renderers.Length)
        {
            _rendererStatesBeforeVisionHide = new bool[_renderers.Length];
        }

        for (int i = 0; i < _renderers.Length; i++)
        {
            Renderer targetRenderer = _renderers[i];

            if (targetRenderer == null)
                continue;

            _rendererStatesBeforeVisionHide[i] = targetRenderer.enabled;
            targetRenderer.enabled = false;
        }
    }

    private void RestoreRenderersHiddenByVision()
    {
        if (!_hiddenByVision)
            return;

        _hiddenByVision = false;

        if (_renderers == null || _rendererStatesBeforeVisionHide == null)
            return;

        int count = Mathf.Min(_renderers.Length, _rendererStatesBeforeVisionHide.Length);

        for (int i = 0; i < count; i++)
        {
            Renderer targetRenderer = _renderers[i];

            if (targetRenderer != null)
                targetRenderer.enabled = _rendererStatesBeforeVisionHide[i];
        }
    }

    private void ResolveRefs()
    {
        if (_enemy == null)
            _enemy = GetComponent<Enemy>() ?? GetComponentInParent<Enemy>();

        if (_damageable == null)
            _damageable = GetComponent<Damageable>() ?? GetComponentInParent<Damageable>();
    }

    private void CacheRenderersIfNeeded()
    {
        if (_renderers != null && _renderers.Length > 0)
            return;

        _renderers = GetComponentsInChildren<Renderer>(true);
        _rendererStatesBeforeVisionHide = new bool[_renderers.Length];
    }
}
