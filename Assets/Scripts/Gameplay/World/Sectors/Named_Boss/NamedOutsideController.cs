/*
 * Role hierarchy: Gameplay > World > Sectors > Named_Boss
 * Primary responsibility: Coordinates the NamedOutsideController subsystem lifecycle and collaborating objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public class NamedOutsideController : MonoBehaviour
{
    [Header("Listening")]
    [Tooltip("Named sector lifecycle changes.")]
    [SerializeField] private NamedSectorPhaseEventChannelSO _namedSectorPhaseChannel;

    [Header("Broadcasting")]
    [Tooltip("Requests outside-sector virus stamping during named battle.")]
    [SerializeField] private NamedOutsidePressureRequestEventChannelSO _outsidePressureRequestChannel;

    [Header("Battle Outside Pressure")]
    [Tooltip("How often outside sectors receive virus pressure while named battle is active.")]
    [SerializeField] private float _outsidePressureTickSeconds = 10f;

    [Tooltip("Min virus percent added per outside sector pressure tick.")]
    [SerializeField, Range(0f, 1f)] private float _outsidePressureMinPercent = 0.01f;

    [Tooltip("Max virus percent added per outside sector pressure tick.")]
    [SerializeField, Range(0f, 1f)] private float _outsidePressureMaxPercent = 0.03f;

    private NamedSectorPhase _phase;
    private SectorRuntime _activeNamedSector;
    private float _outsidePressureTimer;

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_namedSectorPhaseChannel != null)
            _namedSectorPhaseChannel.OnEventRaised += HandleNamedSectorPhaseChanged;
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_namedSectorPhaseChannel != null)
            _namedSectorPhaseChannel.OnEventRaised -= HandleNamedSectorPhaseChanged;
    }

    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
        if (_phase != NamedSectorPhase.Battle)
            return;

        TickOutsidePressure(Time.deltaTime);
    }

    // Purpose: Handles the incoming named sector phase changed request or event.
    private void HandleNamedSectorPhaseChanged(NamedSectorPhaseChange change)
    {
        _phase = change.Phase;
        _activeNamedSector = change.Sector;
        _outsidePressureTimer = _outsidePressureTickSeconds;
    }

    // Purpose: Processes one update step for outside pressure.
    private void TickOutsidePressure(float deltaTime)
    {
        if (_outsidePressureRequestChannel == null || _outsidePressureTickSeconds <= 0f)
            return;

        _outsidePressureTimer -= deltaTime;
        if (_outsidePressureTimer > 0f)
            return;

        _outsidePressureTimer = _outsidePressureTickSeconds;

        float pressureMultiplier = DifficultyRuntime.NamedOutsidePressureMultiplier;

        NamedOutsidePressureRequest request = new NamedOutsidePressureRequest(
            _activeNamedSector,
            Mathf.Clamp01(_outsidePressureMinPercent * pressureMultiplier),
            Mathf.Clamp01(_outsidePressureMaxPercent * pressureMultiplier)
        );

        _outsidePressureRequestChannel.RaiseEvent(request);
    }
}
