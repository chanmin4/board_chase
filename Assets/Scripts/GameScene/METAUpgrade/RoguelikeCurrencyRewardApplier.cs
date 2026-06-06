using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class RoguelikeCurrencyRewardApplier : MonoBehaviour
{
    [Header("Rules")]
    [SerializeField] private RoguelikeCurrencyRewardRulesSO _rewardRules;

    [Header("Refs")]
    [SerializeField] private PlayerCurrencyRuntime _currencyRuntime;
    [SerializeField] private SaveSystem _saveSystem;

    [Header("Events")]
    [SerializeField] private StageProgressSnapshotEventChannelSO _stageProgressChangedChannel;
    [SerializeField] private NamedEnemyKilledEventChannelSO _namedEnemyKilledChannel;

    [Header("Save")]
    [SerializeField] private bool _saveToDiskOnReward = true;

    private readonly HashSet<int> _rewardedEnteredStageIndexes = new();

    private void OnEnable()
    {
        if (_stageProgressChangedChannel != null)
            _stageProgressChangedChannel.OnEventRaised += HandleStageProgressChanged;

        if (_namedEnemyKilledChannel != null)
            _namedEnemyKilledChannel.OnEventRaised += HandleNamedEnemyKilled;
    }

    private void OnDisable()
    {
        if (_stageProgressChangedChannel != null)
            _stageProgressChangedChannel.OnEventRaised -= HandleStageProgressChanged;

        if (_namedEnemyKilledChannel != null)
            _namedEnemyKilledChannel.OnEventRaised -= HandleNamedEnemyKilled;
    }

    public void ResetRunRewardHistory()
    {
        _rewardedEnteredStageIndexes.Clear();
    }

    public void GrantMidBossDefeatedReward()
    {
        GrantRoguelikeCurrency(_rewardRules != null ? _rewardRules.MidBossReward : 0);
    }

    public void GrantFinalBossDefeatedReward()
    {
        GrantRoguelikeCurrency(_rewardRules != null ? _rewardRules.FinalBossReward : 0);
    }

    private void HandleStageProgressChanged(StageProgressSnapshot snapshot)
    {
        if (_rewardRules == null || snapshot.isResting)
            return;

        int enteredStageIndex = snapshot.stageIndex;

        if (!_rewardedEnteredStageIndexes.Add(enteredStageIndex))
            return;

        GrantRoguelikeCurrency(_rewardRules.GetStageTransitionReward(enteredStageIndex));
    }

    private void HandleNamedEnemyKilled(NamedEnemy namedEnemy)
    {
        GrantRoguelikeCurrency(_rewardRules != null ? _rewardRules.NamedEnemyReward : 0);
    }

    private void GrantRoguelikeCurrency(int amount)
    {
        if (amount <= 0)
            return;

        if (_currencyRuntime != null)
        {
            if (_currencyRuntime.AddCurrency(PlayerCurrencyType.Roguelike, amount))
                _currencyRuntime.WriteRoguelikeCurrencyToSaveSystem(_saveToDiskOnReward);

            return;
        }

        if (_saveSystem == null || _saveSystem.saveData == null)
            return;

        _saveSystem.saveData.EnsureRuntimeDefaults();
        _saveSystem.saveData._roguelikeCurrency += amount;

        if (_saveToDiskOnReward)
            _saveSystem.SaveDataToDisk();
    }
}
