using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "SearchInfectionTargetAction",
    menuName = "State Machines/Enemy Actions/Search Infection Target")]
public class SearchInfectionTargetActionSO : StateActionSO
{
    [SerializeField] private float _searchRadius = 8f;
    [SerializeField] private int _candidateCount = 6;
    [SerializeField] private float _minimumScore = 0.1f;
    [SerializeField] private bool _searchOnEnterOnly = true;

    public float SearchRadius => _searchRadius;
    public int CandidateCount => _candidateCount;
    public float MinimumScore => _minimumScore;
    public bool SearchOnEnterOnly => _searchOnEnterOnly;

    protected override StateAction CreateAction() => new SearchInfectionTargetAction();
}

public class SearchInfectionTargetAction : StateAction
{
    private Enemy _enemy;
    private SearchInfectionTargetActionSO _config;

    public override void Awake(StateMachine stateMachine)
    {
        _enemy = stateMachine.GetComponent<Enemy>();
        _config = (SearchInfectionTargetActionSO)OriginSO;
    }

    public override void OnStateEnter()
    {
        Search();
    }

    public override void OnUpdate()
    {
        if (!_config.SearchOnEnterOnly)
            Search();
    }

    private void Search()
    {
        if (_enemy == null)
            return;

        _enemy.SetSearchSucceeded(false);
        _enemy.SetInfectionTargetvalid(false);

        if (TryFindTarget(out Vector3 bestPosition, out float bestScore) &&
            bestScore >= _config.MinimumScore)
        {
            _enemy.SetInfectionTarget(bestPosition);
        }
    }

    private bool TryFindTarget(out Vector3 bestPosition, out float bestScore)
    {
        bestPosition = default;
        bestScore = float.NegativeInfinity;

        Vector3 origin = _enemy.transform.position;

        for (int i = 0; i < _config.CandidateCount; i++)
        {
            Vector2 random2D = Random.insideUnitCircle * _config.SearchRadius;
            Vector3 candidate = new Vector3(origin.x + random2D.x, origin.y, origin.z + random2D.y);

            float score = ScoreCandidate(candidate);

            if (score > bestScore)
            {
                bestScore = score;
                bestPosition = candidate;
            }
        }

        return bestScore > float.NegativeInfinity;
    }

    private float ScoreCandidate(Vector3 candidate)
    {
        // TODO:
        // 여기서 sector 내부 mask / sample 포인트 / 중립-백신-바이러스 점수 계산으로 교체
        return 0f;
    }
}
