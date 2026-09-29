/*
 * Role hierarchy: Gameplay > Combat > Infection
 * WIP retired system: kept only so existing scenes/saves deserialize safely.
 * New content must use StageDefinitionSO Biochemical Gas and PlayerShooterInfection.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public class InfectionControlManager : MonoBehaviour
{
    [Header("WIP Retired")]
    [Tooltip("This whole component is intentionally inactive. Room gas and PlayerShooterInfection replaced Infection Control. Keep it only for safe deserialization of old scenes/saves.")]
    [SerializeField] private bool _retiredWip = true;

    [SerializeField] private InfectionControlRulesSO _rules;
    [SerializeField] private SectorOccupancySummaryEventChannelSO _summaryChangedChannel;
    [SerializeField] private InfectionControlEventChannelSO _infectionControlChangedChannel;

    [Header("Named Pressure")]
    [Tooltip("Named sector phase event. Used to add named-specific control drain.")]
    [SerializeField] private NamedSectorPhaseEventChannelSO _namedSectorPhaseChannel;

    [Header("Listening To")]
    [SerializeField] private FloatEventChannelSO _stageInfectionControlRecoverChannel;

    [Header("Game Over")]
    [SerializeField] private VoidEventChannelSO _gameOverChannel;

    [Header("Manager Ready")]
    [SerializeField] private InfectionControlManagerReadyEventChannelSO _infectionControlManagerReadyChannel;

    private float _currentControl;
    private float _sectorDrainPerSecond;
    private float _namedDrainPerSecond;
    private bool _isDepleted;
    private SectorStateManager _sectorStateManager;
    private bool _wasDrainPausedInHideout;

    public float CurrentControl => Mathf.Max(0f, _currentControl);
    public float MaxControl => _rules != null ? Mathf.Max(0f, _rules.MaxControl) : 0f;
    public bool IsDepleted => _isDepleted;
    public bool CanRecover =>
        _rules != null &&
        !_isDepleted &&
        CurrentControl < MaxControl - 0.001f;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        if (_rules != null)
            _currentControl = _rules.StartControl;
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_retiredWip)
            return;

        if (_summaryChangedChannel != null)
            _summaryChangedChannel.OnEventRaised += OnSectorSummaryChanged;

        if (_namedSectorPhaseChannel != null)
            _namedSectorPhaseChannel.OnEventRaised += OnNamedSectorPhaseChanged;

        if (_stageInfectionControlRecoverChannel != null)
            _stageInfectionControlRecoverChannel.OnEventRaised += Recover;

        _infectionControlManagerReadyChannel?.RaiseEvent(this);

        Publish();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_retiredWip)
            return;

        if (_summaryChangedChannel != null)
            _summaryChangedChannel.OnEventRaised -= OnSectorSummaryChanged;

        if (_namedSectorPhaseChannel != null)
            _namedSectorPhaseChannel.OnEventRaised -= OnNamedSectorPhaseChanged;

        if (_stageInfectionControlRecoverChannel != null)
            _stageInfectionControlRecoverChannel.OnEventRaised -= Recover;

        if (_infectionControlManagerReadyChannel != null)
            _infectionControlManagerReadyChannel.Clear(this);
    }

    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
        if (_retiredWip)
            return;

        if (_rules == null || _isDepleted)
            return;

        bool wasDrainPausedInHideout = _wasDrainPausedInHideout;
        float totalDrainPerSecond = ResolveTotalDrainPerSecond();

        if (wasDrainPausedInHideout != _wasDrainPausedInHideout)
            Publish();

        if (totalDrainPerSecond <= 0f)
            return;

        ApplyDamage(totalDrainPerSecond * Time.deltaTime);
    }

    // Purpose: Responds to the sector summary changed callback.
    private void OnSectorSummaryChanged(SectorOccupancySummary summary)
    {
        if (_rules == null)
            return;

        _sectorDrainPerSecond = _rules.CalculateDrainPerSecond(summary);
        Publish();
    }

    // Purpose: Responds to the named sector phase changed callback.
    private void OnNamedSectorPhaseChanged(NamedSectorPhaseChange change)
    {
        _namedDrainPerSecond = _rules != null
            ? _rules.GetNamedPhaseDrainPerSecond(change.Phase)
            : 0f;

        Publish();
    }

    // Purpose: Applies the requested damage change.
    public void ApplyDamage(float amount)
    {
        if (_rules == null || amount <= 0f || _isDepleted)
            return;

        _currentControl = Mathf.Max(0f, _currentControl - amount);

        if (_currentControl <= 0f)
        {
            _isDepleted = true;
            _currentControl = 0f;
            ApplyMaximumPlayerInfection();
        }

        Publish();
    }

    // Purpose: Performs the recover workflow for this component.
    public void Recover(float amount)
    {
        TryRecover(amount, out _);
    }

    // Purpose: Attempts to recover and reports whether the request succeeded.
    public bool TryRecover(float amount, out float recoveredAmount)
    {
        recoveredAmount = 0f;

        if (_rules == null || amount <= 0f || _isDepleted)
            return false;

        float before = _currentControl;
        _currentControl = Mathf.Min(_rules.MaxControl, _currentControl + amount);
        recoveredAmount = Mathf.Max(0f, _currentControl - before);

        Publish();
        return recoveredAmount > 0.001f;
    }

    // Purpose: Performs the recover named defeated workflow for this component.
    public void RecoverNamedDefeated()
    {
        if (_rules != null)
            Recover(_rules.RecoverOnNamedDefeated);
    }

    // Purpose: Performs the recover sector expanded workflow for this component.
    public void RecoverSectorExpanded()
    {
        if (_rules != null)
            Recover(_rules.RecoverOnSectorExpanded);
    }

    // Purpose: Resolves the total drain per second needed by this component.
    private float ResolveTotalDrainPerSecond()
    {
        bool pausedInHideout = IsInHideout();

        if (_wasDrainPausedInHideout != pausedInHideout)
            _wasDrainPausedInHideout = pausedInHideout;

        if (pausedInHideout)
            return 0f;

        return Mathf.Max(0f, _sectorDrainPerSecond + _namedDrainPerSecond);
    }

    // Purpose: Checks whether in hideout is currently true.
    private bool IsInHideout()
    {
        if (_sectorStateManager == null)
            _sectorStateManager = FindAnyObjectByType<SectorStateManager>();

        SectorRuntime currentSector = _sectorStateManager != null
            ? _sectorStateManager.CurrentSector
            : null;

        // Infection Control is a stage-expedition pressure. Until a stage
        // sector is actually current (including the brief transition period),
        // and while in HomeSector, its continuous drain stays paused.
        return currentSector == null ||
               currentSector.GetComponent<HomeSector>() != null;
    }

    // Purpose: Applies the requested maximum player infection change.
    private void ApplyMaximumPlayerInfection()
    {
        PlayerShooterInfection playerInfection =
            FindFirstObjectByType<PlayerShooterInfection>(FindObjectsInactive.Exclude);

        if (playerInfection != null && !playerInfection.IsDead)
        {
            playerInfection.ForceInfectionToMaximumHealth();
            return;
        }

        Debug.LogError(
            "[InfectionControlManager] Infection Control reached zero but PlayerShooterInfection was unavailable. Falling back to GameOver.",
            this);
        _gameOverChannel?.RaiseEvent();
    }

    // Purpose: Publishes the publish notification.
    private void Publish()
    {
        if (_infectionControlChangedChannel == null || _rules == null)
            return;

        float totalDrainPerSecond = ResolveTotalDrainPerSecond();

        _infectionControlChangedChannel.RaiseEvent(new InfectionControlSnapshot
        {
            current = _currentControl,
            max = _rules.MaxControl,
            normalized = _rules.MaxControl > 0f ? _currentControl / _rules.MaxControl : 0f,
            drainPerSecond = totalDrainPerSecond
        });
    }
    // Purpose: Applies the requested normal battle timer expired penalty change.
    public void ApplyNormalBattleTimerExpiredPenalty()
    {
        if (_rules == null)
            return;

        ApplyDamage(_rules.NormalBattleTimerExpiredExtraDrain);
    }
}
