using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "StageEnemyWavePreset",
    menuName = "Game/Battle/Stage Enemy Wave Preset")]
public class StageEnemyWavePresetSO : ScriptableObject
{
    [Serializable]
    public class EnemySpawn
    {
        [Tooltip("Enemy stat config spawned by this wave entry.")]
        public EnemyStatConfigSO archetype;

        [Tooltip("How many enemies of this archetype are queued by this wave.")]
        [Min(1)] public int count = 1;
    }

    [Header("Display")]
    [SerializeField] private string _displayName;

    [Header("Enemies")]
    [SerializeField] private List<EnemySpawn> _enemies = new();

    public string DisplayName =>
        string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;

    public IReadOnlyList<EnemySpawn> Enemies => _enemies;

    public int TotalSpawnCount
    {
        get
        {
            int total = 0;

            if (_enemies == null)
                return 0;

            for (int i = 0; i < _enemies.Count; i++)
            {
                EnemySpawn spawn = _enemies[i];

                if (IsSpawnValid(spawn))
                    total += Mathf.Max(1, spawn.count);
            }

            return total;
        }
    }

    public bool IsValid => TotalSpawnCount > 0;

    private static bool IsSpawnValid(EnemySpawn spawn)
    {
        return spawn != null &&
               spawn.archetype != null &&
               spawn.archetype.IsValid &&
               spawn.count > 0;
    }
}
