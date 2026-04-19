using UnityEngine;

[DisallowMultipleComponent]
public class StageProgressionManager : MonoBehaviour
{
    [Header("Rules")]
    [SerializeField] private StageProgressionRulesSO _rules;
    [SerializeField] private int _startingStageIndex = 0;

    [Header("Listening To")]
    [SerializeField] private SectorOccupancySummaryEventChannelSO _summaryChangedChannel;
    [SerializeField] private IntEventChannelSO _stageAppliedChannel;

    [Header("Broadcasting On")]
    [SerializeField] private VoidEventChannelSO _requestProgressNextStageChannel;
    [SerializeField] private StageProgressSnapshotEventChannelSO _snapshotChangedChannel;

    private SectorOccupancySummary _latestSummary;
    private StageProgressionRulesSO.StageProgressRule _currentRule;

    private int _currentStageIndex;
    private float _remainingSeconds;
    private bool _hasSummary;
    private bool _hasRule;
    private bool _isCompleted;

    private void OnEnable()
    {
        if (_summaryChangedChannel != null)
            _summaryChangedChannel.OnEventRaised += OnSectorSummaryChanged;

        if (_stageAppliedChannel != null)
            _stageAppliedChannel.OnEventRaised += OnStageApplied;

        BeginStage(_startingStageIndex);
    }

    private void OnDisable()
    {
        if (_summaryChangedChannel != null)
            _summaryChangedChannel.OnEventRaised -= OnSectorSummaryChanged;

        if (_stageAppliedChannel != null)
            _stageAppliedChannel.OnEventRaised -= OnStageApplied;
    }

    private void Update()
    {
        if (!_hasRule || _isCompleted)
            return;

        bool requirementMet = IsRequirementMet();

        if (requirementMet)
        {
            _remainingSeconds -= Time.deltaTime;

            if (_remainingSeconds <= 0f)
            {
                _remainingSeconds = 0f;
                _isCompleted = true;

                PublishSnapshot();

                if (_requestProgressNextStageChannel != null)
                    _requestProgressNextStageChannel.RaiseEvent();

                return;
            }
        }
        else if (_currentRule.resetTimerWhenRequirementLost)
        {
            _remainingSeconds = _currentRule.timerSeconds;
        }

        PublishSnapshot();
    }

    private void OnSectorSummaryChanged(SectorOccupancySummary summary)
    {
        _latestSummary = summary;
        _hasSummary = true;

        PublishSnapshot();
    }

    private void OnStageApplied(int stageIndex)
    {
        BeginStage(stageIndex);
    }

    private void BeginStage(int stageIndex)
    {
        _currentStageIndex = stageIndex;
        _isCompleted = false;
        _hasRule = _rules != null && _rules.TryGetRule(stageIndex, out _currentRule);

        if (_hasRule)
            _remainingSeconds = _currentRule.timerSeconds;
        else
            _remainingSeconds = 0f;

        PublishSnapshot();
    }

    private bool IsRequirementMet()
    {
        if (!_hasSummary || !_hasRule)
            return false;

        if (_currentRule.useStartSectorAsRequirement)
            return _latestSummary.startSectorPlayerOwned;

        return _latestSummary.playerOwnedCount >= _currentRule.requiredPlayerOwnedCount;
    }

    private void PublishSnapshot()
    {
        if (_snapshotChangedChannel == null)
            return;

        StageProgressSnapshot snapshot = new StageProgressSnapshot();

        snapshot.stageIndex = _currentStageIndex;
        snapshot.displayName = _hasRule ? _currentRule.displayName : string.Empty;

        snapshot.remainingSeconds = _remainingSeconds;
        snapshot.durationSeconds = _hasRule ? _currentRule.timerSeconds : 0f;
        snapshot.progress01 = snapshot.durationSeconds > 0f
            ? 1f - Mathf.Clamp01(_remainingSeconds / snapshot.durationSeconds)
            : 0f;

        snapshot.requiredPlayerOwnedCount = _hasRule ? _currentRule.requiredPlayerOwnedCount : 0;

        if (_hasSummary && _hasRule && _currentRule.useStartSectorAsRequirement)
            snapshot.currentPlayerOwnedCount = _latestSummary.startSectorPlayerOwned ? 1 : 0;
        else
            snapshot.currentPlayerOwnedCount = _hasSummary ? _latestSummary.playerOwnedCount : 0;
        snapshot.requirementMet = IsRequirementMet();
        snapshot.isCompleted = _isCompleted;
        snapshot.hasNextStage = _rules != null && _rules.HasNextRule(_currentStageIndex);

        _snapshotChangedChannel.RaiseEvent(snapshot);
    }
}
