using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class VSplatterRange : MonoBehaviour
{
    [Header("Need Ref")]
    [SerializeField] private VSplatterWeaponHolder _weaponHolder;
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private Transform _rangeOrigin;
    [SerializeField] private PlayerStatsRuntime _statsRuntime;

    [Header("Runtime Visual")]
    [SerializeField] private bool showRuntimeRange = true;
    [SerializeField] private bool showOnlyWhileHoldingShootInput = false;

    [FormerlySerializedAs("shootkInputShowsRange")]
    [SerializeField] private bool shootInputShowsRange = true;

    [Header("Visual Options")]
    [SerializeField] private float yOffset = 0.03f;
    [SerializeField] private int circleSegments = 64;
    [SerializeField] private float lineWidth = 0.06f;
    [SerializeField] private Color rangeColor =
        new Color(0f, 1f, 1f, 0.35f);
    [SerializeField] private Material rangeMaterial;

    [Header("Don't Touch Ref Auto")]
    [SerializeField] private LineRenderer _lineRenderer;

    private bool _shootHeld;

    public Transform RangeOrigin =>
        _rangeOrigin != null ? _rangeOrigin : transform;

    public WeaponSO CurrentWeapon =>
        _weaponHolder != null ? _weaponHolder.CurrentWeapon : null;

    public float MaxRange =>
        _statsRuntime != null
            ? Mathf.Max(0.1f, _statsRuntime.Weapon.maxRange)
            : 0f;

    private void Reset()
    {
        ResolveRefs();
    }

    private void Awake()
    {
        ResolveRefs();
        EnsureLineRenderer();
        RefreshVisualImmediate();
    }

    private void OnEnable()
    {
        EnsureLineRenderer();

        if (_inputReader != null)
        {
            _inputReader.ShootEvent += OnShootStarted;
            _inputReader.ShootCanceledEvent += OnShootCanceled;
        }

        RefreshVisualImmediate();
    }

    private void OnDisable()
    {
        if (_inputReader != null)
        {
            _inputReader.ShootEvent -= OnShootStarted;
            _inputReader.ShootCanceledEvent -= OnShootCanceled;
        }

        _shootHeld = false;

        if (_lineRenderer != null)
            _lineRenderer.enabled = false;
    }

    private void LateUpdate()
    {
        RefreshVisualImmediate();
    }

    private void ResolveRefs()
    {
        if (_rangeOrigin == null)
            _rangeOrigin = transform;

        if (_weaponHolder == null)
            _weaponHolder = GetComponent<VSplatterWeaponHolder>();

        if (_statsRuntime == null)
            _statsRuntime = GetComponent<PlayerStatsRuntime>();

        if (_inputReader == null)
            _inputReader = FindAnyObjectByType<InputReader>();
    }

    public bool HasValidWeapon()
    {
        return CurrentWeapon != null && MaxRange > 0f;
    }

    public bool IsWithinRange(Vector3 worldPoint)
    {
        if (!HasValidWeapon())
            return false;

        return VSplatterAimUtility.IsWithinFlatRange(
            RangeOrigin.position,
            worldPoint,
            MaxRange);
    }

    public Vector3 ClampToRange(Vector3 worldPoint)
    {
        if (!HasValidWeapon())
            return worldPoint;

        return VSplatterAimUtility.ClampFlatPointToRange(
            RangeOrigin.position,
            worldPoint,
            MaxRange);
    }

    public bool IsShowingRangeNow()
    {
        if (!showRuntimeRange || !HasValidWeapon())
            return false;

        if (!showOnlyWhileHoldingShootInput)
            return true;

        return shootInputShowsRange && _shootHeld;
    }

    private void EnsureLineRenderer()
    {
        if (_lineRenderer != null)
            return;

        _lineRenderer = GetComponentInChildren<LineRenderer>();

        if (_lineRenderer == null ||
            _lineRenderer.gameObject == gameObject)
        {
            GameObject rangeRing = new GameObject("RangeRing");
            rangeRing.transform.SetParent(transform, false);
            _lineRenderer = rangeRing.AddComponent<LineRenderer>();
        }

        _lineRenderer.useWorldSpace = true;
        _lineRenderer.loop = true;
        _lineRenderer.alignment = LineAlignment.View;
        _lineRenderer.textureMode = LineTextureMode.Stretch;

        _lineRenderer.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;

        _lineRenderer.receiveShadows = false;

        _lineRenderer.lightProbeUsage =
            UnityEngine.Rendering.LightProbeUsage.Off;

        _lineRenderer.reflectionProbeUsage =
            UnityEngine.Rendering.ReflectionProbeUsage.Off;

        _lineRenderer.motionVectorGenerationMode =
            MotionVectorGenerationMode.ForceNoMotion;

        _lineRenderer.widthMultiplier = lineWidth;
        _lineRenderer.positionCount = Mathf.Max(8, circleSegments);
        _lineRenderer.startColor = rangeColor;
        _lineRenderer.endColor = rangeColor;

        if (rangeMaterial != null)
        {
            _lineRenderer.material = rangeMaterial;
            return;
        }

        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
            return;

        Material material = new Material(shader)
        {
            color = rangeColor
        };

        _lineRenderer.material = material;
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

        Vector3 center =
            RangeOrigin.position + Vector3.up * yOffset;

        float radius = MaxRange;

        for (int i = 0; i < segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;

            Vector3 position = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius);

            _lineRenderer.SetPosition(i, position);
        }
    }

    private void OnShootStarted()
    {
        _shootHeld = true;
    }

    private void OnShootCanceled()
    {
        _shootHeld = false;
    }
}