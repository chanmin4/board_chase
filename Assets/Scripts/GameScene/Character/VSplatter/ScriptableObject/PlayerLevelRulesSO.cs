using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerLevelRules",
    menuName = "Game/Player/Player Level Rules")]
public class PlayerLevelRulesSO : ScriptableObject
{
    [Serializable]
    public class LevelRule
    {
        public int level;
        public float xpToNext = 100f;

        [Header("Growth")]
        public float damageMultiplier = 1f;
        public float paintRadiusMultiplier = 1f;
        public float infectionRecoverMultiplier = 1f;
    }

    [Serializable]
    public class StageXpLimitRule
    {
        public int stageIndex;
        public float maxXpInStage = 300f;
    }

    [SerializeField] private List<LevelRule> _levelRules = new();
    [SerializeField] private List<StageXpLimitRule> _stageXpLimits = new();

    public bool TryGetLevelRule(int level, out LevelRule rule)
    {
        for (int i = 0; i < _levelRules.Count; i++)
        {
            if (_levelRules[i] != null && _levelRules[i].level == level)
            {
                rule = _levelRules[i];
                return true;
            }
        }

        rule = null;
        return false;
    }

    public float GetRequiredXp(int level)
    {
        return TryGetLevelRule(level, out LevelRule rule)
            ? Mathf.Max(1f, rule.xpToNext)
            : float.PositiveInfinity;
    }

    public float GetStageXpLimit(int stageIndex)
    {
        for (int i = 0; i < _stageXpLimits.Count; i++)
        {
            if (_stageXpLimits[i] != null && _stageXpLimits[i].stageIndex == stageIndex)
                return Mathf.Max(0f, _stageXpLimits[i].maxXpInStage);
        }

        return 0f; // 0이면 제한 없음
    }

    public bool HasNextLevel(int level)
    {
        return TryGetLevelRule(level, out _);
    }
}
