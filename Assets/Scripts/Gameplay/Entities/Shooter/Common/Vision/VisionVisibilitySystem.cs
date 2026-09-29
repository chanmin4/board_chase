/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Vision
 * Primary responsibility: Handles VisionVisibilitySystem camera, vision, range, or aiming calculations and presentation.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
// Evaluate after entity movement and before the camera-side occlusion/fog passes.
[DefaultExecutionOrder(900)]
public class VisionVisibilitySystem : MonoBehaviour
{
    public static VisionVisibilitySystem Instance { get; private set; }

    [Header("ReadonlyViewer")]
    [Tooltip("Player or entity vision controller used as the viewer. If empty, the first EntityVisionController with PlayerStatsRuntime is used.")]
    [ReadOnly][SerializeField] private EntityVisionController _viewer;

    [SerializeField] private bool _autoFindPlayerViewer = true;

    [Header("Targets")]
    [Tooltip("If true, active Enemy objects get VisionVisibilityTarget automatically at runtime.")]
    [SerializeField] private bool _autoRegisterEnemies = true;

    [Tooltip("How often active Enemy objects are scanned for auto registration.")]
    [SerializeField, Min(0.05f)] private float _scanInterval = 0.25f;

    [Tooltip("Seconds between target LOS checks. Keep 0 to evaluate in LateUpdate every rendered frame so moving entities do not pop at structure corners. Positive values intentionally trade visual responsiveness for fewer raycasts.")]
    [SerializeField, Min(0f)] private float _visibilityUpdateInterval = 0f;

    [Header("Visibility")]
    [Tooltip("If true, enemies that are still in spawn telegraph/setup do not get forced visible/hidden by this system.")]
    [SerializeField] private bool _ignoreEnemiesUntilSpawnReady = true;

    [Header("Paint Reveal")]
    [SerializeField] private bool _revealTargetsOnVaccinePaint = true;
    [SerializeField] private bool _revealTargetsOnCoatedVaccinePaint = true;
    [Tooltip("If false, paint can highlight only targets already inside player vision. Keep false when enemies outside vision must stay hidden.")]
    [SerializeField] private bool _allowPaintRevealOutsideVision = false;
    [SerializeField] private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;

    [Header("Debug")]
    [SerializeField] private bool _debugLogs = false;

    private readonly List<VisionVisibilityTarget> _targets = new();
    private MaskRenderManager _maskRenderManager;
    private float _nextScanTime;
    private float _nextVisibilityUpdateTime;

    public EntityVisionController Viewer => _viewer;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        Instance = this;
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (Instance == null)
            Instance = this;

        ResolveViewer();
        ResolveMaskRenderManager();

        if (_maskRenderManagerReadyChannel != null)
        {
            _maskRenderManagerReadyChannel.OnEventRaised += OnMaskRenderManagerReady;

            if (_maskRenderManagerReadyChannel.Current != null)
                OnMaskRenderManagerReady(_maskRenderManagerReadyChannel.Current);
        }

        ScanTargets();
        UpdateVisibilityImmediate();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        for (int i = 0; i < _targets.Count; i++)
        {
            if (_targets[i] != null)
                _targets[i].SetVisionVisible(true);
        }

        if (_maskRenderManagerReadyChannel != null)
            _maskRenderManagerReadyChannel.OnEventRaised -= OnMaskRenderManagerReady;
    }

    // Purpose: Releases owned resources before this component is destroyed.
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
        if (_viewer == null)
            ResolveViewer();

        if (_autoRegisterEnemies && Time.time >= _nextScanTime)
        {
            _nextScanTime = Time.time + _scanInterval;
            ScanTargets();
        }

    }

    // Purpose: Processes frame work that must run after the regular Update pass.
    private void LateUpdate()
    {
        if (_viewer == null)
            ResolveViewer();

        float interval = Mathf.Max(0f, _visibilityUpdateInterval);

        if (interval <= 0f || Time.time >= _nextVisibilityUpdateTime)
        {
            _nextVisibilityUpdateTime = Time.time + interval;
            UpdateVisibilityImmediate();
        }
    }

    // Purpose: Registers register with its owning runtime service.
    public void Register(VisionVisibilityTarget target)
    {
        if (target == null || _targets.Contains(target))
            return;

        _targets.Add(target);
    }

    // Purpose: Unregisters unregister from its owning runtime service.
    public void Unregister(VisionVisibilityTarget target)
    {
        if (target == null)
            return;

        _targets.Remove(target);
    }

    // Purpose: Checks whether visible is currently true.
    public bool IsVisible(VisionVisibilityTarget target)
    {
        return target == null || target.IsVisible;
    }

    // Purpose: Updates the visibility immediate from the current runtime state.
    private void UpdateVisibilityImmediate()
    {
        if (_viewer == null)
            return;

        for (int i = _targets.Count - 1; i >= 0; i--)
        {
            VisionVisibilityTarget target = _targets[i];

            if (target == null)
            {
                _targets.RemoveAt(i);
                continue;
            }

            if (_ignoreEnemiesUntilSpawnReady &&
                target.Enemy != null &&
                !target.Enemy.IsSpawnReady)
            {
                bool spawnVisible = _viewer.CanRevealVisualPoint(target.VisibilityPoint, 0f);
                target.SetVisionVisible(spawnVisible);
                continue;
            }

            bool inVision = _viewer.CanRevealVisualPoint(target.VisibilityPoint, 0f);
            bool visible = inVision ||
                           (_allowPaintRevealOutsideVision && IsRevealedByVaccinePaint(target));

            target.SetVisionVisible(visible);
        }
    }

    // Purpose: Performs the scan targets workflow for this component.
    private void ScanTargets()
    {
        if (!_autoRegisterEnemies)
            return;

        Enemy[] enemies = FindObjectsByType<Enemy>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < enemies.Length; i++)
        {
            Enemy enemy = enemies[i];

            if (enemy == null)
                continue;

            VisionVisibilityTarget target =
                enemy.GetComponent<VisionVisibilityTarget>();

            if (target == null)
                target = enemy.gameObject.AddComponent<VisionVisibilityTarget>();

            EnemyScreenSpaceHPUIAnchor[] anchors =
                enemy.GetComponentsInChildren<EnemyScreenSpaceHPUIAnchor>(true);

            for (int j = 0; j < anchors.Length; j++)
            {
                if (anchors[j] != null)
                    anchors[j].SetVisionVisibilityTarget(target);
            }

            Register(target);
        }

        if (_debugLogs)
            Debug.Log($"[VisionVisibilitySystem] targetCount={_targets.Count}", this);
    }

    // Purpose: Resolves the viewer needed by this component.
    private void ResolveViewer()
    {
        if (_viewer != null || !_autoFindPlayerViewer)
            return;

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

        if (candidates.Length > 0)
            _viewer = candidates[0];
    }

    // Purpose: Checks whether revealed by vaccine paint is currently true.
    private bool IsRevealedByVaccinePaint(VisionVisibilityTarget target)
    {
        if (!_revealTargetsOnVaccinePaint || target == null)
            return false;

        ResolveMaskRenderManager();

        if (_maskRenderManager == null)
            return false;

        if (!_maskRenderManager.TryGetStateAtWorld(
                target.VisibilityPoint,
                out PaintSurfaceState state,
                true))
        {
            return false;
        }

        if (state == PaintSurfaceState.Vaccine)
            return true;

        return _revealTargetsOnCoatedVaccinePaint &&
               state == PaintSurfaceState.CoatedVaccine;
    }

    // Purpose: Resolves the mask render manager needed by this component.
    private void ResolveMaskRenderManager()
    {
        if (_maskRenderManager != null)
            return;

        if (_maskRenderManagerReadyChannel != null && _maskRenderManagerReadyChannel.Current != null)
        {
            _maskRenderManager = _maskRenderManagerReadyChannel.Current;
            return;
        }

        _maskRenderManager = FindAnyObjectByType<MaskRenderManager>();
    }

    // Purpose: Responds to the mask render manager ready callback.
    private void OnMaskRenderManagerReady(MaskRenderManager manager)
    {
        if (manager != null)
            _maskRenderManager = manager;
    }
}
