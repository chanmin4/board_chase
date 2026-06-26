using UnityEngine;

public abstract class NamedEnemyStatConfigSO : CreatureEnemyStatConfigSO
{
    public enum RepositionSpeedSource
    {
        FixedValue,
        MovementStatsNormal,
        AgentCurrentValue
    }

    [Header("Reposition Distance")]
    [SerializeField, Min(0f)] private float _tooCloseDistance = 3f;
    [SerializeField, Min(0f)] private float _preferredDistance = 6f;
    [SerializeField, Min(0f)] private float _tooFarDistance = 10f;

    [Header("Reposition Movement")]
    [SerializeField] private RepositionSpeedSource _repositionSpeedSource = RepositionSpeedSource.MovementStatsNormal;
    [SerializeField, Min(0f)] private float _repositionFixedMoveSpeed = 2.2f;
    [SerializeField, Min(0f)] private float _strafeDistance = 2.5f;
    [SerializeField, Min(0.01f)] private float _destinationRefreshInterval = 0.35f;
    [SerializeField, Min(0.01f)] private float _navMeshSampleDistance = 2f;

    [Header("Reposition Facing")]
    [SerializeField] private bool _faceTarget = true;
    [SerializeField, Min(0f)] private float _turnSpeedDegPerSecond = 720f;

    [Header("Reposition Debug")]
    [SerializeField] private bool _debugDrawDistances;
    [SerializeField] private float _debugDrawHeight = 0.1f;

    public RepositionSpeedSource MoveSpeedSource => _repositionSpeedSource;
    public float RepositionFixedMoveSpeed => DifficultyRuntime.ApplyEnemyNormalMoveSpeed(_repositionFixedMoveSpeed);
    public float TooCloseDistance => Mathf.Max(0f, _tooCloseDistance);
    public float PreferredDistance => Mathf.Max(0f, _preferredDistance);
    public float TooFarDistance => Mathf.Max(0f, _tooFarDistance);
    public float StrafeDistance => Mathf.Max(0f, _strafeDistance);
    public float DestinationRefreshInterval => Mathf.Max(0.01f, _destinationRefreshInterval);
    public float NavMeshSampleDistance => Mathf.Max(0.01f, _navMeshSampleDistance);
    public bool FaceTarget => _faceTarget;
    public float TurnSpeedDegPerSecond => Mathf.Max(0f, _turnSpeedDegPerSecond);
    public bool DebugDrawDistances => _debugDrawDistances;
    public float DebugDrawHeight => _debugDrawHeight;
}
