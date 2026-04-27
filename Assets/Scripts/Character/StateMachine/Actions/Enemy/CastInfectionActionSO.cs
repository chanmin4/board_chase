using UnityEngine;
using UnityEngine.AI;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "CastInfectionAction",
    menuName = "State Machines/Enemy Actions/Cast Infection")]
public class CastInfectionActionSO : StateActionSO
{
    [SerializeField] private float _infectionRadius = 1.5f;
    [SerializeField] private bool _applyOnStateExit = true;
    [SerializeField] private int _paintPriority = 0;

    [Header("Refs")]
    [SerializeField] private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;

    [Header("Debug")]
    [SerializeField] private bool _debugLogs = true;
    [SerializeField] private bool _debugDraw = true;
    [SerializeField] private float _debugDrawDuration = 2f;

    public float InfectionRadius => _infectionRadius;
    public bool ApplyOnStateExit => _applyOnStateExit;
    public int PaintPriority => _paintPriority;
    public MaskRenderManagerEventChannelSO MaskRenderManagerReadyChannel => _maskRenderManagerReadyChannel;
    public bool DebugLogs => _debugLogs;
    public bool DebugDraw => _debugDraw;
    public float DebugDrawDuration => _debugDrawDuration;

    protected override StateAction CreateAction() => new CastInfectionAction();
}

public class CastInfectionAction : StateAction
{
    private Enemy _enemy;
    private NavMeshAgent _agent;
    private CastInfectionActionSO _config;
    private MaskRenderManager _maskRenderManager;

    public override void Awake(StateMachine stateMachine)
    {
        _enemy = stateMachine.GetComponent<Enemy>();
        _agent = stateMachine.GetComponent<NavMeshAgent>();
        _config = (CastInfectionActionSO)OriginSO;
        CacheMaskRenderManager();
    }

    public override void OnStateEnter()
    {
        if (_agent != null && _agent.isActiveAndEnabled)
            _agent.isStopped = true;
    }

    public override void OnUpdate() { }

    public override void OnStateExit()
    {
        if (_config.ApplyOnStateExit)
            ApplyInfection();

        if (_enemy != null)
        {
            _enemy.ClearInfectionTarget();
            _enemy.SetSearchSucceeded(false);
            _enemy.SetInfectionTargetvalid(false);
        }
    }

    private void ApplyInfection()
    {
        if (_enemy == null || !_enemy.HasInfectionTarget)
            return;

        CacheMaskRenderManager();

        Vector3 center = _enemy.InfectionTargetPosition;

        if (_config.DebugDraw)
            DrawImpactMarker(center, Color.magenta);

        if (_maskRenderManager == null)
        {
            if (_config.DebugLogs)
                Debug.LogWarning("[CastInfectionAction] MaskRenderManager missing. Debug draw only.");
            return;
        }

        bool accepted = _maskRenderManager.RequestCircle(
            MaskRenderManager.PaintChannel.Virus,
            center,
            _config.InfectionRadius,
            _config.PaintPriority,
            _enemy);

        if (_config.DebugLogs)
            Debug.Log($"[CastInfectionAction] accepted={accepted}, center={center}, radius={_config.InfectionRadius}");
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

    private void DrawImpactMarker(Vector3 point, Color color)
    {
        Debug.DrawLine(point + Vector3.left * _config.InfectionRadius, point + Vector3.right * _config.InfectionRadius, color, _config.DebugDrawDuration);
        Debug.DrawLine(point + Vector3.forward * _config.InfectionRadius, point + Vector3.back * _config.InfectionRadius, color, _config.DebugDrawDuration);
        Debug.DrawLine(point, point + Vector3.up * 0.75f, color, _config.DebugDrawDuration);
    }
}
