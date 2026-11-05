/*
 * Role hierarchy: Gameplay > World > Sectors > Traits
 * Primary responsibility: Defines the weighted room-trait pool and shared
 * runtime values used by generated Normal and Boss sectors.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections.Generic;
using UnityEngine;

public enum SectorRoomTraitType
{
    Normal = 0,
    Darkness = 1,
    Armed = 2,
    AmmoScavenger = 3,
    Fear = 4,
    EnemyDamage = 5,
    GasBoost = 6
}

[Serializable]
public sealed class SectorRoomTraitWeight
{
    [Tooltip("Normal means no room-specific penalty or enemy buff.")]
    public SectorRoomTraitType trait = SectorRoomTraitType.Normal;

    [Tooltip("Relative chance inside the eligible pool. The default pool is Normal 75 and each hard trait 6.25, for exactly 25% hard rooms.")]
    [Range(0f, 100f)] public float weight = 1f;

    [Tooltip("Allow this trait to roll for Normal Sector rooms.")]
    public bool allowNormalRooms = true;

    [Tooltip("Allow this trait to roll for Boss Sector rooms. Default is off so bosses are not accidentally modified.")]
    public bool allowBossRooms;

    // Purpose: Checks whether eligible for is currently true.
    public bool IsEligibleFor(StageRoomType roomType)
    {
        if (weight <= 0f)
            return false;

        return roomType switch
        {
            StageRoomType.Normal => allowNormalRooms,
            StageRoomType.Boss => allowBossRooms,
            _ => false
        };
    }
}

[CreateAssetMenu(
    fileName = "SectorRoomTraitTable",
    menuName = "Game/Sector/Room Trait Table")]
public sealed class SectorRoomTraitTableSO : ScriptableObject
{
    [Header("Weighted Room Traits")]
    [Tooltip("One eligible room trait is selected deterministically per generated room. Keep Normal in this list as the no-trait outcome.")]
    [SerializeField] private List<SectorRoomTraitWeight> _roomTraits = new();
    [Header("Raid anomaly gate")]
    [SerializeField] private bool _useAnomalyGate;
    [SerializeField, Range(0f, 1f)] private float _anomalyChance = .1f;

    [Header("Darkness")]
    [Tooltip("Final multiplier applied after player stats, passives, and status effects. This affects both close-circle and forward-cone vision.")]
    [SerializeField, Range(0.1f, 1f)] private float _darknessVisionRangeMultiplier = 0.7f;

    [Tooltip("Additional fog alpha multiplier while the player is in a Darkness room. This is visual-only and leaves scene lighting assets untouched.")]
    [SerializeField, Range(1f, 3f)] private float _darknessFogAlphaMultiplier = 1.55f;

    [Header("Armed Enemy Buff")]
    [SerializeField, Min(1f)] private float _armedEnemyAttackDamageMultiplier = 1.5f;
    [SerializeField, Min(1f)] private float _armedEnemyMaxHealthMultiplier = 1.5f;
    [SerializeField, Min(1f)] private float _armedEnemyMoveSpeedMultiplier = 1.2f;
    [SerializeField, Min(0)] private int _armedEnemyArmorClassBonus = 1;

    [Header("Fear")]
    [SerializeField, Min(1f)] private float _fearInjuryChanceMultiplier = 3f;

    [Header("Biochemical Gas Stage Trait")]
    [Tooltip("Personal player Infection added every tick while this stage trait is enabled. It never drains Infection Control and never applies in Home.")]
    [SerializeField, Min(0f)] private float _biochemicalGasInfectionPerSecond = 1f;

    [SerializeField, Min(0.1f)] private float _biochemicalGasTickInterval = 0.5f;

    public IReadOnlyList<SectorRoomTraitWeight> RoomTraits => _roomTraits;
    public float DarknessVisionRangeMultiplier => Mathf.Clamp(_darknessVisionRangeMultiplier, 0.1f, 1f);
    public float DarknessFogAlphaMultiplier => Mathf.Max(1f, _darknessFogAlphaMultiplier);
    public float ArmedEnemyAttackDamageMultiplier => Mathf.Max(1f, _armedEnemyAttackDamageMultiplier);
    public float ArmedEnemyMaxHealthMultiplier => Mathf.Max(1f, _armedEnemyMaxHealthMultiplier);
    public float ArmedEnemyMoveSpeedMultiplier => Mathf.Max(1f, _armedEnemyMoveSpeedMultiplier);
    public int ArmedEnemyArmorClassBonus => Mathf.Max(0, _armedEnemyArmorClassBonus);
    public float FearInjuryChanceMultiplier => Mathf.Max(1f, _fearInjuryChanceMultiplier);
    public float BiochemicalGasInfectionPerSecond => Mathf.Max(0f, _biochemicalGasInfectionPerSecond);
    public float BiochemicalGasTickInterval => Mathf.Max(0.1f, _biochemicalGasTickInterval);

    // Purpose: Performs the pick trait workflow for this component.
    public SectorRoomTraitType PickTrait(int seed, StageRoomType roomType, int stageIndex = 0, bool combatRoom = true)
    {
        bool Eligible(SectorRoomTraitWeight entry) => entry != null && entry.IsEligibleFor(roomType) &&
            (!_useAnomalyGate || (entry.trait != SectorRoomTraitType.Normal &&
                (entry.trait != SectorRoomTraitType.GasBoost || stageIndex >= 3)));
        if (_useAnomalyGate && (roomType != StageRoomType.Normal || !combatRoom ||
            new System.Random(seed ^ 0x2B194D).NextDouble() >= _anomalyChance)) return SectorRoomTraitType.Normal;
        if (_roomTraits == null ||
            (roomType != StageRoomType.Normal && roomType != StageRoomType.Boss))
        {
            return SectorRoomTraitType.Normal;
        }

        float totalWeight = 0f;
        for (int i = 0; i < _roomTraits.Count; i++)
        {
            SectorRoomTraitWeight entry = _roomTraits[i];
            if (Eligible(entry))
                totalWeight += entry.weight;
        }

        if (totalWeight <= 0f)
            return SectorRoomTraitType.Normal;

        float roll = (float)new System.Random(seed).NextDouble() * totalWeight;
        for (int i = 0; i < _roomTraits.Count; i++)
        {
            SectorRoomTraitWeight entry = _roomTraits[i];
            if (!Eligible(entry))
                continue;

            roll -= entry.weight;
            if (roll < 0f)
                return entry.trait;
        }

        return SectorRoomTraitType.Normal;
    }
}
