using System;
using UnityEngine;

/// <summary>Per-stage spawn policy. Layout decisions are saved once; re-entry never rerolls them.</summary>
[Serializable]
public sealed class StageEncounterRules
{
    [Min(0)] public int infectedRoomCount;
    public bool infectionSpreadEnabled = true;
    [Min(1)] public float infectionSpreadIntervalSeconds = 60f;
    [Min(0)] public int maximumNewInfectedRoomsPerTick; // 0 = one attempt per infected room, no global cap.
    [Min(0)] public float infectedRoomGasPressure = 3f;
    [Range(0,1)] public float infectedEnemyChance = .25f;
    [Range(0,1)] public float extraEnemyChance = .15f;
    public EnemyShooterConfigSO replacementEnemy;
    [Range(0,1)] public float replacementChance;
}
