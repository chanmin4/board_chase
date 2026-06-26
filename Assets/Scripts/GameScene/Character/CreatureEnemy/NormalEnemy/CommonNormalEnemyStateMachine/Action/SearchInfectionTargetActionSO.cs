using UnityEngine;
using UnityEngine.AI;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "SearchInfectionTargetAction",
    menuName = "State Machines/Enemy Actions/Search Infection Target")]
public class SearchInfectionTargetActionSO : StateActionSO
{
    [Header("Definition Config")]
    [Tooltip("Shared config for normal enemy wander/search/infection behavior.")]
    [SerializeField] private NormalEnemyBehaviorConfigSO _behaviorConfig;

    [Header("Refs")]
    [SerializeField] private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;

    public bool HasBehaviorConfig => _behaviorConfig != null;
    public float SearchRadius => _behaviorConfig.SearchRadius;
    public int CandidateCount => _behaviorConfig.SearchCandidateCount;
    public int MaxRetryCount => _behaviorConfig.SearchMaxRetryCount;
    public bool SearchOnEnterOnly => _behaviorConfig.SearchOnEnterOnly;
    public float SearchIntervalSeconds => _behaviorConfig.SearchIntervalSeconds;
    public float MinimumScore => _behaviorConfig.SearchMinimumScore;
    public float MinimumTravelDistance => _behaviorConfig.SearchMinimumTravelDistance;
    public float SampleOffset => _behaviorConfig.SearchSampleOffset;
    public int MinimumValidSamples => _behaviorConfig.SearchMinimumValidSamples;
    public float NavMeshSampleMaxDistance => _behaviorConfig.SearchNavMeshSampleMaxDistance;
    public float VaccineScore => _behaviorConfig.SearchVaccineScore;
    public float NeutralScore => _behaviorConfig.SearchNeutralScore;
    public float VirusScore => _behaviorConfig.SearchVirusScore;
    public MaskRenderManagerEventChannelSO MaskRenderManagerReadyChannel => _maskRenderManagerReadyChannel;
    public bool DebugLogs => _behaviorConfig.SearchDebugLogs;
    public bool DebugDraw => _behaviorConfig.SearchDebugDraw;
    public float DebugDrawDuration => _behaviorConfig.SearchDebugDrawDuration;
    public bool PreferNearestOnTie => _behaviorConfig.SearchPreferNearestOnTie;
    public float DistanceScoreWeight => _behaviorConfig.SearchDistanceScoreWeight;

    protected override StateAction CreateAction() => new SearchInfectionTargetAction();
}

public class SearchInfectionTargetAction : StateAction
{
    private Enemy _enemy;
    private SearchInfectionTargetActionSO _config;
    private MaskRenderManager _maskRenderManager;
    private float _nextSearchTime;
    private bool _hasConfig;

    public override void Awake(StateMachine stateMachine)
    {
        _enemy = stateMachine.GetComponent<Enemy>();
        _config = (SearchInfectionTargetActionSO)OriginSO;
        CacheMaskRenderManager();
    }

    public override void OnStateEnter()
    {
        _hasConfig = _config.HasBehaviorConfig;

        if (!_hasConfig)
        {
            Debug.LogError("[SearchInfectionTargetAction] NormalEnemyBehaviorConfig is missing.", _enemy);
            return;
        }

        Search();
        _nextSearchTime = Time.time + Mathf.Max(0.1f, _config.SearchIntervalSeconds);
    }

    public override void OnUpdate()
    {
        if (!_hasConfig)
            return;

        //Debug.Log("SearchInfectionTargetAction OnUpdate");
        if (_config.SearchOnEnterOnly)
            return;

        if (Time.time < _nextSearchTime)
            return;
        //Debug.Log("SearchInfectionTargetAction OnUpdate Search");
        Search();
        _nextSearchTime = Time.time + Mathf.Max(0.1f, _config.SearchIntervalSeconds);
    }

    private void Search()
    {
        if (_enemy == null)
        {
            if (_config.DebugLogs)
                Debug.Log("[SearchInfectionTarget] Search aborted: enemy is null.");
            return;
        }

        if (_enemy.CurrentSector == null)
        {
            if (_config.DebugLogs)
                Debug.Log($"[SearchInfectionTarget] Search aborted: CurrentSector is null. enemy={_enemy.name}");
            return;
        }

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
            if (_config.DebugLogs)
                Debug.Log($"[SearchInfectionTarget] bestScore={bestScore:F2}, minimumScore={_config.MinimumScore:F2}");
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

        float bestDistance = float.PositiveInfinity;
        int candidateCount = Mathf.Max(1, _config.CandidateCount);

        for (int i = 0; i < candidateCount; i++)
        {
            if (!TryBuildCandidate(origin, sectorBounds, out Vector3 candidate))
                continue;

            float paintScore = ScoreCandidate(candidate, sectorBounds);
            float distance = FlatDistance(origin, candidate);

            if (_config.DebugDraw)
                DrawCandidate(candidate, paintScore);

            if (float.IsNegativeInfinity(paintScore))
                continue;

            float distance01 = _config.SearchRadius > 0f
                ? Mathf.Clamp01(distance / _config.SearchRadius)
                : 1f;

            float finalScore = paintScore - distance01 * _config.DistanceScoreWeight;

            bool isBetter = finalScore > bestScore;

            if (!isBetter && _config.PreferNearestOnTie)
                isBetter = Mathf.Approximately(finalScore, bestScore) && distance < bestDistance;

            if (isBetter)
            {
                bestScore = finalScore;
                bestDistance = distance;
                bestPosition = candidate;
            }
        }

        return bestScore > float.NegativeInfinity;
    }

    private bool TryBuildCandidate(Vector3 origin, Bounds sectorBounds, out Vector3 candidate)
    {
        candidate = default;

        const float edgePadding = 0.75f;

        float minX = sectorBounds.min.x + edgePadding;
        float maxX = sectorBounds.max.x - edgePadding;
        float minZ = sectorBounds.min.z + edgePadding;
        float maxZ = sectorBounds.max.z - edgePadding;

        if (minX >= maxX || minZ >= maxZ)
            return false;

        float searchRadius = Mathf.Max(_config.MinimumTravelDistance, _config.SearchRadius);

        for (int i = 0; i < 12; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * searchRadius;

            Vector3 raw = new Vector3(
                origin.x + randomCircle.x,
                origin.y,
                origin.z + randomCircle.y);

            raw.x = Mathf.Clamp(raw.x, minX, maxX);
            raw.z = Mathf.Clamp(raw.z, minZ, maxZ);

            float travelDistance = FlatDistance(origin, raw);

            if (travelDistance < _config.MinimumTravelDistance)
                continue;

            if (travelDistance > _config.SearchRadius)
                continue;

            if (!NavMesh.SamplePosition(raw, out NavMeshHit hit, _config.NavMeshSampleMaxDistance, NavMesh.AllAreas))
                continue;

            if (!IsInsideBoundsXZ(sectorBounds, hit.position))
                continue;

            if (FlatDistance(origin, hit.position) > _config.SearchRadius)
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

        if (!_maskRenderManager.TryGetStateAtWorld(sample, out PaintSurfaceState state, false))
            return _config.NeutralScore;

        switch (state)
        {
            case PaintSurfaceState.Vaccine:
            case PaintSurfaceState.CoatedVaccine:
                return _config.VaccineScore;

            case PaintSurfaceState.Virus:
            case PaintSurfaceState.PoisonPuddle:
            case PaintSurfaceState.CoatedVirus:
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
    //for debug
    private void DrawCandidate(Vector3 candidate, float score)
    {
        if (!_config.DebugDraw)
            return;

        Color color = float.IsNegativeInfinity(score)
            ? Color.red
            : Color.Lerp(Color.red, Color.green, Mathf.Clamp01(score));

        Debug.DrawLine(candidate, candidate + Vector3.up * 0.6f, color, _config.DebugDrawDuration);
    }
    //for debug
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
