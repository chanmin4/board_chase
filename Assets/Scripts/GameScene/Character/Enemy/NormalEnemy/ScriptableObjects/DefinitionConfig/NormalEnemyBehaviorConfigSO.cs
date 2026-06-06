using UnityEngine;

[CreateAssetMenu(
    fileName = "NormalEnemyBehaviorConfig",
    menuName = "ScriptableObject/GameScene/Enemy/Definition_Config/Normal Enemy Behavior Config")]
public class NormalEnemyBehaviorConfigSO : ScriptableObject
{
    [Header("Wander")]
    [SerializeField] private float _wanderMoveSpeed = 2.2f;
    [SerializeField] private float _wanderMinTravelDistance = 1.25f;
    [SerializeField] private float _wanderEdgePadding = 0.75f;
    [SerializeField] private float _wanderNavMeshSampleMaxDistance = 1.25f;
    [SerializeField] private int _wanderCandidateCount = 8;
    [SerializeField] private int _wanderMaxRepickCount = 2;
    [SerializeField] private bool _wanderDebugLogs;
    [SerializeField] private bool _wanderDebugDraw;
    [SerializeField] private float _wanderDebugDrawDuration = 1.5f;

    [Header("Search Infection Target")]
    [SerializeField] private float _searchRadius = 16f;
    [SerializeField] private int _searchCandidateCount = 6;
    [SerializeField] private int _searchMaxRetryCount = 3;
    [SerializeField] private bool _searchOnEnterOnly;
    [SerializeField] private float _searchIntervalSeconds = 2f;
    [SerializeField] private float _searchMinimumScore = 0.3f;
    [SerializeField] private float _searchMinimumTravelDistance = 1.25f;
    [SerializeField] private bool _searchPreferNearestOnTie = true;
    [SerializeField] private float _searchDistanceScoreWeight = 0.25f;

    [Header("Search Sampling")]
    [SerializeField] private float _searchSampleOffset = 0.75f;
    [SerializeField] private int _searchMinimumValidSamples = 3;
    [SerializeField] private float _searchNavMeshSampleMaxDistance = 1.25f;

    [Header("Search Paint Score")]
    [Range(0f, 1f)]
    [SerializeField] private float _searchVaccineScore = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float _searchNeutralScore = 0.6f;

    [Range(0f, 1f)]
    [SerializeField] private float _searchVirusScore;

    [Header("Search Debug")]
    [SerializeField] private bool _searchDebugLogs = true;
    [SerializeField] private bool _searchDebugDraw = true;
    [SerializeField] private float _searchDebugDrawDuration = 2f;

    [Header("Move To Infection Target")]
    [SerializeField] private float _infectionMoveSpeed = 3.5f;
    [SerializeField] private float _infectionMoveStoppingDistance = 0.8f;

    [Header("Cast Infection")]
    [SerializeField] private float _castInfectionRadius = 1.5f;
    [SerializeField] private bool _castApplyOnStateExit = true;
    [SerializeField] private int _castPaintPriority;

    [Header("Cast Infection Debug")]
    [SerializeField] private bool _castDebugLogs = true;
    [SerializeField] private bool _castDebugDraw = true;
    [SerializeField] private float _castDebugDrawDuration = 2f;

    public float WanderMoveSpeed => DifficultyRuntime.ApplyEnemyNormalMoveSpeed(_wanderMoveSpeed);
    public float WanderMinTravelDistance => Mathf.Max(0f, _wanderMinTravelDistance);
    public float WanderEdgePadding => Mathf.Max(0f, _wanderEdgePadding);
    public float WanderNavMeshSampleMaxDistance => Mathf.Max(0.01f, _wanderNavMeshSampleMaxDistance);
    public int WanderCandidateCount => Mathf.Max(1, _wanderCandidateCount);
    public int WanderMaxRepickCount => Mathf.Max(0, _wanderMaxRepickCount);
    public bool WanderDebugLogs => _wanderDebugLogs;
    public bool WanderDebugDraw => _wanderDebugDraw;
    public float WanderDebugDrawDuration => Mathf.Max(0f, _wanderDebugDrawDuration);

    public float SearchRadius => Mathf.Max(0f, _searchRadius);
    public int SearchCandidateCount => Mathf.Max(1, _searchCandidateCount);
    public int SearchMaxRetryCount => Mathf.Max(0, _searchMaxRetryCount);
    public bool SearchOnEnterOnly => _searchOnEnterOnly;
    public float SearchIntervalSeconds => Mathf.Max(0.1f, _searchIntervalSeconds);
    public float SearchMinimumScore => _searchMinimumScore;
    public float SearchMinimumTravelDistance => Mathf.Max(0f, _searchMinimumTravelDistance);
    public float SearchSampleOffset => Mathf.Max(0f, _searchSampleOffset);
    public int SearchMinimumValidSamples => Mathf.Max(1, _searchMinimumValidSamples);
    public float SearchNavMeshSampleMaxDistance => Mathf.Max(0.01f, _searchNavMeshSampleMaxDistance);
    public float SearchVaccineScore => _searchVaccineScore;
    public float SearchNeutralScore => _searchNeutralScore;
    public float SearchVirusScore => _searchVirusScore;
    public bool SearchDebugLogs => _searchDebugLogs;
    public bool SearchDebugDraw => _searchDebugDraw;
    public float SearchDebugDrawDuration => Mathf.Max(0f, _searchDebugDrawDuration);
    public bool SearchPreferNearestOnTie => _searchPreferNearestOnTie;
    public float SearchDistanceScoreWeight => _searchDistanceScoreWeight;

    public float InfectionMoveSpeed => DifficultyRuntime.ApplyEnemyNormalMoveSpeed(_infectionMoveSpeed);
    public float InfectionMoveStoppingDistance => Mathf.Max(0f, _infectionMoveStoppingDistance);

    public float CastInfectionRadius => DifficultyRuntime.ApplyEnemyVirusPaintRadius(_castInfectionRadius);
    public bool CastApplyOnStateExit => _castApplyOnStateExit;
    public int CastPaintPriority => _castPaintPriority;
    public bool CastDebugLogs => _castDebugLogs;
    public bool CastDebugDraw => _castDebugDraw;
    public float CastDebugDrawDuration => Mathf.Max(0f, _castDebugDrawDuration);
}
