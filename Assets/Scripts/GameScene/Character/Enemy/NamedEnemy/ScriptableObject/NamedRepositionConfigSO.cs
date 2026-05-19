using UnityEngine;

[CreateAssetMenu(
    fileName = "NamedRepositionConfig",
    menuName = "ScriptableObject/GameScene/Enemy/Definition_Config/Named Reposition Config")]
public class NamedRepositionConfigSO : ScriptableObject
{
    public enum SpeedSource
    {
        FixedValue,
        MovementStatsNormal,
        AgentCurrentValue
    }

    [Header("Distance")]
    [SerializeField, Min(0f)] private float _tooCloseDistance = 3f;
    [SerializeField, Min(0f)] private float _preferredDistance = 6f;
    [SerializeField, Min(0f)] private float _tooFarDistance = 10f;

    [Header("Movement")]
    [SerializeField] private SpeedSource _speedSource = SpeedSource.MovementStatsNormal;
    [SerializeField, Min(0f)] private float _fixedMoveSpeed = 2.2f;
    [SerializeField, Min(0f)] private float _strafeDistance = 2.5f;
    [SerializeField, Min(0.01f)] private float _destinationRefreshInterval = 0.35f;
    [SerializeField, Min(0.01f)] private float _navMeshSampleDistance = 2f;

    [Header("Facing")]
    [SerializeField] private bool _faceTarget = true;
    [SerializeField, Min(0f)] private float _turnSpeedDegPerSecond = 720f;

    [Header("Debug")]
    [SerializeField] private bool _debugDrawDistances;
    [SerializeField] private float _debugDrawHeight = 0.1f;

    public SpeedSource MoveSpeedSource => _speedSource;
    public float FixedMoveSpeed => DifficultyRuntime.ApplyEnemyNormalMoveSpeed(_fixedMoveSpeed);
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
