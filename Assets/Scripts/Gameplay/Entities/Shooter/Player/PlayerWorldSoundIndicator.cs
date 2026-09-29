/*
 * Role hierarchy: Gameplay > Entities > Shooter > Player
 * Primary responsibility: Handles PlayerWorldSoundIndicator audio playback and sound-stimulus delivery.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[AddComponentMenu("Scripts/Player World Sound Indicator")]
[DefaultExecutionOrder(950)]
public class PlayerWorldSoundIndicator : MonoBehaviour
{
    private const float MinimumReadableScale = 6f;
    private static readonly Quaternion AuthoredArrowDirectionCorrection =
        Quaternion.Euler(0f, 0f, 180f);

    [Header("Events")]
    [Tooltip("World sound events received by this player indicator.")]
    [SerializeField] private SoundStimulusEventChannelSO _soundStimulusEvent;

    [Header("Refs")]
    [Tooltip("Child SpriteRenderer or MeshRenderer used as the direction arrow.")]
    [SerializeField] private string _soundIndicatorChildName = "SoundIndicator";

    [Header("Sound Types")]
    [SerializeField] private bool _showFootstepDirection = true;
    [SerializeField] private bool _showGunshotDirection = true;
    [SerializeField] private bool _showProjectileDirection = true;
    [SerializeField] private bool _showImpactDirection = true;
    [SerializeField] private bool _showExplosionDirection = true;

    [Header("Distance Color")]
    [Tooltip("Color used for sounds close to their listener. Alpha is always forced to 1.")]
    [SerializeField] private Color _nearSoundIndicatorColor = new(1f, 0.05f, 0.02f, 1f);

    [Tooltip("Color used near the edge of the SoundStimulus audible radius. Alpha is always forced to 1.")]
    [SerializeField] private Color _farSoundIndicatorColor = new(0.08f, 1f, 0.12f, 1f);

    [FormerlySerializedAs("_fullRedDistanceRatio")]
    [SerializeField, Range(0f, 0.95f)] private float _fullRedAudibleRadiusRatio = 0.5f;

    [FormerlySerializedAs("_fullGreenDistanceRatio")]
    [SerializeField, Range(0.05f, 1f)] private float _fullGreenAudibleRadiusRatio = 1f;

    [Header("Filter")]
    [SerializeField] private bool _ignoreOwnSounds = true;

    [FormerlySerializedAs("_soundInnerMuteRadius")]
    [FormerlySerializedAs("_ignoreFootstepSoundsCloserThan")]
    [SerializeField, Min(0f)] private float _minimumSoundIndicatorDistance;

    [Header("Display")]
    [FormerlySerializedAs("_soundVisibleSeconds")]
    [SerializeField, Min(0.01f)] private float _soundIndicatorVisibleSeconds = 2f;

    [Tooltip(
        "Fraction of CloseVisionRadius used by the conceptual audio orbit around the player. " +
        "This orbit is intentionally independent from walls and the LOS-clipped visible boundary.")]
    [SerializeField, Range(0.1f, 1f)] private float _soundIndicatorRadiusMultiplier = 0.75f;

    [SerializeField, Min(0f)] private float _soundIndicatorYOffset = 0.35f;
    [SerializeField] private Vector3 _soundIndicatorLocalScale = new(6f, 6f, 6f);
    [SerializeField] private int _soundIndicatorSortingOrder = 500;

    [Tooltip("Direction smoothing only. The arrow is still re-anchored to the player's conceptual audio orbit every rendered frame.")]
    [SerializeField, Min(0f)] private float _soundIndicatorMoveSmoothTime = 0.05f;

    [SerializeField] private Vector3 _soundIndicatorRotationOffset;

    [Header("Debug")]
    [SerializeField] private bool _debugLogs;
    [SerializeField, Min(0.1f)] private float _debugLogInterval = 1f;

    private EntityVisionController _vision;
    private PlayerStatsRuntime _playerStats;
    private Transform _origin;
    private Transform _ownerRoot;
    private Transform _indicator;
    private SpriteRenderer _spriteRenderer;
    private Renderer _renderer;
    private VisionVisibilitySystem _visibilitySystem;
    private LineRenderer _legacyRangeLine;

    private readonly Dictionary<int, IndicatorInstance> _activeIndicators = new();
    private readonly List<int> _indicatorKeyBuffer = new();
    private readonly List<IndicatorInstance> _indicatorSortBuffer = new();
    private int _nextIndicatorSortingOffset;

    private sealed class IndicatorInstance
    {
        public int key;
        public Transform visual;
        public SpriteRenderer spriteRenderer;
        public Renderer renderer;
        public MaterialPropertyBlock propertyBlock;
        public GameObject source;
        public Vector3 sourcePosition;
        public float audibleRadius;
        public float hideTime;
        public bool trackMovingSource;
        public Vector3 displayDirection = Vector3.forward;
        public float nextDebugLogTime;
        public float lastUpdatedTime;
    }

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        ResolveRefs();
        DisableLegacyRangeLine();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        ResolveRefs();
        DisableLegacyRangeLine();
        ClearIndicators();
        SetTemplateVisible(false);
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        ResolveRefs();
        DisableLegacyRangeLine();

        if (_soundStimulusEvent != null)
            _soundStimulusEvent.OnEventRaised += OnSoundStimulus;

        ClearIndicators();
        SetTemplateVisible(false);
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_soundStimulusEvent != null)
            _soundStimulusEvent.OnEventRaised -= OnSoundStimulus;

        ClearIndicators();
    }

    // Purpose: Processes frame work that must run after the regular Update pass.
    private void LateUpdate()
    {
        ResolveRefs();
        DisableLegacyRangeLine();

        if (AreSoundIndicatorsSuppressed())
        {
            ClearIndicators();
            return;
        }

        UpdateIndicatorInstances();
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        if (_vision == null)
            _vision = EntityComponentResolver.Resolve<EntityVisionController>(this);

        if (_origin == null)
            _origin = _vision != null ? _vision.VisualOrigin : EntityComponentResolver.ResolveEntityRoot(this);

        if (_ownerRoot == null)
            _ownerRoot = EntityComponentResolver.ResolveEntityRoot(this) ?? _origin;

        if (_playerStats == null)
            _playerStats = EntityComponentResolver.Resolve<PlayerStatsRuntime>(this);

        if (_indicator == null)
            _indicator = FindChildByName(transform, _soundIndicatorChildName);

        if (_indicator != null)
        {
            _spriteRenderer ??= _indicator.GetComponent<SpriteRenderer>();
            _renderer ??= _indicator.GetComponent<Renderer>();

            _soundIndicatorLocalScale = new Vector3(
                Mathf.Max(MinimumReadableScale, _soundIndicatorLocalScale.x),
                Mathf.Max(MinimumReadableScale, _soundIndicatorLocalScale.y),
                Mathf.Max(MinimumReadableScale, _soundIndicatorLocalScale.z));
            _indicator.localScale = _soundIndicatorLocalScale;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.sortingOrder = _soundIndicatorSortingOrder;
                _spriteRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _spriteRenderer.receiveShadows = false;
                _spriteRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                _spriteRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            }
        }

        _visibilitySystem ??= VisionVisibilitySystem.Instance;
        _legacyRangeLine ??= GetComponent<LineRenderer>();

        if (_fullGreenAudibleRadiusRatio <= _fullRedAudibleRadiusRatio + 0.01f)
        {
            _fullRedAudibleRadiusRatio = 0.5f;
            _fullGreenAudibleRadiusRatio = 1f;
        }
    }

    // Purpose: Disables legacy range line for this component.
    private void DisableLegacyRangeLine()
    {
        if (_legacyRangeLine != null)
            _legacyRangeLine.enabled = false;
    }

    // Purpose: Responds to the sound stimulus callback.
    private void OnSoundStimulus(SoundStimulus stimulus)
    {
        if (!stimulus.IsValid || _vision == null || _origin == null || _indicator == null)
            return;

        if (AreSoundIndicatorsSuppressed())
            return;

        if (!ShouldShow(stimulus.type))
            return;

        if (_ignoreOwnSounds && stimulus.source != null && IsOwnSource(stimulus.source.transform))
            return;

        if (IsSourceVisible(stimulus.source))
            return;

        Vector3 delta = stimulus.position - _origin.position;
        delta.y = 0f;
        float distance = delta.magnitude;

        if (distance < Mathf.Max(0f, _minimumSoundIndicatorDistance) ||
            distance > Mathf.Max(0f, stimulus.radius) ||
            distance <= 0.0001f)
        {
            return;
        }

        int sourceKey = ResolveSourceKey(stimulus);
        bool alreadyTracking = _activeIndicators.TryGetValue(
            sourceKey,
            out IndicatorInstance instance);

        if (!alreadyTracking)
        {
            instance = CreateIndicatorInstance(sourceKey);
            if (instance == null)
                return;

            _activeIndicators.Add(sourceKey, instance);
        }

        ResolveTrackedSource(
            stimulus,
            out GameObject trackedSource,
            out Vector3 trackedPosition,
            out bool trackMovingSource);

        instance.source = trackedSource;
        instance.sourcePosition = trackedPosition;
        instance.audibleRadius = Mathf.Max(0.01f, stimulus.radius);
        instance.trackMovingSource = trackMovingSource;

        if (!alreadyTracking)
        {
            Vector3 displayDelta = trackedPosition - _origin.position;
            displayDelta.y = 0f;
            instance.displayDirection = displayDelta.sqrMagnitude > 0.0001f
                ? displayDelta.normalized
                : delta / distance;
        }

        instance.hideTime = Time.unscaledTime + Mathf.Max(0.01f, _soundIndicatorVisibleSeconds);
        instance.lastUpdatedTime = Time.unscaledTime;
        SetIndicatorVisible(instance, true);
        BringIndicatorToFront(instance);
        UpdateIndicatorTransform(instance);
    }

    // Purpose: Updates the indicator instances from the current runtime state.
    private void UpdateIndicatorInstances()
    {
        if (_activeIndicators.Count == 0)
            return;

        _indicatorKeyBuffer.Clear();
        foreach (int key in _activeIndicators.Keys)
            _indicatorKeyBuffer.Add(key);

        for (int i = 0; i < _indicatorKeyBuffer.Count; i++)
        {
            int key = _indicatorKeyBuffer[i];
            if (!_activeIndicators.TryGetValue(key, out IndicatorInstance instance))
                continue;

            if (Time.unscaledTime >= instance.hideTime ||
                IsSourceVisible(instance.source) ||
                !UpdateIndicatorTransform(instance))
            {
                RemoveIndicator(key);
            }
        }
    }

    // Purpose: Updates the indicator transform from the current runtime state.
    private bool UpdateIndicatorTransform(IndicatorInstance instance)
    {
        if (instance == null || instance.visual == null || _vision == null || _origin == null)
            return false;

        if (instance.trackMovingSource && instance.source != null)
            instance.sourcePosition = instance.source.transform.position;

        Vector3 delta = instance.sourcePosition - _origin.position;
        delta.y = 0f;
        float distance = delta.magnitude;

        if (distance <= 0.0001f)
            return false;

        Vector3 targetDirection = delta / distance;

        if (_soundIndicatorMoveSmoothTime <= 0.0001f || instance.displayDirection.sqrMagnitude <= 0.0001f)
        {
            instance.displayDirection = targetDirection;
        }
        else
        {
            float blend = 1f - Mathf.Exp(
                -Time.unscaledDeltaTime / Mathf.Max(0.0001f, _soundIndicatorMoveSmoothTime));
            instance.displayDirection = Vector3.Slerp(
                instance.displayDirection,
                targetDirection,
                blend).normalized;
        }

        float closeRadius = Mathf.Max(0f, _vision.CloseVisionRadius);
        if (closeRadius <= 0f)
            return false;

        // Sound direction is UI information, not visible-world geometry. Keeping
        // it on a stable orbit prevents nearby walls from pushing the arrow into
        // the dark LOS-clipped region or directly onto an occluder.
        float displayRadius = closeRadius * Mathf.Clamp(
            _soundIndicatorRadiusMultiplier,
            0.1f,
            1f);

        instance.visual.position =
            _origin.position +
            instance.displayDirection * displayRadius +
            Vector3.up * _soundIndicatorYOffset;

        float redDistance = instance.audibleRadius * Mathf.Clamp01(_fullRedAudibleRadiusRatio);
        float greenDistance = instance.audibleRadius * Mathf.Clamp(
            _fullGreenAudibleRadiusRatio,
            _fullRedAudibleRadiusRatio + 0.01f,
            1f);
        float nearT = 1f - Mathf.InverseLerp(redDistance, greenDistance, distance);
        Color color = Color.Lerp(
            _farSoundIndicatorColor,
            _nearSoundIndicatorColor,
            Mathf.Clamp01(nearT));
        color.a = 1f;
        SetIndicatorColor(instance, color);

        instance.visual.rotation =
            Quaternion.LookRotation(Vector3.up, instance.displayDirection) *
            AuthoredArrowDirectionCorrection *
            Quaternion.Euler(_soundIndicatorRotationOffset);

        if (_debugLogs && Time.unscaledTime >= instance.nextDebugLogTime)
        {
            instance.nextDebugLogTime = Time.unscaledTime + _debugLogInterval;
            Debug.Log(
                $"[PlayerWorldSoundIndicator] sourceKey={instance.key}, active={_activeIndicators.Count}, " +
                $"distance={distance:0.##}, audible={instance.audibleRadius:0.##}, audioOrbit={displayRadius:0.##}",
                this);
        }

        return true;
    }

    // Purpose: Performs the should show workflow for this component.
    private bool ShouldShow(SoundStimulusType type)
    {
        return type switch
        {
            SoundStimulusType.Footstep => _showFootstepDirection,
            SoundStimulusType.Gunshot => _showGunshotDirection,
            SoundStimulusType.ProjectileFlyby => _showProjectileDirection,
            SoundStimulusType.Impact => _showImpactDirection,
            SoundStimulusType.ProjectileImpact => _showImpactDirection,
            SoundStimulusType.Explosion => _showExplosionDirection,
            _ => false
        };
    }

    // Purpose: Performs the are sound indicators suppressed workflow for this component.
    private bool AreSoundIndicatorsSuppressed()
    {
        return _playerStats != null &&
               _playerStats.StatusEffects != null &&
               _playerStats.StatusEffects.SuppressesSoundIndicators;
    }

    // Purpose: Checks whether source visible is currently true.
    private bool IsSourceVisible(GameObject source)
    {
        if (source == null)
            return false;

        _visibilitySystem ??= VisionVisibilitySystem.Instance;
        if (_visibilitySystem == null)
            return false;

        VisionVisibilityTarget target =
            source.GetComponentInParent<VisionVisibilityTarget>() ??
            source.GetComponentInChildren<VisionVisibilityTarget>();

        return target != null && _visibilitySystem.IsVisible(target);
    }

    // Purpose: Checks whether own source is currently true.
    private bool IsOwnSource(Transform source)
    {
        return source != null && _ownerRoot != null &&
               (source == _ownerRoot || source.IsChildOf(_ownerRoot));
    }

    // Purpose: Resolves the tracked source needed by this component.
    private void ResolveTrackedSource(
        SoundStimulus stimulus,
        out GameObject trackedSource,
        out Vector3 trackedPosition,
        out bool trackMovingSource)
    {
        trackedSource = stimulus.source;
        trackedPosition = stimulus.position;
        trackMovingSource =
            stimulus.type == SoundStimulusType.Footstep ||
            stimulus.type == SoundStimulusType.Gunshot ||
            stimulus.type == SoundStimulusType.ProjectileFlyby;

        if (stimulus.source == null)
            return;

        Transform sourceRoot = EntityComponentResolver.ResolveEntityRoot(stimulus.source.transform);
        if (sourceRoot == null || sourceRoot == _ownerRoot)
            return;

        // Gunshot, flyby, and impact stimuli from one enemy share that enemy's
        // indicator. Keep it anchored to the emitting entity instead of letting
        // a later projectile impact move the arrow to the hit point.
        trackedSource = sourceRoot.gameObject;
        trackedPosition = sourceRoot.position;
        trackMovingSource = true;
    }

    // Purpose: Resolves the source key needed by this component.
    private int ResolveSourceKey(SoundStimulus stimulus)
    {
        if (stimulus.source != null)
        {
            Transform sourceRoot = EntityComponentResolver.ResolveEntityRoot(stimulus.source.transform);
            if (sourceRoot != null && sourceRoot != _ownerRoot)
                return sourceRoot.gameObject.GetInstanceID();

            return stimulus.source.GetInstanceID();
        }

        if (stimulus.HasEmitter)
            return stimulus.emitterId;

        // Source-less world stimuli of different types remain independently readable.
        return int.MinValue + (int)stimulus.type;
    }

    // Purpose: Creates the indicator instance owned by this workflow.
    private IndicatorInstance CreateIndicatorInstance(int sourceKey)
    {
        if (_indicator == null)
            return null;

        GameObject visualObject = Instantiate(_indicator.gameObject, transform);
        visualObject.name = $"{_soundIndicatorChildName}_Runtime_{sourceKey}";
        visualObject.SetActive(true);

        Transform visual = visualObject.transform;
        visual.localScale = _soundIndicatorLocalScale;

        IndicatorInstance instance = new()
        {
            key = sourceKey,
            visual = visual,
            spriteRenderer = visualObject.GetComponent<SpriteRenderer>(),
            renderer = visualObject.GetComponent<Renderer>(),
            lastUpdatedTime = Time.unscaledTime
        };

        if (instance.renderer == null)
        {
            Destroy(visualObject);
            return null;
        }

        if (instance.spriteRenderer != null)
        {
            instance.spriteRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            instance.spriteRenderer.receiveShadows = false;
            instance.spriteRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            instance.spriteRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }

        SetIndicatorVisible(instance, true);
        return instance;
    }

    // Purpose: Performs the bring indicator to front workflow for this component.
    private void BringIndicatorToFront(IndicatorInstance instance)
    {
        if (instance == null || instance.renderer == null)
            return;

        if (_nextIndicatorSortingOffset >= 30000)
            RebaseIndicatorSortingOrders();

        instance.renderer.sortingOrder = _soundIndicatorSortingOrder + _nextIndicatorSortingOffset;
        _nextIndicatorSortingOffset++;
    }

    // Purpose: Performs the rebase indicator sorting orders workflow for this component.
    private void RebaseIndicatorSortingOrders()
    {
        _indicatorSortBuffer.Clear();
        foreach (IndicatorInstance instance in _activeIndicators.Values)
            _indicatorSortBuffer.Add(instance);

        _indicatorSortBuffer.Sort((a, b) => a.lastUpdatedTime.CompareTo(b.lastUpdatedTime));
        _nextIndicatorSortingOffset = 0;

        for (int i = 0; i < _indicatorSortBuffer.Count; i++)
        {
            IndicatorInstance instance = _indicatorSortBuffer[i];
            if (instance.renderer == null)
                continue;

            instance.renderer.sortingOrder = _soundIndicatorSortingOrder + _nextIndicatorSortingOffset;
            _nextIndicatorSortingOffset++;
        }
    }

    // Purpose: Updates the indicator visible state.
    private static void SetIndicatorVisible(IndicatorInstance instance, bool visible)
    {
        if (instance == null || instance.visual == null)
            return;

        if (instance.renderer != null)
            instance.renderer.enabled = visible;

        if (instance.renderer == null)
            instance.visual.gameObject.SetActive(visible);
    }

    // Purpose: Updates the indicator color state.
    private static void SetIndicatorColor(IndicatorInstance instance, Color color)
    {
        if (instance == null)
            return;

        if (instance.spriteRenderer != null)
            instance.spriteRenderer.color = color;

        if (instance.renderer == null || instance.renderer == instance.spriteRenderer)
            return;

        instance.propertyBlock ??= new MaterialPropertyBlock();
        instance.renderer.GetPropertyBlock(instance.propertyBlock);
        instance.propertyBlock.SetColor("_BaseColor", color);
        instance.propertyBlock.SetColor("_Color", color);
        instance.renderer.SetPropertyBlock(instance.propertyBlock);
    }

    // Purpose: Removes the requested indicator from this component.
    private void RemoveIndicator(int sourceKey)
    {
        if (!_activeIndicators.TryGetValue(sourceKey, out IndicatorInstance instance))
            return;

        _activeIndicators.Remove(sourceKey);

        if (instance.visual != null)
            Destroy(instance.visual.gameObject);
    }

    // Purpose: Clears the current indicators state.
    private void ClearIndicators()
    {
        foreach (IndicatorInstance instance in _activeIndicators.Values)
        {
            if (instance.visual != null)
                Destroy(instance.visual.gameObject);
        }

        _activeIndicators.Clear();
        _indicatorKeyBuffer.Clear();
        _indicatorSortBuffer.Clear();
        _nextIndicatorSortingOffset = 0;
    }

    // Purpose: Updates the template visible state.
    private void SetTemplateVisible(bool visible)
    {
        if (_indicator == null)
            return;

        if (_renderer != null)
            _renderer.enabled = visible;

        if (_renderer == null)
            _indicator.gameObject.SetActive(visible);
    }

    // Purpose: Finds the requested child by name.
    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        Transform direct = root.Find(childName);
        if (direct != null)
            return direct;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != root && children[i].name == childName)
                return children[i];
        }

        return null;
    }
}
