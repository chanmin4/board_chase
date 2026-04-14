using UnityEngine;

public class InfectionControlManager : MonoBehaviour
{
    [SerializeField] private InfectionControlRulesSO _rules;
    [SerializeField] private SectorOccupancySummaryEventChannelSO _summaryChangedChannel;
    [SerializeField] private InfectionControlEventChannelSO _infectionControlChangedChannel;

    private float _currentControl;
    private float _drainPerSecond;

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
        if (_rules == null || _drainPerSecond <= 0f)
            return;

        _currentControl = Mathf.Max(0f, _currentControl - _drainPerSecond * Time.deltaTime);
        Publish();
    }

    private void OnSectorSummaryChanged(SectorOccupancySummary summary)
    {
        if (_rules == null)
            return;

        _drainPerSecond =
            summary.virusOwnedCount * _rules.DrainPerVirusSector +
            summary.namedActiveCount * _rules.NamedDrainBonus +
            summary.bossActiveCount * _rules.BossDrainBonus;

        Publish();
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
