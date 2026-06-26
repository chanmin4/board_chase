using UnityEngine;

[CreateAssetMenu(
    fileName = "SpitterAttackConfig",
    menuName = "Game/Enemy/Spitter Attack Config")]
public class SpitterAttackConfigSO : EnemyFireProjectileConfigSO
{
    [Header("Range")]
    [SerializeField] private float _preferredDistance = 7f;
    [SerializeField] private float _tooCloseDistance = 4.5f;
    [SerializeField] private float _tooFarDistance = 9f;

    [Header("Movement")]
    [SerializeField] private float _windupMoveSpeed = 2.2f;
    [SerializeField] private float _recoverMoveSpeed = 3f;
    [SerializeField] private float _strafeDistance = 3f;
    [SerializeField] private float _destinationRefreshInterval = 0.35f;
    [SerializeField] private float _navMeshSampleDistance = 2f;

    [Header("Rotation")]
    [SerializeField] private float _turnSpeedDegPerSecond = 720f;

    [Header("Debug")]
    [SerializeField] private bool _debugDrawRecoverDistances = true;
    [SerializeField] private bool _debugLogRecoverDistance = false;
    [SerializeField] private float _debugLogInterval = 0.5f;
    [SerializeField] private float _debugDrawHeight = 0.1f;

    public float PreferredDistance => _preferredDistance;
    public float TooCloseDistance => _tooCloseDistance;
    public float TooFarDistance => _tooFarDistance;

    public float WindupMoveSpeed => DifficultyRuntime.ApplyEnemyNormalMoveSpeed(_windupMoveSpeed);
    public float RecoverMoveSpeed => DifficultyRuntime.ApplyEnemyNormalMoveSpeed(_recoverMoveSpeed);
    public float StrafeDistance => _strafeDistance;
    public float DestinationRefreshInterval => _destinationRefreshInterval;
    public float NavMeshSampleDistance => _navMeshSampleDistance;

    public float TurnSpeedDegPerSecond => _turnSpeedDegPerSecond;

    public bool DebugDrawRecoverDistances => _debugDrawRecoverDistances;
    public bool DebugLogRecoverDistance => _debugLogRecoverDistance;
    public float DebugLogInterval => _debugLogInterval;
    public float DebugDrawHeight => _debugDrawHeight;
}