using UnityEngine;

[CreateAssetMenu(
    fileName = "NormalEnemySearchInfectionTargetConfig",
    menuName = "ScriptableObject/GameScene/Enemy/Definition_Config/Normal Enemy Search Infection Target Config")]
public class NormalEnemySearchInfectionTargetConfigSO : ScriptableObject
{
    [Header("Search")]
    [SerializeField] private float _searchRadius = 16f;
    [SerializeField] private int _candidateCount = 6;
    [SerializeField] private int _maxRetryCount = 3;
    [SerializeField] private bool _searchOnEnterOnly = false;
    [SerializeField] private float _searchIntervalSeconds = 2f;
    [SerializeField] private float _minimumScore = 0.3f;
    [SerializeField] private float _minimumTravelDistance = 1.25f;
    [SerializeField] private bool _preferNearestOnTie = true;
    [SerializeField] private float _distanceScoreWeight = 0.25f;

    [Header("Sampling")]
    [SerializeField] private float _sampleOffset = 0.75f;
    [SerializeField] private int _minimumValidSamples = 3;
    [SerializeField] private float _navMeshSampleMaxDistance = 1.25f;

    [Header("Contaminate Score")]
    [Range(0f, 1f)] [SerializeField] private float _vaccineScore = 1f;
    [Range(0f, 1f)] [SerializeField] private float _neutralScore = 0.6f;
    [Range(0f, 1f)] [SerializeField] private float _virusScore = 0f;

    [Header("Debug")]
    [SerializeField] private bool _debugLogs = true;
    [SerializeField] private bool _debugDraw = true;
    [SerializeField] private float _debugDrawDuration = 2f;

    public float SearchRadius => Mathf.Max(0f, _searchRadius);
    public int CandidateCount => Mathf.Max(1, _candidateCount);
    public int MaxRetryCount => Mathf.Max(0, _maxRetryCount);
    public bool SearchOnEnterOnly => _searchOnEnterOnly;
    public float SearchIntervalSeconds => Mathf.Max(0.1f, _searchIntervalSeconds);
    public float MinimumScore => _minimumScore;
    public float MinimumTravelDistance => Mathf.Max(0f, _minimumTravelDistance);
    public float SampleOffset => Mathf.Max(0f, _sampleOffset);
    public int MinimumValidSamples => Mathf.Max(1, _minimumValidSamples);
    public float NavMeshSampleMaxDistance => Mathf.Max(0.01f, _navMeshSampleMaxDistance);
    public float VaccineScore => _vaccineScore;
    public float NeutralScore => _neutralScore;
    public float VirusScore => _virusScore;
    public bool DebugLogs => _debugLogs;
    public bool DebugDraw => _debugDraw;
    public float DebugDrawDuration => Mathf.Max(0f, _debugDrawDuration);
    public bool PreferNearestOnTie => _preferNearestOnTie;
    public float DistanceScoreWeight => _distanceScoreWeight;
}
