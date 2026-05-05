using UnityEngine;

[DisallowMultipleComponent]
public class VSplatterRange : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private VSplatterWeaponHolder _weaponHolder;
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private Transform _rangeOrigin;

    [Header("Runtime Visual")]
    [SerializeField] private bool showRuntimeRange = true;
    [SerializeField] private bool showOnlyWhileHoldingAimInput = false;
    [SerializeField] private bool attackInputShowsRange = true;
    [SerializeField] private bool paintInputShowsRange = true;

    [SerializeField] private float yOffset = 0.03f;
    [SerializeField] private int circleSegments = 64;
    [SerializeField] private float lineWidth = 0.06f;
    [SerializeField] private Color rangeColor = new Color(0f, 1f, 1f, 0.35f);
    [SerializeField] private Material rangeMaterial;

    private LineRenderer _lineRenderer;

    private bool _attackHeld;
    private bool _paintHeld;

    public Transform RangeOrigin => _rangeOrigin != null ? _rangeOrigin : transform;
    public WeaponSO CurrentWeapon => _weaponHolder != null ? _weaponHolder.CurrentWeapon : null;
    public float MaxRange => CurrentWeapon != null ? CurrentWeapon.MaxRange : 0f;

    private void Reset()
    {
        if (_rangeOrigin == null)
            _rangeOrigin = transform;

        if (_inputReader == null)
            _inputReader = FindAnyObjectByType<InputReader>();
    }

    private void Awake()
    {
        if (_rangeOrigin == null)
            _rangeOrigin = transform;

        if (_inputReader == null)
            _inputReader = FindAnyObjectByType<InputReader>();

        EnsureLineRenderer();
        RefreshVisualImmediate();
    }

    private void OnEnable()
    {
        EnsureLineRenderer();

        if (_inputReader != null)
        {
            _inputReader.AttackEvent += OnAttackStarted;
            _inputReader.AttackCanceledEvent += OnAttackCanceled;
            _inputReader.PaintEvent += OnPaintStarted;
            _inputReader.PaintCanceledEvent += OnPaintCanceled;
        }

        RefreshVisualImmediate();
    }

    private void OnDisable()
    {
        if (_inputReader != null)
        {
            _inputReader.AttackEvent -= OnAttackStarted;
            _inputReader.AttackCanceledEvent -= OnAttackCanceled;
            _inputReader.PaintEvent -= OnPaintStarted;
            _inputReader.PaintCanceledEvent -= OnPaintCanceled;
        }
    }

    private void LateUpdate()
    {
        RefreshVisualImmediate();
    }

    public bool HasValidWeapon()
    {
        return CurrentWeapon != null;
    }

    public bool IsWithinRange(Vector3 worldPoint)
    {
        if (CurrentWeapon == null)
            return false;

        return VSplatterAimUtility.IsWithinFlatRange(
            RangeOrigin.position,
            worldPoint,
            CurrentWeapon.MaxRange);
    }

    public bool IsShowingRangeNow()
    {
        if (!showRuntimeRange || !HasValidWeapon())
            return false;

        if (!showOnlyWhileHoldingAimInput)
            return true;

        bool showByAttack = attackInputShowsRange && _attackHeld;
        bool showByPaint = paintInputShowsRange && _paintHeld;

        return showByAttack || showByPaint;
    }

    private void EnsureLineRenderer()
    {
        if (_lineRenderer != null)
            return;

        _lineRenderer = GetComponentInChildren<LineRenderer>();

        if (_lineRenderer == null || _lineRenderer.gameObject == gameObject)
        {
            GameObject go = new GameObject("RangeRing");
            go.transform.SetParent(transform, false);
            _lineRenderer = go.AddComponent<LineRenderer>();
        }

        _lineRenderer.useWorldSpace = true;
        _lineRenderer.loop = true;
        _lineRenderer.alignment = LineAlignment.View;
        _lineRenderer.textureMode = LineTextureMode.Stretch;
        _lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _lineRenderer.receiveShadows = false;
        _lineRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        _lineRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        _lineRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        _lineRenderer.widthMultiplier = lineWidth;
        _lineRenderer.positionCount = Mathf.Max(8, circleSegments);
        _lineRenderer.startColor = rangeColor;
        _lineRenderer.endColor = rangeColor;

        if (rangeMaterial != null)
        {
            _lineRenderer.material = rangeMaterial;
        }
        else
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                Material mat = new Material(shader);
                mat.color = rangeColor;
                _lineRenderer.material = mat;
            }
        }
    }

    private void RefreshVisualImmediate()
    {
        if (_lineRenderer == null)
            return;

        bool shouldShow = IsShowingRangeNow();
        _lineRenderer.enabled = shouldShow;

        if (!shouldShow)
            return;

        _lineRenderer.widthMultiplier = lineWidth;
        _lineRenderer.startColor = rangeColor;
        _lineRenderer.endColor = rangeColor;

        int segments = Mathf.Max(8, circleSegments);
        if (_lineRenderer.positionCount != segments)
            _lineRenderer.positionCount = segments;

        Vector3 center = RangeOrigin.position + Vector3.up * yOffset;
        float radius = MaxRange;

        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)segments;
            float angle = t * Mathf.PI * 2f;

            Vector3 pos = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius);

            _lineRenderer.SetPosition(i, pos);
        }
    }
    public Vector3 ClampToRange(Vector3 worldPoint)
    {
        if (CurrentWeapon == null)
            return worldPoint;

        return VSplatterAimUtility.ClampFlatPointToRange(
            RangeOrigin.position,
            worldPoint,
            CurrentWeapon.MaxRange);
    }

    private void OnAttackStarted() => _attackHeld = true;
    private void OnAttackCanceled() => _attackHeld = false;
    private void OnPaintStarted() => _paintHeld = true;
    private void OnPaintCanceled() => _paintHeld = false;
}