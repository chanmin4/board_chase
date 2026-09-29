/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Core
 * Primary responsibility: Handles EntityFootstepSoundEmitter audio playback and sound-stimulus delivery.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class EntityFootstepSoundEmitter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ShooterStatsRuntime _statsRuntime;
    [SerializeField] private SoundStimulusEventChannelSO _soundStimulusEvent;
    [SerializeField] private Transform _soundOrigin;
    [Tooltip("Entity root whose planar displacement is used as the reliable movement fallback.")]
    [SerializeField] private Transform _movementRoot;
    [SerializeField] private ShooterDashController _dashController;

    [Header("Movement Sources")]
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private NavMeshAgent _navMeshAgent;
    [SerializeField] private Rigidbody _rigidbody;

    [Header("Options")]
    [SerializeField, Min(0f)] private float _minMoveSpeedToEmit = 0.1f;

    [Header("Runtime")]
    [ReadOnly] [SerializeField] private float _nextEmitTime;
    [ReadOnly] [SerializeField] private float _lastResolvedMoveSpeed;
    [ReadOnly] [SerializeField] private float _lastEmitTime = -1f;

    private Vector3 _lastPosition;

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        ResolveRefs();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        ResolveRefs();
        _lastPosition = ResolveMovementPosition();
    }

    // Purpose: Configures this component''s configure.
    public void Configure(
        ShooterStatsRuntime statsRuntime,
        SoundStimulusEventChannelSO soundStimulusEvent,
        Transform soundOrigin)
    {
        if (statsRuntime != null)
            _statsRuntime = statsRuntime;

        if (soundStimulusEvent != null)
            _soundStimulusEvent = soundStimulusEvent;

        if (soundOrigin != null)
            _soundOrigin = soundOrigin;

        if (statsRuntime != null)
            _movementRoot = statsRuntime.transform;

        ResolveRefs();
        _lastPosition = ResolveMovementPosition();
    }

    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
        if (_soundStimulusEvent == null)
            return;

        if (_dashController != null && _dashController.IsDashing)
            return;

        float radius = ResolveRadius();
        if (radius <= 0f)
            return;

        if (Time.time < _nextEmitTime)
            return;

        float speed = ResolveMoveSpeed();
        _lastResolvedMoveSpeed = speed;

        if (speed < _minMoveSpeedToEmit)
            return;

        _nextEmitTime = Time.time + ResolveInterval();
        _lastEmitTime = Time.time;
        Vector3 position = _soundOrigin != null ? _soundOrigin.position : transform.position;
        _soundStimulusEvent.RaiseEvent(new SoundStimulus(
            _statsRuntime != null ? _statsRuntime.gameObject : gameObject,
            position,
            radius,
            _statsRuntime != null ? _statsRuntime.SoundInvestigateDelaySeconds : 0f,
            SoundStimulusType.Footstep,
            Mathf.Clamp01(speed / Mathf.Max(0.01f, _statsRuntime != null ? _statsRuntime.MoveSpeed : speed))));
    }

    // Purpose: Processes frame work that must run after the regular Update pass.
    private void LateUpdate()
    {
        _lastPosition = ResolveMovementPosition();
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        if (_statsRuntime == null)
            _statsRuntime = GetComponent<ShooterStatsRuntime>() ?? GetComponentInParent<ShooterStatsRuntime>();

        if (_soundOrigin == null)
            _soundOrigin = _statsRuntime != null ? _statsRuntime.transform : transform;

        if (_movementRoot == null)
            _movementRoot = _statsRuntime != null ? _statsRuntime.transform : transform;

        if (_dashController == null)
            _dashController = EntityComponentResolver.Resolve<ShooterDashController>(this);

        if (_characterController == null)
            _characterController = GetComponent<CharacterController>() ?? GetComponentInParent<CharacterController>();

        if (_navMeshAgent == null)
            _navMeshAgent = GetComponent<NavMeshAgent>() ?? GetComponentInParent<NavMeshAgent>();

        if (_rigidbody == null)
            _rigidbody = GetComponent<Rigidbody>() ?? GetComponentInParent<Rigidbody>();
    }

    // Purpose: Resolves the radius needed by this component.
    private float ResolveRadius()
    {
        return _statsRuntime != null
            ? Mathf.Max(0f, _statsRuntime.FootstepSoundRadius)
            : 0f;
    }

    // Purpose: Resolves the interval needed by this component.
    private float ResolveInterval()
    {
        return _statsRuntime != null
            ? Mathf.Max(0.05f, _statsRuntime.FootstepSoundInterval)
            : 0.35f;
    }

    // Purpose: Resolves the move speed needed by this component.
    private float ResolveMoveSpeed()
    {
        float resolvedSpeed = 0f;

        if (_characterController != null && _characterController.enabled)
        {
            resolvedSpeed = Mathf.Max(resolvedSpeed, new Vector3(
                _characterController.velocity.x,
                0f,
                _characterController.velocity.z).magnitude);
        }

        if (_navMeshAgent != null && _navMeshAgent.isActiveAndEnabled)
        {
            resolvedSpeed = Mathf.Max(resolvedSpeed, new Vector3(
                _navMeshAgent.velocity.x,
                0f,
                _navMeshAgent.velocity.z).magnitude);
        }

        if (_rigidbody != null)
        {
            resolvedSpeed = Mathf.Max(resolvedSpeed, new Vector3(
                _rigidbody.linearVelocity.x,
                0f,
                _rigidbody.linearVelocity.z).magnitude);
        }

        Vector3 delta = ResolveMovementPosition() - _lastPosition;
        delta.y = 0f;
        float displacementSpeed = Time.deltaTime > 0f
            ? delta.magnitude / Time.deltaTime
            : 0f;

        return Mathf.Max(resolvedSpeed, displacementSpeed);
    }

    // Purpose: Resolves the movement position needed by this component.
    private Vector3 ResolveMovementPosition()
    {
        return _movementRoot != null ? _movementRoot.position : transform.position;
    }
}
