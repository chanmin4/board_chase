using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "StageProgressionRules",
    menuName = "Game/Stage/Stage Progression Rules")]
public class StageProgressionRulesSO : ScriptableObject
{
    [Serializable]
    public class StageProgressRule
    {
        public int stageIndex;
        public string displayName;

        [Min(0f)]
        public float timerSeconds = 30f;

        [Min(0)]
        public int requiredPlayerOwnedCount = 1;

        [Tooltip("조건이 깨졌을 때 타이머를 다시 처음으로 돌릴지 여부. OFF면 일시정지처럼 멈춤.")]
        public bool resetTimerWhenRequirementLost=false;
        [Tooltip("start sector 전용 플래그 (스타트 전용 playerowned 조건을사용) ")]
        public bool useStartSectorAsRequirement=false;
    }

    [SerializeField] private List<StageProgressRule> _rules = new();

    public bool TryGetRule(int stageIndex, out StageProgressRule rule)
    {
        for (int i = 0; i < _rules.Count; i++)
        {
            if (_rules[i] != null && _rules[i].stageIndex == stageIndex)
            {
                rule = _rules[i];
                return true;
            }
        }

        rule = null;
        return false;
    }

    public bool HasNextRule(int stageIndex)
    {
        return TryGetRule(stageIndex + 1, out _);
    }
}
