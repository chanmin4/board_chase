using UnityEngine;

[CreateAssetMenu(
    fileName = "NormalEnemyWanderConfig",
    menuName = "ScriptableObject/GameScene/Enemy/Definition_Config/Normal Enemy Wander Config")]
public class NormalEnemyWanderConfigSO : ScriptableObject
{
    [SerializeField] private float _moveSpeed = 2.2f;
    [SerializeField] private float _minTravelDistance = 1.25f;
    [SerializeField] private float _edgePadding = 0.75f;
    [SerializeField] private float _navMeshSampleMaxDistance = 1.25f;
    [SerializeField] private int _candidateCount = 8;
    [SerializeField] private int _maxRepickCount = 2;
    [SerializeField] private bool _debugLogs = false;
    [SerializeField] private bool _debugDraw = false;
    [SerializeField] private float _debugDrawDuration = 1.5f;

    public float MoveSpeed => DifficultyRuntime.ApplyEnemyNormalMoveSpeed(_moveSpeed);
    public float MinTravelDistance => Mathf.Max(0f, _minTravelDistance);
    public float EdgePadding => Mathf.Max(0f, _edgePadding);
    public float NavMeshSampleMaxDistance => Mathf.Max(0.01f, _navMeshSampleMaxDistance);
    public int CandidateCount => Mathf.Max(1, _candidateCount);
    public int MaxRepickCount => Mathf.Max(0, _maxRepickCount);
    public bool DebugLogs => _debugLogs;
    public bool DebugDraw => _debugDraw;
    public float DebugDrawDuration => Mathf.Max(0f, _debugDrawDuration);
}
