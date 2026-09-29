/*
 * Role hierarchy: WIP > Legacy > Gameplay > Player > ExistingWIP
 * Primary responsibility: Isolates the legacy or deferred PlayerWorldRangeRing implementation from active production code.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[RequireComponent(typeof(LineRenderer))]
[AddComponentMenu("Scripts/WIP/Player World Range Ring")]
[DefaultExecutionOrder(950)]
public class PlayerWorldRangeRing : MonoBehaviour
{
    private const float MinimumReadableSoundIndicatorScale = 6f;
    private static readonly Quaternion AuthoredArrowDirectionCorrection =
        Quaternion.Euler(0f, 0f, 180f);

    [Header("Events")]
    [Tooltip("World sound events are received from this channel. Footsteps, gunshots, projectile sounds, impacts, and explosions can each be displayed.")]
    [SerializeField] private SoundStimulusEventChannelSO _soundStimulusEvent;

    [Header("Sound Direction")]
    [Tooltip("Child object name used as the sound direction indicator. Add this child under this object and give it a SpriteRenderer or MeshRenderer.")]
    [SerializeField] private string _soundIndicatorChildName = "SoundIndicator";

    [Header("Ring Visual")]
    [Tooltip("If false, the max range ring is hidden but sound direction can still be processed.")]
    [SerializeField] private bool _showRing = true;

    [Tooltip("Small Y offset that keeps the ring above the floor to reduce z-fighting.")]
    [SerializeField, Min(0f)] private float _ringYOffset = 0.05f;

    [Tooltip("How many points are used to draw the circular ring. Higher values are smoother.")]
    [SerializeField, Min(8)] private int _ringSegments = 96;

    [Tooltip("World-space line width for the ring.")]
    [SerializeField, Min(0.001f)] private float _ringLineWidth = 0.06f;

    [Tooltip("Ring line color.")]
    [SerializeField] private Color _ringColor = Color.black;

    [Tooltip("Optional line material. If empty, Unity's current LineRenderer material is used.")]
    [SerializeField] private Material _ringMaterial;

    [Tooltip("If true, the LineRenderer is disabled when range data is unavailable.")]
    [SerializeField] private bool _hideWhenUnavailable = true;

    [Header("Sound Types")]
    [Tooltip("Show audible footstep directions.")]
    [SerializeField] private bool _showFootstepDirection = true;

    [Tooltip("Show audible gunshot directions.")]
    [SerializeField] private bool _showGunshotDirection = true;

    [Tooltip("Show projectile flyby directions.")]
    [SerializeField] private bool _showProjectileDirection = true;

    [Tooltip("Show generic and projectile impact directions.")]
    [SerializeField] private bool _showImpactDirection = true;

    [Tooltip("Show audible explosion directions.")]
    [SerializeField] private bool _showExplosionDirection = true;

    [Header("Sound Distance Visual")]
    [Tooltip("Indicator color for a sound very close to the player.")]
    [SerializeField] private Color _nearSoundIndicatorColor = new Color(1f, 0.05f, 0.02f, 1f);

    [Tooltip("Indicator color for a sound near the edge of its audible radius.")]
    [SerializeField] private Color _farSoundIndicatorColor = new Color(0.08f, 1f, 0.12f, 1f);

    [Tooltip("At or below this fraction of the received SoundStimulus radius, the indicator is fully red.")]
    [FormerlySerializedAs("_fullRedDistanceRatio")]
    [SerializeField, Range(0f, 0.95f)] private float _fullRedAudibleRadiusRatio = 0.5f;

    [Tooltip("At or above this fraction of the received SoundStimulus radius, the indicator is fully green.")]
    [FormerlySerializedAs("_fullGreenDistanceRatio")]
    [SerializeField, Range(0.05f, 1f)] private float _fullGreenAudibleRadiusRatio = 1f;

    [Header("Sound Filter")]
    [Tooltip("If true, sound events emitted by the player entity or any of its child components are ignored.")]
    [SerializeField] private bool _ignoreOwnSounds = true;

    [Header("Sound Display")]
    [Tooltip("Sounds closer than this world distance are ignored. Keep this at 0 to show every audible combat cue.")]
    [FormerlySerializedAs("_soundInnerMuteRadius")]
    [FormerlySerializedAs("_ignoreFootstepSoundsCloserThan")]
    [SerializeField, Min(0f)] private float _minimumSoundIndicatorDistance = 0f;

    [Tooltip("Seconds the sound indicator stays visible after the latest accepted sound event.")]
    [FormerlySerializedAs("_soundVisibleSeconds")]
    [SerializeField, Min(0.01f)] private float _soundIndicatorVisibleSeconds = 2f;

    [Tooltip("Y offset for the sound indicator above the floor.")]
    [SerializeField, Min(0f)] private float _soundIndicatorYOffset = 0.35f;

    [Tooltip("Where on the ring the indicator appears. 1 means exactly on max range, 0.95 means slightly inside.")]
    [SerializeField, Min(0f)] private float _soundIndicatorRadiusMultiplier = 0.9f;

    [Tooltip("Local scale applied to the sound direction indicator child. Increase this when the arrow is difficult to read in a top-down camera.")]
    [SerializeField] private Vector3 _soundIndicatorLocalScale = new Vector3(6f, 6f, 6f);

    [Tooltip("Sprite sorting order used by the direction cue. Its assigned overlay material also ignores scene depth so structures cannot hide it.")]
    [SerializeField] private int _soundIndicatorSortingOrder = 500;

    [Tooltip("Smooth time used when the indicator moves to a new sound direction. 0 snaps immediately; 0.08~0.15 usually feels responsive without teleporting.")]
    [SerializeField, Min(0f)] private float _soundIndicatorMoveSmoothTime = 0.05f;

    [Tooltip("If true, a 2D arrow sprite whose graphic points upward will point away from the player on the range ring.")]
    [SerializeField] private bool _soundIndicatorPointOutwardOnRing = true;

    [Tooltip("If true, the indicator faces the main camera. Ignored when Point Outward On Ring is true.")]
    [SerializeField] private bool _soundIndicatorBillboardToCamera = true;

    [Tooltip("If billboard is off and Point Outward On Ring is false, this rotates the indicator toward the sound source direction.")]
    [SerializeField] private bool _soundIndicatorRotateTowardSource = true;

    [Tooltip("Extra local rotation applied after the built-in DropdownArrow direction correction.")]
    [SerializeField] private Vector3 _soundIndicatorRotationOffset;

    [Header("Debug")]
    [SerializeField] private bool _debugLogs;
    [SerializeField, Min(0.1f)] private float _debugLogInterval = 1f;

    private Transform _rangeOrigin;
    private EntityRange _range;
    private ShooterStatsRuntime _statsRuntime;
    private LineRenderer _ringLine;
    private Transform _soundIndicator;
    private SpriteRenderer _soundIndicatorSpriteRenderer;
    private Renderer _soundIndicatorRenderer;
    private VisionVisibilitySystem _visibilitySystem;
    private MaterialPropertyBlock _soundPropertyBlock;
    private Camera _worldCamera;
    private Transform _ownerRoot;
    private float _currentWorldRadius;
    private float _soundHideTime;
    private float _nextDebugLogTime;
    private bool _hasSoundIndicatorTarget;
    private GameObject _soundIndicatorSource;
    private Vector3 _soundIndicatorSourcePosition;
    private float _soundIndicatorAudibleRadius;
    private bool _soundIndicatorTracksMovingSource;
    private Vector3 _soundIndicatorOutwardDirection = Vector3.forward;
    private Color _soundIndicatorTargetColor = Color.clear;

    public float CurrentWorldRadius => _currentWorldRadius;
    public Transform RangeOrigin => _rangeOrigin;

    private void Reset()
    {
        ResolveRefs();
        ConfigureLineRenderer();
    }

    private void Awake()
    {
        ResolveRefs();
        ConfigureLineRenderer();
        HideSoundIndicator();
    }

    private void OnEnable()
    {
        if (_soundStimulusEvent != null)
            _soundStimulusEvent.OnEventRaised += OnSoundStimulus;

        HideSoundIndicator();
    }

    private void OnDisable()
    {
        if (_soundStimulusEvent != null)
            _soundStimulusEvent.OnEventRaised -= OnSoundStimulus;
    }

    private void LateUpdate()
    {
        ResolveRefs();
        RefreshRing();
        UpdateSoundIndicatorLifetime();
        UpdateSoundIndicatorTransform();
    }

    private void ResolveRefs()
    {
        if (_range == null)
            _range = EntityComponentResolver.Resolve<EntityRange>(this);

        if (_statsRuntime == null)
            _statsRuntime = EntityComponentResolver.Resolve<ShooterStatsRuntime>(this);

        if (_rangeOrigin == null)
        {
            if (_range != null && _range.RangeOrigin != null)
                _rangeOrigin = _range.RangeOrigin;
            else
                _rangeOrigin = EntityComponentResolver.ResolveEntityRoot(this) ?? transform;
        }

        if (_ownerRoot == null)
            _ownerRoot = EntityComponentResolver.ResolveEntityRoot(this) ?? _rangeOrigin;

        if (_ringLine == null)
            _ringLine = GetComponent<LineRenderer>();

        if (_soundIndicator == null)
            _soundIndicator = FindChildByName(transform, _soundIndicatorChildName);

        if (_soundIndicator != null)
        {
            if (_soundIndicatorSpriteRenderer == null)
                _soundIndicatorSpriteRenderer = _soundIndicator.GetComponent<SpriteRenderer>();

            if (_soundIndicatorRenderer == null)
                _soundIndicatorRenderer = _soundIndicator.GetComponent<Renderer>();

            // Also migrates already-spawned Player instances during script hot reload.
            _soundIndicatorLocalScale = new Vector3(
                Mathf.Max(MinimumReadableSoundIndicatorScale, _soundIndicatorLocalScale.x),
                Mathf.Max(MinimumReadableSoundIndicatorScale, _soundIndicatorLocalScale.y),
                Mathf.Max(MinimumReadableSoundIndicatorScale, _soundIndicatorLocalScale.z));
            _soundIndicator.localScale = _soundIndicatorLocalScale;

            if (_soundIndicatorSpriteRenderer != null)
            {
                _soundIndicatorSpriteRenderer.sortingOrder = _soundIndicatorSortingOrder;
                _soundIndicatorSpriteRenderer.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.Off;
                _soundIndicatorSpriteRenderer.receiveShadows = false;
                _soundIndicatorSpriteRenderer.lightProbeUsage =
                    UnityEngine.Rendering.LightProbeUsage.Off;
                _soundIndicatorSpriteRenderer.reflectionProbeUsage =
                    UnityEngine.Rendering.ReflectionProbeUsage.Off;
            }
        }

        if (_fullGreenAudibleRadiusRatio <= _fullRedAudibleRadiusRatio + 0.01f)
        {
            _fullRedAudibleRadiusRatio = 0.5f;
            _fullGreenAudibleRadiusRatio = 1f;
        }

        if (_visibilitySystem == null)
            _visibilitySystem = VisionVisibilitySystem.Instance;

        if (_worldCamera == null)
            _worldCamera = Camera.main;
    }

    private void ConfigureLineRenderer()
    {
        if (_ringLine == null)
            return;

        _ringLine.useWorldSpace = true;
        _ringLine.loop = true;
        _ringLine.alignment = LineAlignment.View;
        _ringLine.textureMode = LineTextureMode.Stretch;
        _ringLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _ringLine.receiveShadows = false;
        _ringLine.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        _ringLine.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        _ringLine.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

        _ringLine.widthMultiplier = _ringLineWidth;
        _ringLine.startColor = _ringColor;
        _ringLine.endColor = _ringColor;

        if (_ringMaterial != null)
            _ringLine.material = _ringMaterial;
    }

    private void RefreshRing()
    {
        _currentWorldRadius = ResolveWorldRadius();

        if (_ringLine == null)
            return;

        if (!_showRing || _rangeOrigin == null || _currentWorldRadius <= 0f)
        {
            _ringLine.enabled = !_hideWhenUnavailable;
            return;
        }

        ConfigureLineRenderer();

        int segments = Mathf.Max(8, _ringSegments);
        if (_ringLine.positionCount != segments)
            _ringLine.positionCount = segments;

        Vector3 center = _rangeOrigin.position + Vector3.up * _ringYOffset;
        float radius = _currentWorldRadius;

        for (int i = 0; i < segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            Vector3 position = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius);

            _ringLine.SetPosition(i, position);
        }

        _ringLine.enabled = true;
        LogDebug();
    }

    private float ResolveWorldRadius()
    {
        if (_range != null && _range.MaxRange > 0f)
            return _range.MaxRange;

        if (_statsRuntime != null && _statsRuntime.MaxRange > 0f)
            return _statsRuntime.MaxRange;

        return 0f;
    }

    private void OnSoundStimulus(SoundStimulus stimulus)
    {
        if (!stimulus.IsValid ||
            _rangeOrigin == null ||
            _soundIndicator == null)
        {
            return;
        }

        if (!ShouldShowSoundIndicator(stimulus.type))
            return;

        if (_ignoreOwnSounds &&
            stimulus.source != null &&
            IsOwnSoundSource(stimulus.source.transform))
        {
            return;
        }

        if (IsSourceVisible(stimulus.source))
            return;

        float radius = _currentWorldRadius > 0f
            ? _currentWorldRadius
            : ResolveWorldRadius();

        if (radius <= 0f)
            return;

        Vector3 delta = stimulus.position - _rangeOrigin.position;
        delta.y = 0f;

        float distance = delta.magnitude;
        float minimumDistance = Mathf.Max(0f, _minimumSoundIndicatorDistance);

        if (distance < minimumDistance || distance > stimulus.radius)
            return;

        Vector3 direction = delta.normalized;
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        bool wasTrackingSound = _hasSoundIndicatorTarget;
        _soundIndicatorSource = stimulus.source;
        _soundIndicatorSourcePosition = stimulus.position;
        _soundIndicatorAudibleRadius = Mathf.Max(0.01f, stimulus.radius);
        _soundIndicatorTracksMovingSource =
            stimulus.type == SoundStimulusType.Footstep ||
            stimulus.type == SoundStimulusType.Gunshot ||
            stimulus.type == SoundStimulusType.ProjectileFlyby;
        _hasSoundIndicatorTarget = true;

        if (!wasTrackingSound)
            _soundIndicatorOutwardDirection = direction;

        SetSoundIndicatorVisible(true);
        UpdateSoundIndicatorTransform();

        _soundHideTime = Time.unscaledTime + _soundIndicatorVisibleSeconds;
    }

    private bool ShouldShowSoundIndicator(SoundStimulusType type)
    {
        switch (type)
        {
            case SoundStimulusType.Footstep when _showFootstepDirection:
                return true;

            case SoundStimulusType.Gunshot when _showGunshotDirection:
                return true;

            case SoundStimulusType.ProjectileFlyby when _showProjectileDirection:
                return true;

            case SoundStimulusType.Impact when _showImpactDirection:
            case SoundStimulusType.ProjectileImpact when _showImpactDirection:
                return true;

            case SoundStimulusType.Explosion when _showExplosionDirection:
                return true;

            default:
                return false;
        }
    }

    private void UpdateSoundIndicatorLifetime()
    {
        if (_soundIndicator == null)
            return;

        if (Time.unscaledTime >= _soundHideTime)
            HideSoundIndicator();
    }

    private void UpdateSoundIndicatorTransform()
    {
        if (_soundIndicator == null ||
            !_hasSoundIndicatorTarget ||
            !IsSoundIndicatorVisible() ||
            _rangeOrigin == null)
        {
            return;
        }

        if (IsSourceVisible(_soundIndicatorSource))
        {
            HideSoundIndicator();
            return;
        }

        // Track a moving emitter every rendered frame. The stimulus position is
        // retained only as a fallback for sources that have already disappeared.
        if (_soundIndicatorTracksMovingSource && _soundIndicatorSource != null)
            _soundIndicatorSourcePosition = _soundIndicatorSource.transform.position;

        Vector3 delta = _soundIndicatorSourcePosition - _rangeOrigin.position;
        delta.y = 0f;

        float distance = delta.magnitude;
        if (distance <= 0.0001f)
            return;

        Vector3 targetDirection = delta / distance;

        if (_soundIndicatorMoveSmoothTime <= 0.0001f ||
            _soundIndicatorOutwardDirection.sqrMagnitude <= 0.0001f)
        {
            _soundIndicatorOutwardDirection = targetDirection;
        }
        else
        {
            float directionBlend = 1f - Mathf.Exp(
                -Time.unscaledDeltaTime / Mathf.Max(0.0001f, _soundIndicatorMoveSmoothTime));
            _soundIndicatorOutwardDirection = Vector3.Slerp(
                _soundIndicatorOutwardDirection,
                targetDirection,
                directionBlend).normalized;
        }

        float radius = _currentWorldRadius > 0f
            ? _currentWorldRadius
            : ResolveWorldRadius();
        float displayRadius = radius * Mathf.Max(0f, _soundIndicatorRadiusMultiplier);

        // Re-anchor to the moving Player every rendered frame. Direction is smoothed,
        // but radial distance remains exact so the cue never drifts off the range ring.
        _soundIndicator.position =
            _rangeOrigin.position +
            _soundIndicatorOutwardDirection * displayRadius +
            Vector3.up * _soundIndicatorYOffset;

        // Color communicates how close the listener is relative to this sound's
        // own audible radius. Player range only decides where the arrow is drawn.
        float redDistance = _soundIndicatorAudibleRadius *
            Mathf.Clamp01(_fullRedAudibleRadiusRatio);
        float greenDistance = _soundIndicatorAudibleRadius * Mathf.Clamp(
            _fullGreenAudibleRadiusRatio,
            _fullRedAudibleRadiusRatio + 0.01f,
            1f);
        float nearT = 1f - Mathf.InverseLerp(redDistance, greenDistance, distance);

        _soundIndicatorTargetColor = Color.Lerp(
            _farSoundIndicatorColor,
            _nearSoundIndicatorColor,
            Mathf.Clamp01(nearT));
        _soundIndicatorTargetColor.a = 1f;
        SetSoundIndicatorColor(_soundIndicatorTargetColor);

        UpdateSoundIndicatorRotation();
    }

    private void UpdateSoundIndicatorRotation()
    {
        if (_soundIndicator == null)
            return;

        if (_soundIndicatorPointOutwardOnRing)
        {
            Vector3 outward = _soundIndicatorOutwardDirection;
            outward.y = 0f;

            if (outward.sqrMagnitude <= 0.0001f)
                return;

            // SpriteRenderer's local +Y is the usual "arrow points up" axis.
            // This keeps the sprite flat on the XZ plane while local +Y points away from the player.
            _soundIndicator.rotation =
                Quaternion.LookRotation(Vector3.up, outward.normalized) *
                AuthoredArrowDirectionCorrection *
                Quaternion.Euler(_soundIndicatorRotationOffset);
            return;
        }

        if (_soundIndicatorBillboardToCamera)
        {
            if (_worldCamera == null)
                _worldCamera = Camera.main;

            if (_worldCamera == null)
                return;

            Vector3 toCamera = _soundIndicator.position - _worldCamera.transform.position;
            if (toCamera.sqrMagnitude <= 0.0001f)
                return;

            _soundIndicator.rotation =
                Quaternion.LookRotation(toCamera.normalized, _worldCamera.transform.up) *
                AuthoredArrowDirectionCorrection *
                Quaternion.Euler(_soundIndicatorRotationOffset);
            return;
        }

        if (_soundIndicatorRotateTowardSource)
        {
            Vector3 direction = _soundIndicatorOutwardDirection;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.0001f)
                return;

            _soundIndicator.rotation =
                Quaternion.LookRotation(direction.normalized, Vector3.up) *
                AuthoredArrowDirectionCorrection *
                Quaternion.Euler(_soundIndicatorRotationOffset);
        }
    }

    private void HideSoundIndicator()
    {
        _hasSoundIndicatorTarget = false;
        _soundIndicatorSource = null;
        _soundIndicatorSourcePosition = Vector3.zero;
        _soundIndicatorAudibleRadius = 0f;
        _soundIndicatorTracksMovingSource = false;
        _soundIndicatorOutwardDirection = Vector3.forward;
        SetSoundIndicatorVisible(false);
    }

    private bool IsSoundIndicatorVisible()
    {
        if (_soundIndicator == null)
            return false;

        if (_soundIndicatorSpriteRenderer != null)
            return _soundIndicatorSpriteRenderer.enabled;

        if (_soundIndicatorRenderer != null)
            return _soundIndicatorRenderer.enabled;

        return _soundIndicator.gameObject.activeSelf;
    }

    private void SetSoundIndicatorVisible(bool visible)
    {
        if (_soundIndicator == null)
            return;

        if (_soundIndicatorSpriteRenderer != null)
            _soundIndicatorSpriteRenderer.enabled = visible;

        if (_soundIndicatorRenderer != null &&
            _soundIndicatorRenderer != _soundIndicatorSpriteRenderer)
        {
            _soundIndicatorRenderer.enabled = visible;
        }

        if (_soundIndicatorSpriteRenderer == null &&
            _soundIndicatorRenderer == null)
        {
            _soundIndicator.gameObject.SetActive(visible);
        }
    }

    private void SetSoundIndicatorColor(Color color)
    {
        if (_soundIndicatorSpriteRenderer != null)
            _soundIndicatorSpriteRenderer.color = color;

        if (_soundIndicatorRenderer == null ||
            _soundIndicatorRenderer == _soundIndicatorSpriteRenderer)
        {
            return;
        }

        _soundPropertyBlock ??= new MaterialPropertyBlock();
        _soundIndicatorRenderer.GetPropertyBlock(_soundPropertyBlock);
        _soundPropertyBlock.SetColor("_BaseColor", color);
        _soundPropertyBlock.SetColor("_Color", color);
        _soundIndicatorRenderer.SetPropertyBlock(_soundPropertyBlock);
    }

    private bool IsSourceVisible(GameObject source)
    {
        if (source == null)
            return false;

        if (_visibilitySystem == null)
            _visibilitySystem = VisionVisibilitySystem.Instance;

        if (_visibilitySystem == null)
            return false;

        VisionVisibilityTarget target =
            source.GetComponentInParent<VisionVisibilityTarget>() ??
            source.GetComponentInChildren<VisionVisibilityTarget>();

        return target != null && _visibilitySystem.IsVisible(target);
    }

    private bool IsOwnSoundSource(Transform source)
    {
        if (source == null || _ownerRoot == null)
            return false;

        return source == _ownerRoot || source.IsChildOf(_ownerRoot);
    }

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

    private void LogDebug()
    {
        if (!_debugLogs || Time.unscaledTime < _nextDebugLogTime)
            return;

        _nextDebugLogTime = Time.unscaledTime + _debugLogInterval;

        Debug.Log(
            $"[PlayerWorldRangeRing] radius={_currentWorldRadius:0.##}, origin={_rangeOrigin.name}, line={_ringLine.name}",
            this);
    }
}
