using UnityEngine;

public class InfectionControlManager : MonoBehaviour
{
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

    private void Awake()
    {
        if (_rules != null)
            _currentControl = _rules.StartControl;
    }

    private void OnEnable()
    {
        if (_summaryChangedChannel != null)
            _summaryChangedChannel.OnEventRaised += OnSectorSummaryChanged;

        if (_namedSectorPhaseChannel != null)
            _namedSectorPhaseChannel.OnEventRaised += OnNamedSectorPhaseChanged;

        if (_stageInfectionControlRecoverChannel != null)
            _stageInfectionControlRecoverChannel.OnEventRaised += Recover;

        _infectionControlManagerReadyChannel?.RaiseEvent(this);

        Publish();
    }

    private void OnDisable()
    {
        if (_summaryChangedChannel != null)
            _summaryChangedChannel.OnEventRaised -= OnSectorSummaryChanged;

        if (_namedSectorPhaseChannel != null)
            _namedSectorPhaseChannel.OnEventRaised -= OnNamedSectorPhaseChanged;

        if (_stageInfectionControlRecoverChannel != null)
            _stageInfectionControlRecoverChannel.OnEventRaised -= Recover;

        if (_infectionControlManagerReadyChannel != null)
            _infectionControlManagerReadyChannel.Clear(this);
    }

    private void Update()
    {
        if (_rules == null || _isDepleted)
            return;

        float totalDrainPerSecond = ResolveTotalDrainPerSecond();

        if (totalDrainPerSecond <= 0f)
            return;

        ApplyDamage(totalDrainPerSecond * Time.deltaTime);
    }

    private void OnSectorSummaryChanged(SectorOccupancySummary summary)
    {
        if (_rules == null)
            return;

        _sectorDrainPerSecond = _rules.CalculateDrainPerSecond(summary);
        Publish();
    }

    private void OnNamedSectorPhaseChanged(NamedSectorPhaseChange change)
    {
        _namedDrainPerSecond = _rules != null
            ? _rules.GetNamedPhaseDrainPerSecond(change.Phase)
            : 0f;

        Publish();
    }

    public void ApplyDamage(float amount)
    {
        if (_rules == null || amount <= 0f || _isDepleted)
            return;

        _currentControl = Mathf.Max(0f, _currentControl - amount);

        if (_currentControl <= 0f)
        {
            _isDepleted = true;
            _currentControl = 0f;

            if (_gameOverChannel != null)
                _gameOverChannel.RaiseEvent();
        }

        Publish();
    }

    public void Recover(float amount)
    {
        if (_rules == null || amount <= 0f)
            return;

        _currentControl = Mathf.Min(_rules.MaxControl, _currentControl + amount);

        if (_currentControl > 0f)
            _isDepleted = false;

        Publish();
    }

    public void RecoverNamedDefeated()
    {
        if (_rules != null)
            Recover(_rules.RecoverOnNamedDefeated);
    }

    public void RecoverSectorExpanded()
    {
        if (_rules != null)
            Recover(_rules.RecoverOnSectorExpanded);
    }

    private float ResolveTotalDrainPerSecond()
    {
        return Mathf.Max(0f, _sectorDrainPerSecond + _namedDrainPerSecond);
    }

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
    public void ApplyNormalBattleTimerExpiredPenalty()
    {
        if (_rules == null)
            return;

        ApplyDamage(_rules.NormalBattleTimerExpiredExtraDrain);
    }
}