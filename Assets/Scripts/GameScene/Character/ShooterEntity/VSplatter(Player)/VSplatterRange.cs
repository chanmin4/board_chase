using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class VSplatterRange : EntityRange
{
    [Header("Need Ref")]
    [SerializeField] private EntityWeaponHolder _weaponHolder;
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private Transform _rangeOrigin;
    [SerializeField] private ShooterStatsRuntime _statsRuntime;
    [SerializeField] private PlayerAimAction _aimAction;

    [Header("Runtime Visual")]
    [SerializeField] private bool showRuntimeRange = true;
    [SerializeField] private bool showOnlyWhileHoldingShootInput = false;

    [FormerlySerializedAs("shootkInputShowsRange")]
    [SerializeField] private bool shootInputShowsRange = true;

    [Header("Visual Options")]
    [SerializeField] private float yOffset = 0.03f;
    [SerializeField] private int circleSegments = 64;
    [SerializeField] private float lineWidth = 0.06f;
    [SerializeField] private Color rangeColor = new Color(0f, 1f, 1f, 0.35f);
    [SerializeField] private Material rangeMaterial;

    [Header("Camera Projection Visual")]
    [SerializeField] private bool _matchVisionFogProjection = true;

    [ReadOnly][HideInInspector] private Camera _worldCamera;
    [SerializeField] private WorldCameraEventChannelSO _worldCameraReadyChannel;

    [Header("Don't Touch Ref Auto")]
    [SerializeField] private LineRenderer _lineRenderer;

    private bool _shootHeld;

    public override Transform RangeOrigin => _rangeOrigin != null ? _rangeOrigin : transform;

    public override WeaponSO CurrentWeapon =>
        _weaponHolder != null ? _weaponHolder.CurrentWeapon : null;

    public override float MaxRange =>
        _statsRuntime != null
            ? Mathf.Max(
                0.1f,
                _statsRuntime.MaxRange * (_aimAction != null ? _aimAction.RangeMultiplier : 1f))
            : 0f;

    protected override void Reset()
    {
        ResolveRefs();
    }

    protected override void Awake()
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

        if (_worldCameraReadyChannel != null)
        {
            _worldCameraReadyChannel.OnEventRaised += OnWorldCameraReady;

            if (_worldCameraReadyChannel.Current != null)
                OnWorldCameraReady(_worldCameraReadyChannel.Current);
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

        if (_worldCameraReadyChannel != null)
            _worldCameraReadyChannel.OnEventRaised -= OnWorldCameraReady;

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
            _weaponHolder = GetComponent<EntityWeaponHolder>();

        if (_statsRuntime == null)
            _statsRuntime = GetComponent<ShooterStatsRuntime>();

        if (_aimAction == null)
            _aimAction = GetComponent<PlayerAimAction>();

        if (_inputReader == null)
            _inputReader = FindAnyObjectByType<InputReader>();

        if (_worldCamera == null)
            _worldCamera = Camera.main;
    }

    public override bool HasValidWeapon()
    {
        return CurrentWeapon != null && MaxRange > 0f;
    }

    public override bool IsWithinRange(Vector3 worldPoint)
    {
        if (!HasValidWeapon())
            return false;

        return VSplatterAimUtility.IsWithinFlatRange(
            RangeOrigin.position,
            worldPoint,
            MaxRange);
    }

    public override Vector3 ClampToRange(Vector3 worldPoint)
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

        if (_lineRenderer == null || _lineRenderer.gameObject == gameObject)
        {
            GameObject rangeRing = new GameObject("RangeRing");
            rangeRing.transform.SetParent(transform, false);
            _lineRenderer = rangeRing.AddComponent<LineRenderer>();
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

        if (_matchVisionFogProjection && TryDrawCameraProjectedRange(segments))
            return;

        DrawWorldFlatRange(segments);
    }

    private bool TryDrawCameraProjectedRange(int segments)
    {
        if (_worldCamera == null)
            _worldCamera = Camera.main;

        if (_worldCamera == null)
            return false;

        Vector3 centerWorld = RangeOrigin.position + Vector3.up * yOffset;
        Vector3 centerScreen = _worldCamera.WorldToScreenPoint(centerWorld);

        if (centerScreen.z <= 0f)
            return false;

        float range = MaxRange;

        Vector3 worldRight = _worldCamera.transform.right;
        worldRight.y = 0f;

        if (worldRight.sqrMagnitude <= 0.001f)
            worldRight = Vector3.right;
        else
            worldRight.Normalize();

        Vector3 worldForward = Vector3.ProjectOnPlane(
            _worldCamera.transform.forward,
            Vector3.up);

        if (worldForward.sqrMagnitude <= 0.001f)
            worldForward = Vector3.forward;
        else
            worldForward.Normalize();

        Vector3 rightScreen = _worldCamera.WorldToScreenPoint(centerWorld + worldRight * range);
        Vector3 forwardScreen = _worldCamera.WorldToScreenPoint(centerWorld + worldForward * range);

        float radiusX = Mathf.Max(1f, Vector2.Distance(
            new Vector2(centerScreen.x, centerScreen.y),
            new Vector2(rightScreen.x, rightScreen.y)));

        float radiusY = Mathf.Max(1f, Vector2.Distance(
            new Vector2(centerScreen.x, centerScreen.y),
            new Vector2(forwardScreen.x, forwardScreen.y)));

        Plane drawPlane = new Plane(Vector3.up, centerWorld);

        for (int i = 0; i < segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;

            Vector3 screenPoint = new Vector3(
                centerScreen.x + Mathf.Cos(angle) * radiusX,
                centerScreen.y + Mathf.Sin(angle) * radiusY,
                centerScreen.z);

            Ray ray = _worldCamera.ScreenPointToRay(screenPoint);

            if (drawPlane.Raycast(ray, out float enter))
            {
                _lineRenderer.SetPosition(i, ray.GetPoint(enter));
                continue;
            }

            return false;
        }

        return true;
    }

    private void DrawWorldFlatRange(int segments)
    {
        Vector3 center = RangeOrigin.position + Vector3.up * yOffset;
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

    private void OnWorldCameraReady(Camera camera)
    {
        _worldCamera = camera;
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
