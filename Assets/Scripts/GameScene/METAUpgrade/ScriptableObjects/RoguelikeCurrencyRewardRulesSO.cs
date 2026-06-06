using UnityEngine;

[CreateAssetMenu(
    fileName = "RoguelikeCurrencyRewardRules",
    menuName = "Game/Meta/Roguelike Currency Reward Rules")]
public class RoguelikeCurrencyRewardRulesSO : ScriptableObject
{
    [Header("Stage Transition Rewards")]
    [Tooltip("Index is the entered zero-based stage index. Stage 0 should usually be 0.")]
    [SerializeField] private int[] _stageTransitionRewards = { 0, 5, 10, 20, 30 };

    [Header("Kill Rewards")]
    [SerializeField, Min(0)] private int _namedEnemyReward = 10;
    [SerializeField, Min(0)] private int _midBossReward = 50;
    [SerializeField, Min(0)] private int _finalBossReward = 100;

    public int NamedEnemyReward => _namedEnemyReward;
    public int MidBossReward => _midBossReward;
    public int FinalBossReward => _finalBossReward;

    public int GetStageTransitionReward(int enteredStageIndex)
    {
        if (_stageTransitionRewards == null ||
            enteredStageIndex < 0 ||
            enteredStageIndex >= _stageTransitionRewards.Length)
        {
            return 0;
        }

        return Mathf.Max(0, _stageTransitionRewards[enteredStageIndex]);
    }
}
