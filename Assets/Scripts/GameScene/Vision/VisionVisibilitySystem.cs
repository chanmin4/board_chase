using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
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

    [Tooltip("How often visibility is recalculated. Lower values react faster but cost more.")]
    [SerializeField, Min(0.02f)] private float _visibilityUpdateInterval = 0.05f;

    [Header("Visibility")]
    [Tooltip("If true, enemies that are still in spawn telegraph/setup do not get forced visible/hidden by this system.")]
    [SerializeField] private bool _ignoreEnemiesUntilSpawnReady = true;

    [Header("Paint Reveal")]
    [SerializeField] private bool _revealTargetsOnVaccinePaint = true;
    [SerializeField] private bool _revealTargetsOnCoatedVaccinePaint = true;
    [SerializeField] private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;

    [Header("Debug")]
    [SerializeField] private bool _debugLogs = false;

    private readonly List<VisionVisibilityTarget> _targets = new();
    private MaskRenderManager _maskRenderManager;
    private float _nextScanTime;
    private float _nextVisibilityUpdateTime;

    public EntityVisionController Viewer => _viewer;

    private void Awake()
    {
        Instance = this;
    }

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

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (_viewer == null)
            ResolveViewer();

        if (_autoRegisterEnemies && Time.time >= _nextScanTime)
        {
            _nextScanTime = Time.time + _scanInterval;
            ScanTargets();
        }

        if (Time.time >= _nextVisibilityUpdateTime)
        {
            _nextVisibilityUpdateTime = Time.time + _visibilityUpdateInterval;
            UpdateVisibilityImmediate();
        }
    }

    public void Register(VisionVisibilityTarget target)
    {
        if (target == null || _targets.Contains(target))
            return;

        _targets.Add(target);
    }

    public void Unregister(VisionVisibilityTarget target)
    {
        if (target == null)
            return;

        _targets.Remove(target);
    }

    public bool IsVisible(VisionVisibilityTarget target)
    {
        return target == null || target.IsVisible;
    }

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
                target.SetVisionVisible(true);
                continue;
            }

            bool visible = _viewer.CanSeeWorldPoint(target.VisibilityPoint, 0f) ||
                           IsRevealedByVaccinePaint(target);

            target.SetVisionVisible(visible);
        }
    }

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

    private void OnMaskRenderManagerReady(MaskRenderManager manager)
    {
        if (manager != null)
            _maskRenderManager = manager;
    }
}
