using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "StageBattleEncounterPreset",
    menuName = "Game/Battle/Stage Battle Encounter Preset")]
public class StageBattleEncounterPresetSO : ScriptableObject
{
    [Serializable]
    public class EnemyWavePresetCandidate
    {
        [Tooltip("Reusable enemy wave preset candidate.")]
        public StageEnemyWavePresetSO preset;

        [Tooltip("Weighted chance for this wave preset when this encounter wave starts.")]
        [Min(0)] public int weight = 1;
    }

    [Serializable]
    public class EncounterWave
    {
        [Tooltip("Delay after room encounter starts before this wave is queued.")]
        [Min(0f)] public float delaySeconds;

        [Tooltip("One candidate is picked by weight when this wave starts.")]
        public List<EnemyWavePresetCandidate> enemyWavePresetCandidates = new();
    }

    [Header("Display")]
    [SerializeField] private string _displayName;

    [Header("Enemy Limit")]
    [Tooltip("Maximum enemies alive at the same time. 0 means no cap.")]
    [SerializeField, Min(0)] private int _sectorMaxAlive = 0;

    [Header("Waves")]
    [SerializeField] private List<EncounterWave> _waves = new();

    public string DisplayName =>
        string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;

    public int SectorMaxAlive => Mathf.Max(0, _sectorMaxAlive);
    public IReadOnlyList<EncounterWave> Waves => _waves;
    public int WaveCount => _waves != null ? _waves.Count : 0;

    public bool IsValid
    {
        get
        {
            if (_waves == null || _waves.Count <= 0)
                return false;

            for (int i = 0; i < _waves.Count; i++)
            {
                if (!IsWaveValid(_waves[i]))
                    return false;
            }

            return true;
        }
    }

    public bool TryPickEnemyWavePreset(
        int waveIndex,
        int seed,
        out StageEnemyWavePresetSO preset)
    {
        preset = null;

        if (_waves == null ||
            waveIndex < 0 ||
            waveIndex >= _waves.Count)
        {
            return false;
        }

        EncounterWave wave = _waves[waveIndex];

        if (wave == null || wave.enemyWavePresetCandidates == null)
            return false;

        int totalWeight = 0;

        for (int i = 0; i < wave.enemyWavePresetCandidates.Count; i++)
        {
            EnemyWavePresetCandidate candidate = wave.enemyWavePresetCandidates[i];

            if (!IsCandidateValid(candidate))
                continue;

            totalWeight += Mathf.Max(0, candidate.weight);
        }

        if (totalWeight <= 0)
            return false;

        int roll = new System.Random(seed).Next(0, totalWeight);

        for (int i = 0; i < wave.enemyWavePresetCandidates.Count; i++)
        {
            EnemyWavePresetCandidate candidate = wave.enemyWavePresetCandidates[i];

            if (!IsCandidateValid(candidate))
                continue;

            int weight = Mathf.Max(0, candidate.weight);

            if (roll < weight)
            {
                preset = candidate.preset;
                return true;
            }

            roll -= weight;
        }

        return false;
    }

    private static bool IsWaveValid(EncounterWave wave)
    {
        if (wave == null || wave.enemyWavePresetCandidates == null)
            return false;

        for (int i = 0; i < wave.enemyWavePresetCandidates.Count; i++)
        {
            if (IsCandidateValid(wave.enemyWavePresetCandidates[i]))
                return true;
        }

        return false;
    }

    private static bool IsCandidateValid(EnemyWavePresetCandidate candidate)
    {
        return candidate != null &&
               candidate.weight > 0 &&
               candidate.preset != null &&
               candidate.preset.IsValid;
    }
}
