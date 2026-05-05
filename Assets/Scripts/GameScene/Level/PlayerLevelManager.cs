using UnityEngine;

[DisallowMultipleComponent]
public class PlayerLevelManager : MonoBehaviour
{
    [Header("Rules")]
    [SerializeField] private PlayerLevelRulesSO _rules;
    [SerializeField] private PlayerLevelUpgradePointRulesSO _upgradePointRules;
    [SerializeField] private int _startingLevel = 0;

    [Header("Listening To")]
    [SerializeField] private PlayerExperienceGainEventChannelSO _xpGainChannel;
    [SerializeField] private StageProgressSnapshotEventChannelSO _stageProgressChannel;
    [SerializeField] private VoidEventChannelSO _requestLevelSnapshotChannel;
    [SerializeField] private PlayerUpgradeStateReadyEventChannelSO _upgradeStateReadyChannel;

    [Header("Broadcasting On")]
    [SerializeField] private PlayerLevelSnapshotEventChannelSO _levelSnapshotChannel;

    [Header("Runtime")]
    [SerializeField] private int _level;
    [SerializeField] private float _currentXp;
    [SerializeField] private int _currentStageIndex;
    [SerializeField] private float _stageEarnedXp;

    private PlayerUpgradeState _upgradeState;

    public int Level => _level;
    public float CurrentXp => _currentXp;

    private void Awake()
    {
        _level = _startingLevel;
    }

    private void OnEnable()
    {
        if (_xpGainChannel != null)
            _xpGainChannel.OnEventRaised += OnXpGainRequested;

        if (_stageProgressChannel != null)
            _stageProgressChannel.OnEventRaised += OnStageProgressChanged;

        if (_requestLevelSnapshotChannel != null)
            _requestLevelSnapshotChannel.OnEventRaised += PublishSnapshot;

        if (_upgradeStateReadyChannel != null)
        {
            _upgradeStateReadyChannel.OnEventRaised += OnUpgradeStateReady;

            if (_upgradeStateReadyChannel.Current != null)
                OnUpgradeStateReady(_upgradeStateReadyChannel.Current);
        }

        PublishSnapshot();
    }

    private void OnDisable()
    {
        if (_xpGainChannel != null)
            _xpGainChannel.OnEventRaised -= OnXpGainRequested;

        if (_stageProgressChannel != null)
            _stageProgressChannel.OnEventRaised -= OnStageProgressChanged;

        if (_requestLevelSnapshotChannel != null)
            _requestLevelSnapshotChannel.OnEventRaised -= PublishSnapshot;

        if (_upgradeStateReadyChannel != null)
            _upgradeStateReadyChannel.OnEventRaised -= OnUpgradeStateReady;

        _upgradeState = null;
    }

    private void OnUpgradeStateReady(PlayerUpgradeState upgradeState)
    {
        _upgradeState = upgradeState;
    }

    private void OnStageProgressChanged(StageProgressSnapshot snapshot)
    {
        if (_currentStageIndex == snapshot.stageIndex)
            return;

        _currentStageIndex = snapshot.stageIndex;
        _stageEarnedXp = 0f;

        PublishSnapshot();
    }

    private void OnXpGainRequested(PlayerExperienceGain gain)
    {
        if (_rules == null || gain.amount <= 0f)
            return;

        float acceptedXp = ApplyStageXpLimit(gain.amount);

        if (acceptedXp <= 0f)
        {
            PublishSnapshot();
            return;
        }

        _currentXp += acceptedXp;
        _stageEarnedXp += acceptedXp;

        ProcessLevelUps();
        PublishSnapshot();
    }

    private float ApplyStageXpLimit(float requestedXp)
    {
        float stageLimit = _rules.GetStageXpLimit(_currentStageIndex);

        if (stageLimit <= 0f)
            return requestedXp;

        float remaining = Mathf.Max(0f, stageLimit - _stageEarnedXp);
        return Mathf.Min(requestedXp, remaining);
    }

    private void ProcessLevelUps()
    {
        int safety = 64;

        while (safety-- > 0 && _rules.HasNextLevel(_level))
        {
            float requiredXp = _rules.GetRequiredXp(_level);

            if (_currentXp < requiredXp)
                break;

            _currentXp -= requiredXp;
            _level++;

            GrantUpgradePointsForLevel(_level);
        }
    }

    private void GrantUpgradePointsForLevel(int reachedLevel)
    {
        if (_upgradeState == null)
            return;

        int points = _upgradePointRules != null
            ? _upgradePointRules.GetPointsForLevel(reachedLevel)
            : 1;

        if (points <= 0)
            return;

        _upgradeState.AddPoints(points);
    }

    private void PublishSnapshot()
    {
        if (_levelSnapshotChannel == null || _rules == null)
            return;

        _levelSnapshotChannel.RaiseEvent(new PlayerLevelSnapshot(
            _level,
            _currentXp,
            _rules.GetRequiredXp(_level),
            _currentStageIndex,
            _stageEarnedXp,
            _rules.GetStageXpLimit(_currentStageIndex)));
    }
}
