using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerLevelUpgradePointRules",
    menuName = "Game/Player/Level Upgrade Point Rules")]
public class PlayerLevelUpgradePointRulesSO : ScriptableObject
{
    [Serializable]
    public struct LevelPointEntry
    {
        [Min(1)]
        [Tooltip("Player level reached after leveling up.")]
        public int level;

        [Min(0)]
        [Tooltip("Upgrade points granted when the player reaches this level.")]
        public int points;
    }

    [Header("Default")]
    [Min(0)]
    [SerializeField] private int _defaultPointsPerLevel = 1;

    [Header("Overrides")]
    [Tooltip("If a reached level is listed here, this value overrides the default point grant.")]
    [SerializeField] private LevelPointEntry[] _levelPointOverrides;

    public int GetPointsForLevel(int reachedLevel)
    {
        if (_levelPointOverrides != null)
        {
            for (int i = 0; i < _levelPointOverrides.Length; i++)
            {
                if (_levelPointOverrides[i].level == reachedLevel)
                    return Mathf.Max(0, _levelPointOverrides[i].points);
            }
        }

        return Mathf.Max(0, _defaultPointsPerLevel);
    }
}
