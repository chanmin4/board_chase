using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class EntityFootstepSoundEmitter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ShooterStatsRuntime _statsRuntime;
    [SerializeField] private SoundStimulusEventChannelSO _soundStimulusEvent;
    [SerializeField] private Transform _soundOrigin;

    [Header("Movement Sources")]
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private NavMeshAgent _navMeshAgent;
    [SerializeField] private Rigidbody _rigidbody;

    [Header("Options")]
    [SerializeField, Min(0f)] private float _minMoveSpeedToEmit = 0.1f;

    [Header("Runtime")]
    [ReadOnly] [SerializeField] private float _nextEmitTime;

    private Vector3 _lastPosition;

    private void Reset()
    {
        ResolveRefs();
    }

    private void Awake()
    {
        ResolveRefs();
        _lastPosition = transform.position;
    }

    private void Update()
    {
        if (_soundStimulusEvent == null)
            return;

        float radius = ResolveRadius();
        if (radius <= 0f)
            return;

        if (Time.time < _nextEmitTime)
            return;

        float speed = ResolveMoveSpeed();
        if (speed < _minMoveSpeedToEmit)
            return;

        _nextEmitTime = Time.time + ResolveInterval();
        Vector3 position = _soundOrigin != null ? _soundOrigin.position : transform.position;
        _soundStimulusEvent.RaiseEvent(new SoundStimulus(
            gameObject,
            position,
            radius,
            _statsRuntime != null ? _statsRuntime.SoundInvestigateDelaySeconds : 0f,
            SoundStimulusType.Footstep,
            Mathf.Clamp01(speed / Mathf.Max(0.01f, _statsRuntime != null ? _statsRuntime.MoveSpeed : speed))));
    }

    private void LateUpdate()
    {
        _lastPosition = transform.position;
    }

    private void ResolveRefs()
    {
        if (_statsRuntime == null)
            _statsRuntime = GetComponent<ShooterStatsRuntime>() ?? GetComponentInParent<ShooterStatsRuntime>();

        if (_soundOrigin == null)
            _soundOrigin = transform;

        if (_characterController == null)
            _characterController = GetComponent<CharacterController>() ?? GetComponentInParent<CharacterController>();

        if (_navMeshAgent == null)
            _navMeshAgent = GetComponent<NavMeshAgent>() ?? GetComponentInParent<NavMeshAgent>();

        if (_rigidbody == null)
            _rigidbody = GetComponent<Rigidbody>() ?? GetComponentInParent<Rigidbody>();
    }

    private float ResolveRadius()
    {
        return _statsRuntime != null
            ? Mathf.Max(0f, _statsRuntime.FootstepSoundRadius)
            : 0f;
    }

    private float ResolveInterval()
    {
        return _statsRuntime != null
            ? Mathf.Max(0.05f, _statsRuntime.FootstepSoundInterval)
            : 0.35f;
    }

    private float ResolveMoveSpeed()
    {
        if (_characterController != null)
            return new Vector3(
                _characterController.velocity.x,
                0f,
                _characterController.velocity.z).magnitude;

        if (_navMeshAgent != null && _navMeshAgent.isActiveAndEnabled)
            return new Vector3(
                _navMeshAgent.velocity.x,
                0f,
                _navMeshAgent.velocity.z).magnitude;

        if (_rigidbody != null)
            return new Vector3(
                _rigidbody.linearVelocity.x,
                0f,
                _rigidbody.linearVelocity.z).magnitude;

        Vector3 delta = transform.position - _lastPosition;
        delta.y = 0f;
        return Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
    }
}
