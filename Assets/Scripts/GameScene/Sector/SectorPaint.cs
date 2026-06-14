using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SectorPaint : MonoBehaviour
{

    [SerializeField] private PaintSurfaceState _coatingState = PaintSurfaceState.Neutral;
    [Serializable]
    public struct StoredCirclePaint
    {
        public PaintChannel channel;
        public Vector3 worldPos;
        public float radiusWorld;
        public int priority;
        public float appliedTime;
        public object sender;
        public PoisonPuddleDamageConfigSO poisonPuddleDamageConfig;
    }

    [Header("Refs")]
    [SerializeField] private SectorRuntime runtime;
    


    [Header("Options")]
    [SerializeField] private bool allowPaintWhenClosed = false;
    [SerializeField] private float boundsPadding = 0.1f;

    [Header("Gameplay Grid")]
    [SerializeField, Min(0.05f)] private float _paintGridCellSize = 0.5f;
    [SerializeField, Range(0.01f, 1f)] private float _paintQueryThreshold = 0.2f;

    [HideInInspector] public Bounds worldBounds;
    [HideInInspector] public bool initialized;

    [HideInInspector] public RenderTexture vaccineMask;
    [HideInInspector] public RenderTexture virusMask;
    [HideInInspector] public RenderTexture poisonPuddleMask;

    [HideInInspector] public Material vaccineMaterialInstance;
    [HideInInspector] public Material virusMaterialInstance;
    [HideInInspector] public Material poisonPuddleMaterialInstance;
    [HideInInspector] public MeshRenderer vaccineRenderer;
    [HideInInspector] public MeshRenderer virusRenderer;
    [HideInInspector] public MeshRenderer poisonPuddleRenderer;
    [HideInInspector] public MeshFilter vaccineFilter;
    [HideInInspector] public MeshFilter virusFilter;
    [HideInInspector] public MeshFilter poisonPuddleFilter;
    [HideInInspector] public int textureWidth;
    [HideInInspector] public int textureHeight;
    private MaskRenderManager _maskRenderManager;
    private readonly List<StoredCirclePaint> _playerPaints = new();
    private readonly List<StoredCirclePaint> _enemyPaints = new();
    public PaintSurfaceState CoatingState => _coatingState;
    public bool IsCoated => PaintTypeUtility.IsCoated(_coatingState);    private Bounds _paintGridBounds;
    private float[] _vaccineCoverage;
    private float[] _virusCoverage;
    private float[] _poisonPuddleCoverage;
    private float _vaccineCoverageSum;
    private float _virusCoverageSum;
    private float _poisonPuddleCoverageSum;
    private float _paintGridCellSizeRuntime;
    private int _paintGridWidth;
    private int _paintGridHeight;

    public event Action<SectorPaint, StoredCirclePaint> OnCircleApplied;

    public int PlayerPaintCount => _playerPaints.Count;
    public int EnemyPaintCount => _enemyPaints.Count;
    public SectorRuntime Runtime => runtime;
    public bool AllowPaintWhenClosed => allowPaintWhenClosed;
    public float BoundsPadding => boundsPadding;
    public int PaintGridWidth => _paintGridWidth;
    public int PaintGridHeight => _paintGridHeight;
    public float PaintGridCellSize => Mathf.Max(0.05f, _paintGridCellSize);

    private void Reset()
    {
        if (!runtime)
            runtime = GetComponent<SectorRuntime>();

        if (!_maskRenderManager)
            _maskRenderManager = FindAnyObjectByType<MaskRenderManager>();
    }

    private void Awake()
    {
        if (!runtime)
            runtime = GetComponent<SectorRuntime>();

        if (!_maskRenderManager)
            _maskRenderManager = FindAnyObjectByType<MaskRenderManager>();
    }

    private void OnEnable()
    {
        if (_maskRenderManager != null)
            _maskRenderManager.RegisterSector(this);
    }

    private void OnDisable()
    {
        if (_maskRenderManager != null)
            _maskRenderManager.UnregisterSector(this);
    }

    public Bounds GetWorldBounds()
    {
        if (runtime != null)
            return runtime.GetWorldBounds();

        return new Bounds(transform.position, Vector3.one);
    }

    public bool CanPaintNow()
    {
        if (IsCoated)
            return false;
        if (runtime != null && runtime.IsCleared)
            return false;

        if (allowPaintWhenClosed)
            return true;

        if (runtime == null)
            return true;

        return runtime.isOpened;
    }

    public bool CanQueryPaintNow()
    {
        if (allowPaintWhenClosed)
            return true;

        if (runtime == null)
            return true;

        return runtime.IsOpened;
    }

    public bool ContainsPoint(Vector3 worldPos)
    {
        Bounds bounds = GetWorldBounds();

        return worldPos.x >= bounds.min.x &&
            worldPos.x <= bounds.max.x &&
            worldPos.z >= bounds.min.z &&
            worldPos.z <= bounds.max.z;
    }

    public bool IntersectsCircle(Vector3 worldPos, float radiusWorld)
    {
        Bounds bounds = GetWorldBounds();

        float closestX = Mathf.Clamp(worldPos.x, bounds.min.x, bounds.max.x);
        float closestZ = Mathf.Clamp(worldPos.z, bounds.min.z, bounds.max.z);
        float dx = worldPos.x - closestX;
        float dz = worldPos.z - closestZ;

        return dx * dx + dz * dz <= radiusWorld * radiusWorld;
    }

    public bool CanAcceptCircle(MaskRenderManager.CirclePaintRequest request)
    {
        if (!CanPaintNow())
            return false;

        return IntersectsCircle(request.worldPos, request.radiusWorld + boundsPadding);
    }

    public void ApplyCircle(MaskRenderManager.CirclePaintRequest request)
    {
        if (_maskRenderManager == null)
            return;

        StoredCirclePaint stored = new StoredCirclePaint
        {
            channel = request.channel,
            worldPos = request.worldPos,
            radiusWorld = request.radiusWorld,
            priority = request.priority,
            appliedTime = Time.time,
            sender = request.sender,
            poisonPuddleDamageConfig = request.poisonPuddleDamageConfig
        };

        if (request.channel == PaintChannel.Vaccine)
            _playerPaints.Add(stored);
        else
            _enemyPaints.Add(stored);

        _maskRenderManager.StampCircle(this, request);
        OnCircleApplied?.Invoke(this, stored);
    }

    public IReadOnlyList<StoredCirclePaint> GetStoredPaints(PaintChannel channel)
    {
        if (channel == PaintChannel.Vaccine)
            return _playerPaints;

        return _enemyPaints;
    }

    public void ClearAllStoredPaint()
    {
        _playerPaints.Clear();
        _enemyPaints.Clear();
    }

    public void ClearStoredPaint(PaintChannel channel)
    {
        if (channel == PaintChannel.Vaccine)
            _playerPaints.Clear();
        else
            _enemyPaints.Clear();
    }

    public void ClearRuntimeData()
    {
        initialized = false;
        vaccineMask = null;
        virusMask = null;
        poisonPuddleMask = null;
        vaccineMaterialInstance = null;
        virusMaterialInstance = null;
        poisonPuddleMaterialInstance = null;
        vaccineRenderer = null;
        virusRenderer = null;
        poisonPuddleRenderer = null;
        vaccineFilter = null;
        virusFilter = null;
        poisonPuddleFilter = null;
        textureWidth = 0;
        textureHeight = 0;
        ClearAllPaintCoverage();
    }

    public bool ApplyGameplayCircle(
        PaintChannel channel,
        Vector3 worldPos,
        float radiusWorld,
        bool overwriteOther = true)
    {
        if (IsCoated)
            return false;
        bool changed = false;

        switch (channel)
        {
            case PaintChannel.Vaccine:
                changed |= PaintCoverageCircle(PaintChannel.Vaccine, worldPos, radiusWorld, true);

                if (overwriteOther)
                {
                    changed |= PaintCoverageCircle(PaintChannel.Virus, worldPos, radiusWorld, false);
                    changed |= PaintCoverageCircle(PaintChannel.PoisonPuddle, worldPos, radiusWorld, false);
                }

                break;

            case PaintChannel.Virus:
                changed |= PaintCoverageCircle(PaintChannel.Virus, worldPos, radiusWorld, true);

                if (overwriteOther)
                    changed |= PaintCoverageCircle(PaintChannel.Vaccine, worldPos, radiusWorld, false);

                break;

            case PaintChannel.PoisonPuddle:
                changed |= PaintCoverageCircle(PaintChannel.Virus, worldPos, radiusWorld, true);
                changed |= PaintCoverageCircle(PaintChannel.PoisonPuddle, worldPos, radiusWorld, true);

                if (overwriteOther)
                    changed |= PaintCoverageCircle(PaintChannel.Vaccine, worldPos, radiusWorld, false);

                break;
        }

        return changed;
    }

    public bool AddVirusTrailCircle(Vector3 worldPos, float radiusWorld)
    {
        if (!CanPaintNow())
            return false;

        if (!IntersectsCircle(worldPos, radiusWorld + boundsPadding))
            return false;

        bool changed = PaintCoverageCircle(PaintChannel.Virus, worldPos, radiusWorld, true);
        changed |= PaintCoverageCircle(PaintChannel.Vaccine, worldPos, radiusWorld, false);
        return changed;
    }

    public void FillGameplay(PaintChannel channel, bool clearOtherChannel)
    {
        EnsurePaintGrid();

        switch (channel)
        {
            case PaintChannel.Vaccine:
                FillCoverage(PaintChannel.Vaccine, 1f);

                if (clearOtherChannel)
                {
                    FillCoverage(PaintChannel.Virus, 0f);
                    FillCoverage(PaintChannel.PoisonPuddle, 0f);
                }

                break;

            case PaintChannel.Virus:
                FillCoverage(PaintChannel.Virus, 1f);

                if (clearOtherChannel)
                    FillCoverage(PaintChannel.Vaccine, 0f);

                break;

            case PaintChannel.PoisonPuddle:
                FillCoverage(PaintChannel.Virus, 1f);
                FillCoverage(PaintChannel.PoisonPuddle, 1f);

                if (clearOtherChannel)
                    FillCoverage(PaintChannel.Vaccine, 0f);

                break;
        }
    }
    public void SetCoating(PaintChannel channel)
    {
        _coatingState = PaintTypeUtility.ToCoatedState(channel);
    }
    public void ClearCoating()
    {
        _coatingState = PaintSurfaceState.Neutral;
    }
    public void ClearAllPaintCoverage()
    {
        ClearCoverage(PaintChannel.Vaccine);
        ClearCoverage(PaintChannel.Virus);
        ClearCoverage(PaintChannel.PoisonPuddle);
        ClearCoating();
    }

    public bool HasPaintAtWorld(PaintChannel channel, Vector3 worldPos)
    {
        return GetCoverageAtWorld(channel, worldPos) >= _paintQueryThreshold;
    }

    public float GetCoverageAtWorld(PaintChannel channel, Vector3 worldPos)
    {
        if (!HasCoverageGrid())
            return 0f;

        EnsurePaintGrid();

        if (!TryWorldToPaintCell(worldPos, out int x, out int y))
            return 0f;

        return GetCoverage(channel, y * _paintGridWidth + x);
    }

    public float GetCoverageRatio(PaintChannel channel)
    {
        if (!HasCoverageGrid())
            return 0f;

        EnsurePaintGrid();

        int count = Mathf.Max(1, _paintGridWidth * _paintGridHeight);
        return Mathf.Clamp01(GetCoverageSum(channel) / count);
    }

    public PaintSurfaceState GetPaintStateAtWorld(Vector3 worldPos)
    {
        if (IsCoated)
            return _coatingState;

        float vaccine = GetCoverageAtWorld(PaintChannel.Vaccine, worldPos);
        float virus = GetCoverageAtWorld(PaintChannel.Virus, worldPos);
        float poison = GetCoverageAtWorld(PaintChannel.PoisonPuddle, worldPos);

        if (poison >= _paintQueryThreshold)
            return PaintSurfaceState.PoisonPuddle;

        if (vaccine >= _paintQueryThreshold && vaccine >= virus)
            return PaintSurfaceState.Vaccine;

        if (virus >= _paintQueryThreshold)
            return PaintSurfaceState.Virus;

        return PaintSurfaceState.Neutral;
    }

    public bool TryWorldToMaskUV(Vector3 worldPos, out Vector2 uv)
    {
        Bounds bounds = GetWorldBounds();
        uv = new Vector2(
            Mathf.InverseLerp(bounds.min.x, bounds.max.x, worldPos.x),
            Mathf.InverseLerp(bounds.min.z, bounds.max.z, worldPos.z));

        return uv.x >= 0f && uv.x <= 1f && uv.y >= 0f && uv.y <= 1f;
    }

    public float EstimatePaintCoverage(Vector3 worldPos, float radiusWorld, int cellX, int cellY)
    {
        EnsurePaintGrid();
        return EstimateCircleCellCoverage(worldPos, radiusWorld, cellX, cellY);
    }

    public bool TryGetPaintCellRange(
        Vector3 worldPos,
        float radiusWorld,
        out int minX,
        out int maxX,
        out int minY,
        out int maxY)
    {
        EnsurePaintGrid();

        minX = maxX = minY = maxY = 0;

        radiusWorld = Mathf.Max(0.01f, radiusWorld);
        Bounds bounds = _paintGridBounds;

        float circleMinX = worldPos.x - radiusWorld;
        float circleMaxX = worldPos.x + radiusWorld;
        float circleMinZ = worldPos.z - radiusWorld;
        float circleMaxZ = worldPos.z + radiusWorld;

        if (circleMaxX < bounds.min.x ||
            circleMinX > bounds.max.x ||
            circleMaxZ < bounds.min.z ||
            circleMinZ > bounds.max.z)
        {
            return false;
        }

        float cellWidth = bounds.size.x / Mathf.Max(1, _paintGridWidth);
        float cellHeight = bounds.size.z / Mathf.Max(1, _paintGridHeight);

        minX = Mathf.Clamp(Mathf.FloorToInt((circleMinX - bounds.min.x) / Mathf.Max(0.0001f, cellWidth)), 0, _paintGridWidth - 1);
        maxX = Mathf.Clamp(Mathf.FloorToInt((circleMaxX - bounds.min.x) / Mathf.Max(0.0001f, cellWidth)), 0, _paintGridWidth - 1);
        minY = Mathf.Clamp(Mathf.FloorToInt((circleMinZ - bounds.min.z) / Mathf.Max(0.0001f, cellHeight)), 0, _paintGridHeight - 1);
        maxY = Mathf.Clamp(Mathf.FloorToInt((circleMaxZ - bounds.min.z) / Mathf.Max(0.0001f, cellHeight)), 0, _paintGridHeight - 1);
        return true;
    }

    public float GetCoverageAtCell(PaintChannel channel, int x, int y)
    {
        EnsurePaintGrid();

        if (x < 0 || x >= _paintGridWidth || y < 0 || y >= _paintGridHeight)
            return 0f;

        return GetCoverage(channel, y * _paintGridWidth + x);
    }

    private bool PaintCoverageCircle(
        PaintChannel channel,
        Vector3 worldPos,
        float radiusWorld,
        bool add)
    {
        EnsurePaintGrid();
        radiusWorld = Mathf.Max(0.01f, radiusWorld);

        if (!TryGetPaintCellRange(worldPos, radiusWorld, out int minX, out int maxX, out int minY, out int maxY))
            return false;

        bool changed = false;

        for (int y = minY; y <= maxY; y++)
        {
            int row = y * _paintGridWidth;

            for (int x = minX; x <= maxX; x++)
            {
                float coverage = EstimateCircleCellCoverage(worldPos, radiusWorld, x, y);

                if (coverage <= 0f)
                    continue;

                int index = row + x;
                float current = GetCoverage(channel, index);
                float next = add ? Mathf.Max(current, coverage) : Mathf.Max(0f, current - coverage);

                if (Mathf.Approximately(current, next))
                    continue;

                SetCoverage(channel, index, next);
                changed = true;
            }
        }

        return changed;
    }

    private void EnsurePaintGrid()
    {
        Bounds bounds = GetWorldBounds();
        float cellSize = Mathf.Max(0.05f, _paintGridCellSize);
        int width = Mathf.Max(1, Mathf.CeilToInt(bounds.size.x / cellSize));
        int height = Mathf.Max(1, Mathf.CeilToInt(bounds.size.z / cellSize));

        bool needsRebuild =
            _vaccineCoverage == null ||
            _virusCoverage == null ||
            _poisonPuddleCoverage == null ||
            _paintGridWidth != width ||
            _paintGridHeight != height ||
            !Mathf.Approximately(_paintGridCellSizeRuntime, cellSize) ||
            !ApproximatelySameBoundsXZ(_paintGridBounds, bounds);

        if (!needsRebuild)
            return;

        _paintGridBounds = bounds;
        _paintGridCellSizeRuntime = cellSize;
        _paintGridWidth = width;
        _paintGridHeight = height;
        int count = width * height;
        _vaccineCoverage = new float[count];
        _virusCoverage = new float[count];
        _poisonPuddleCoverage = new float[count];
        _vaccineCoverageSum = 0f;
        _virusCoverageSum = 0f;
        _poisonPuddleCoverageSum = 0f;
    }

    private bool HasCoverageGrid()
    {
        return _vaccineCoverage != null &&
            _virusCoverage != null &&
            _poisonPuddleCoverage != null &&
            _paintGridWidth > 0 &&
            _paintGridHeight > 0;
    }

    private void ClearCoverage(PaintChannel channel)
    {
        float[] coverage = GetCoverageArray(channel);

        if (coverage != null)
            Array.Clear(coverage, 0, coverage.Length);

        SetCoverageSum(channel, 0f);
    }

    private void FillCoverage(PaintChannel channel, float value)
    {
        EnsurePaintGrid();

        float[] coverage = GetCoverageArray(channel);

        if (coverage == null)
            return;

        value = Mathf.Clamp01(value);

        for (int i = 0; i < coverage.Length; i++)
            coverage[i] = value;

        SetCoverageSum(channel, value * coverage.Length);
    }

    private float GetCoverage(PaintChannel channel, int index)
    {
        float[] coverage = GetCoverageArray(channel);
        return coverage != null && index >= 0 && index < coverage.Length ? coverage[index] : 0f;
    }

    private void SetCoverage(PaintChannel channel, int index, float value)
    {
        float[] coverage = GetCoverageArray(channel);

        if (coverage == null || index < 0 || index >= coverage.Length)
            return;

        value = Mathf.Clamp01(value);
        float previous = coverage[index];
        coverage[index] = value;
        AddCoverageSum(channel, value - previous);
    }

    private float[] GetCoverageArray(PaintChannel channel)
    {
        switch (channel)
        {
            case PaintChannel.Vaccine:
                return _vaccineCoverage;

            case PaintChannel.Virus:
                return _virusCoverage;

            case PaintChannel.PoisonPuddle:
                return _poisonPuddleCoverage;

            default:
                return null;
        }
    }

    private float GetCoverageSum(PaintChannel channel)
    {
        switch (channel)
        {
            case PaintChannel.Vaccine:
                return _vaccineCoverageSum;

            case PaintChannel.Virus:
                return _virusCoverageSum;

            case PaintChannel.PoisonPuddle:
                return _poisonPuddleCoverageSum;

            default:
                return 0f;
        }
    }

    private void AddCoverageSum(PaintChannel channel, float delta)
    {
        SetCoverageSum(channel, GetCoverageSum(channel) + delta);
    }

    private void SetCoverageSum(PaintChannel channel, float value)
    {
        switch (channel)
        {
            case PaintChannel.Vaccine:
                _vaccineCoverageSum = value;
                break;

            case PaintChannel.Virus:
                _virusCoverageSum = value;
                break;

            case PaintChannel.PoisonPuddle:
                _poisonPuddleCoverageSum = value;
                break;
        }
    }

    private float EstimateCircleCellCoverage(Vector3 worldPos, float radiusWorld, int cellX, int cellY)
    {
        Vector3 center = PaintCellCenterToWorld(cellX, cellY);
        float halfX = _paintGridBounds.size.x / Mathf.Max(1, _paintGridWidth) * 0.5f;
        float halfZ = _paintGridBounds.size.z / Mathf.Max(1, _paintGridHeight) * 0.5f;

        int inside = 0;
        inside += IsInsideCircleXZ(center, worldPos, radiusWorld) ? 1 : 0;
        inside += IsInsideCircleXZ(center + new Vector3(-halfX, 0f, -halfZ), worldPos, radiusWorld) ? 1 : 0;
        inside += IsInsideCircleXZ(center + new Vector3(-halfX, 0f, halfZ), worldPos, radiusWorld) ? 1 : 0;
        inside += IsInsideCircleXZ(center + new Vector3(halfX, 0f, -halfZ), worldPos, radiusWorld) ? 1 : 0;
        inside += IsInsideCircleXZ(center + new Vector3(halfX, 0f, halfZ), worldPos, radiusWorld) ? 1 : 0;

        return inside / 5f;
    }

    private bool TryWorldToPaintCell(Vector3 worldPos, out int x, out int y)
    {
        Bounds bounds = _paintGridBounds;

        float x01 = Mathf.InverseLerp(bounds.min.x, bounds.max.x, worldPos.x);
        float y01 = Mathf.InverseLerp(bounds.min.z, bounds.max.z, worldPos.z);

        bool inside = x01 >= 0f && x01 <= 1f && y01 >= 0f && y01 <= 1f;

        x = Mathf.Clamp(Mathf.FloorToInt(x01 * _paintGridWidth), 0, _paintGridWidth - 1);
        y = Mathf.Clamp(Mathf.FloorToInt(y01 * _paintGridHeight), 0, _paintGridHeight - 1);
        return inside;
    }

    private Vector3 PaintCellCenterToWorld(int x, int y)
    {
        float x01 = (x + 0.5f) / Mathf.Max(1, _paintGridWidth);
        float y01 = (y + 0.5f) / Mathf.Max(1, _paintGridHeight);

        return new Vector3(
            Mathf.Lerp(_paintGridBounds.min.x, _paintGridBounds.max.x, x01),
            _paintGridBounds.center.y,
            Mathf.Lerp(_paintGridBounds.min.z, _paintGridBounds.max.z, y01));
    }

    private static bool IsInsideCircleXZ(Vector3 sample, Vector3 center, float radius)
    {
        float dx = sample.x - center.x;
        float dz = sample.z - center.z;
        return dx * dx + dz * dz <= radius * radius;
    }

    private static bool ApproximatelySameBoundsXZ(Bounds a, Bounds b)
    {
        return Mathf.Approximately(a.min.x, b.min.x) &&
            Mathf.Approximately(a.min.z, b.min.z) &&
            Mathf.Approximately(a.max.x, b.max.x) &&
            Mathf.Approximately(a.max.z, b.max.z);
    }
}
