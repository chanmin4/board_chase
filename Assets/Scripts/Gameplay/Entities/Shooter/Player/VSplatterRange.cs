/*
 * Role hierarchy: Gameplay > Entities > Shooter > Player
 * Primary responsibility: Handles VSplatterRange camera, vision, range, or aiming calculations and presentation.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

#if false
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class VSplatterRange : EntityRange
{
    [Header("Need Ref")]
    [FormerlySerializedAs("_weaponHolder")]
    [SerializeField] private ShooterEquipmentController _equipmentController;
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private Transform _rangeOrigin;
    [SerializeField] private ShooterStatsRuntime _statsRuntime;
    [SerializeField] private PlayerAimAction _aimAction;

    [Header("Runtime Visual")]
    [SerializeField] private bool showRuntimeRange = true;
    [Tooltip("Disable this when the range ring is rendered by PlayerRangeRingHUD instead of the world LineRenderer.")]
    [SerializeField] private bool _renderWorldRangeRing = true;
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

    [Header("Debug")]
    [SerializeField] private bool _debugRangeValues;
    [SerializeField, Min(0.1f)] private float _debugLogInterval = 1f;

    [Header("Don't Touch Ref Auto")]
    [SerializeField] private LineRenderer _lineRenderer;

    private bool _shootHeld;
    private float _nextDebugLogTime;

    public override Transform RangeOrigin => _rangeOrigin != null ? _rangeOrigin : transform;

    public override WeaponSO CurrentWeapon =>
        _equipmentController != null ? _equipmentController.CurrentWeapon : null;

    public override float MaxRange =>
        _statsRuntime != null
            ? Mathf.Max(
                0.1f,
                _statsRuntime.MaxRange * (_aimAction != null ? _aimAction.RangeMultiplier : 1f))
            : 0f;

    // Purpose: Initializes Inspector defaults for this component.
    protected override void Reset()
    {
        ResolveRefs();
    }

    // Purpose: Caches the component references required before runtime initialization.
    protected override void Awake()
    {
        ResolveRefs();
        EnsureLineRenderer();
        RefreshVisualImmediate();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
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

    // Purpose: Releases active subscriptions and transient state while disabled.
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

    // Purpose: Processes frame work that must run after the regular Update pass.
    private void LateUpdate()
    {
        RefreshVisualImmediate();
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        if (_rangeOrigin == null)
            _rangeOrigin = transform;

        if (_equipmentController == null)
            _equipmentController = EntityComponentResolver.Resolve<ShooterEquipmentController>(this);

        if (_statsRuntime == null)
            _statsRuntime = EntityComponentResolver.Resolve<ShooterStatsRuntime>(this);

        if (_aimAction == null)
            _aimAction = EntityComponentResolver.Resolve<PlayerAimAction>(this);

        if (_inputReader == null)
            _inputReader = FindAnyObjectByType<InputReader>();

        if (_worldCamera == null)
            _worldCamera = Camera.main;
    }

    // Purpose: Checks whether this component has valid weapon.
    public override bool HasValidWeapon()
    {
        return CurrentWeapon != null && MaxRange > 0f;
    }

    // Purpose: Checks whether within range is currently true.
    public override bool IsWithinRange(Vector3 worldPoint)
    {
        if (!HasValidWeapon())
            return false;

        return VSplatterAimUtility.IsWithinFlatRange(
            RangeOrigin.position,
            worldPoint,
            MaxRange);
    }

    // Purpose: Performs the clamp to range workflow for this component.
    public override Vector3 ClampToRange(Vector3 worldPoint)
    {
        if (!HasValidWeapon())
            return worldPoint;

        return VSplatterAimUtility.ClampFlatPointToRange(
            RangeOrigin.position,
            worldPoint,
            MaxRange);
    }

    // Purpose: Checks whether showing range now is currently true.
    public bool IsShowingRangeNow()
    {
        if (!_renderWorldRangeRing || !showRuntimeRange || !HasValidWeapon())
            return false;

        if (!showOnlyWhileHoldingShootInput)
            return true;

        return shootInputShowsRange && _shootHeld;
    }

    // Purpose: Ensures line renderer is ready before the workflow continues.
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

    // Purpose: Refreshes the displayed or cached visual immediate.
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
        {
            LogRangeDebug("camera-projected");
            return;
        }

        DrawWorldFlatRange(segments);
        LogRangeDebug("world-flat");
    }

    // Purpose: Performs the log range debug workflow for this component.
    private void LogRangeDebug(string drawMode)
    {
        if (!_debugRangeValues || Time.unscaledTime < _nextDebugLogTime)
            return;

        _nextDebugLogTime = Time.unscaledTime + _debugLogInterval;

        Debug.Log(
            $"[VSplatterRange] drawMode={drawMode}, origin={RangeOrigin.name}, originPos={RangeOrigin.position}, " +
            $"maxRange={MaxRange:0.##}, hasWeapon={CurrentWeapon != null}, showing={_lineRenderer != null && _lineRenderer.enabled}",
            this);
    }

    // Purpose: Attempts to draw camera projected range and reports whether the request succeeded.
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

    // Purpose: Draws the world flat range authoring UI.
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

    // Purpose: Responds to the world camera ready callback.
    private void OnWorldCameraReady(Camera camera)
    {
        _worldCamera = camera;
    }

    // Purpose: Responds to the shoot started callback.
    private void OnShootStarted()
    {
        _shootHeld = true;
    }

    // Purpose: Responds to the shoot canceled callback.
    private void OnShootCanceled()
    {
        _shootHeld = false;
    }
}
#endif
