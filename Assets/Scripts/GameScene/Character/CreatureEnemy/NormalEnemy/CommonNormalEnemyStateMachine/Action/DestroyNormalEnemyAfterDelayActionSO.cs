using UnityEngine;
using UnityEngine.AI;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "DestroyNormalEnemyAfterDelayAction",
    menuName = "State Machines/Enemy Actions/Destroy Normal Enemy After Delay")]
public class DestroyNormalEnemyAfterDelayActionSO : StateActionSO<DestroyEnemyAfterDelayAction>
{
    [SerializeField, Min(0f)] private float _delaySeconds = 1.2f;
    [SerializeField] private bool _disableCollidersOnEnter = true;
    [SerializeField] private bool _stopAgentOnEnter = true;

    public float DelaySeconds => _delaySeconds;
    public bool DisableCollidersOnEnter => _disableCollidersOnEnter;
    public bool StopAgentOnEnter => _stopAgentOnEnter;
}

public class DestroyEnemyAfterDelayAction : StateAction
{
    private DestroyNormalEnemyAfterDelayActionSO _origin;
    private GameObject _target;
    private Collider[] _colliders;
    private NavMeshAgent _agent;

    private float _timer;
    private bool _destroyed;

    public override void Awake(StateMachine stateMachine)
    {
        _origin = (DestroyNormalEnemyAfterDelayActionSO)OriginSO;

        Enemy enemy = stateMachine.GetComponentInParent<Enemy>();
        Damageable damageable = stateMachine.GetComponentInParent<Damageable>();

        if (enemy != null)
            _target = enemy.gameObject;
        else if (damageable != null)
            _target = damageable.gameObject;
        else
            _target = stateMachine.gameObject;

        if (_target != null)
        {
            _colliders = _target.GetComponentsInChildren<Collider>(true);
            _agent = _target.GetComponentInChildren<NavMeshAgent>(true);
        }
    }

    public override void OnStateEnter()
    {
        _timer = 0f;
        _destroyed = false;

        if (_origin.DisableCollidersOnEnter)
            SetColliders(false);

        if (_origin.StopAgentOnEnter)
            StopAgent();

        if (_origin.DelaySeconds <= 0f)
            DestroyNow();
    }

    public override void OnUpdate()
    {
        if (_destroyed)
            return;

        _timer += Time.deltaTime;

        if (_timer >= _origin.DelaySeconds)
            DestroyNow();
    }

    private void SetColliders(bool enabled)
    {
        if (_colliders == null)
            return;

        for (int i = 0; i < _colliders.Length; i++)
        {
            if (_colliders[i] != null)
                _colliders[i].enabled = enabled;
        }
    }

    private void StopAgent()
    {
        if (_agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
            return;

        _agent.ResetPath();
        _agent.isStopped = true;
    }

    private void DestroyNow()
    {
        if (_destroyed)
            return;

        _destroyed = true;

        if (_target == null)
            return;

        _target.SetActive(false);
        Object.Destroy(_target);
    }
}
