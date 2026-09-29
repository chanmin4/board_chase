/*
 * Role hierarchy: Presentation > UI > Gameplay > Vision
 * Primary responsibility: Presents PlayerVisionFogOverlayUI state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
// CameraManager and the shared camera occlusion pass finish their LateUpdate work first.
[DefaultExecutionOrder(1500)]
public class PlayerVisionFogOverlayUI : MonoBehaviour
{
    private const int MaxOcclusionSamples = 256;
    private const float OcclusionOriginChangeEpsilonSqr = 0.00000001f;

    private static readonly int FogColorId = Shader.PropertyToID("_FogColor");
    private static readonly int VisionOriginWorldId = Shader.PropertyToID("_VisionOriginWorld");
    private static readonly int ForwardWorldId = Shader.PropertyToID("_ForwardWorld");
    private static readonly int CircleRadiusWorldId = Shader.PropertyToID("_CircleRadiusWorld");
    private static readonly int ConeRangeWorldId = Shader.PropertyToID("_ConeRangeWorld");
    private static readonly int WorldCameraPositionId = Shader.PropertyToID("_WorldCameraPosition");
    private static readonly int WorldCameraRightId = Shader.PropertyToID("_WorldCameraRight");
    private static readonly int WorldCameraUpId = Shader.PropertyToID("_WorldCameraUp");
    private static readonly int WorldCameraForwardId = Shader.PropertyToID("_WorldCameraForward");
    private static readonly int CameraProjectionId = Shader.PropertyToID("_CameraProjection");
    private static readonly int ArcCosId = Shader.PropertyToID("_ArcCos");
    private static readonly int UseForwardArcId = Shader.PropertyToID("_UseForwardArc");
    private static readonly int SoftnessWorldId = Shader.PropertyToID("_SoftnessWorld");
    private static readonly int OcclusionDistancesId = Shader.PropertyToID("_OcclusionDistances");
    private static readonly int OcclusionSampleCountId = Shader.PropertyToID("_OcclusionSampleCount");
    private static readonly int UseOcclusionId = Shader.PropertyToID("_UseOcclusion");
    private static readonly int OcclusionSoftnessWorldId = Shader.PropertyToID("_OcclusionSoftnessWorld");
    private static readonly int OcclusionSafetyBiasWorldId = Shader.PropertyToID("_OcclusionSafetyBiasWorld");

    [Header("Refs")]
    [Tooltip("Full-screen Image used as the fog overlay. If empty, this object's Image is used.")]
    [SerializeField] private Image _fogImage;
    [Header("Auto Refs")]
    [Tooltip("Player EntityVisionController. If empty, the component tries to find one through VisionVisibilitySystem or scene search.")]

    [ReadOnly][SerializeField] private EntityVisionController _viewer;

    [Tooltip("World camera used to convert player vision range into screen pixels.")]
    [ReadOnly][SerializeField] private Camera _worldCamera;

    [Header("Listening To")]
    [SerializeField] private WorldCameraEventChannelSO _worldCameraReadyChannel;

    [Header("Fog")]
    [SerializeField] private Color _fogColor = new Color(0f, 0f, 0f, 0.55f);
    [Tooltip("Visual width in screen pixels of the outer transition. MaxRange and VisionRange boundaries remain centered inside this soft edge.")]
    [SerializeField, Min(0f)] private float _edgeSoftnessPixels = 80f;

    [Tooltip("Optional direct shader reference. Assign UIVisionFog.shader here to avoid shader stripping in builds.")]
    [SerializeField] private Shader _fogShader;

    [Tooltip("Fallback shader name. Keep this unless the shader file was renamed.")]
    [SerializeField] private string _shaderName = "UI/VisionFog";

    [Header("Structure Occlusion")]
    [Tooltip("Number of radial LOS samples used to clip both the close circle and forward cone. Higher values make structure corners more precise but perform more physics raycasts.")]
    [SerializeField, Range(32, MaxOcclusionSamples)] private int _occlusionAngularSamples = MaxOcclusionSamples;

    [Tooltip("Maximum seconds between radial LOS rebuilds while the viewer is completely stationary. Viewer movement always rebuilds in the current render frame so the shader origin never uses stale ray distances.")]
    [SerializeField, Min(0.02f)] private float _occlusionRefreshInterval = 0.05f;

    [Tooltip("Visual softness in screen pixels at a structure-blocked vision edge. Keep this much smaller than the outer fog edge.")]
    [SerializeField, Min(0f)] private float _occlusionEdgeSoftnessPixels = 4f;

    [Tooltip("World-space distance subtracted from interpolated blocker hits. This prevents thin structure corners from revealing a bright sliver between adjacent angular samples.")]
    [SerializeField, Min(0f)] private float _occlusionSafetyBiasWorld = 0.08f;

    [Header("Options")]
    [SerializeField] private bool _autoFindViewer = true;
    [SerializeField] private bool _hideWhenMissingRefs = true;

    [Header("Debug")]
    [SerializeField] private bool _debugVisionValues;
    [SerializeField, Min(0.1f)] private float _debugLogInterval = 1f;

    private Material _runtimeMaterial;
    private float _nextDebugLogTime;
    private float _nextOcclusionRefreshTime;
    private readonly float[] _rawOcclusionDistances = new float[MaxOcclusionSamples];
    private readonly float[] _occlusionDistances = new float[MaxOcclusionSamples];
    private EntityVisionController _sampledViewer;
    private Vector3 _sampledVisionOriginWorld;
    private Vector3 _sampledEyePositionWorld;
    private float _sampledMaximumVisionRange;
    private int _sampledBlockerMask;
    private int _sampledOcclusionCount;
    private bool _sampledUseOcclusion;
    private bool _hasOcclusionSample;

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        ResolveRefs();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        ResolveRefs();
        EnsureMaterial();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_worldCameraReadyChannel != null)
        {
            _worldCameraReadyChannel.OnEventRaised += OnWorldCameraReady;

            if (_worldCameraReadyChannel.Current != null)
                OnWorldCameraReady(_worldCameraReadyChannel.Current);
        }

        ResolveRefs();
        EnsureMaterial();
        _nextOcclusionRefreshTime = 0f;
        _hasOcclusionSample = false;
        SetImageVisible(true);
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_worldCameraReadyChannel != null)
            _worldCameraReadyChannel.OnEventRaised -= OnWorldCameraReady;
    }

    // Purpose: Processes frame work that must run after the regular Update pass.
    private void LateUpdate()
    {
        if (_viewer == null && _autoFindViewer)
            ResolveViewer();

        if (_worldCamera == null)
            _worldCamera = Camera.main;

        if (_fogImage == null || _runtimeMaterial == null || _viewer == null || _worldCamera == null)
        {
            if (_hideWhenMissingRefs)
                SetImageVisible(false);

            return;
        }

        SetImageVisible(true);
        UpdateFogMaterial();
    }

    // Purpose: Releases owned resources before this component is destroyed.
    private void OnDestroy()
    {
        if (_runtimeMaterial != null)
            Destroy(_runtimeMaterial);
    }

    // Purpose: Updates the fog material from the current runtime state.
    private void UpdateFogMaterial()
    {
        Vector3 currentVisionOriginWorld = _viewer.VisualOrigin.position;
        Vector3 currentEyePositionWorld = currentVisionOriginWorld + Vector3.up * _viewer.EyeHeight;
        float circleRadius = Mathf.Max(0f, _viewer.CloseVisionRadius);
        float coneRange = Mathf.Max(0f, _viewer.VisionRange);
        float maximumVisionRange = Mathf.Max(circleRadius, coneRange);
        bool useOcclusion =
            maximumVisionRange > 0f &&
            _viewer.UsesLineOfSight &&
            _viewer.LineOfSightBlockerMask.value != 0;

        Vector3 centerWorld = UpdateOcclusionDistances(
            maximumVisionRange,
            useOcclusion,
            currentVisionOriginWorld,
            currentEyePositionWorld);
        Vector3 centerScreen = _worldCamera.WorldToScreenPoint(centerWorld);

        if (centerScreen.z <= 0f)
        {
            SetImageVisible(false);
            return;
        }

        Vector3 viewerForward = _viewer.ForwardDirection;
        viewerForward.y = 0f;

        if (viewerForward.sqrMagnitude <= 0.001f)
            viewerForward = Vector3.forward;
        else
            viewerForward.Normalize();

        Transform cameraTransform = _worldCamera.transform;
        float tanHalfFov = Mathf.Tan(_worldCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float aspect = Mathf.Max(0.001f, _worldCamera.aspect);
        float softnessWorld = ResolveWorldDistanceForPixels(centerWorld, _edgeSoftnessPixels);
        float occlusionSoftnessWorld = ResolveWorldDistanceForPixels(centerWorld, _occlusionEdgeSoftnessPixels);
        Color resolvedFogColor = _fogColor;
        resolvedFogColor.a = Mathf.Clamp01(
            resolvedFogColor.a *
            SectorRoomTraitRuntime.CurrentPlayerFogAlphaMultiplier);
        _runtimeMaterial.SetColor(FogColorId, resolvedFogColor);
        _runtimeMaterial.SetVector(VisionOriginWorldId, centerWorld);
        _runtimeMaterial.SetVector(ForwardWorldId, viewerForward);
        _runtimeMaterial.SetFloat(CircleRadiusWorldId, circleRadius);
        _runtimeMaterial.SetFloat(ConeRangeWorldId, coneRange);
        _runtimeMaterial.SetVector(WorldCameraPositionId, cameraTransform.position);
        _runtimeMaterial.SetVector(WorldCameraRightId, cameraTransform.right);
        _runtimeMaterial.SetVector(WorldCameraUpId, cameraTransform.up);
        _runtimeMaterial.SetVector(WorldCameraForwardId, cameraTransform.forward);
        _runtimeMaterial.SetVector(
            CameraProjectionId,
            new Vector4(
                tanHalfFov,
                aspect,
                Mathf.Max(0.001f, _worldCamera.orthographicSize),
                _worldCamera.orthographic ? 1f : 0f));

        bool useForwardArc =
            _viewer.UsesForwardArc &&
            _viewer.ForwardArcAngle < 359.9f;

        _runtimeMaterial.SetFloat(ArcCosId, Mathf.Cos(_viewer.ForwardArcAngle * 0.5f * Mathf.Deg2Rad));
        _runtimeMaterial.SetFloat(UseForwardArcId, useForwardArc ? 1f : 0f);
        _runtimeMaterial.SetFloat(SoftnessWorldId, softnessWorld);
        _runtimeMaterial.SetFloat(OcclusionSampleCountId, Mathf.Clamp(_occlusionAngularSamples, 32, MaxOcclusionSamples));
        _runtimeMaterial.SetFloat(UseOcclusionId, useOcclusion ? 1f : 0f);
        _runtimeMaterial.SetFloat(OcclusionSoftnessWorldId, occlusionSoftnessWorld);
        _runtimeMaterial.SetFloat(OcclusionSafetyBiasWorldId, _occlusionSafetyBiasWorld);

        LogDebugVisionValues(
            coneRange,
            circleRadius,
            softnessWorld,
            viewerForward,
            useForwardArc,
            useOcclusion);
    }

    // Purpose: Performs the log debug vision values workflow for this component.
    private void LogDebugVisionValues(
        float visionRange,
        float closeVisionRadius,
        float softnessWorld,
        Vector3 forwardWorld,
        bool useForwardArc,
        bool useOcclusion)
    {
        if (!_debugVisionValues || Time.unscaledTime < _nextDebugLogTime)
            return;

        _nextDebugLogTime = Time.unscaledTime + _debugLogInterval;

        Debug.Log(
            $"[PlayerVisionFogOverlayUI] viewer={_viewer.name}, " +
            $"fogRange={visionRange:0.##}, closeVisionRadius={closeVisionRadius:0.##}, gameplayVisionRange={_viewer.VisionRange:0.##}, " +
            $"softnessWorld={softnessWorld:0.###}, arc={_viewer.ForwardArcAngle:0.##}, " +
            $"useArc={useForwardArc}, useOcclusion={useOcclusion}, blockerMask={_viewer.LineOfSightBlockerMask.value}, " +
            $"forwardWorld={forwardWorld}",
            this);
    }

    // Purpose: Resolves the world distance for pixels needed by this component.
    private float ResolveWorldDistanceForPixels(Vector3 centerWorld, float screenPixels)
    {
        if (screenPixels <= 0f)
            return 0f;

        Vector3 worldRight = _worldCamera.transform.right;
        worldRight.y = 0f;

        if (worldRight.sqrMagnitude <= 0.001f)
            worldRight = Vector3.right;
        else
            worldRight.Normalize();

        Vector3 centerScreen = _worldCamera.WorldToScreenPoint(centerWorld);
        Vector3 oneUnitScreen = _worldCamera.WorldToScreenPoint(centerWorld + worldRight);
        float pixelsPerWorldUnit = Vector2.Distance(centerScreen, oneUnitScreen);

        return pixelsPerWorldUnit > 0.001f
            ? screenPixels / pixelsPerWorldUnit
            : 0f;
    }

    // Purpose: Updates the occlusion distances from the current runtime state.
    private Vector3 UpdateOcclusionDistances(
        float maximumVisionRange,
        bool useOcclusion,
        Vector3 currentVisionOriginWorld,
        Vector3 currentEyePositionWorld)
    {
        int sampleCount = Mathf.Clamp(_occlusionAngularSamples, 32, MaxOcclusionSamples);
        int blockerMask = _viewer.LineOfSightBlockerMask.value;
        bool originChanged =
            !_hasOcclusionSample ||
            (currentVisionOriginWorld - _sampledVisionOriginWorld).sqrMagnitude > OcclusionOriginChangeEpsilonSqr ||
            (currentEyePositionWorld - _sampledEyePositionWorld).sqrMagnitude > OcclusionOriginChangeEpsilonSqr;
        bool settingsChanged =
            !_hasOcclusionSample ||
            _sampledViewer != _viewer ||
            _sampledOcclusionCount != sampleCount ||
            _sampledUseOcclusion != useOcclusion ||
            _sampledBlockerMask != blockerMask ||
            !Mathf.Approximately(_sampledMaximumVisionRange, maximumVisionRange);
        bool stationaryRefreshDue = Time.unscaledTime >= _nextOcclusionRefreshTime;

        if (!originChanged && !settingsChanged && !stationaryRefreshDue)
            return _sampledVisionOriginWorld;

        _nextOcclusionRefreshTime =
            Time.unscaledTime + Mathf.Max(0.02f, _occlusionRefreshInterval);
        _sampledViewer = _viewer;
        _sampledVisionOriginWorld = currentVisionOriginWorld;
        _sampledEyePositionWorld = currentEyePositionWorld;
        _sampledMaximumVisionRange = maximumVisionRange;
        _sampledBlockerMask = blockerMask;
        _sampledOcclusionCount = sampleCount;
        _sampledUseOcclusion = useOcclusion;
        _hasOcclusionSample = true;

        for (int i = 0; i < sampleCount; i++)
        {
            float angle = i / (float)sampleCount * Mathf.PI * 2f;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

            _rawOcclusionDistances[i] = useOcclusion
                ? _viewer.GetVisibleDistanceFrom(
                    currentEyePositionWorld,
                    direction,
                    maximumVisionRange)
                : maximumVisionRange;
        }

        // Store conservative values at each angular sample before the shader interpolates
        // them. A one-sample thin corner therefore darkens its neighbours instead of leaking.
        for (int i = 0; i < sampleCount; i++)
        {
            int previous = i > 0 ? i - 1 : sampleCount - 1;
            int next = i + 1 < sampleCount ? i + 1 : 0;
            _occlusionDistances[i] = Mathf.Min(
                _rawOcclusionDistances[previous],
                Mathf.Min(_rawOcclusionDistances[i], _rawOcclusionDistances[next]));
        }

        // Unity fixes a material array's capacity on its first assignment, so always send the full buffer.
        for (int i = sampleCount; i < MaxOcclusionSamples; i++)
            _occlusionDistances[i] = maximumVisionRange;

        _runtimeMaterial.SetFloatArray(OcclusionDistancesId, _occlusionDistances);
        return _sampledVisionOriginWorld;
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        if (_fogImage == null)
            _fogImage = GetComponent<Image>();

        if (_viewer == null && _autoFindViewer)
            ResolveViewer();

        if (_worldCamera == null)
            _worldCamera = Camera.main;
    }

    // Purpose: Resolves the viewer needed by this component.
    private void ResolveViewer()
    {
        if (VisionVisibilitySystem.Instance != null &&
            VisionVisibilitySystem.Instance.Viewer != null)
        {
            _viewer = VisionVisibilitySystem.Instance.Viewer;
            return;
        }

        EntityVisionController[] candidates = FindObjectsByType<EntityVisionController>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < candidates.Length; i++)
        {
            EntityVisionController candidate = candidates[i];

            if (candidate == null)
                continue;

            if (candidate.GetComponent<PlayerStatsRuntime>() != null ||
                candidate.GetComponentInParent<PlayerStatsRuntime>() != null)
            {
                _viewer = candidate;
                return;
            }
        }
    }

    // Purpose: Ensures material is ready before the workflow continues.
    private void EnsureMaterial()
    {
        if (_fogImage == null || _runtimeMaterial != null)
            return;

        Shader shader = _fogShader != null
            ? _fogShader
            : Shader.Find(_shaderName);

        if (shader == null)
        {
            Debug.LogWarning($"[PlayerVisionFogOverlayUI] Shader not found. shader={_shaderName}", this);
            return;
        }

        _runtimeMaterial = new Material(shader)
        {
            name = "Runtime_PlayerVisionFog"
        };

        _fogImage.material = _runtimeMaterial;
        _fogImage.raycastTarget = false;
    }

    // Purpose: Updates the image visible state.
    private void SetImageVisible(bool visible)
    {
        if (_fogImage != null && _fogImage.enabled != visible)
            _fogImage.enabled = visible;
    }

    // Purpose: Responds to the world camera ready callback.
    private void OnWorldCameraReady(Camera camera)
    {
        _worldCamera = camera;
    }
}
