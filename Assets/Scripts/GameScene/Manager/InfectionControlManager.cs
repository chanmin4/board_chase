using UnityEngine;

public class InfectionControlManager : MonoBehaviour
{
    [SerializeField] private InfectionControlRulesSO _rules;
    [SerializeField] private SectorOccupancySummaryEventChannelSO _summaryChangedChannel;
    [SerializeField] private InfectionControlEventChannelSO _infectionControlChangedChannel;

    [Header("Game Over")]
    [SerializeField] private VoidEventChannelSO _gameOverChannel;

    private float _currentControl;
    private float _drainPerSecond;
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

        Publish();
    }

    private void OnDisable()
    {
        if (_summaryChangedChannel != null)
            _summaryChangedChannel.OnEventRaised -= OnSectorSummaryChanged;
    }

    private void Update()
    {
        if (_rules == null || _isDepleted || _drainPerSecond <= 0f)
            return;

        _currentControl = Mathf.Max(0f, _currentControl - _drainPerSecond * Time.deltaTime);

        if (_currentControl <= 0f)
        {
            _isDepleted = true;
            _currentControl = 0f;

            if (_gameOverChannel != null)
                _gameOverChannel.RaiseEvent();
        }

        Publish();
    }

    private void OnSectorSummaryChanged(SectorOccupancySummary summary)
    {
        if (_rules == null)
            return;

        _drainPerSecond = _rules.CalculateDrainPerSecond(summary);
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

    private void Publish()
    {
        if (_infectionControlChangedChannel == null || _rules == null)
            return;

        _infectionControlChangedChannel.RaiseEvent(new InfectionControlSnapshot
        {
            current = _currentControl,
            max = _rules.MaxControl,
            normalized = _rules.MaxControl > 0f ? _currentControl / _rules.MaxControl : 0f,
            drainPerSecond = _drainPerSecond
        });
    }
}
