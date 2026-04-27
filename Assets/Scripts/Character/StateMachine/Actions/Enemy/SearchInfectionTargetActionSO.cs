using UnityEngine;
using UnityEngine.AI;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "SearchInfectionTargetAction",
    menuName = "State Machines/Enemy Actions/Search Infection Target")]
public class SearchInfectionTargetActionSO : StateActionSO
{
    [Header("Search")]
    [SerializeField] private float _searchRadius = 8f;
    [SerializeField] private int _candidateCount = 6;
    [SerializeField] private int _maxRetryCount = 2;
    [SerializeField] private bool _searchOnEnterOnly = true;
    [SerializeField] private float _searchIntervalSeconds = 1.5f;
    [SerializeField] private float _minimumScore = 0.45f;
    [SerializeField] private float _minimumTravelDistance = 1.25f;

    [Header("Sampling")]
    [SerializeField] private float _sampleOffset = 0.75f;
    [SerializeField] private int _minimumValidSamples = 3;
    [SerializeField] private float _navMeshSampleMaxDistance = 1.25f;

    [Header("Contaminate Score")]
    [Range(0f, 1f)] [SerializeField] private float _vaccineScore = 1f;
    [Range(0f, 1f)] [SerializeField] private float _neutralScore = 0.6f;
    [Range(0f, 1f)] [SerializeField] private float _virusScore = 0f;

    [Header("Refs")]
    [SerializeField] private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;

    [Header("Debug")]
    [SerializeField] private bool _debugLogs = true;
    [SerializeField] private bool _debugDraw = true;
    [SerializeField] private float _debugDrawDuration = 2f;

    public float SearchRadius => _searchRadius;
    public int CandidateCount => _candidateCount;
    public int MaxRetryCount => _maxRetryCount;
    public bool SearchOnEnterOnly => _searchOnEnterOnly;
    public float SearchIntervalSeconds => _searchIntervalSeconds;
    public float MinimumScore => _minimumScore;
    public float MinimumTravelDistance => _minimumTravelDistance;
    public float SampleOffset => _sampleOffset;
    public int MinimumValidSamples => _minimumValidSamples;
    public float NavMeshSampleMaxDistance => _navMeshSampleMaxDistance;
    public float VaccineScore => _vaccineScore;
    public float NeutralScore => _neutralScore;
    public float VirusScore => _virusScore;
    public MaskRenderManagerEventChannelSO MaskRenderManagerReadyChannel => _maskRenderManagerReadyChannel;
    public bool DebugLogs => _debugLogs;
    public bool DebugDraw => _debugDraw;
    public float DebugDrawDuration => _debugDrawDuration;

    protected override StateAction CreateAction() => new SearchInfectionTargetAction();
}

public class SearchInfectionTargetAction : StateAction
{
    private Enemy _enemy;
    private SearchInfectionTargetActionSO _config;
    private MaskRenderManager _maskRenderManager;
    private float _nextSearchTime;

    public override void Awake(StateMachine stateMachine)
    {
        _enemy = stateMachine.GetComponent<Enemy>();
        _config = (SearchInfectionTargetActionSO)OriginSO;
        CacheMaskRenderManager();
    }

    public override void OnStateEnter()
    {
        Search();
        _nextSearchTime = Time.time + Mathf.Max(0.1f, _config.SearchIntervalSeconds);
    }

    public override void OnUpdate()
    {
        if (_config.SearchOnEnterOnly)
            return;

        if (Time.time < _nextSearchTime)
            return;

        Search();
        _nextSearchTime = Time.time + Mathf.Max(0.1f, _config.SearchIntervalSeconds);
    }

    private void Search()
    {
        if (_enemy == null || _enemy.CurrentSector == null)
            return;

        CacheMaskRenderManager();

        _enemy.SetSearchSucceeded(false);
        _enemy.SetInfectionTargetvalid(false);

        for (int attempt = 0; attempt <= _config.MaxRetryCount; attempt++)
        {
            if (!TryFindTarget(out Vector3 bestPosition, out float bestScore))
            {
                if (_config.DebugLogs)
                    Debug.Log($"[SearchInfectionTarget] No valid candidate. attempt={attempt + 1}");
                continue;
            }

            if (_config.DebugLogs)
                Debug.Log($"[SearchInfectionTarget] attempt={attempt + 1}, bestScore={bestScore:F2}, bestPos={bestPosition}");

            if (_config.DebugDraw)
                DrawMarker(bestPosition, bestScore >= _config.MinimumScore ? Color.green : Color.yellow);

            if (bestScore >= _config.MinimumScore)
            {
                _enemy.SetInfectionTarget(bestPosition);

                if (_config.DebugLogs)
                    Debug.Log($"[SearchInfectionTarget] Target selected at {bestPosition}, score={bestScore:F2}");

                return;
            }
        }

        if (_config.DebugLogs)
            Debug.Log("[SearchInfectionTarget] Search failed after all retries.");
    }

    private bool TryFindTarget(out Vector3 bestPosition, out float bestScore)
    {
        bestPosition = default;
        bestScore = float.NegativeInfinity;

        Bounds sectorBounds = _enemy.CurrentSector.GetWorldBounds();
        Vector3 origin = _enemy.transform.position;

        int candidateCount = Mathf.Max(1, _config.CandidateCount);

        for (int i = 0; i < candidateCount; i++)
        {
            if (!TryBuildCandidate(origin, sectorBounds, out Vector3 candidate))
                continue;

            float score = ScoreCandidate(candidate, sectorBounds);

            if (_config.DebugDraw)
                DrawCandidate(candidate, score);

            if (score > bestScore)
            {
                bestScore = score;
                bestPosition = candidate;
            }
        }

        return bestScore > float.NegativeInfinity;
    }

    private bool TryBuildCandidate(Vector3 origin, Bounds sectorBounds, out Vector3 candidate)
    {
        candidate = default;

        for (int i = 0; i < 8; i++)
        {
            Vector2 random2D = Random.insideUnitCircle * _config.SearchRadius;
            Vector3 raw = new Vector3(origin.x + random2D.x, origin.y, origin.z + random2D.y);

            if (!IsInsideBoundsXZ(sectorBounds, raw))
                continue;

            if (FlatDistance(origin, raw) < _config.MinimumTravelDistance)
                continue;

            if (!NavMesh.SamplePosition(raw, out NavMeshHit hit, _config.NavMeshSampleMaxDistance, NavMesh.AllAreas))
                continue;

            if (!IsInsideBoundsXZ(sectorBounds, hit.position))
                continue;

            candidate = hit.position;
            return true;
        }

        return false;
    }

    private float ScoreCandidate(Vector3 candidate, Bounds sectorBounds)
    {
        int validSamples = 0;
        float totalScore = 0f;

        validSamples += ScoreSample(candidate, sectorBounds, ref totalScore);
        validSamples += ScoreSample(candidate + Vector3.forward * _config.SampleOffset, sectorBounds, ref totalScore);
        validSamples += ScoreSample(candidate + Vector3.back * _config.SampleOffset, sectorBounds, ref totalScore);
        validSamples += ScoreSample(candidate + Vector3.right * _config.SampleOffset, sectorBounds, ref totalScore);
        validSamples += ScoreSample(candidate + Vector3.left * _config.SampleOffset, sectorBounds, ref totalScore);

        if (validSamples < _config.MinimumValidSamples)
            return float.NegativeInfinity;

        return totalScore / validSamples;
    }

    private int ScoreSample(Vector3 sample, Bounds sectorBounds, ref float totalScore)
    {
        if (!IsInsideBoundsXZ(sectorBounds, sample))
            return 0;

        totalScore += ResolveSampleScore(sample);
        return 1;
    }

    private float ResolveSampleScore(Vector3 sample)
    {
        CacheMaskRenderManager();

        if (_maskRenderManager == null)
            return _config.NeutralScore;

        if (!_maskRenderManager.TryGetStateAtWorld(sample, out MaskRenderManager.PaintState state, false))
            return _config.NeutralScore;

        switch (state)
        {
            case MaskRenderManager.PaintState.Vaccine:
                return _config.VaccineScore;

            case MaskRenderManager.PaintState.Virus:
                return _config.VirusScore;

            default:
                return _config.NeutralScore;
        }
    }

    private void CacheMaskRenderManager()
    {
        if (_maskRenderManager != null)
            return;

        if (_config.MaskRenderManagerReadyChannel != null && _config.MaskRenderManagerReadyChannel.Current != null)
        {
            _maskRenderManager = _config.MaskRenderManagerReadyChannel.Current;
            return;
        }

        _maskRenderManager = Object.FindAnyObjectByType<MaskRenderManager>();
    }

    private void DrawCandidate(Vector3 candidate, float score)
    {
        if (!_config.DebugDraw)
            return;

        Color color = float.IsNegativeInfinity(score)
            ? Color.red
            : Color.Lerp(Color.red, Color.green, Mathf.Clamp01(score));

        Debug.DrawLine(candidate, candidate + Vector3.up * 0.6f, color, _config.DebugDrawDuration);
    }

    private void DrawMarker(Vector3 point, Color color)
    {
        Debug.DrawLine(point + Vector3.left * 0.35f, point + Vector3.right * 0.35f, color, _config.DebugDrawDuration);
        Debug.DrawLine(point + Vector3.forward * 0.35f, point + Vector3.back * 0.35f, color, _config.DebugDrawDuration);
        Debug.DrawLine(point, point + Vector3.up * 0.75f, color, _config.DebugDrawDuration);
        Debug.DrawLine(_enemy.transform.position, point, color, _config.DebugDrawDuration);
    }

    private static bool IsInsideBoundsXZ(Bounds bounds, Vector3 point)
    {
        return point.x >= bounds.min.x &&
               point.x <= bounds.max.x &&
               point.z >= bounds.min.z &&
               point.z <= bounds.max.z;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
