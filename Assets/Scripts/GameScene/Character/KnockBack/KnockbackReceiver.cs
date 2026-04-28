using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class KnockbackReceiver : MonoBehaviour
{
    [Header("Options")]
    [SerializeField] private bool _autoConsumePendingRequest = false;
    [SerializeField] private bool _stopNavMeshAgentWhileActive = true;

    [Header("Debug")]
    [ReadOnly] [SerializeField] private bool _hasPendingRequest;
    [ReadOnly] [SerializeField] private bool _isKnockbackActive;
    [ReadOnly] [SerializeField] private float _remainingTime;

    private CharacterController _characterController;
    private NavMeshAgent _navMeshAgent;
    private Rigidbody _rigidbody;

    private KnockbackRequest _pendingRequest;
    private KnockbackRequest _activeRequest;

    private float _elapsed;
    private bool _agentWasStoppedBeforeKnockback;

    public bool HasPendingRequest => _hasPendingRequest;
    public bool IsKnockbackActive => _isKnockbackActive;
    public float RemainingTime => _remainingTime;

    private void Awake()
    {
        TryGetComponent(out _characterController);
        TryGetComponent(out _navMeshAgent);
        TryGetComponent(out _rigidbody);
    }

    private void Update()
    {
        if (_autoConsumePendingRequest && _hasPendingRequest && !_isKnockbackActive)
            ConsumePendingRequest();

        if (_isKnockbackActive)
            TickKnockback(Time.deltaTime);
    }

    public void RequestKnockback(KnockbackRequest request)
    {
        if (request.duration <= 0f || request.distance <= 0f)
            return;

        Vector3 direction = request.worldDirection;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        direction.Normalize();
        request.worldDirection = direction;

        _pendingRequest = request;
        _hasPendingRequest = true;
    }

    public bool ConsumePendingRequest()
    {
        if (!_hasPendingRequest)
            return false;

        _activeRequest = _pendingRequest;
        _hasPendingRequest = false;

        _isKnockbackActive = true;
        _elapsed = 0f;
        _remainingTime = _activeRequest.duration;

        if (_navMeshAgent != null && _navMeshAgent.isActiveAndEnabled && _navMeshAgent.isOnNavMesh)
        {
            _agentWasStoppedBeforeKnockback = _navMeshAgent.isStopped;

            if (_stopNavMeshAgentWhileActive)
                _navMeshAgent.isStopped = true;
        }

        return true;
    }

    public void ClearPendingRequest()
    {
        _hasPendingRequest = false;
    }

    public void CancelActiveKnockback()
    {
        _isKnockbackActive = false;
        _remainingTime = 0f;
        RestoreNavMeshAgentState();
    }

    private void TickKnockback(float deltaTime)
    {
        if (_activeRequest.duration <= 0f)
        {
            FinishKnockback();
            return;
        }

        float previous01 = _elapsed / _activeRequest.duration;
        _elapsed = Mathf.Min(_elapsed + deltaTime, _activeRequest.duration);
        float current01 = _elapsed / _activeRequest.duration;

        float deltaDistance = _activeRequest.distance * (current01 - previous01);
        Vector3 delta = _activeRequest.worldDirection * deltaDistance;

        ApplyDelta(delta);

        _remainingTime = Mathf.Max(0f, _activeRequest.duration - _elapsed);

        if (_elapsed >= _activeRequest.duration)
            FinishKnockback();
    }

    private void ApplyDelta(Vector3 delta)
    {
        if (_navMeshAgent != null && _navMeshAgent.isActiveAndEnabled && _navMeshAgent.isOnNavMesh)
        {
            _navMeshAgent.Move(delta);
            return;
        }

        if (_characterController != null && _characterController.enabled)
        {
            _characterController.Move(delta);
            return;
        }

        if (_rigidbody != null && !_rigidbody.isKinematic)
        {
            _rigidbody.MovePosition(_rigidbody.position + delta);
            return;
        }

        transform.position += delta;
    }

    private void FinishKnockback()
    {
        _isKnockbackActive = false;
        _remainingTime = 0f;
        RestoreNavMeshAgentState();
    }

    private void RestoreNavMeshAgentState()
    {
        if (_navMeshAgent != null && _navMeshAgent.isActiveAndEnabled && _navMeshAgent.isOnNavMesh)
        {
            if (_stopNavMeshAgentWhileActive)
                _navMeshAgent.isStopped = _agentWasStoppedBeforeKnockback;
        }
    }

}
