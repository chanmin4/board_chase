using UnityEngine;

[CreateAssetMenu(
    fileName = "RollerAttackConfig",
    menuName = "Game/Enemy/Definition_Config/NormalEnemy/Roller/Roller Attack Config")]
public class RollerAttackConfigSO : ScriptableObject
{
    [Header("Roll Movement")]
    [SerializeField, Min(0f)] private float _rollMoveSpeed = 8f;
    [SerializeField, Min(0f)] private float _attackRollMoveSpeed = 12f;
    [SerializeField] private bool _pickRandomDirectionOnEnter = true;
    [SerializeField] private bool _faceMoveDirection = true;
    [SerializeField, Min(0f)] private float _turnSpeedDegPerSecond = 1440f;

    [Header("Attack Retarget Timing")]
    [SerializeField, Min(0f)] private float _minAttackReadyDelay = 1f;
    [SerializeField, Min(0f)] private float _maxAttackReadyDelay = 2f;
    [SerializeField] private bool _retargetOnlyWhenTargetExists = true;

    [Header("Bounce")]
    [SerializeField, Min(0.01f)] private float _collisionRadius = 0.5f;
    [SerializeField, Min(0f)] private float _collisionSkin = 0.05f;
    [SerializeField, Min(1)] private int _maxBouncesPerFrame = 2;
    [SerializeField] private LayerMask _wallMask = 0;
    [SerializeField] private QueryTriggerInteraction _wallTriggerInteraction = QueryTriggerInteraction.Ignore;
    [SerializeField, Min(1)] private int _maxWallHitBufferSize = 8;

    [Header("Sector Bounds Fallback")]
    [SerializeField] private bool _useCurrentSectorBounds = true;
    [SerializeField, Min(0f)] private float _sectorEdgePadding = 0.5f;

    [Header("NavMeshAgent")]
    [SerializeField] private bool _disableNavMeshAgentWhileRolling = true;

    [Header("Debug")]
    [SerializeField] private bool _debugDrawDirection = false;
    [SerializeField, Min(0f)] private float _debugDrawDuration = 0f;

    public float RollMoveSpeed => DifficultyRuntime.ApplyEnemyNormalMoveSpeed(_rollMoveSpeed);
    public float AttackRollMoveSpeed => DifficultyRuntime.ApplyEnemyChaseMoveSpeed(_attackRollMoveSpeed);
    public bool PickRandomDirectionOnEnter => _pickRandomDirectionOnEnter;
    public bool FaceMoveDirection => _faceMoveDirection;
    public float TurnSpeedDegPerSecond => Mathf.Max(0f, _turnSpeedDegPerSecond);

    public float MinAttackReadyDelay => Mathf.Max(0f, _minAttackReadyDelay);
    public float MaxAttackReadyDelay => Mathf.Max(MinAttackReadyDelay, _maxAttackReadyDelay);
    public bool RetargetOnlyWhenTargetExists => _retargetOnlyWhenTargetExists;

    public float CollisionRadius => Mathf.Max(0.01f, _collisionRadius);
    public float CollisionSkin => Mathf.Max(0f, _collisionSkin);
    public int MaxBouncesPerFrame => Mathf.Max(1, _maxBouncesPerFrame);
    public LayerMask WallMask => _wallMask;
    public QueryTriggerInteraction WallTriggerInteraction => _wallTriggerInteraction;
    public int MaxWallHitBufferSize => Mathf.Max(1, _maxWallHitBufferSize);

    public bool UseCurrentSectorBounds => _useCurrentSectorBounds;
    public float SectorEdgePadding => Mathf.Max(0f, _sectorEdgePadding);

    public bool DisableNavMeshAgentWhileRolling => _disableNavMeshAgentWhileRolling;

    public bool DebugDrawDirection => _debugDrawDirection;
    public float DebugDrawDuration => Mathf.Max(0f, _debugDrawDuration);
}
